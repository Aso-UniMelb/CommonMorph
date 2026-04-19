using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  // [ApiController]
  public class StructureController : Controller
  {

    private readonly AppDbContext _context;
    public StructureController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("list")]
    public IActionResult list(int InflectionClassID)
    {
      return Ok(_context.structures
      .Where(s => s.inflectionclassid == InflectionClassID).OrderBy(x => x.order)
      .Select(x => new { x.id, x.unimorphtags, x.formula, x.order, x.reusablelayerid, x.title }).ToList());
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.structures.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert(Structure structure)
    {
      if (_context.structures.Any(x => x.unimorphtags == structure.unimorphtags && x.inflectionclassid == structure.inflectionclassid))
        return BadRequest("duplicate");
      _context.structures.Add(structure);
      _context.SaveChanges();
      var id = structure.id;
      // log
      var userLog = new UserLog()
      {
        log = $"Inserted structure {structure.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update(Structure structure)
    {
      var old = _context.structures.FirstOrDefault(x => x.id == structure.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.structures.Update(structure);
      // log
      var userLog = new UserLog()
      {
        log = $"Updated structure {structure.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);

      _context.SaveChanges();
      return Ok(structure.id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("import")]
    public IActionResult import(string file, int langid)
    {
      var lines = file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
      var curParClassId = 0;
      foreach (var l in lines)
      {
        var line = l.Trim();
        if (line.StartsWith("#"))
        {
          var pClass = new InflectionClass()
          {
            title = line.Replace("#", "").Trim(),
            langid = langid
          };

          if (_context.inflectionclasses.Any(x => x.title == pClass.title && x.langid == pClass.langid))
          {
            curParClassId = _context.inflectionclasses.FirstOrDefault(x => x.title == pClass.title && x.langid == pClass.langid).id;
          }
          else
          {
            _context.inflectionclasses.Add(pClass);
            _context.SaveChanges();
            curParClassId = pClass.id;
          }
        }
        else
        {
          if (!string.IsNullOrEmpty(line) && line.Trim().Length > 1)
          {
            var structure = new Structure()
            {
              unimorphtags = line.Split('\t')[0].Trim(),
              formula = line.Split('\t')[1].Trim(),
              title = line.Split('\t')[2].Trim(),
              order = 0,
              inflectionclassid = curParClassId,
            };
            _context.structures.Add(structure);
            _context.SaveChanges();
          }
        }
      }
      return Ok();
    }
  }
}