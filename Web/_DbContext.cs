using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace common_morph_backend
{
  public class AppDbContext : DbContext
  {
    public DbSet<User> users { get; set; }
    public DbSet<Lang> langs { get; set; }
    public DbSet<UsersLang> userslangs { get; set; }
    public DbSet<UserLog> userlogs { get; set; }
    public DbSet<ParadigmClass> paradigmclasses { get; set; }
    public DbSet<AgreementGroup> agreementgroups { get; set; }
    public DbSet<Agreement> agreements { get; set; }
    public DbSet<Lemma> lemmas { get; set; }
    public DbSet<Slot> slots { get; set; }
    public DbSet<Cell> cells { get; set; }
    public DbSet<Suggestion> suggestions { get; set; }
    public DbSet<CellRating> cellratings { get; set; }
    public DbSet<QuestionTemplate> questions { get; set; }
    public DbSet<QTemplate> qtemplates { get; set; }
    public DbSet<Survey> surveys { get; set; }

    // ======================================================================
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    // ======================================================================
    public class User
    {
      [Key]
      public int id { get; set; }
      [StringLength(50)]
      public string username { get; set; }
      public string? passwordhash { get; set; }
      public string? passwordsalt { get; set; }
      [StringLength(50)]
      public string? name { get; set; }
      public UserRole role { get; set; }
      public UserRole? desiredrole { get; set; }
      public string? registrationcode { get; set; }
    }

    public enum UserRole
    {
      admin,
      linguist,
      speaker,
      viewer,
      pending
    }

    public class Lang
    {
      [Key]
      public int id { get; set; }
      [Required]
      [StringLength(6)]
      public string code { get; set; }
      [Required]
      [StringLength(50)]
      public string title { get; set; }
      public string? description { get; set; }
      public float? latitude { get; set; }
      public float? longitude { get; set; }
      [StringLength(100)]
      public string validchars { get; set; }
      public bool isdeleted { get; set; }
    }

    public class UsersLang
    {
      [Key]
      public int id { get; set; }
      public int userid { get; set; }
      public int langid { get; set; }
    }

    // userLog
    public class UserLog
    {
      [Key]
      public int id { get; set; }
      public int userid { get; set; }
      public string log { get; set; }
      public DateTime logdate { get; set; }
    }

    public class ParadigmClass // e.g. TransitiveVerbs
    {
      [Key]
      public int id { get; set; }
      public int langid { get; set; }
      [Required]
      [StringLength(25)]
      public string title { get; set; }
      public string? description { get; set; }
      public bool isdeleted { get; set; }
    }

    public class AgreementGroup // e.g. PresentVerbAgreements
    {
      [Key]
      public int id { get; set; }
      public string title { get; set; }
      public int langid { get; set; }
    }

    public class Agreement // e.g. -s (for 3;SG)
    {
      [Key]
      public int id { get; set; }
      public int agreementgroupid { get; set; }
      public string title { get; set; }
      public string unimorphtags { get; set; }
      public string realization { get; set; }
      public Priority? priority { get; set; }
      public bool isdeleted { get; set; }
    }

    public enum Priority
    {
      Low,
      Medium,
      High
    }

    public class Lemma // e.g. 'bring'
    {
      [Key]
      public int id { get; set; }
      [StringLength(25)]
      public string entry { get; set; }
      public int paradigmclassid { get; set; }
      public string? stem1 { get; set; }
      public string? stem2 { get; set; }
      public string? stem3 { get; set; }
      public string? stem4 { get; set; }
      public string? engmeaning { get; set; }
      public string? description { get; set; }
      public Priority priority { get; set; }
      public bool isdeleted { get; set; }
    }

    public class Slot // Paradigm Slots, e.g. 'V;PST' with formula 'S+ed'
    {
      [Key]
      public int id { get; set; }
      public string? title { get; set; }
      public string? unimorphtags { get; set; }
      public string? formula { get; set; }
      public Priority priority { get; set; }
      public int paradigmclassid { get; set; }
      public int agreementgroupid { get; set; }
      public bool isdeleted { get; set; }
    }

    public class Cell // inflected form, each cell in a paradigm, e.g. 'brings' 
    {
      [Key]
      public int id { get; set; }
      public int langid { get; set; }
      public int lemmaid { get; set; }
      public int slotid { get; set; }
      public int agreementid { get; set; }
      public string submitted { get; set; }
      public int byuserid { get; set; } // userId should be checked. the user submitted it should not rate the same cell
      public DateTime datesubmitted { get; set; }
      public bool isdeleted { get; set; }
    }


    public class Suggestion
    {
      [Key]
      public int id { get; set; }
      public int cellid { get; set; }
      public string suggested { get; set; }
      public string source { get; set; }
      public int shots { get; set; }
      public DateTime datesubmitted { get; set; }
    }

    public class CellRating // rating of a cell, if rated OK by two users then it is verified
    {
      [Key]
      public int id { get; set; }
      public int cellid { get; set; }
      public bool rate { get; set; }
      public int userid { get; set; }
      public DateTime daterated { get; set; }
    }
    public class QuestionTemplate
    {
      [Key]
      public int id { get; set; }
      public string questionlang { get; set; }
      public string unimorphtags { get; set; }
      public string question { get; set; }
      public int userid { get; set; }
      public bool isdeleted { get; set; }
    }

    public class QTemplate // for storing the elicitation question templates
    {
      [Key]
      public int id { get; set; }
      public string questionlang { get; set; }
      public string unimorphtags { get; set; }
      public string question { get; set; }
      // public int SlotId { get; set; }
      // public int AgreementID { get; set; }
      public string llm { get; set; }
      public string prompt { get; set; }
      public DateTime dategenerated { get; set; }
      public TemplateFeedbackResponse? response { get; set; }
      public int userid { get; set; }
      public bool isdeleted { get; set; }
    }
    public enum TemplateFeedbackResponse
    {
      Correct,
      WrongLanguage,
      NotUnderstandable,
      NotPlainLanguage,
      NotAllFeaturesCovered,
      Other1,
      Other2,
    }

    public class Survey
    {
      [Key]
      public int id { get; set; }
      public int userid { get; set; }
      public int langid { get; set; }
      public UserRole role { get; set; }
      public DateTime surveydate { get; set; }
      public string? feedback { get; set; }
      public string? q1 { get; set; }
      public string? q2 { get; set; }
      public string? q3 { get; set; }
      public string? q4 { get; set; }
      public string? q5 { get; set; }
      public string? q6 { get; set; }
      public string? q7 { get; set; }
      public string? q8 { get; set; }
      public string? q9 { get; set; }
    }

  }
}