using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;

namespace CommonMorphAPI.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class SlotController : Controller
  {
    [HttpGet("list")]
    public IActionResult list(int wordClassID)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Slots
        .Where(s => s.WordClassID == wordClassID)
        .Select(x => new { x.Id, x.UniMorphTags, x.Formula, x.priority }).ToList());
      }
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Slots.FirstOrDefault(x => x.Id == id));
      }
    }

    [HttpPost("insert")]
    public IActionResult insert([FromForm] Slot slot)
    {
      using (var context = new _DbContext())
      {
        if (context.Slots.Any(x => x.UniMorphTags == slot.UniMorphTags && x.WordClassID == slot.WordClassID))
          return BadRequest("duplicate");

        context.Slots.Add(slot);
        context.SaveChanges();
        var id = slot.Id;
        return Ok(id.ToString());
      }
    }

    [HttpPost("update")]
    public IActionResult update([FromForm] Slot slot)
    {
      using (var context = new _DbContext())
      {
        var old = context.Slots.FirstOrDefault(x => x.Id == slot.Id);
        if (old == null)
          return BadRequest("not exist");
        context.Entry(old).State = EntityState.Detached;
        context.Slots.Update(slot);
        context.SaveChanges();
        return Ok(slot.Id.ToString());
      }
    }
  }
}