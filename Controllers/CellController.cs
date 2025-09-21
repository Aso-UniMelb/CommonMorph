using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using common_morph_backend;
using static common_morph_backend.AppDbContext;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
// using MySqlConnector;
// using Microsoft.Data.SqlClient;
using Npgsql;
using Dapper;

namespace common_morph_backend.Controllers
{
  [Route("[controller]")]
  public class CellController : Controller
  {

    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private string connectionString;
    public CellController(AppDbContext context, IConfiguration configuration)
    {
      _context = context;
      _configuration = configuration;
      // For the complex query used in this controller, we need Dapper to connect to DB
      connectionString =Environment.GetEnvironmentVariable("CONNECTION_STRING") ??  _configuration.GetConnectionString("DefaultConnection");
    }

    [HttpPost("batchApprove")]
    public IActionResult batchApprove([FromBody] List<CellRating> cells)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      foreach (var cell in cells)
      {
        if (_context.cellratings.Any(x => x.cellid == cell.id && x.userid == userId))
        {

        }
        else
        {
          cell.userid = userId;
          cell.rate = true;
          _context.cellratings.Add(cell);
        }
      }
      _context.SaveChanges();
      return Ok(cells.Count);
    }

    [HttpPost("batchInsert")]
    public IActionResult batchInsert([FromBody] List<Cell> cells)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      foreach (var cell in cells)
      {
        // update if already deleted 
        if (_context.cells.Any(x => x.lemmaid == cell.lemmaid && x.slotid == cell.slotid && x.agreementid == cell.agreementid && x.isdeleted == true))
        {
          var old = _context.cells.FirstOrDefault(x => x.lemmaid == cell.lemmaid && x.slotid == cell.slotid && x.isdeleted == true);
          if (old == null)
            return BadRequest("not exist");

          cell.datesubmitted = DateTime.UtcNow;
          cell.byuserid = userId;
          _context.Entry(old).State = EntityState.Detached;
          old.isdeleted = false;
          old.submitted = cell.submitted;
          old.datesubmitted = cell.datesubmitted;
          _context.cells.Update(old);
          _context.SaveChanges();
          return Ok(cell.id.ToString());
        }
        else
        {
          cell.datesubmitted = DateTime.UtcNow;
          cell.byuserid = userId;
          _context.cells.Add(cell);
        }
      }
      _context.SaveChanges();
      return Ok(cells.Count);
    }


    [HttpGet("getRatings")]
    public IActionResult getRatings(string Ids)
    {
      using var connection = new NpgsqlConnection(connectionString);
      var Ratingss = connection.Query(@$"
          SELECT cellid, rate, COUNT(Id) AS num
          FROM cellratings
          WHERE cellid IN ({Ids})
          GROUP BY rate, cellid ");
      return Ok(Ratingss);
    }

    [HttpGet("get")]
    public IActionResult get(int id)
    {
      return Ok(_context.cells.FirstOrDefault(x => x.id == id));
    }


    [Authorize]
    [HttpPost("insert")]
    public IActionResult insert(Cell cell, List<Suggestion> suggestions)
    {
      var userId = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);

      cell.datesubmitted = DateTime.UtcNow;
      cell.byuserid = userId;

      if (_context.cells.Any(x => x.lemmaid == cell.lemmaid && x.slotid == cell.slotid && x.isdeleted == true))
      {
        // update if already deleted 
        var old = _context.cells.FirstOrDefault(x => x.lemmaid == cell.lemmaid && x.slotid == cell.slotid && x.isdeleted == true);
        if (old == null)
          return BadRequest("not exist");

        _context.Entry(old).State = EntityState.Detached;
        old.isdeleted = false;
        old.submitted = cell.submitted;
        old.datesubmitted = cell.datesubmitted;
        _context.cells.Update(old);
        _context.SaveChanges();
        return Ok(cell.id.ToString());
      }
      _context.cells.Add(cell);
      _context.SaveChanges();

      var id = cell.id;
      // TODO: store the suggestions in the database
      foreach (var suggestion in suggestions)
      {
        var sug = new Suggestion()
        {
          cellid = id,
          source = suggestion.source,
          shots = suggestion.shots,
          datesubmitted = DateTime.UtcNow,
          suggested = suggestion.suggested
        };
        _context.suggestions.Add(sug);
      }
      _context.SaveChanges();

      // log
      var userLog = new UserLog()
      {
        log = $"Inserted cell {cell.id}",
        userid = userId,
        logdate = DateTime.UtcNow
      };
      _context.userlogs.Add(userLog);
      _context.SaveChanges();
      return Ok(id.ToString());
    }


    [Authorize]
    [HttpPost("approve")]
    public IActionResult approve(int cellid)
    {
      CellRating vt = new CellRating();
      vt.cellid = cellid;
      vt.rate = true;
      vt.userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      vt.daterated = DateTime.UtcNow;
      // check if already rated
      var old = _context.cellratings.FirstOrDefault(x => x.cellid == cellid && x.userid == vt.userid);
      if (old != null)
        return BadRequest("You have already rated this cell!");
      _context.cellratings.Add(vt);
      _context.SaveChanges();
      return Ok(vt.id.ToString());
    }

    [Authorize]
    [HttpPost("disapprove")]
    public IActionResult disapprove(int cellid)
    {
      CellRating vt = new CellRating();
      vt.cellid = cellid;
      vt.rate = false;
      vt.userid = Convert.ToInt32(User.FindFirst(ClaimTypes.NameIdentifier).Value);
      vt.daterated = DateTime.UtcNow;

      // check if already rated
      var old = _context.cellratings.FirstOrDefault(x => x.cellid == cellid && x.userid == vt.userid);
      if (old != null)
        return BadRequest("You have already rated this cell!");
      _context.cellratings.Add(vt);
      _context.SaveChanges();
      return Ok(vt.id.ToString());
    }

    [Authorize(Roles = "admin, linguist")]
    [HttpPost("delete")]
    public IActionResult delete(int id)
    {
      var cell = _context.cells.FirstOrDefault(x => x.id == id);
      if (cell == null)
        return BadRequest("not exist");
      _context.Entry(cell).State = EntityState.Detached;
      cell.isdeleted = true;
      _context.cells.Update(cell);
      _context.SaveChanges();
      return Ok(id.ToString());
    }

    [Authorize]
    [HttpPost("update")]
    public IActionResult update(Cell cell)
    {
      var old = _context.cells.FirstOrDefault(x => x.id == cell.id);
      if (old == null)
        return BadRequest("not exist");
      _context.Entry(old).State = EntityState.Detached;
      _context.cells.Update(cell);
      _context.SaveChanges();
      return Ok(cell.id.ToString());
    }

    [HttpPost("downloadUnimorph")]
    public IActionResult downloadUnimorph(int langid)
    {
      // using dapper
      using var connection = new NpgsqlConnection(connectionString);

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
      return Ok(result);
    }


    [HttpPost("downloadFull")]
    public IActionResult downloadFull(int langid)
    {
      // using dapper
      using var connection = new NpgsqlConnection(connectionString);

      var forms = connection.Query(@$"
SELECT 
	c.lemmaid AS lemma,
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

      // var slots = connection.Query(@$"
      // SELECT s.id, s.title, s.unimorphtags, s.formula, s.agreementgroupid, s.paradigmclassid
      // FROM slots s
      // INNER JOIN paradigmclasses pc ON pc.id = s.paradigmclassid
      // WHERE pc.langid ={langid}");

      // var agreements = connection.Query(@$"SELECT a.id, a.agreementgroupid, a.title, a.unimorphtags, a.realization
      // FROM agreements a
      // INNER JOIN agreementgroups ag ON ag.id = a.agreementgroupid
      // INNER JOIN paradigmclasses pc ON pc.id = ag.id
      // WHERE pc.langid ={langid}");

      return Ok(new { forms, lemmas });
    }
  }
}