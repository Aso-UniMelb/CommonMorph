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

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insertLayer")]
    public IActionResult insertLayer([FromBody] ReusableLayer agr)
    {
      if (_context.reusablelayers.Any(x => x.title == agr.title && x.langid == agr.langid))
        return BadRequest("duplicate");

      _context.reusablelayers.Add(agr);
      _context.SaveChanges();
      return Ok(new { id = agr.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("updateLayer")]
    public IActionResult updateLayer([FromBody] ReusableLayer agr)
    {
      var old = _context.reusablelayers.FirstOrDefault(x => x.id == agr.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.reusablelayers.Update(agr);
      _context.SaveChanges();
      return Ok(new { id = agr.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("deleteLayer")]
    public IActionResult deleteLayer([FromQuery] int id)
    {
      var old = _context.reusablelayers.FirstOrDefault(x => x.id == id);
      if (old == null) return BadRequest("not exist");
      _context.reusablelayers.Remove(old);
      _context.SaveChanges();
      return Ok(new { id, success = true });
    }
    // ============

    [HttpGet("listAffixes")]
    public IActionResult listAffixes(int ReusableLayerId)
    {
      return Ok(_context.affixes
      .Where(x => x.reusablelayerid == ReusableLayerId && x.isdeleted == false)
      .Select(x => new { x.id, x.realization, x.order, x.title, x.unimorphtags, x.reusablelayerid })
      .OrderBy(x => x.order).ToList());
    }

    [HttpGet("getAffix")]
    public IActionResult getAffix(int id)
    {
      return Ok(_context.affixes.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insertAffix")]
    public IActionResult insertAffix([FromBody] Affix agr)
    {
      if (_context.affixes.Any(x => x.unimorphtags == agr.unimorphtags && x.reusablelayerid == agr.reusablelayerid && !x.isdeleted))
        return BadRequest("duplicate");

      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;

      agr.isdeleted = false;
      _context.affixes.Add(agr);
      _context.SaveChanges();

      var userLog = new UserLog()
      {
        log = $"Inserted affix {agr.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(new { id = agr.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("updateAffix")]
    public IActionResult updateAffix([FromBody] Affix agr)
    {
      var old = _context.affixes.FirstOrDefault(x => x.id == agr.id);
      if (old == null)
        return BadRequest("not exist");

      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var userId = userIdClaim != null ? Convert.ToInt32(userIdClaim) : 0;

      old.title = agr.title ?? old.title;
      old.realization = agr.realization ?? old.realization;
      old.unimorphtags = agr.unimorphtags ?? old.unimorphtags;
      old.order = agr.order;

      var userLog = new UserLog()
      {
        log = $"Updated affix {agr.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(new { id = old.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("deleteAffix")]
    public IActionResult deleteAffix([FromQuery] int id)
    {
      var old = _context.affixes.FirstOrDefault(x => x.id == id);
      if (old == null) return BadRequest("not exist");
      old.isdeleted = true;
      _context.SaveChanges();
      return Ok(new { id, success = true });
    }

    public class AffixImportRequest
    {
      public string file { get; set; } = "";
      public int langid { get; set; }
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("import")]
    public IActionResult import([FromBody] AffixImportRequest req)
    {
      if (string.IsNullOrWhiteSpace(req.file)) return BadRequest("Empty content");
      var lines = req.file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
      var curGroupId = 0;

      foreach (var l in lines)
      {
        var line = l.Trim();
        if (string.IsNullOrEmpty(line)) continue;

        if (line.StartsWith("#"))
        {
          var title = line.Replace("#", "").Trim();
          var agrGr = _context.reusablelayers.FirstOrDefault(x => x.title == title && x.langid == req.langid);
          if (agrGr == null)
          {
            agrGr = new ReusableLayer
            {
              title = title,
              langid = req.langid
            };
            _context.reusablelayers.Add(agrGr);
            _context.SaveChanges();
          }
          curGroupId = agrGr.id;
        }
        else if (curGroupId > 0)
        {
          var parts = line.Split('\t');
          if (parts.Length >= 2)
          {
            int order = 1;
            string tags = "";
            string realization = "";
            string title = "";

            if (int.TryParse(parts[0].Trim(), out int parsedOrder))
            {
              order = parsedOrder;
              tags = parts.Length > 1 ? parts[1].Trim() : "";
              realization = parts.Length > 2 ? parts[2].Trim() : "";
              title = parts.Length > 3 ? parts[3].Trim() : "";
            }
            else
            {
              tags = parts[0].Trim();
              realization = parts.Length > 1 ? parts[1].Trim() : "";
              title = parts.Length > 2 ? parts[2].Trim() : "";
            }

            var existing = _context.affixes.FirstOrDefault(x => x.reusablelayerid == curGroupId && x.realization == realization && x.unimorphtags == tags && !x.isdeleted);
            if (existing != null)
            {
              existing.order = order;
              existing.title = title;
            }
            else
            {
              var affix = new Affix
              {
                reusablelayerid = curGroupId,
                order = order,
                unimorphtags = tags,
                realization = realization,
                title = title,
                isdeleted = false
              };
              _context.affixes.Add(affix);
            }
            _context.SaveChanges();
          }
        }
      }
      return Ok(new { success = true });
    }
  }
}