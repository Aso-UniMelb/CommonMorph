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
  public class SlotController : Controller
  {

    private readonly AppDbContext _context;
    public SlotController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("list")]
    public IActionResult list(int ParadigmClassID)
    {
      return Ok(_context.slots
      .Where(s => s.paradigmclassid == ParadigmClassID).OrderBy(x => x.title)
      .Select(x => new { x.id, x.unimorphtags, x.formula, x.priority, x.agreementgroupid, x.title }).ToList());
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.slots.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert(Slot slot)
    {
      if (_context.slots.Any(x => x.unimorphtags == slot.unimorphtags && x.paradigmclassid == slot.paradigmclassid))
        return BadRequest("duplicate");
      _context.slots.Add(slot);
      _context.SaveChanges();
      var id = slot.id;
      // log
      var userLog = new UserLog()
      {
        log = $"Inserted slot {slot.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update(Slot slot)
    {
      var old = _context.slots.FirstOrDefault(x => x.id == slot.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.slots.Update(slot);
      // log
      var userLog = new UserLog()
      {
        log = $"Updated slot {slot.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);

      _context.SaveChanges();
      return Ok(slot.id.ToString());
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
          var pClass = new ParadigmClass()
          {
            title = line.Replace("#", "").Trim(),
            langid = langid
          };

          if (_context.paradigmclasses.Any(x => x.title == pClass.title && x.langid == pClass.langid))
          {
            curParClassId = _context.paradigmclasses.FirstOrDefault(x => x.title == pClass.title && x.langid == pClass.langid).id;
          }
          else
          {
            _context.paradigmclasses.Add(pClass);
            _context.SaveChanges();
            curParClassId = pClass.id;
          }
        }
        else
        {
          if (!string.IsNullOrEmpty(line) && line.Trim().Length > 1)
          {
            var slot = new Slot()
            {
              unimorphtags = line.Split('\t')[0].Trim(),
              formula = line.Split('\t')[1].Trim(),
              title = line.Split('\t')[2].Trim(),
              priority = 0,
              paradigmclassid = curParClassId,
            };
            _context.slots.Add(slot);
            _context.SaveChanges();
          }
        }
      }
      return Ok();
    }
  }
}