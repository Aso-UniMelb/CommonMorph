using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;

namespace CommonMorphAPI.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class AgreementController : Controller
	{
    [HttpGet("listGroups")]
    public IActionResult listGroups(int DialectId)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.AgreementGroups
        .Where(x => x.DialectID == DialectId)
        .Select(x => new { x.Id, x.Title }).ToList());
      }
    }

    [HttpGet("getGroup")]
    public IActionResult getGroup(int id)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.AgreementGroups.FirstOrDefault(x => x.Id == id));
      }
    }
    
    [HttpPost("insertGroup")]
    public IActionResult insertGroup([FromForm] AgreementGroup agr)
    {
      using (var context = new _DbContext())
      {
        if (context.AgreementGroups.Any(x => x.Title == agr.Title && x.DialectID == agr.DialectID))
          return BadRequest("duplicate");

        context.AgreementGroups.Add(agr);
        context.SaveChanges();
        var id = agr.Id;
        return Ok(id.ToString());
      }
    }

    [HttpPost("updateGroup")]
    public IActionResult updateGroup([FromForm] AgreementGroup agr)
    {
      using (var context = new _DbContext())
      {
        var old = context.AgreementGroups.FirstOrDefault(x => x.Id == agr.Id);
        if (old == null)
          return BadRequest("not exist");
        context.Entry(old).State = EntityState.Detached;
        context.AgreementGroups.Update(agr);
        context.SaveChanges();
        return Ok(agr.Id.ToString());
      }
    }

    [HttpGet("listItems")]
    public IActionResult listItems(int AgreementGroupId)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Agreements
        .Where(x => x.AgreementGroupID == AgreementGroupId && x.isDeleted == false)
        .Select(x => new { x.Id, x.AgreementGroupID, x.UniMorphTags, x.Formula, x.priority }).ToList());
      }
    }
    [HttpGet("getItem")]
    public IActionResult getItem(int id)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Agreements.FirstOrDefault(x => x.Id == id));
      }
    }
    [HttpPost("insertItem")]
    public IActionResult insertItem([FromForm] Agreement agr)
    {
      using (var context = new _DbContext())
      {
        if (context.Agreements.Any(x => x.UniMorphTags == agr.UniMorphTags && x.AgreementGroupID == agr.AgreementGroupID))
          return BadRequest("duplicate");

        context.Agreements.Add(agr);
        context.SaveChanges();
        var id = agr.Id;
        return Ok(id.ToString());
      }
    }
    [HttpPost("updateItem")]
    public IActionResult updateItem([FromForm] Agreement agr)
    {
      using (var context = new _DbContext())
      {
        var old = context.Agreements.FirstOrDefault(x => x.Id == agr.Id);
        if (old == null)
          return BadRequest("not exist");
        context.Entry(old).State = EntityState.Detached;
        context.Agreements.Update(agr);
        context.SaveChanges();
        return Ok(agr.Id.ToString());
      }
    }
  }
}