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
  public class LemmaController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private string connectionString;
    public LemmaController(AppDbContext context, IConfiguration configuration)
    {
      _context = context;
      _configuration = configuration;
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
    }

    [HttpGet("list")]
    public IActionResult list(int LangID)
    {
      var result = from l in _context.lemmas
                   join w in _context.paradigmclasses
                   on l.paradigmclassid equals w.id
                   where w.langid == LangID
                   select new
                   {
                     l.id,
                     l.entry,
                     l.engmeaning,
                     l.stem1,
                     l.stem2,
                     l.stem3,
                     l.stem4,
                     l.description,
                     l.paradigmclassid,
                     l.priority,
                     wClass = w.title
                   };

      return Ok(result.ToList());
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.lemmas.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert(Lemma lem)
    {
      if (_context.lemmas.Any(x => x.entry == lem.entry && x.paradigmclassid == lem.paradigmclassid))
        return BadRequest("duplicate");

      _context.lemmas.Add(lem);
      _context.SaveChanges();

      var id = lem.id;
      var userLog = new UserLog()
      {
        log = $"Inserted lemma {lem.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update(Lemma lem)
    {
      var old = _context.lemmas.FirstOrDefault(x => x.id == lem.id);
      if (old == null)
        return BadRequest("not exist");

      _context.Entry(old).State = EntityState.Detached;
      _context.lemmas.Update(lem);

      // delete all cells with this lemma
      var cells = _context.cells.Where(x => x.lemmaid == lem.id).ToList();
      foreach (var cell in cells)
      {
        _context.cells.Remove(cell);
      }

      // log
      var userLog = new UserLog()
      {
        log = $"Updatede lemma {lem.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);

      _context.SaveChanges();
      return Ok(lem.id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("import")]
    public IActionResult import(string file, int langid)
    {
      var lines = file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
      var curParClassId = 0;
      foreach (var ln in lines)
      {
        var line = ln.Trim();
        if (!string.IsNullOrEmpty(line) && line.Trim().Length > 1)
        {
          var l = line.Split('\t');
          var pClass = new ParadigmClass()
          {
            title = l[0].Trim(),
            langid = langid
          };
          if (_context.paradigmclasses.Any(x => x.title == pClass.title && x.langid == pClass.langid))
            curParClassId = _context.paradigmclasses.FirstOrDefault(x => x.title == pClass.title && x.langid == pClass.langid).id;
          else
            return BadRequest("paradigm class not found");

          var lemma = new Lemma()
          {
            entry = l[1].Trim(),
            engmeaning = l[2].Trim(),
            stem1 = (l.Length > 3) ? l[3].Trim() : "",
            stem2 = (l.Length > 4) ? l[4].Trim() : "",
            stem3 = (l.Length > 5) ? l[5].Trim() : "",
            stem4 = (l.Length > 6) ? l[6].Trim() : "",
            paradigmclassid = curParClassId,
            priority = 0,
          };
          _context.lemmas.Add(lemma);
          _context.SaveChanges();
        }

      }
      return Ok();
    }
    [HttpPost("downloadFull")]
    public IActionResult downloadFull(int langid)
    {
      // using dapper
      using var connection = new NpgsqlConnection(connectionString);

      var lemmas = connection.Query(@$"
SELECT l.entry AS lemma, pc.title AS classtitle, l.engmeaning AS meaning, l.stem1, l.stem2, l.stem3, l.stem4
FROM lemmas l
INNER JOIN paradigmclasses pc ON pc.id = l.paradigmclassid
INNER JOIN langs ON langs.id = pc.langid
WHERE pc.langid = {langid} AND l.isdeleted is FALSE
ORDER BY l.entry");
      return Ok(lemmas);
    }
  }
}