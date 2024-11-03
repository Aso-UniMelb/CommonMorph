using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;

namespace CommonMorphAPI.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class DialectController : Controller
  {
    [HttpGet("list")]
    public IActionResult list()
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Dialects.Select(x => new { x.Id, x.Code, x.Title, x.KeyboardLayout }).ToList());
      }
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Dialects.FirstOrDefault(x => x.Id == id));
      }
    }

    [HttpPost("insert")]
    public IActionResult insert([FromForm] Dialect dialect)
    {
      using (var context = new _DbContext())
      {
        if (context.Dialects.Any(x => x.Code == dialect.Code))
          return BadRequest("duplicate");

        context.Dialects.Add(dialect);
        context.SaveChanges();
        var id = dialect.Id;
        return Ok(id.ToString());
      }
    }

    [HttpPost("update")]
    public IActionResult update([FromForm] Dialect dialect)
    {
      using (var context = new _DbContext())
      {
        var old = context.Dialects.FirstOrDefault(x => x.Id == dialect.Id);
        if (old == null)
          return BadRequest("not exist");
        context.Entry(old).State = EntityState.Detached;
        context.Dialects.Update(dialect);
        context.SaveChanges();
        return Ok(dialect.Id.ToString());
      }
    }
  }
}