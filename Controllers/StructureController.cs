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
      .Where(s => s.inflectionclassid == InflectionClassID && !s.isdeleted).OrderBy(x => x.order)
      .Select(x => new { x.id, x.unimorphtags, x.formula, x.order, x.reusablelayerid, x.title, x.inflectionclassid }).ToList());
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.structures.FirstOrDefault(x => x.id == id && !x.isdeleted));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert([FromBody] Structure structure)
    {
      if (_context.structures.Any(x => x.unimorphtags == structure.unimorphtags && x.inflectionclassid == structure.inflectionclassid && !x.isdeleted))
        return BadRequest("duplicate");

      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;

      structure.isdeleted = false;
      _context.structures.Add(structure);
      _context.SaveChanges();

      var userLog = new UserLog()
      {
        log = $"Inserted structure {structure.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(new { id = structure.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update([FromBody] Structure structure)
    {
      var old = _context.structures.FirstOrDefault(x => x.id == structure.id);
      if (old == null)
        return BadRequest("not exist");

      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;

      old.title = structure.title ?? old.title;
      old.unimorphtags = structure.unimorphtags ?? old.unimorphtags;
      old.formula = structure.formula ?? old.formula;
      old.order = structure.order;
      old.reusablelayerid = structure.reusablelayerid;
      old.inflectionclassid = structure.inflectionclassid != 0 ? structure.inflectionclassid : old.inflectionclassid;

      var userLog = new UserLog()
      {
        log = $"Updated structure {structure.id}",
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
      var old = _context.structures.FirstOrDefault(x => x.id == id);
      if (old == null) return BadRequest("not exist");
      old.isdeleted = true;
      _context.SaveChanges();
      return Ok(new { id = id, success = true });
    }

    public class StructureImportRequest
    {
      public string file { get; set; } = "";
      public int langid { get; set; }
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("import")]
    public IActionResult import([FromBody] StructureImportRequest req)
    {
      if (string.IsNullOrWhiteSpace(req.file)) return BadRequest("Empty content");
      var lines = req.file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
      var curParClassId = 0;

      foreach (var l in lines)
      {
        var line = l.Trim();
        if (string.IsNullOrEmpty(line)) continue;

        if (line.StartsWith("#"))
        {
          var title = line.Replace("#", "").Trim();
          var pClass = _context.inflectionclasses.FirstOrDefault(x => x.title == title && x.langid == req.langid && !x.isdeleted);
          if (pClass == null)
          {
            pClass = new InflectionClass
            {
              title = title,
              langid = req.langid,
              isdeleted = false
            };
            _context.inflectionclasses.Add(pClass);
            _context.SaveChanges();
          }
          curParClassId = pClass.id;
        }
        else if (curParClassId > 0)
        {
          var parts = line.Split('\t');
          if (parts.Length >= 2)
          {
            var tags = parts[0].Trim();
            var formula = parts.Length > 1 ? parts[1].Trim() : "";
            var title = parts.Length > 2 ? parts[2].Trim() : tags;

            var existing = _context.structures.FirstOrDefault(x => x.inflectionclassid == curParClassId && x.unimorphtags == tags && !x.isdeleted);
            if (existing != null)
            {
              existing.formula = formula;
              existing.title = title;
            }
            else
            {
              var structure = new Structure
              {
                unimorphtags = tags,
                formula = formula,
                title = title,
                order = 0,
                inflectionclassid = curParClassId,
                isdeleted = false
              };
              _context.structures.Add(structure);
            }
            _context.SaveChanges();
          }
        }
      }
      return Ok(new { success = true });
    }
  }
}