using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;

namespace common_morph_backend.Controllers
{
  public class HomeController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private string connectionString;
    public HomeController(AppDbContext context, IConfiguration configuration)
    {
      _context = context;
      _configuration = configuration;
      // For the complex query used in this controller, we need Dapper to connect to DB
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
    }
    public IActionResult Index()
    {
      return View();
    }

    public IActionResult Datasets()
    {
      // list all languages that have data in cells table
      using var connection = new NpgsqlConnection(connectionString);
      ViewBag.submittedByLang = connection.Query(@$"
SELECT cells.langid, langs.title, langs.code, COUNT(*) AS cnt
FROM cells
INNER JOIN langs ON langs.id = cells.langid
GROUP BY langid, title, code
ORDER BY cnt DESC").ToList();

      ViewBag.lexiconStats = connection.Query(@$"
SELECT langs.id AS langid, langs.title, langs.code, COUNT(*) AS cnt
FROM lemmas l
INNER JOIN paradigmclasses pc ON pc.id = l.paradigmclassid
INNER JOIN langs ON langs.id = pc.langid
GROUP BY langs.id, langs.title, langs.code
ORDER BY cnt DESC").ToList();

      return View();
    }
    public IActionResult About()
    {
      return View();
    }
    public IActionResult Privacy()
    {
      return View();
    }
    public IActionResult tutorials()
    {
      return View();
    }
  }
}