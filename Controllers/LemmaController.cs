using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;

namespace CommonMorphAPI.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class LemmaController : Controller
  {
    [HttpGet("list")]
    public IActionResult list(int dialectID)
    {
      using (var context = new _DbContext())
      {
        var result = from l in context.Lemmas
          join w in context.WordClasses
          on l.WordClassID equals w.Id
          where w.DialectID == dialectID
          select new 
          {
              l.Id,
              l.Entry,
              l.Stem1,
              l.Stem2,
              l.Description,
              l.WordClassID,
              l.priority,
              wClass = w.Title
          };

        return Ok(result.ToList());
      }
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Lemmas.FirstOrDefault(x => x.Id == id));
      }
    }

    [HttpPost("insert")]
    public IActionResult insert([FromForm] Lemma lem)
    {
      using (var context = new _DbContext())
      {
        if (context.Lemmas.Any(x => x.Entry == lem.Entry && x.WordClassID == lem.WordClassID))
          return BadRequest("duplicate");

        context.Lemmas.Add(lem);
        context.SaveChanges();
        var id = lem.Id;
        return Ok(id.ToString());
      }
    }

    [HttpPost("update")]
    public IActionResult update([FromForm] Lemma lem)
    {
      using (var context = new _DbContext())
      {
        var old = context.Lemmas.FirstOrDefault(x => x.Id == lem.Id);
        if (old == null)
          return BadRequest("not exist");
        context.Entry(old).State = EntityState.Detached;
        context.Lemmas.Update(lem);
        context.SaveChanges();
        return Ok(lem.Id.ToString());
      }
    }
  }
}