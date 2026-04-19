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
  public class AffixController : Controller
  {
    private readonly AppDbContext _context;
    public AffixController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("listLayers")]
    public IActionResult listLayers(int LangId)
    {
      return Ok(_context.reusablelayers
      .Where(x => x.langid == LangId)
      .Select(x => new { x.id, x.title }).ToList());
    }

    [HttpGet("getLayer")]
    public IActionResult getLayer(int id)
    {
      return Ok(_context.reusablelayers.FirstOrDefault(x => x.id == id));
    }

    [HttpPost("insertLayer")]
    public IActionResult insertLayer(ReusableLayer agr)
    {
      if (_context.reusablelayers.Any(x => x.title == agr.title && x.langid == agr.langid))
        return BadRequest("duplicate");

      _context.reusablelayers.Add(agr);
      _context.SaveChanges();
      var id = agr.id;
      return Ok(id.ToString());
    }

    [HttpPost("updateLayer")]
    public IActionResult updateLayer(ReusableLayer agr)
    {
      var old = _context.reusablelayers.FirstOrDefault(x => x.id == agr.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.reusablelayers.Update(agr);
      _context.SaveChanges();
      return Ok(agr.id.ToString());
    }
    // ============

    [HttpGet("listAffixes")]
    public IActionResult listAffixes(int ReusableLayerId)
    {
      return Ok(_context.affixes
      .Where(x => x.reusablelayerid == ReusableLayerId && x.isdeleted == false)
      .Select(x => new { x.id, x.realization, x.order, x.title, x.unimorphtags }).OrderBy(x => x.order).ToList());
    }

    [HttpGet("getAffix")]
    public IActionResult getAffix(int id)
    {
      return Ok(_context.affixes.FirstOrDefault(x => x.id == id));
    }

    [HttpPost("insertAffix")]
    public IActionResult insertAffix(Affix agr)
    {
      if (_context.affixes.Any(x => x.unimorphtags == agr.unimorphtags && x.reusablelayerid == agr.reusablelayerid))
        return BadRequest("duplicate");

      _context.affixes.Add(agr);

      _context.SaveChanges();

      var id = agr.id;
      // log
      var userLog = new UserLog()
      {
        log = $"Inserted affix {agr.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [HttpPost("updateAffix")]
    public IActionResult updateAffix(Affix agr)
    {
      var old = _context.affixes.FirstOrDefault(x => x.id == agr.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.affixes.Update(agr);
      _context.SaveChanges();
      return Ok(agr.id.ToString());
    }

    [HttpPost("import")]
    public IActionResult import(string file, int langid)
    {
      // read the string line by line
      var lines = file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
      var curGroupId = 0;
      foreach (var l in lines)
      {
        var line = l.Trim();
        if (line.StartsWith("#"))
        {
          var agrGr = new ReusableLayer()
          {
            title = line.Replace("#", "").Trim(),
            langid = langid
          };
          if (_context.reusablelayers.Any(x => x.title == agrGr.title && x.langid == agrGr.langid))
          {
            curGroupId = _context.reusablelayers.FirstOrDefault(x => x.title == agrGr.title && x.langid == agrGr.langid).id;
          }
          else
          {
            _context.reusablelayers.Add(agrGr);
            _context.SaveChanges();
            curGroupId = agrGr.id;
          }
        }
        else
        {
          if (!string.IsNullOrEmpty(line) && line.Trim().Length > 1)
          {
            var agr = new Affix()
            {
              order = Int32.Parse(line.Split('\t')[0].Trim()),
              unimorphtags = line.Split('\t')[1].Trim(),
              realization = line.Split('\t')[2].Trim(),
              title = line.Split('\t')[3].Trim(),
              reusablelayerid = curGroupId,
            };
            _context.affixes.Add(agr);
            _context.SaveChanges();
          }
        }
      }
      return Ok();
    }
  }
}