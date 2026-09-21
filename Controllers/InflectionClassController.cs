using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class InflectionClassController : Controller
  {
    private readonly AppDbContext _context;
    public InflectionClassController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("list")]
    public IActionResult list(int LangId)
    {
      return Ok(_context.inflectionclasses
        .Where(x => x.langid == LangId && !x.isdeleted).OrderBy(x => x.title)
        .Select(x => new { x.id, x.langid, x.title, x.description }).ToList());
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.inflectionclasses.FirstOrDefault(x => x.id == id && !x.isdeleted));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert([FromBody] InflectionClass pClass)
    {
      if (pClass == null)
        return BadRequest("Invalid inflection class payload");

      var title = pClass.title ?? "";
      var langId = pClass.langid;

      if (_context.inflectionclasses.Any(x => x.title == title && x.langid == langId && !x.isdeleted))
        return BadRequest("An inflection class with this title already exists.");

      pClass.isdeleted = false;
      _context.inflectionclasses.Add(pClass);
      _context.SaveChanges();
      return Ok(new { id = pClass.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update([FromBody] InflectionClass pClass)
    {
      if (pClass == null || pClass.id == 0)
        return BadRequest("Invalid inflection class payload");

      var targetId = pClass.id;
      var old = _context.inflectionclasses.FirstOrDefault(x => x.id == targetId);
      if (old == null)
        return BadRequest("Inflection class does not exist");

      old.title = pClass.title ?? old.title;
      old.description = pClass.description ?? old.description;
      _context.SaveChanges();
      return Ok(new { id = old.id, success = true });
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("delete")]
    public IActionResult delete([FromQuery] int id)
    {
      var old = _context.inflectionclasses.FirstOrDefault(x => x.id == id);
      if (old == null)
        return BadRequest("not exist");

      old.isdeleted = true;
      _context.SaveChanges();
      return Ok(new { id = id, success = true });
    }
  }
}