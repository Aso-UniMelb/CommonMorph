using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.Text;
using common_morph_backend;

namespace common_morph_backend.Controllers
{
  public class HomeController : Controller
  {
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private string connectionString;
    private readonly IMyCacheService _cacheService;
    public HomeController(AppDbContext context, IConfiguration configuration, IMyCacheService cacheService)
    {
      _context = context;
      _configuration = configuration;
      _cacheService = cacheService;
      // For the complex query used in this controller, we need Dapper to connect to DB
      connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? _configuration.GetConnectionString("DefaultConnection");
    }
    public IActionResult Index()
    {
      return View();
    }

    public IActionResult Datasets()
    {
      using var connection = new NpgsqlConnection(connectionString);
      // list all languages that have lexical data
      var lexiconStats = connection.Query(@$"
SELECT langs.id AS langid, langs.title, langs.code, COUNT(*) AS cnt
FROM lexicon l
INNER JOIN inflectionclasses pc ON pc.id = l.inflectionclassid
INNER JOIN langs ON langs.id = pc.langid
GROUP BY langs.id, langs.title, langs.code
ORDER BY cnt DESC").ToList();
      ViewBag.lexiconStats = lexiconStats;
      // get the count of inflected forms for each language
      var langs = string.Join(",", lexiconStats.Select(l => l.langid));
      ViewBag.submittedByLang = connection.Query(@$"
SELECT cells.langid, COUNT(*) AS cnt
FROM cells
INNER JOIN langs ON langs.id = cells.langid
WHERE cells.langid IN ({langs})
GROUP BY langid, title, code
ORDER BY cnt DESC").ToDictionary(x => x.langid, x => x.cnt);
      return View();
    }

    public IActionResult Dataset(int langid)
    {
      using var connection = new NpgsqlConnection(connectionString);
      ViewBag.lang = _context.langs.FirstOrDefault(x => x.id == langid);
      ViewBag.submitted = connection.QueryFirstOrDefault<int>(@$"
SELECT COUNT(*) AS cnt
FROM cells
INNER JOIN langs ON langs.id = cells.langid
WHERE cells.langid = {langid}
GROUP BY langid
ORDER BY cnt DESC");
      return View();
    }

    public IActionResult Download(int langid, string type)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var sb = new StringBuilder();
      switch (type)
      {
        // ==================================
        case "unimorph":
          var result = connection.Query(@$"
SELECT 
	(SELECT entry FROM lexicon WHERE id = c.lemmaid) AS lemma,
	c.submitted AS form,
	(SELECT unimorphtags FROM structures WHERE id = c.structureid) AS stags,
	(SELECT unimorphtags FROM affixes WHERE id = c.affixid) AS atags,
	(SELECT unimorphtags FROM lexicon WHERE id = c.lemmaid) AS ltags,
  COUNT(r.cellid) AS trueratingscount
FROM cells c
LEFT JOIN cellratings r ON c.id = r.cellid
WHERE c.langid = {langid}
GROUP BY c.id, c.affixid, c.structureid, c.lemmaid, c.submitted, r.rate
ORDER BY r.rate, c.lemmaid, c.structureid, c.affixid");
          foreach (var record in result)
          {
            var tags = "";
            if (!string.IsNullOrEmpty(record.ltags))
              tags += record.ltags;
            if (!string.IsNullOrEmpty(record.stags))
              tags += ";" + record.stags;
            if (!string.IsNullOrEmpty(record.atags))
              tags += ";" + record.atags;
            tags = tags.Trim(';');
            sb.AppendLine($"{record.lemma}\t{record.form}\t{_cacheService.UM_Sort(tags)}");
          }
          break;
        // ==================================
        case "extended":
          var forms = connection.Query(@$"
SELECT 
	c.lemmaid AS lemmaid,
	c.submitted AS form,
	(SELECT unimorphtags FROM structures WHERE id = c.structureid) AS stags,
	(SELECT unimorphtags FROM affixes WHERE id = c.affixid) AS atags,
	(SELECT unimorphtags FROM lexicon WHERE id = c.lemmaid) AS ltags,
	(SELECT realization  FROM affixes WHERE id = c.affixid) AS affix,
  (SELECT formula  FROM structures WHERE id = c.structureid) AS formula,
  COUNT(r.cellid) AS trueratingscount
FROM cells c
LEFT JOIN cellratings r ON c.id = r.cellid
WHERE c.langid = {langid}
GROUP BY c.id, c.affixid, c.structureid, c.lemmaid, c.submitted, r.rate
ORDER BY r.rate, c.lemmaid, c.structureid, c.affixid");

          var lemmas = connection.Query(@$"
SELECT l.id, l.entry, pc.title AS pcalss, l.engmeaning AS meaning, l.stem1, l.stem2, l.stem3, l.stem4, l.unimorphtags
FROM lexicon l
INNER JOIN inflectionclasses pc ON pc.id = l.inflectionclassid
WHERE pc.langid = {langid} AND l.isdeleted = FALSE");
          sb.AppendLine("LEMMA\tFORM\tTAGS\tGLOSS\tCLASS\tFORMULA\tAFFIX\tSTEM1\tSTEM2\tSTEM3\tSTEM4");
          foreach (var form in forms)
          {
            var l = lemmas.FirstOrDefault(x => x.id == form.lemmaid);
            var tags = "";
            if (!string.IsNullOrEmpty(form.ltags))
              tags += form.ltags;
            if (!string.IsNullOrEmpty(form.stags))
              tags += ";" + form.stags;
            if (!string.IsNullOrEmpty(form.atags))
              tags += ";" + form.atags;
            tags = tags.Trim(';');
            var line = $"{l.entry}\t{form.form}\t{_cacheService.UM_Sort(tags)}\t{l.meaning}\t{l.pcalss}\t{form.formula}\t{form.affix}\t";
            line += $"{l.stem1 ?? ""}\t{l.stem2 ?? ""}\t{l.stem3 ?? ""}\t{l.stem4 ?? ""}";
            sb.AppendLine(line);
          }
          break;
        // ==================================
        case "lexicon":
          var lexicon = connection.Query(@$"
SELECT l.entry AS lemma, pc.title AS classtitle, l.engmeaning AS meaning, l.stem1, l.stem2, l.stem3, l.stem4, l.unimorphtags
FROM lexicon l
INNER JOIN inflectionclasses pc ON pc.id = l.inflectionclassid
INNER JOIN langs ON langs.id = pc.langid
WHERE pc.langid = {langid} AND l.isdeleted is FALSE
ORDER BY l.entry");
          sb.AppendLine("LEMMA\tCLASS\tGLOSS\tSTEM1\tSTEM2\tSTEM3\tSTEM4\tTAGS");
          foreach (var record in lexicon)
          {
            var line = $"{record.lemma}\t{record.classtitle}\t{record.meaning}\t";
            line += $"{record.stem1 ?? ""}\t{record.stem2 ?? ""}\t{record.stem3 ?? ""}\t{record.stem4 ?? ""}\t{record.unimorphtags}";
            sb.AppendLine(line);
          }
          break;
          // ==================================
      }
      var bytes = Encoding.UTF8.GetBytes(sb.ToString());
      var lang = _context.langs.FirstOrDefault(x => x.id == langid).title;
      var fileName = lang + "_" + type + ".tsv";
      return File(bytes, "text/tab-separated-values", fileName);
    }
    
    public IActionResult Analyser()
    {
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