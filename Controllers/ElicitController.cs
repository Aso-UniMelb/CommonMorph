using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Npgsql;
using Dapper;

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

    // =====================================================================
    // Called by dashboard.js and elicit-expert.js
    [HttpGet("GetStats")]
    public IActionResult GetStats(int langid)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var countAll = connection.Query<int>(@$"
WITH
c1 AS (
SELECT COUNT(*) as cnt 
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
WHERE p.langid = {langid}), 
c2 AS (
SELECT COUNT(*) as cnt
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
WHERE s.formula NOT LIKE '%A%' AND p.langid = {langid})
SELECT c1.cnt + c2.cnt AS total_count
FROM c1, c2; ").First();

      var remaining = connection.Query<int>(@$"
WITH
c1 AS (
SELECT COUNT(*) as cnt 
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
), 
c2 AS (
SELECT COUNT(*) as cnt
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id 
WHERE s.formula NOT LIKE '%A%' AND p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
)
SELECT c1.cnt + c2.cnt AS total_count
FROM c1, c2;").First();
      return Ok(new { countAll, remaining });
    }

    // =====================================================================
    // Called by elicit-expert.js
    [HttpGet("EntryGetTableByLemma")]
    public IActionResult EntryGetTableByLemma(int langid, int page = 1)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var lemma_ids = connection.Query(@$"
SELECT l.id
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE)
GROUP BY l.id
ORDER BY l.priority DESC, l.entry").ToList();

      var lemma_ids2 = connection.Query(@$"
SELECT l.id
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE AND s.formula NOT LIKE '%A%')
GROUP BY l.id
ORDER BY l.priority DESC, l.entry").ToList();

      lemma_ids.AddRange(lemma_ids2);
      if (lemma_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int lemmaId = lemma_ids.ElementAtOrDefault(page - 1).id;
      var lemma = _context.lexicon.FirstOrDefault(l => l.id == lemmaId);
      // If the structures contain affixes:
      var results1 = connection.Query(@$"
SELECT s.id AS structureid, a.id AS affixid, a.realization AS a,
	s.formula AS formula, s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE l.id= {lemmaId} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY s.order, s.unimorphtags, a.order");
      // If the structures dose not contain affixes:
      var results2 = connection.Query(@$"
SELECT s.id AS structureid, s.formula AS formula, s.title AS stitle, s.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
WHERE l.id= {lemmaId} AND (c.submitted IS NULL OR c.isdeleted = TRUE) AND s.formula NOT LIKE '%A%'
ORDER BY l.priority DESC, l.entry, s.order, s.unimorphtags");
      //  merge the results
      return Ok(new { lemma = lemma, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    // Called by check-expert.js
    [HttpGet("CheckGetTableByLemma")]
    public IActionResult CheckGetTableByLemma(int langid, int page = 1)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      using var connection = new NpgsqlConnection(connectionString);
      var lemma_ids = connection.Query(@$"
WITH xx AS (
  SELECT l.id lemma, c.id cell, r.userid
  FROM lexicon l 
  INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
  INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
  INNER JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
  LEFT JOIN cellratings r ON r.cellid = c.id
  WHERE p.langid = {langid} AND (c.byuserid != {userId}) 
)
SELECT DISTINCT lemma FROM xx 
WHERE cell NOT IN (SELECT cell FROM xx WHERE userid ={userId})
");

      if (lemma_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int lemmaId = lemma_ids.ElementAtOrDefault(page - 1).lemma;
      var lemma = _context.lexicon.FirstOrDefault(l => l.id == lemmaId);
      // If the structures contain affixes:
      var results1 = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags || ';' || a.unimorphtags AS tags,
  s.title AS stitle, a.title AS atitle, c.submitted AS submitted
FROM cells c
INNER JOIN lexicon l ON l.id = c.lemmaid
INNER JOIN structures s ON s.id = c.structureid
INNER JOIN affixes a ON a.id = c.affixid
WHERE c.lemmaid = {lemmaId} AND c.byuserid != {userId}
ORDER BY s.order, s.unimorphtags, a.order");
      // If the structures dose not contain affixes:
      var results2 = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags AS tags, s.title AS stitle, c.submitted AS submitted
FROM cells c
INNER JOIN lexicon l ON l.id = c.lemmaid
INNER JOIN structures s ON s.id = c.structureid
LEFT JOIN cellratings r ON r.cellid = c.id
WHERE c.lemmaid = {lemmaId} AND c.byuserid != {userId} AND r.userid IS NULL AND s.formula NOT LIKE '%A%'
ORDER BY s.order, s.unimorphtags");
      //  merge the results
      return Ok(new { lemma = lemma, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    // Called by elicit-expert.js
    [HttpGet("EntryGetTableByStructure")]
    public IActionResult EntryGetTableByStructure(int langid, int page = 1)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var structure_ids = connection.Query(@$"
SELECT s.id
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE)
GROUP BY s.id
ORDER BY s.order");

      if (structure_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int structureId = structure_ids.ElementAtOrDefault(page - 1).id;
      var slt = _context.structures.FirstOrDefault(l => l.id == structureId);
      // If the structures contain affixes:
      var results1 = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, l.stem1, l.stem2, l.stem3, l.stem4, l.unimorphtags AS ltags, a.id AS affixid, a.realization AS a,
	a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE s.id= {structureId} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY a.order, l.priority DESC, l.entry");
      // If the structures dose not contain affixes:
      var results2 = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, l.stem1, l.stem2, l.stem3, l.stem4, l.unimorphtags AS ltags, s.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
WHERE s.id= {structureId} AND (s.formula NOT LIKE '%A%')  AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
ORDER BY l.priority DESC, l.entry");
      //  merge the results
      return Ok(new { structure = slt, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    // Called by check-expert.js
    [HttpGet("CheckGetTableByStructure")]
    public IActionResult CheckGetTableByStructure(int langid, int page = 1)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      using var connection = new NpgsqlConnection(connectionString);
      // find the structures that need to be checked by this user
      var structure_ids = connection.Query(@$"
WITH xx AS (
  SELECT s.id structure, c.id cell, r.userid
  FROM lexicon l 
  INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
  INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
  INNER JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
  LEFT JOIN cellratings r ON r.cellid = c.id
  WHERE p.langid = {langid} AND (c.byuserid != {userId}) 
)
SELECT DISTINCT structure FROM xx 
WHERE cell NOT IN (SELECT cell FROM xx WHERE userid ={userId})");

      if (structure_ids.ElementAtOrDefault(page - 1) == null)
        return Ok(new List<Cell>());

      int structureId = structure_ids.ElementAtOrDefault(page - 1).structure;
      var slt = _context.structures.FirstOrDefault(l => l.id == structureId);
      // If the structures contain affixes:
      var results1 = connection.Query(@$"
SELECT c.id AS cellid, l.entry AS lemma, a.title AS atitle, a.unimorphtags AS tags, c.submitted AS submitted
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE s.id= {structureId} AND c.byuserid != {userId} AND  (c.submitted IS NOT NULL OR c.isdeleted = TRUE)
ORDER BY a.order, l.priority DESC, l.entry");
      // If the structures dose not contain affixes:
      var results2 = connection.Query(@$"
SELECT  c.id AS cellid, l.entry AS lemma, c.submitted AS submitted
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id
WHERE s.id= {structureId} AND c.byuserid != {userId} AND (s.formula NOT LIKE '%A%')  AND (c.submitted IS NOT NULL OR c.isdeleted = TRUE) 
ORDER BY l.priority DESC, l.entry");
      //  merge the results
      return Ok(new { structure = slt, pool = results1.Union(results2).ToList() });
    }
    // =====================================================================
    // Called by elicit.js
    [HttpGet("listForEntry")]
    public IActionResult listForEntry(int langid, int page = 1, string metalang = "en")
    {
      // using dapper
      // using var connection = new MySqlConnection(connectionString);
      // using var connection = new SqlConnection(connectionString);
      using var connection = new NpgsqlConnection(connectionString);

      // If the structures contain affixes:
      var result = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, s.id AS structureid, a.id AS affixid,
	l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3, a.realization AS a,
	l.engmeaning AS eng, s.formula AS formula, s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id AND c.affixid = a.id
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
	AND s.unimorphtags || ';' || a.unimorphtags IN (SELECT unimorphtags FROM questions
WHERE questionlang='{metalang}'
GROUP BY unimorphtags having count(distinct questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.order, s.title
OFFSET 1*({page}-1) LIMIT 1").ToList();

      // If the structures dose not contain affixes:
      var result2 = connection.Query(@$"
SELECT l.id AS lemmaid, l.entry AS lemma, s.id AS structureid,
	l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3,
	l.engmeaning AS eng, s.formula AS formula, s.title AS stitle, s.unimorphtags AS tags
FROM lexicon l 
INNER JOIN inflectionclasses p ON p.id = l.inflectionclassid
INNER JOIN structures s ON s.inflectionclassid = l.inflectionclassid
LEFT JOIN cells c ON c.lemmaid = l.id AND c.structureid = s.id 
WHERE p.langid = {langid} AND (c.submitted IS NULL OR c.isdeleted = TRUE) 
	AND s.unimorphtags IN (SELECT unimorphtags FROM questions
WHERE questionlang ='{metalang}'
GROUP BY unimorphtags HAVING COUNT(DISTINCT questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.order, s.title
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
        // get previous samples from the same structure and affixes
        int structureid = Convert.ToInt32(result.First().structureid);
        int affixid = Convert.ToInt32(result.First().affixid);

        var samples = connection.Query(@$"
SELECT c.submitted AS form, l.entry AS lemma, l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3
FROM cells c
INNER JOIN lexicon l On l.id=c.lemmaid
WHERE c.langid={langid} AND c.structureid={structureid} AND c.affixid={affixid} 
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
        // get previous samples from the same structure
        int structureid = Convert.ToInt32(result2.First().structureid);
        var samples = connection.Query(@$"
SELECT c.submitted AS form, l.entry AS lemma, l.stem1 AS stem1, l.stem2 AS stem2, l.stem3 AS stem3
FROM cells c
INNER JOIN lexicon l On l.id=c.lemmaid
WHERE c.langid={langid} AND c.structureid={structureid} 
ORDER BY c.datesubmitted DESC 
LIMIT 5").ToList();
        // now we could get suggestions from LLM here but as it needs API call, better to do it in the frontend
        return Ok(new { r = result2.First(), q = question, s = samples });
      }
      return Ok("No more Cells");
    }
    // =====================================================================
    // Called by check.js
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
INNER JOIN lexicon l ON l.id = c.lemmaid
INNER JOIN structures s ON s.id = c.structureid
INNER JOIN affixes a ON a.id = c.affixid
LEFT JOIN cellratings r ON r.cellid = c.id AND r.userid = {userId}
WHERE c.langid = {langid} AND c.byuserid != {userId} AND  r.cellid IS NULL
  AND s.unimorphtags || ';' || a.unimorphtags IN (SELECT unimorphtags FROM questions
  WHERE questionlang='{metalang}'
  GROUP BY unimorphtags having count(distinct questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.order, s.title
OFFSET 1*({page}-1) LIMIT 1").ToList();

      var result2 = connection.Query(@$"
SELECT c.id AS cellid, s.unimorphtags AS tags, s.title AS stitle, 
	l.entry AS lemma, l.engmeaning AS eng, c.submitted AS submitted
FROM cells c
INNER JOIN lexicon l ON l.id = c.lemmaid
INNER JOIN structures s ON s.id = c.structureid
LEFT JOIN cellratings r ON r.cellid = c.id AND r.userid = {userId}
WHERE c.langid = {langid} AND c.byuserid != {userId} AND r.cellid IS NULL
  AND s.unimorphtags IN (SELECT unimorphtags FROM questions
  WHERE questionlang ='{metalang}'
  GROUP BY unimorphtags HAVING COUNT(DISTINCT questionlang) = 1)
ORDER BY l.priority DESC, l.entry, s.order, s.title
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