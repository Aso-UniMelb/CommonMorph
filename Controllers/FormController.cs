using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using CommonMorphAPI.Model;
using static CommonMorphAPI.Model._DbContext;
using Microsoft.Data.SqlClient;
using Dapper;

namespace CommonMorphAPI.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class FormController : Controller
  {
    [HttpGet("list")]
    public IActionResult list(int dialectID)
    {
      // using dapper
      using var connection = new SqlConnection(_M.ConStr);
      var result = connection.Query(@$"
      SELECT lemmaId = L.Id, lemma = L.Entry, S1 = L.Stem1, S2 = L.Stem2, formula = S.Formula, tags = S.UniMorphTags, word = F.Word, slotId = S.Id 
      FROM Lemmas L 
      INNER JOIN WordClasses W ON W.Id = L.WordClassID
      INNER JOIN Slots S ON S.WordClassID = L.WordClassID
      LEFT  JOIN Forms F ON F.LemmaId = L.Id AND F.SlotId = S.Id
      WHERE DialectID = {dialectID}
      ORDER BY L.priority DESC, L.Entry, S.priority DESC, S.UniMorphTags");
      return Ok(result);
    }


    [HttpGet("get")]
    public IActionResult get(int id)
    {
      using (var context = new _DbContext())
      {
        return Ok(context.Forms.FirstOrDefault(x => x.Id == id));
      }
    }

    [HttpPost("insert")]
    public IActionResult insert([FromForm] Form form)
    {
      form.DateAdded = DateTime.Now;
      using (var context = new _DbContext())
      {
        context.Forms.Add(form);
        context.SaveChanges();
        var id = form.Id;
        return Ok(id.ToString());
      }
    }

    [HttpPost("update")]
    public IActionResult update([FromForm] Form form)
    {
      using (var context = new _DbContext())
      {
        var old = context.Forms.FirstOrDefault(x => x.Id == form.Id);
        if (old == null)
          return BadRequest("not exist");
        context.Entry(old).State = EntityState.Detached;
        context.Forms.Update(form);
        context.SaveChanges();
        return Ok(form.Id.ToString());
      }
    }
  }
}