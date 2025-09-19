using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace common_morph_backend.Controllers
{
  public class AppController : Controller
  {
    private readonly AppDbContext _context;
    public AppController(AppDbContext context)
    {
      _context = context;
    }

    [Authorize]
    public IActionResult dashboard()
    {
      ViewBag.Name = User.FindFirst(ClaimTypes.GivenName).Value;
      ViewBag.Role = User.FindFirst(ClaimTypes.Role).Value;
      return View();
    }

    [Authorize(Roles = "admin")]
    public IActionResult Admin()
    {
      return View();
    }

    [Authorize(Roles = "admin, linguist")]
    public IActionResult Linguist()
    {
      return View();
    }

    [Authorize(Roles = "admin, linguist")]
    public IActionResult qtemplates()
    {
      return View();
    }

    [Authorize]
    public IActionResult Elicit()
    {
      return View();
    }

    [Authorize]
    public IActionResult Check()
    {
      return View();
    }

    [Authorize]
    public IActionResult Survey()
    {
      ViewBag.Role = (UserRole)Enum.Parse(typeof(UserRole), User.FindFirst(ClaimTypes.Role).Value);
      ViewBag.RoleTitle = User.FindFirst(ClaimTypes.Role).Value;
      return View();
    }
  }
}