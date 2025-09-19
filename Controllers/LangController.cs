using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class LangController : Controller
  {

    private readonly AppDbContext _context;
    public LangController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("list")]
    public IActionResult list()
    {
      return Ok(_context.langs.Where(x => x.isdeleted == false).Select(x => new { x.id, x.code, x.title, x.validchars }).ToList());
    }

    [HttpGet("mapdata")]
    public IActionResult mapdata()
    {
      return Ok(_context.langs.Where(x => x.isdeleted == false).Select(x => new { x.id, x.code, x.title, x.latitude, x.longitude }).ToList());
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.langs.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin")]
    [HttpPost("insert")]
    public IActionResult insert(Lang lang)
    {
      if (_context.langs.Any(x => x.code == lang.code && x.title == lang.title))
        return BadRequest("duplicate");
      _context.langs.Add(lang);
      _context.SaveChanges();
      var id = lang.id;
      return Ok(id.ToString());
    }

    [Authorize(Roles = "admin")]
    [HttpPost("update")]
    public IActionResult update(Lang lang)
    {
      var old = _context.langs.FirstOrDefault(x => x.id == lang.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.langs.Update(lang);
      _context.SaveChanges();
      return Ok(lang.id.ToString());
    }
  }
}