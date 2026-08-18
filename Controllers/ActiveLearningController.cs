using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Npgsql;
using Dapper;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace common_morph_backend.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class ActiveLearningController : ControllerBase
  {
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string connectionString;

    private string ServerUrl => _configuration["FastAPI_Server"] ?? Environment.GetEnvironmentVariable("FastAPI_Server") ?? "http://localhost:8000";

    public ActiveLearningController(AppDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
      _context = context;
      _configuration = configuration;
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection") ?? "";
      _httpClientFactory = httpClientFactory;
    }

    public class NNresult
    {
      public int poolorder { get; set; }
      public string? pred { get; set; }
      public float conf { get; set; }
    }

    public class TrainModelRequest
    {
      public string langid { get; set; } = "";
      public int? epochs { get; set; }
    }

    // =====================================================================
    [HttpGet("EntryGetTableByNN")]
    public async Task<IActionResult> EntryGetTableByNNAsync(int langid, int page = 1)
    {
      try
      {
        using var connection = new NpgsqlConnection(connectionString);
        var pool = (await connection.QueryAsync(@$"
SELECT l.id AS lemmaid, s.id AS structureid, a.id AS affixid, (l.priority) AS priority,
  s.title AS stitle, a.title AS atitle, a.realization AS a, s.formula AS formula,
  l.stem1, l.stem2, l.stem3, l.stem4, l.unimorphtags,
  l.entry lemma, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY priority DESC, lemmaid")).ToList();

        var pool2 = (await connection.QueryAsync(@$"
SELECT l.id AS lemmaid, s.id AS structureid, (l.priority) AS priority,
  s.title AS stitle, s.formula AS formula,  l.stem1, l.stem2, l.stem3, l.stem4, l.unimorphtags,
  l.entry lemma, s.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id 
WHERE s.formula NOT LIKE '%A%' AND p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY priority DESC, lemmaid")).ToList();
        pool.AddRange(pool2);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(8);
        string url = $"{ServerUrl}/is_model_trained?langid={langid}";
        var responseget = await httpClient.GetAsync(url);
        
        if (!responseget.IsSuccessStatusCode)
        {
          Random rand = new Random();
          var randomPool = pool.Take(100).OrderBy(x => rand.Next()).Take(80).ToList();
          return Ok(new { pool = randomPool });
        }
        else
        {
          var words = new List<string>();
          foreach (var record in pool)
            words.Add(record.lemma + "_" + record.tags);

          var requestBody = new { langid = langid.ToString(), words = words.ToArray() };
          var httpContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
          
          var response = await httpClient.PostAsync($"{ServerUrl}/listpredict", httpContent);
          if (response.IsSuccessStatusCode)
          {
            string responseBody = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NNresult>>(responseBody);
            if (results != null)
            {
              for (int i = 0; i < results.Count; i++)
                results[i].poolorder = i;
              var newpool = results.OrderBy(x => x.conf).Take(80).ToList();
              var bIndexes = new HashSet<int>(newpool.Select(b => b.poolorder));
              var filteredpool = pool.Where(a => bIndexes.Contains(a.Id)).ToList();
              return Ok(new { pool = filteredpool });
            }
          }
        }
        return Ok(new { pool = pool.Take(80).ToList() });
      }
      catch (Exception ex)
      {
        return Ok(new { pool = new List<object>(), error = ex.Message });
      }
    }

    public class NNSuggestRequest
    {
      public string? langid { get; set; }
      public string? lemma { get; set; }
      public string? tags { get; set; }
    }

    // =====================================================================
    [HttpPost("suggest")]
    public async Task<IActionResult> suggest([FromBody] NNSuggestRequest? body, [FromQuery] string? langid, [FromQuery] string? lemma, [FromQuery] string? tags)
    {
      var targetLangId = !string.IsNullOrWhiteSpace(langid) ? langid : body?.langid;
      var targetLemma = !string.IsNullOrWhiteSpace(lemma) ? lemma : body?.lemma;
      var targetTags = !string.IsNullOrWhiteSpace(tags) ? tags : body?.tags ?? "";

      if (string.IsNullOrWhiteSpace(targetLangId) || string.IsNullOrWhiteSpace(targetLemma))
      {
        return Ok(new { success = false, message = "langid and lemma are required" });
      }

      try
      {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(6);
        var input_data = $"{targetLemma.Trim()}_{targetTags.Trim()}";
        var payload = new { langid = targetLangId, vocab_id = targetLangId, input_data };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{ServerUrl}/suggest", content);
        if (response.IsSuccessStatusCode)
        {
          var resultString = await response.Content.ReadAsStringAsync();
          using var doc = JsonDocument.Parse(resultString);
          var root = doc.RootElement;

          string predicted = "";
          double avgConf = 0.0;

          if (root.TryGetProperty("predicted", out var predProp))
            predicted = predProp.GetString() ?? "";
          if (root.TryGetProperty("avg_confidence", out var confProp))
            avgConf = confProp.GetDouble();

          if (!string.IsNullOrWhiteSpace(predicted))
          {
            return Ok(new { success = true, predicted = predicted.Trim(), avg_confidence = avgConf });
          }
        }
        return Ok(new { success = false, message = "No neural prediction available" });
      }
      catch (Exception ex)
      {
        return Ok(new { success = false, message = ex.Message });
      }
    }

    // =====================================================================
    [HttpGet("checkModelTrained")]
    public async Task<IActionResult> checkModelTrained([FromQuery] string langid)
    {
      if (string.IsNullOrWhiteSpace(langid))
      {
        return BadRequest(new { isTrained = false, message = "Language ID is required" });
      }

      try
      {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var response = await httpClient.GetAsync($"{ServerUrl}/is_model_trained?langid={langid}");
        
        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          using var doc = JsonDocument.Parse(content);
          string lastTrained = "";
          if (doc.RootElement.TryGetProperty("message", out var msgProp))
          {
            lastTrained = msgProp.GetString() ?? "";
          }
          return Ok(new { isTrained = true, lastTrained, message = $"Trained on {lastTrained}" });
        }
        else
        {
          return Ok(new { isTrained = false, lastTrained = "", message = "No trained model found" });
        }
      }
      catch
      {
        return Ok(new { isTrained = false, lastTrained = "", isOffline = true, message = "Active learning service offline" });
      }
    }

    // =====================================================================
    [HttpPost("train")]
    public async Task<IActionResult> train([FromQuery] string? langid, [FromQuery] int? epochs, [FromBody] TrainModelRequest? body)
    {
      var targetLangId = !string.IsNullOrWhiteSpace(langid) ? langid : body?.langid;
      var targetEpochs = epochs ?? body?.epochs ?? 15;

      if (string.IsNullOrWhiteSpace(targetLangId))
      {
        return BadRequest(new { error = "Language ID is required for training" });
      }

      try
      {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(180); // allow sufficient time for training epochs

        var json = JsonSerializer.Serialize(new { langid = targetLangId, epochs = targetEpochs });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{ServerUrl}/train", content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
          string message = "Model trained successfully";
          try
          {
            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
            {
              message = msgProp.GetString() ?? message;
            }
          }
          catch {}

          return Ok(new { success = true, message });
        }
        else
        {
          string errorDetail = "Training service returned an error";
          try
          {
            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("detail", out var detailProp))
            {
              errorDetail = detailProp.GetString() ?? errorDetail;
            }
          }
          catch {}

          return BadRequest(new { success = false, error = errorDetail });
        }
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, error = $"Connection to FastAPI server failed: {ex.Message}" });
      }
    }
  }
}