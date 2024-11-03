using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;

namespace CommonMorphAPI.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class WordClassController : Controller
	{
		[HttpGet("list")]
		public IActionResult list(int DialectId)
		{
			using (var context = new _DbContext())
			{
				return Ok(context.WordClasses
				.Where(x => x.DialectID == DialectId)
				.Select(x => new { x.Id, x.DialectID, x.Title }).ToList());
			}
		}


		[HttpGet("get")]
		public IActionResult get(int id)
		{
			using (var context = new _DbContext())
			{
				return Ok(context.WordClasses.FirstOrDefault(x => x.Id == id));
			}
		}

		[HttpPost("insert")]
		public IActionResult insert([FromForm] WordClass wClass)
		{
			using (var context = new _DbContext())
			{
				if (context.WordClasses.Any(x => x.Title == wClass.Title && x.DialectID == wClass.DialectID))
					return BadRequest("duplicate");

				context.WordClasses.Add(wClass);
				context.SaveChanges();
				var id = wClass.Id;
				return Ok(id.ToString());
			}
		}

		[HttpPost("update")]
		public IActionResult update([FromForm] WordClass wClass)
		{
			using (var context = new _DbContext())
			{
				var old = context.WordClasses.FirstOrDefault(x => x.Id == wClass.Id);
				if (old == null)
					return BadRequest("not exist");
				context.Entry(old).State = EntityState.Detached;
				context.WordClasses.Update(wClass);
				context.SaveChanges();
				return Ok(wClass.Id.ToString());
			}
		}
	}
}