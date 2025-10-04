using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.Text;

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
      using var connection = new NpgsqlConnection(connectionString);
      // list all languages that have lexical data
      var lexiconStats = connection.Query(@$"
SELECT langs.id AS langid, langs.title, langs.code, COUNT(*) AS cnt
FROM lemmas l
INNER JOIN paradigmclasses pc ON pc.id = l.paradigmclassid
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
	(SELECT entry FROM lemmas WHERE id = c.lemmaid) AS lemma,
	c.submitted AS form,
	(SELECT unimorphtags FROM slots WHERE id = c.slotid) AS stags,
	(SELECT unimorphtags FROM agreements WHERE id = c.agreementid) AS atags,
  COUNT(r.cellid) AS trueratingscount
FROM cells c
LEFT JOIN cellratings r ON c.id = r.cellid
WHERE c.langid = {langid}
GROUP BY c.id, c.agreementid, c.slotid, c.lemmaid, c.submitted, r.rate
ORDER BY r.rate, c.lemmaid, c.slotid, c.agreementid");
          foreach (var record in result)
          {
            var tags = record.stags;
            if (!string.IsNullOrEmpty(record.atags))
              tags += ";" + record.atags;
            sb.AppendLine($"{record.lemma}\t{record.form}\t{tags}"); // TODO: UM_sort(tags)
          }
          break;
        // ==================================
        case "extended":
          var forms = connection.Query(@$"
SELECT 
	c.lemmaid AS lemmaid,
	c.submitted AS form,
	(SELECT unimorphtags FROM slots WHERE id = c.slotid) AS stags,
	(SELECT unimorphtags FROM agreements WHERE id = c.agreementid) AS atags,
  COUNT(r.cellid) AS trueratingscount
FROM cells c
LEFT JOIN cellratings r ON c.id = r.cellid
WHERE c.langid = {langid}
GROUP BY c.id, c.agreementid, c.slotid, c.lemmaid, c.submitted, r.rate
ORDER BY r.rate, c.lemmaid, c.slotid, c.agreementid");

          var lemmas = connection.Query(@$"
SELECT l.id, l.entry, pc.title AS pcalss, l.engmeaning AS meaning, l.stem1, l.stem2, l.stem3, l.stem4
FROM lemmas l
INNER JOIN paradigmclasses pc ON pc.id = l.paradigmclassid
WHERE pc.langid = {langid} AND l.isdeleted = FALSE");
          sb.AppendLine("Lemma\tForm\tTags\tClass\tStem1\tStem2\tStem3\tStem4");
          foreach (var form in forms)
          {
            var l = lemmas.FirstOrDefault(x => x.id == form.lemmaid);
            var tags = form.stags;
            if (!string.IsNullOrEmpty(form.atags))
              tags += ";" + form.atags;
            var line = $"{l.entry}\t{form.form}\t{tags}\t{l.pcalss}\t";
            line += $"{l.stem1 ?? ""}\t{l.stem2 ?? ""}\t{l.stem3 ?? ""}\t{l.stem4 ?? ""}";
            sb.AppendLine(line);
          }
          break;
        // ==================================
        case "lexicon":
          var lexicon = connection.Query(@$"
SELECT l.entry AS lemma, pc.title AS classtitle, l.engmeaning AS meaning, l.stem1, l.stem2, l.stem3, l.stem4
FROM lemmas l
INNER JOIN paradigmclasses pc ON pc.id = l.paradigmclassid
INNER JOIN langs ON langs.id = pc.langid
WHERE pc.langid = {langid} AND l.isdeleted is FALSE
ORDER BY l.entry");
          sb.AppendLine("Lemma\tClass\tGloss\tStem1\tStem2\tStem3\tStem4");
          foreach (var record in lexicon)
          {
            var line = $"{record.lemma}\t{record.classtitle}\t{record.meaning}\t";
            line += $"{record.stem1 ?? ""}\t{record.stem2 ?? ""}\t{record.stem3 ?? ""}\t{record.stem4 ?? ""}";
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