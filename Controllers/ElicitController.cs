using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
// using MySqlConnector;
// using Microsoft.Data.SqlClient;
using Npgsql;
using Dapper;
using System.Text.Json;
using System.Text;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class ElicitController : Controller
  {

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private string connectionString;
    public ElicitController(AppDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
      _context = context;
      _configuration = configuration;
      // For the complex query used in this controller, we need Dapper to connect to DB
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
      _httpClientFactory = httpClientFactory;
    }

    public class NNresult
    {
      public int poolorder { get; set; }
      public string pred { get; set; }
      public float conf { get; set; }
    }

    [HttpGet("GetStats")]
    public IActionResult GetStats(int langid)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var countAll = connection.Query<int>(@$"
WITH
c1 AS (
SELECT COUNT(*) as cnt 
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
WHERE p.langid = {langid}), 
c2 AS (
SELECT COUNT(*) as cnt
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
WHERE s.formula NOT LIKE '%A%' AND p.langid = {langid})
SELECT c1.cnt + c2.cnt AS total_count
FROM c1, c2; ").First();

      var remaining = connection.Query<int>(@$"
WITH
c1 AS (
SELECT COUNT(*) as cnt 
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
), 
c2 AS (
SELECT COUNT(*) as cnt
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id 
WHERE s.formula NOT LIKE '%A%' AND p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
)
SELECT c1.cnt + c2.cnt AS total_count
FROM c1, c2;").First();
      return Ok(new { countAll, remaining });
    }

    // =====================================================================
    [HttpGet("EntryGetTableByNN")]
    public async Task<IActionResult> EntryGetTableByNNAsync(int langid, int page = 1)
    {
      var server = "http://3.107.48.91";
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
    [HttpGet("EntryGetTableByLemma")]
    public IActionResult EntryGetTableByLemma(int langid, int page = 1)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var lemma_ids = connection.Query(@$"
SELECT l.id
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE)
GROUP BY l.id
ORDER BY l.priority DESC, l.entry").ToList();

      var lemma_ids2 = connection.Query(@$"
SELECT l.id
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE AND s.formula NOT LIKE '%A%')
GROUP BY l.id
ORDER BY l.priority DESC, l.entry").ToList();

      lemma_ids.AddRange(lemma_ids2);
      if (lemma_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int lemmaId = lemma_ids.ElementAtOrDefault(page - 1).id;
      var lemma = _context.lemmas.FirstOrDefault(l => l.id == lemmaId);
      // If the slots contain agreement:
      var results1 = connection.Query(@$"
SELECT s.id AS slotid, a.id AS agreementid, a.realization AS a,
	s.formula AS formula, s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE l.id= {lemmaId} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY s.priority DESC, s.unimorphtags, a.unimorphtags");
      // If the slots dose not contain agreement:
      var results2 = connection.Query(@$"
SELECT s.id AS slotid, s.formula AS formula, s.title AS stitle, s.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id
WHERE l.id= {lemmaId} AND (c.submitted IS NULL OR c.isdeleted = TRUE) AND s.formula NOT LIKE '%A%'
ORDER BY l.priority DESC, l.entry, s.priority DESC, s.unimorphtags");
      //  merge the results
      return Ok(new { lemma = lemma, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    [HttpGet("CheckGetTableByLemma")]
    public IActionResult CheckGetTableByLemma(int langid, int page = 1)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      using var connection = new NpgsqlConnection(connectionString);
      var lemma_ids = connection.Query(@$"
WITH xx AS (
  SELECT l.id lemma, c.id cell, r.userid
  FROM lemmas l 
  INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
  INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
  INNER JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id
  LEFT JOIN cellratings r ON r.cellid = c.id
  WHERE p.langid = {langid} AND (c.byuserid != {userId}) 
)
SELECT DISTINCT lemma FROM xx 
WHERE cell NOT IN (SELECT cell FROM xx WHERE userid ={userId})
");

      if (lemma_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int lemmaId = lemma_ids.ElementAtOrDefault(page - 1).lemma;
      var lemma = _context.lemmas.FirstOrDefault(l => l.id == lemmaId);
      // If the slots contain agreement:
      var results1 = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags || ';' || a.unimorphtags AS tags,
  s.title AS stitle, a.title AS atitle, c.submitted AS submitted
FROM cells c
INNER JOIN lemmas l ON l.id = c.lemmaid
INNER JOIN slots s ON s.id = c.slotid
INNER JOIN agreements a ON a.id = c.agreementid
WHERE c.lemmaid = {lemmaId} AND c.byuserid != {userId}
ORDER BY s.priority DESC, s.unimorphtags, a.unimorphtags");
      // If the slots dose not contain agreement:
      var results2 = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags AS tags, s.title AS stitle, c.submitted AS submitted
FROM cells c
INNER JOIN lemmas l ON l.id = c.lemmaid
INNER JOIN slots s ON s.id = c.slotid
LEFT JOIN cellratings r ON r.cellid = c.id
WHERE c.lemmaid = {lemmaId} AND c.byuserid != {userId} AND r.userid IS NULL AND s.formula NOT LIKE '%A%'
ORDER BY s.priority DESC, s.unimorphtags");
      //  merge the results
      return Ok(new { lemma = lemma, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    [HttpGet("EntryGetTableBySlot")]
    public IActionResult EntryGetTableBySlot(int langid, int page = 1)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var slot_ids = connection.Query(@$"
SELECT s.id
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE)
GROUP BY s.id
ORDER BY s.priority DESC");

      if (slot_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int slotId = slot_ids.ElementAtOrDefault(page - 1).id;
      var slt = _context.slots.FirstOrDefault(l => l.id == slotId);
      // If the slots contain agreement:
      var results1 = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, l.stem1, l.stem2, l.stem3, l.stem4, a.id AS agreementid, a.realization AS a,
	a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE s.id= {slotId} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY a.unimorphtags, l.priority DESC, l.entry");
      // If the slots dose not contain agreement:
      var results2 = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, l.stem1, l.stem2, l.stem3, l.stem4, s.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id
WHERE s.id= {slotId} AND (s.formula NOT LIKE '%A%')  AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY l.priority DESC, l.entry");
      //  merge the results
      return Ok(new { slot = slt, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    [HttpGet("CheckGetTableBySlot")]
    public IActionResult CheckGetTableBySlot(int langid, int page = 1)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      using var connection = new NpgsqlConnection(connectionString);
      // find the slots that need to be checked by this user
      var slot_ids = connection.Query(@$"
WITH xx AS (
  SELECT s.id slot, c.id cell, r.userid
  FROM lemmas l 
  INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
  INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
  INNER JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id
  LEFT JOIN cellratings r ON r.cellid = c.id
  WHERE p.langid = {langid} AND (c.byuserid != {userId}) 
)
SELECT DISTINCT slot FROM xx 
WHERE cell NOT IN (SELECT cell FROM xx WHERE userid ={userId})");

      if (slot_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int slotId = slot_ids.ElementAtOrDefault(page - 1).slot;
      var slt = _context.slots.FirstOrDefault(l => l.id == slotId);
      // If the slots contain agreement:
      var results1 = connection.Query(@$"
SELECT c.id AS cellid, l.entry AS lemma, a.title AS atitle, a.unimorphtags AS tags, c.submitted AS submitted
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE s.id= {slotId} AND c.byuserid != {userId} AND  (c.submitted IS NOT NULL OR c.isdeleted = TRUE)
ORDER BY a.unimorphtags, l.priority DESC, l.entry");
      // If the slots dose not contain agreement:
      var results2 = connection.Query(@$"
SELECT  c.id AS cellid, l.entry AS lemma, c.submitted AS submitted
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id
WHERE s.id= {slotId} AND c.byuserid != {userId} AND (s.formula NOT LIKE '%A%')  AND (c.submitted IS NOT NULL OR c.isdeleted = TRUE) 
ORDER BY l.priority DESC, l.entry");
      //  merge the results
      return Ok(new { slot = slt, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    [HttpGet("listForEntry")]
    public IActionResult listForEntry(int langid, int page = 1, string metalang = "en")
    {
      // using dapper
      // using var connection = new MySqlConnection(connectionString);
      // using var connection = new SqlConnection(connectionString);
      using var connection = new NpgsqlConnection(connectionString);

      // If the slots contain agreement:
      var result = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, s.id AS slotid, a.id AS agreementid,
	l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3, a.realization AS a,
	l.engmeaning AS eng, s.formula AS formula, s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id AND c.agreementid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
	AND s.unimorphtags || ';' || a.unimorphtags IN (SELECT unimorphtags FROM questions
WHERE questionlang='{metalang}'
GROUP BY unimorphtags having count(distinct questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.priority DESC, s.title
OFFSET 1*({page}-1) LIMIT 1").ToList();

      // If the slots dose not contain agreement:
      var result2 = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, s.id AS slotid,
	l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3,
	l.engmeaning AS eng, s.formula AS formula, s.title AS stitle, s.unimorphtags AS tags
FROM lemmas l 
INNER JOIN paradigmclasses p ON p.id = l.paradigmclassid
INNER JOIN slots s ON s.paradigmclassid = l.paradigmclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.slotid = s.id 
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
	AND s.unimorphtags IN (SELECT unimorphtags FROM questions
WHERE questionlang ='{metalang}'
GROUP BY unimorphtags HAVING COUNT(DISTINCT questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.priority DESC, s.title
OFFSET 1*({page}-1) LIMIT 1").ToList();

      if (result.Count > 0)
      {
        string tags = (string)(result.First().tags);
        var question = _context.questions
          .Where(q => q.unimorphtags == tags && q.questionlang == metalang)
          .Select(q => new
          {
            q.question
          })
          .First();
        // get previous samples from the same slot and agreement
        int slotid = Convert.ToInt32(result.First().slotid);
        int agreementid = Convert.ToInt32(result.First().agreementid);

        var samples = connection.Query(@$"
SELECT c.submitted AS form, l.entry AS lemma, l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3
FROM cells c
INNER JOIN lemmas l On l.id=c.lemmaid
WHERE c.langid={langid} AND c.slotid={slotid} AND c.agreementid={agreementid} 
ORDER BY c.datesubmitted DESC 
LIMIT 5").ToList();
        return Ok(new { r = result.First(), q = question, s = samples });
      }

      if (result2.Count > 0)
      {
        var tags = (string)(result2.First().tags);
        var question = _context.questions
          .Where(q => q.unimorphtags == tags && q.questionlang == metalang)
          .Select(q => new
          {
            q.question
          })
          .First();
        // get previous samples from the same slot
        int slotid = Convert.ToInt32(result2.First().slotid);
        var samples = connection.Query(@$"
SELECT c.submitted AS form, l.entry AS lemma, l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3
FROM cells c
INNER JOIN lemmas l On l.id=c.lemmaid
WHERE c.langid={langid} AND c.slotid={slotid} 
ORDER BY c.datesubmitted DESC 
LIMIT 5").ToList();
        // now we could get suggestions from LLM here but as it needs API call, better to do it in the frontend
        return Ok(new { r = result2.First(), q = question, s = samples });
      }
      return Ok("No more Cells");
    }
    // =====================================================================
    [HttpGet("listForCheck")]
    public IActionResult listForCheck(int langid, int page = 1, string metalang = "en")
    {
      // using dapper
      using var connection = new NpgsqlConnection(connectionString);

      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      var result = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags || ';' || a.unimorphtags AS tags, s.title AS stitle, a.title AS atitle, 
	l.entry AS lemma, l.engmeaning AS eng, c.submitted AS submitted
FROM cells c
INNER JOIN lemmas l ON l.id = c.lemmaid
INNER JOIN slots s ON s.id = c.slotid
INNER JOIN agreements a ON a.id = c.agreementid
LEFT JOIN cellratings r ON r.cellid = c.id AND r.userid = {userId}
WHERE c.langid = {langid} AND c.byuserid != {userId} AND  r.cellid IS NULL
  AND s.unimorphtags || ';' || a.unimorphtags IN (SELECT unimorphtags FROM questions
  WHERE questionlang='{metalang}'
  GROUP BY unimorphtags having count(distinct questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.priority DESC, s.title
OFFSET 1*({page}-1) LIMIT 1").ToList();

      var result2 = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags AS tags, s.title AS stitle, 
	l.entry AS lemma, l.engmeaning AS eng, c.submitted AS submitted
FROM cells c
INNER JOIN lemmas l ON l.id = c.lemmaid
INNER JOIN slots s ON s.id = c.slotid
LEFT JOIN cellratings r ON r.cellid = c.id AND r.userid = {userId}
WHERE c.langid = {langid} AND c.byuserid != {userId} AND r.cellid IS NULL
  AND s.unimorphtags IN (SELECT unimorphtags FROM questions
  WHERE questionlang ='{metalang}'
  GROUP BY unimorphtags HAVING COUNT(DISTINCT questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.priority DESC, s.title
OFFSET 1*({page}-1) LIMIT 1").ToList();

      if (result.Count > 0)
      {
        var tags = (string)(result.First().tags);
        var question = _context.questions
          .Where(q => q.unimorphtags == tags && q.questionlang == metalang)
          .Select(q => new
          {
            q.question
          })
          .First();
        return Ok(new { r = result.First(), q = question });
      }

      if (result2.Count > 0)
      {
        var tags = (string)(result2.First().tags);
        var question = _context.questions
          .Where(q => q.unimorphtags == tags && q.questionlang == metalang)
          .Select(q => new
          {
            q.question
          })
          .First();
        return Ok(new { r = result2.First(), q = question });
      }
      return Ok(result);
    }
  }
}