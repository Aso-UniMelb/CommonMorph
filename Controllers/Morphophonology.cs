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
      var result = _context.morphophonemicrules.Where(r => r.langid == LangID).Select(x => new { x.id, x.title, x.replacefrom, x.replaceto });

      return Ok(result.ToList());
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.morphophonemicrules.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert(MorphophonemicRule rul)
    {
      if (_context.morphophonemicrules.Any(x => x.langid == rul.langid && x.replacefrom == rul.replacefrom && x.replaceto == rul.replaceto))
        return BadRequest("duplicate");

      _context.morphophonemicrules.Add(rul);
      _context.SaveChanges();

      var id = rul.id;
      var userLog = new UserLog()
      {
        log = $"Inserted rule {rul.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update(MorphophonemicRule rul)
    {
      var old = _context.morphophonemicrules.FirstOrDefault(x => x.id == rul.id);
      if (old == null)
        return BadRequest("not exist");

      _context.Entry(old).State = EntityState.Detached;
      _context.morphophonemicrules.Update(rul);

      // delete all cells with this lemma
      var cells = _context.cells.Where(x => x.lemmaid == rul.id).ToList();
      foreach (var cell in cells)
      {
        _context.cells.Remove(cell);
      }

      // log
      var userLog = new UserLog()
      {
        log = $"Updatede lemma {rul.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);

      _context.SaveChanges();
      return Ok(rul.id.ToString());
    }
  }
}