using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class ParadigmClassController : Controller
  {
    private readonly AppDbContext _context;
    public ParadigmClassController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("list")]
    public IActionResult list(int LangId)
    {
      return Ok(_context.paradigmclasses
        .Where(x => x.langid == LangId).OrderBy(x => x.title)
        .Select(x => new { x.id, x.langid, x.title }).ToList());
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.paradigmclasses.FirstOrDefault(x => x.id == id));
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("insert")]
    public IActionResult insert(ParadigmClass pClass)
    {
      if (_context.paradigmclasses.Any(x => x.title == pClass.title && x.langid == pClass.langid))
        return BadRequest("duplicate");

      _context.paradigmclasses.Add(pClass);
      _context.SaveChanges();
      var id = pClass.id;
      return Ok(id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("update")]
    public IActionResult update(ParadigmClass pClass)
    {
      var old = _context.paradigmclasses.FirstOrDefault(x => x.id == pClass.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.paradigmclasses.Update(pClass);
      _context.SaveChanges();
      return Ok(pClass.id.ToString());
    }
  }
}