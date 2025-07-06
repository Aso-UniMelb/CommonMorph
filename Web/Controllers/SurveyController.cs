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
  // [ApiController]
  public class SurveyController : Controller
  {

    private readonly AppDbContext _context;
    public SurveyController(AppDbContext context)
    {
      _context = context;
    }

    [HttpGet("list")]
    public IActionResult list()
    {
      return Ok(_context.surveys.ToList());
    }


    [Authorize]
    [HttpPost("insert")]
    public IActionResult insert(Survey survey)
    {
      survey.surveydate = DateTime.UtcNow;
      survey.userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      if (_context.surveys.Any(x => x.userid == survey.userid && x.langid == survey.langid))
        return BadRequest("duplicate");
      _context.surveys.Add(survey);
      _context.SaveChanges();
      var id = survey.id;
      return Ok(id.ToString());
    }
  }
}