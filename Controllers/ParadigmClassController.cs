using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;

namespace CommonMorphAPI.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class ParadigmClassController : Controller
	{
		[HttpGet("list")]
		public IActionResult list(int DialectId)
		{
			using (var context = new _DbContext())
			{
				return Ok(context.ParadigmClasses
				.Where(x => x.DialectID == DialectId)
				.Select(x => new { x.Id, x.DialectID, x.Title }).ToList());
			}
		}


		[HttpGet("get")]
		public IActionResult get(int id)
		{
			using (var context = new _DbContext())
			{
				return Ok(context.ParadigmClasses.FirstOrDefault(x => x.Id == id));
			}
		}

		[HttpPost("insert")]
		public IActionResult insert([FromForm] ParadigmClass pClass)
		{
			using (var context = new _DbContext())
			{
				if (context.ParadigmClasses.Any(x => x.Title == pClass.Title && x.DialectID == pClass.DialectID))
					return BadRequest("duplicate");

				context.ParadigmClasses.Add(pClass);
				context.SaveChanges();
				var id = pClass.Id;
				return Ok(id.ToString());
			}
		}

		[HttpPost("update")]
		public IActionResult update([FromForm] ParadigmClass pClass)
		{
			using (var context = new _DbContext())
			{
				var old = context.ParadigmClasses.FirstOrDefault(x => x.Id == pClass.Id);
				if (old == null)
					return BadRequest("not exist");
				context.Entry(old).State = EntityState.Detached;
				context.ParadigmClasses.Update(pClass);
				context.SaveChanges();
				return Ok(pClass.Id.ToString());
			}
		}
	}
}