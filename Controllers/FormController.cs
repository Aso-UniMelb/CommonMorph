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
    [HttpGet("listForEntery")]
    public IActionResult listForEntery(int dialectID, int page = 1)
    {
      // using dapper
      using var connection = new SqlConnection(_M.ConStr);
      var result = connection.Query(@$"
      SELECT lemmaId = L.Id, lemma = L.Entry, slotId = S.Id, agreementId = A.Id,
        S1 = L.Stem1, S2 = L.Stem2, S3 = L.Stem3, A = A.Formula,
        Eng= L.EngMeaning, formula = S.Formula, tags = S.UniMorphTags + ';' + A.UniMorphTags
      FROM Lemmas L 
      INNER JOIN ParadigmClasses P ON P.Id = L.ParadigmClassID
      INNER JOIN Slots S ON S.ParadigmClassID = L.ParadigmClassID
			INNER JOIN AgreementGroups AG ON AG.Id = S.AgreementGroupID
			INNER JOIN Agreements A ON A.AgreementGroupID = AG.Id
      LEFT  JOIN Forms F ON F.LemmaId = L.Id AND F.SlotId = S.Id AND F.AgreementID = A.Ids
      WHERE P.DialectID = {dialectID} AND (F.Word IS NULL OR F.isDeleted = 1)
      ORDER BY L.priority DESC, L.Entry, S.priority DESC, S.UniMorphTags
      OFFSET 20*({page}-1) ROWS FETCH NEXT 20 ROWS ONLY");
      return Ok(result);
    }

    [HttpGet("listForCheck")]
    public IActionResult listForCheck(int dialectID, int page = 1)
    {
      // using dapper
      using var connection = new SqlConnection(_M.ConStr);
      var result = connection.Query(@$"
      SELECT id = F.Id , lemma = L.Entry, word = F.Word,
				tags = S.UniMorphTags + ';' + (SELECT UniMorphTags FROM Agreements WHERE Id = F.AgreementID)
      FROM Lemmas L 
      INNER JOIN ParadigmClasses P ON P.Id = L.ParadigmClassID
      INNER JOIN Slots S ON S.ParadigmClassID = L.ParadigmClassID
      LEFT  JOIN Forms F ON F.LemmaId = L.Id AND F.SlotId = S.Id
      WHERE P.DialectID = {dialectID} AND (F.Word IS NOT NULL AND F.isDeleted=0)
      ORDER BY L.priority DESC, L.Entry, S.priority DESC, S.UniMorphTags
      OFFSET 20*({page}-1) ROWS FETCH NEXT 20 ROWS ONLY");
      return Ok(result);
    }

    [HttpGet("getVotes")]
    public IActionResult getVotes(string Ids)
    {
      using var connection = new SqlConnection(_M.ConStr);
      var Votes = connection.Query(@$"SELECT FormId, type, num=Count(Id) FROM Votes WHERE FormId IN ({Ids}) GROUP BY type, FormId");
      return Ok(Votes);
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
        if (context.Forms.Any(x => x.LemmaId == form.LemmaId && x.SlotId == form.SlotId && x.isDeleted == true))
        {
          // update if already deleted 
          var old = context.Forms.FirstOrDefault(x => x.LemmaId == form.LemmaId && x.SlotId == form.SlotId && x.isDeleted == true);
          if (old == null)
            return BadRequest("not exist");
          context.Entry(old).State = EntityState.Detached;
          old.isDeleted = false;
          old.Word = form.Word;
          old.DateAdded = form.DateAdded;
          context.Forms.Update(old);
          context.SaveChanges();
          return Ok(form.Id.ToString());
        }
        context.Forms.Add(form);
        context.SaveChanges();
        var id = form.Id;
        return Ok(id.ToString());
      }
    }

    [HttpGet("VoteYes")]
    public IActionResult VoteYes(int formId)
    {
      Vote vt = new Vote();
      vt.FormId = formId;
      vt.type = VoteType.Yes;
      vt.UserId = 1;
      vt.DateVoted = DateTime.Now;
      using (var context = new _DbContext())
      {
        context.Votes.Add(vt);
        context.SaveChanges();
        return Ok(vt.Id.ToString());
      }
    }

    [HttpGet("VoteNo")]
    public IActionResult VoteNo(int formId)
    {
      Vote vt = new Vote();
      vt.FormId = formId;
      vt.type = VoteType.No;
      vt.UserId = 1;
      vt.DateVoted = DateTime.Now;
      using (var context = new _DbContext())
      {
        context.Votes.Add(vt);
        context.SaveChanges();
        return Ok(vt.Id.ToString());
      }
    }

    [HttpGet("delete")]
    public IActionResult delete(int id)
    {
      using (var context = new _DbContext())
      {
        var form = context.Forms.FirstOrDefault(x => x.Id == id);
        if (form == null)
          return BadRequest("not exist");
        context.Entry(form).State = EntityState.Detached;
        form.isDeleted = true;
        context.Forms.Update(form);
        context.SaveChanges();
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