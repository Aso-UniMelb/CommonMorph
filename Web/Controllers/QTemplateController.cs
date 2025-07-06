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
      // connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
      connectionString = _configuration.GetConnectionString("DefaultConnection");
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpGet("available")]
    public IActionResult available(string metalang, int langid)
    {

      using var connection = new NpgsqlConnection(connectionString);
      var available = connection.Query(@$"
SELECT DISTINCT  s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM slots s
INNER JOIN paradigmclasses p ON p.id = s.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
WHERE p.langid = {langid} AND s.unimorphtags || ';' || a.unimorphtags IN
(SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = '{metalang}')
ORDER BY s.title, a.title").ToList();
      // search for slots without agreement
      var availableWithoutAgr = connection.Query(@$"
SELECT DISTINCT s.title AS stitle, s.unimorphtags AS tags
FROM slots s
INNER JOIN paradigmclasses p ON p.id = s.paradigmclassid
WHERE p.langid = {langid} AND s.formula NOT LIKE '%A%' AND s.unimorphtags IN
(SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = '{metalang}')
ORDER BY s.title").ToList();
      if (availableWithoutAgr.Count > 0)
        available.AddRange(availableWithoutAgr);

      return Ok(available);
    }


    [Authorize(Roles = "admin, linguist")]
    [HttpGet("unavailable")]
    public IActionResult unavailable(string metalang, int langid)
    {
      // using dapper
      // using var connection = new MySqlConnection(connectionString);
      // using var connection = new SqlConnection(connectionString);
      using var connection = new NpgsqlConnection(connectionString);

      var unavailable = connection.Query(@$"
SELECT DISTINCT  s.title AS stitle, a.title AS atitle, s.unimorphtags || ';' || a.unimorphtags AS tags
FROM slots s
INNER JOIN paradigmclasses p ON p.id = s.paradigmclassid
INNER JOIN agreementgroups ag ON ag.id = s.agreementgroupid
INNER JOIN agreements a ON a.agreementgroupid = ag.id
WHERE p.langid = {langid} AND s.unimorphtags || ';' || a.unimorphtags NOT IN
(SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = '{metalang}')
ORDER BY s.title, a.title").ToList();
      // search for slots without agreement
      var unavailableWithoutAgr = connection.Query(@$"
SELECT DISTINCT s.title AS stitle, s.unimorphtags AS tags
FROM slots s
INNER JOIN paradigmclasses p ON p.id = s.paradigmclassid
WHERE p.langid = {langid} AND s.formula NOT LIKE '%A%' AND s.unimorphtags NOT IN
(SELECT DISTINCT unimorphtags FROM questions WHERE questionlang = '{metalang}')
ORDER BY s.title").ToList();
      if (unavailableWithoutAgr.Count > 0)
        unavailable.AddRange(unavailableWithoutAgr);

      return Ok(unavailable);
    }

    [HttpGet("get")]
    public IActionResult get(string metalang, string tags)
    {
      var q = _context.questions.FirstOrDefault(x => x.unimorphtags == tags && x.questionlang == metalang);
      if (q == null)
        return BadRequest("Not found");
      return Ok(q.question);
    }


    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert(QuestionTemplate qtempl)
    {
      qtempl.userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      _context.questions.Add(qtempl);
      _context.SaveChanges();
      var id = qtempl.id;
      // log
      var userLog = new UserLog()
      {
        log = $"Inserted qTemplate {qtempl.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }


    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update(QuestionTemplate qtempl)
    {
      var old = _context.questions.FirstOrDefault(x => x.unimorphtags == qtempl.unimorphtags && x.questionlang == qtempl.questionlang);
      if (old == null)
        return BadRequest("not exist");
      qtempl.id = old.id;
      _context.Entry(old).State = EntityState.Detached;
      _context.questions.Update(qtempl);
      _context.SaveChanges();
      return Ok(qtempl.id.ToString());
    }

    [HttpGet("download")]
    public IActionResult download(string metalang)
    {
      var result = _context.questions.Where(x => x.questionlang == metalang).Select(x => new { x.unimorphtags, x.question }).ToList();
      return Ok(result);
    }
  }
}