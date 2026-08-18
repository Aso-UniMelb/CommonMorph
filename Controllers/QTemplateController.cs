using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using System.Security.Claims;
using Npgsql;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class QTemplateController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private string connectionString;

    public QTemplateController(AppDbContext context, IConfiguration configuration)
    {
      _context = context;
      _configuration = configuration;
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpGet("available")]
    public IActionResult available(string metalang, int langid)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var parameters = new { metalang, langid };
      
      var availableWithAgr = connection.Query(@$"
SELECT DISTINCT s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM structures s
INNER JOIN inflectionclasses p ON p.id = s.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
WHERE p.langid = @langid 
  AND s.isdeleted IS NOT TRUE 
  AND a.isdeleted IS NOT TRUE
  AND s.unimorphtags || ';' || a.unimorphtags IN
    (SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = @metalang AND isdeleted IS NOT TRUE)
ORDER BY s.title, a.title", parameters).ToList();

      var availableWithoutAgr = connection.Query(@$"
SELECT DISTINCT s.title AS stitle, '' AS atitle, s.unimorphtags AS tags
FROM structures s
INNER JOIN inflectionclasses p ON p.id = s.inflectionclassid
WHERE p.langid = @langid 
  AND s.isdeleted IS NOT TRUE 
  AND (s.formula NOT LIKE '%A%' OR s.formula IS NULL) 
  AND s.unimorphtags IN
    (SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = @metalang AND isdeleted IS NOT TRUE)
ORDER BY s.title", parameters).ToList();

      var available = new List<dynamic>(availableWithAgr);
      if (availableWithoutAgr.Count > 0)
      {
        available.AddRange(availableWithoutAgr);
      }

      return Ok(available);
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpGet("unavailable")]
    public IActionResult unavailable(string metalang, int langid)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var parameters = new { metalang, langid };

      var unavailableWithAgr = connection.Query(@$"
SELECT DISTINCT s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM structures s
INNER JOIN inflectionclasses p ON p.id = s.inflectionclassid
INNER JOIN reusablelayers ag ON ag.id = s.reusablelayerid
INNER JOIN affixes a ON a.reusablelayerid = ag.id
WHERE p.langid = @langid 
  AND s.isdeleted IS NOT TRUE 
  AND a.isdeleted IS NOT TRUE
  AND s.unimorphtags || ';' || a.unimorphtags NOT IN
    (SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = @metalang AND isdeleted IS NOT TRUE)
ORDER BY s.title, a.title", parameters).ToList();

      var unavailableWithoutAgr = connection.Query(@$"
SELECT DISTINCT s.title AS stitle, '' AS atitle, s.unimorphtags AS tags
FROM structures s
INNER JOIN inflectionclasses p ON p.id = s.inflectionclassid
WHERE p.langid = @langid 
  AND s.isdeleted IS NOT TRUE 
  AND (s.formula NOT LIKE '%A%' OR s.formula IS NULL) 
  AND s.unimorphtags NOT IN
    (SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = @metalang AND isdeleted IS NOT TRUE)
ORDER BY s.title", parameters).ToList();

      var unavailable = new List<dynamic>(unavailableWithAgr);
      if (unavailableWithoutAgr.Count > 0)
      {
        unavailable.AddRange(unavailableWithoutAgr);
      }

      return Ok(unavailable);
    }

    [HttpGet("get")]
    public IActionResult get(string metalang, string tags)
    {
      var q = _context.questions.FirstOrDefault(x => x.unimorphtags == tags && x.questionlang == metalang && !x.isdeleted);
      if (q == null)
        return Ok("");
      return Ok(q.question);
    }

    public class QTemplateDto
    {
      public int? id { get; set; }
      public string? questionlang { get; set; }
      public string? metalang { get; set; }
      public string? unimorphtags { get; set; }
      public string? question { get; set; }
    }

    [HttpGet("list")]
    public IActionResult list([FromQuery] string? metalang)
    {
      var q = _context.questions.Where(x => !x.isdeleted);
      if (!string.IsNullOrWhiteSpace(metalang))
      {
        q = q.Where(x => x.questionlang == metalang);
      }
      var list = q.OrderBy(x => x.unimorphtags).Select(x => new {
        id = x.id,
        questionlang = x.questionlang,
        unimorphtags = x.unimorphtags,
        question = x.question
      }).ToList();
      return Ok(list);
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert([FromBody] QTemplateDto? dto)
    {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;
      var lang = dto?.questionlang ?? dto?.metalang ?? "en";
      var tags = dto?.unimorphtags ?? "";
      var text = dto?.question ?? "";

      var existing = _context.questions.FirstOrDefault(x => x.unimorphtags == tags && x.questionlang == lang && !x.isdeleted);
      if (existing != null)
      {
        existing.question = text;
        _context.SaveChanges();
        return Ok(new { id = existing.id, success = true });
      }

      var qtempl = new QuestionTemplate
      {
        questionlang = lang,
        unimorphtags = tags,
        question = text,
        userid = userId,
        isdeleted = false
      };

      _context.questions.Add(qtempl);
      _context.SaveChanges();

      var userLog = new UserLog()
      {
        log = $"Inserted qTemplate {qtempl.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();

      return Ok(new { id = qtempl.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update([FromBody] QTemplateDto? dto)
    {
      var lang = dto?.questionlang ?? dto?.metalang ?? "en";
      var tags = dto?.unimorphtags ?? "";
      var text = dto?.question ?? "";

      var q = (dto?.id != null && dto.id > 0)
        ? _context.questions.FirstOrDefault(x => x.id == dto.id)
        : _context.questions.FirstOrDefault(x => x.unimorphtags == tags && x.questionlang == lang && !x.isdeleted);
      
      if (q == null)
      {
        // Fallback to insert
        return insert(dto);
      }

      q.question = text;
      if (!string.IsNullOrWhiteSpace(tags)) q.unimorphtags = tags;
      if (!string.IsNullOrWhiteSpace(lang)) q.questionlang = lang;
      
      _context.SaveChanges();
      return Ok(new { id = q.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("delete")]
    public IActionResult delete([FromQuery] int id)
    {
      var q = _context.questions.FirstOrDefault(x => x.id == id);
      if (q == null)
        return BadRequest("Template does not exist");
      q.isdeleted = true;
      _context.SaveChanges();
      return Ok(new { id = id, success = true });
    }

    [HttpGet("download")]
    public IActionResult download(string metalang)
    {
      var result = _context.questions.Where(x => x.questionlang == metalang && !x.isdeleted).Select(x => new { x.id, x.unimorphtags, x.question }).ToList();
      return Ok(result);
    }
  }
}