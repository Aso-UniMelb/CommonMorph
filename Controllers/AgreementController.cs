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
  public class AgreementController : Controller
  {
    private readonly AppDbContext _context;
    public AgreementController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("listGroups")]
    public IActionResult listGroups(int LangId)
    {
      return Ok(_context.agreementgroups
      .Where(x => x.langid == LangId)
      .Select(x => new { x.id, x.title }).ToList());
    }

    [HttpGet("getGroup")]
    public IActionResult getGroup(int id)
    {
      return Ok(_context.agreementgroups.FirstOrDefault(x => x.id == id));
    }

    [HttpPost("insertGroup")]
    public IActionResult insertGroup(AgreementGroup agr)
    {
      if (_context.agreementgroups.Any(x => x.title == agr.title && x.langid == agr.langid))
        return BadRequest("duplicate");

      _context.agreementgroups.Add(agr);
      _context.SaveChanges();
      var id = agr.id;
      return Ok(id.ToString());
    }

    [HttpPost("updateGroup")]
    public IActionResult updateGroup(AgreementGroup agr)
    {
      var old = _context.agreementgroups.FirstOrDefault(x => x.id == agr.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.agreementgroups.Update(agr);
      _context.SaveChanges();
      return Ok(agr.id.ToString());
    }
    // ============

    [HttpGet("listItems")]
    public IActionResult listItems(int AgreementGroupId)
    {
      return Ok(_context.agreements
      .Where(x => x.agreementgroupid == AgreementGroupId && x.isdeleted == false)
      .Select(x => new { x.id, x.realization, x.order, x.title, x.unimorphtags }).OrderBy(x => x.unimorphtags).ToList());
    }

    [HttpGet("getItem")]
    public IActionResult getItem(int id)
    {
      return Ok(_context.agreements.FirstOrDefault(x => x.id == id));
    }

    [HttpPost("insertItem")]
    public IActionResult insertItem(Agreement agr)
    {
      if (_context.agreements.Any(x => x.unimorphtags == agr.unimorphtags && x.agreementgroupid == agr.agreementgroupid))
        return BadRequest("duplicate");

      _context.agreements.Add(agr);

      _context.SaveChanges();

      var id = agr.id;
      // log
      var userLog = new UserLog()
      {
        log = $"Inserted agreement {agr.id}",
        userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value),
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [HttpPost("updateItem")]
    public IActionResult updateItem(Agreement agr)
    {
      var old = _context.agreements.FirstOrDefault(x => x.id == agr.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.agreements.Update(agr);
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
          var agrGr = new AgreementGroup()
          {
            title = line.Replace("#", "").Trim(),
            langid = langid
          };
          if (_context.agreementgroups.Any(x => x.title == agrGr.title && x.langid == agrGr.langid))
          {
            curGroupId = _context.agreementgroups.FirstOrDefault(x => x.title == agrGr.title && x.langid == agrGr.langid).id;
          }
          else
          {
            _context.agreementgroups.Add(agrGr);
            _context.SaveChanges();
            curGroupId = agrGr.id;
          }
        }
        else
        {
          if (!string.IsNullOrEmpty(line) && line.Trim().Length > 1)
          {
            var agr = new Agreement()
            {
              order = Int32.Parse(line.Split('\t')[0].Trim()),
              unimorphtags = line.Split('\t')[1].Trim(),
              realization = line.Split('\t')[2].Trim(),
              title = line.Split('\t')[3].Trim(),
              agreementgroupid = curGroupId,
            };
            _context.agreements.Add(agr);
            _context.SaveChanges();
          }
        }
      }
      return Ok();
    }
  }
}