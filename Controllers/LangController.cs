using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using NETCore.MailKit.Core;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class LangController : Controller
  {
    private readonly IEmailService _EmailService;
    private readonly AppDbContext _context;
    public LangController(IEmailService emailService, AppDbContext context)
    {
      _EmailService = emailService;
      _context = context;
    }

    [Authorize]
    [HttpPost("request")]
    public IActionResult RequestLang(Lang lang)
    {
      var requester = User.Identity.Name;
      var admins = _context.users.Where(x => x.role == UserRole.admin).ToList();
      foreach (var admin in admins)
      {
        _EmailService.Send(
          admin.username,
          "New Language Variety Request",
          @$"<h2>New Language Variety Request</h2>
          <p><strong>Title:</strong> {lang.title}</p>
          <p><strong>Code (ISO 639-3):</strong> {lang.code}</p>
          <p><strong>Valid Characters:</strong> {lang.validchars}</p>
          <p><strong>Description:</strong> {lang.description}</p>
          <p><strong>Coordinates:</strong> Latitude: {lang.latitude}, Longitude: {lang.longitude}</p>
          <br/>
          <p><em>Requested by: {requester}</em></p>",
          isHtml: true);
      }
      return Ok("Request accepted successfully.");
    }

    [HttpGet("list")]
    public IActionResult list()
    {
      return Ok(_context.langs.Where(x => x.isdeleted == false)
        .OrderBy(x => x.title)
        .Select(x => new { x.id, x.code, x.title, x.validchars, x.description, x.latitude, x.longitude })
        .ToList());
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
    public IActionResult insert([FromBody] Lang lang)
    {
      if (_context.langs.Any(x => x.code == lang.code && x.title == lang.title && x.isdeleted == false))
        return BadRequest("A variety with this code and title already exists.");
      lang.isdeleted = false;
      _context.langs.Add(lang);
      _context.SaveChanges();
      return Ok(new { id = lang.id, success = true });
    }

    [Authorize(Roles = "admin")]
    [HttpPost("update")]
    public IActionResult update([FromBody] Lang lang)
    {
      var old = _context.langs.FirstOrDefault(x => x.id == lang.id);
      if (old == null)
        return BadRequest("Language variety does not exist.");
      
      old.title = lang.title ?? old.title;
      old.code = lang.code ?? old.code;
      old.validchars = lang.validchars ?? old.validchars;
      old.description = lang.description ?? old.description;
      old.latitude = lang.latitude ?? old.latitude;
      old.longitude = lang.longitude ?? old.longitude;

      _context.SaveChanges();
      return Ok(new { id = lang.id, success = true });
    }
  }
}