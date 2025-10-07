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
  [Route("[controller]")]
  public class ActiveLearningController : Controller
  {

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private string connectionString;
    private string server = "http://51.21.248.63";
    public ActiveLearningController(AppDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
      _context = context;
      _configuration = configuration;
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
      _httpClientFactory = httpClientFactory;
    }
    public class NNresult
    {
      public int poolorder { get; set; }
      public string pred { get; set; }
      public float conf { get; set; }
    }

    // =====================================================================
    [HttpGet("EntryGetTableByNN")]
    public async Task<IActionResult> EntryGetTableByNNAsync(int langid, int page = 1)
    {
      // extract data from the database
      using var connection = new NpgsqlConnection(connectionString);
      var pool = connection.Query(@$"
SELECT l.id AS lemmaid, s.id AS slotid, a.id AS agreementid, (l.priority + s.priority) AS priority,
  s.title AS stitle, a.title AS atitle, a.realization AS a, s.formula AS formula,
  l.stem1, l.stem2, l.stem3, l.stem4,
  l.entry lemma, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY priority DESC, lemmaid").ToList();

      var pool2 = connection.Query(@$"
SELECT l.id AS lemmaid, s.id AS slotid, (l.priority + s.priority) AS priority,
  s.title AS stitle, s.formula AS formula,  l.stem1, l.stem2, l.stem3, l.stem4,
  l.entry lemma, s.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id 
WHERE s.formula NOT LIKE '%A%' AND p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY priority DESC, lemmaid").ToList();
      pool.AddRange(pool2);

      // check if the model is trained using GET endpoint of API
      var httpClient = _httpClientFactory.CreateClient();
      string url = server + "/is_model_trained?langid=" + langid;
      var responseget = httpClient.GetAsync(url).Result;
      if (!responseget.IsSuccessStatusCode)
      {
        // take 100 samples randomly from first 500 of the pool
        Random rand = new Random();
        var randomPool = pool.Take(100).OrderBy(x => rand.Next()).Take(80).ToList();
        return Ok(new { pool = randomPool });
      }
      else
      {
        httpClient = _httpClientFactory.CreateClient();
        var httpContent = new StringContent("");
        url = server + "/listpredict";
        var words = new List<string>();
        foreach (var record in pool)
          words.Add(record.lemma + "_" + record.tags);

        var requestBody = new { langid = langid.ToString(), words = words.ToArray() };
        httpContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        try
        {
          HttpResponseMessage response = await httpClient.PostAsync(url, httpContent);
          if (response.IsSuccessStatusCode)
          {
            string responseBody = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NNresult>>(responseBody);
            for (int i = 0; i < results.Count; i++)
              results[i].poolorder = i;
            // order by confidence
            var newpool = results.OrderBy(x => x.conf).Take(80).ToList();
            var bIndexes = new HashSet<int>(newpool.Select(b => b.poolorder));
            var filteredpool = pool.Where(a => bIndexes.Contains(a.Id)).ToList();

            // add 
            return Ok(new { pool = filteredpool });
          }
          else
          {
            // Console.WriteLine($"Error: {(int)response.StatusCode} {response.StatusCode}");
            string errorBody = await response.Content.ReadAsStringAsync();
          }
        }
        catch (HttpRequestException e)
        {
          //
        }
      }
      return Ok("No more Cells");
    }
    // =====================================================================
    // single suggest
    [HttpPost("suggest")]
    public async Task<IActionResult> suggest(string langid, string lemma, string tags)
    {
      string url = server + "/suggest";
      var httpClient = _httpClientFactory.CreateClient();
      var vocab_id = langid;
      var input_data = lemma + "_" + tags;
      var json = JsonSerializer.Serialize(new { langid, vocab_id, input_data });
      var content = new StringContent(json, Encoding.UTF8, "application/json");
      httpClient.DefaultRequestHeaders.Add("accept", "application/json");
      var response = await httpClient.PostAsync(url, content);
      if (response.IsSuccessStatusCode)
      {
        var result = await response.Content.ReadAsStringAsync();
        return Ok(result);
      }
      else
      {
        return BadRequest("Error");
      }
    }
    // =====================================================================
    [HttpGet("checkModelTrained")]
    public IActionResult checkModelTrained(string langid)
    {
      // check if the model is trained using GET endpoint of API
      var httpClient = _httpClientFactory.CreateClient();
      string url = server + "/is_model_trained?langid=" + langid;
      var responseget = httpClient.GetAsync(url).Result;
      if (!responseget.IsSuccessStatusCode)
      {
        return BadRequest(responseget.Content.ReadAsStringAsync().Result);
      }
      else
      {
        return Ok(responseget.Content.ReadAsStringAsync().Result);
      }
    }
    // =====================================================================

    // train the model
    [HttpPost("train")]
    public IActionResult train(string langid)
    {
      string url = server + "/train";
      var httpClient = _httpClientFactory.CreateClient();
      var json = JsonSerializer.Serialize(new { langid });
      var content = new StringContent(json, Encoding.UTF8, "application/json");
      httpClient.DefaultRequestHeaders.Clear();
      httpClient.DefaultRequestHeaders.Add("accept", "application/json");
      var response = httpClient.PostAsync(url, content).Result;
      if (response.IsSuccessStatusCode)
      {
        string responseString = response.Content.ReadAsStringAsync().Result;
        // return train_loss_records, train_time
        return Ok(responseString);
      }
      else
      {
        return BadRequest("Error");
      }
    }

    // =====================================================================
    // finetune the model

  }
}