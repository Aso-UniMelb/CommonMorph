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
  public class MorphophonologyController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private string connectionString;
    public MorphophonologyController(AppDbContext context, IConfiguration configuration)
    {
      _context = context;
      _configuration = configuration;
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet("list")]
    public IActionResult list(int LangID)
    {
      var result = _context.morphophonemicrules
        .Where(r => r.langid == LangID && !r.isdeleted)
        .OrderBy(r => r.title)
        .Select(x => new { x.id, x.title, x.replacefrom, x.replaceto, x.langid })
        .ToList();

      return Ok(result);
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.morphophonemicrules.FirstOrDefault(x => x.id == id && !x.isdeleted));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert([FromBody] MorphophonemicRule rul)
    {
      if (_context.morphophonemicrules.Any(x => x.langid == rul.langid && x.replacefrom == rul.replacefrom && x.replaceto == rul.replaceto && !x.isdeleted))
        return BadRequest("A rule with this replacement pattern already exists.");

      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;

      rul.isdeleted = false;
      _context.morphophonemicrules.Add(rul);
      _context.SaveChanges();

      var userLog = new UserLog()
      {
        log = $"Inserted rule {rul.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();

      return Ok(new { id = rul.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update([FromBody] MorphophonemicRule rul)
    {
      var old = _context.morphophonemicrules.FirstOrDefault(x => x.id == rul.id);
      if (old == null)
        return BadRequest("Rule does not exist");

      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;

      old.title = rul.title ?? old.title;
      old.replacefrom = rul.replacefrom ?? old.replacefrom;
      old.replaceto = rul.replaceto ?? old.replaceto;

      var userLog = new UserLog()
      {
        log = $"Updated rule {rul.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();

      return Ok(new { id = old.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("delete")]
    public IActionResult delete([FromQuery] int id)
    {
      var old = _context.morphophonemicrules.FirstOrDefault(x => x.id == id);
      if (old == null)
        return BadRequest("Rule does not exist");

      old.isdeleted = true;
      _context.SaveChanges();
      return Ok(new { id = id, success = true });
    }
  }
}