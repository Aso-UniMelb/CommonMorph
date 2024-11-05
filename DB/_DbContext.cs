using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommonMorphAPI.Model
{
  public class _DbContext : DbContext
  {
    public DbSet<Dialect> Dialects { get; set; }
    public DbSet<Lemma> Lemmas { get; set; }
    public DbSet<WordClass> WordClasses { get; set; }
    public DbSet<Slot> Slots { get; set; }
    public DbSet<Form> Forms { get; set; }
    public DbSet<Vote> Votes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlServer(_M.ConStr);
    }

    public class Dialect
    {
      [Key]
      public int? Id { get; set; }
      [Required]
      [StringLength(3)]
      public string Code { get; set; }
      [Required]
      [StringLength(50)]
      public string Title { get; set; }
      public string? Description { get; set; }
      public string? KeyboardLayout { get; set; }
      public bool isDeleted { get; set; }
      public ICollection<WordClass>? WordClasses { get; set; }
    }

    public class WordClass
    {
      [Key]
      public int? Id { get; set; }
      [Required]
      [StringLength(25)]
      public string Title { get; set; }
      public string? Description { get; set; }
      public bool isDeleted { get; set; }
      // Foreign key to the Dialect table
      [ForeignKey("Dialect")]
      public int DialectID { get; set; }
      public Dialect? Dialect { get; set; } 
      // public ICollection<Slot>? Slots { get; set; }
    }

    public class Slot
    {
      [Key]
      public int? Id { get; set; }
      public string? UniMorphTags { get; set; }
      public string? Formula { get; set; }
      public Priority? priority { get; set; }
      public bool isDeleted { get; set; }
      // Foreign key to the WordClass table
      [ForeignKey("WordClass")]
      public int WordClassID { get; set; }
      // public WordClass? WordClass { get; set; }
    }

    public class Lemma
    {
      [Key]
      public int? Id { get; set; }
      [Required]
      [StringLength(25)]
      public string Entry { get; set; }
      public string? Stem1 { get; set; }
      public string? Stem2 { get; set; }
      public string? Stem3 { get; set; }
      [Required]
      public int WordClassID { get; set; }
      [Required]
      public string? EngMeaning { get; set; }
      public string? Description { get; set; }
      public Priority? priority { get; set; }
      public bool isDeleted { get; set; }
    }

    public class Form
    {
      [Key]
      public int? Id { get; set; }
      public int UserId { get; set; }
      [Required]
      public int? LemmaId { get; set; }
      [Required]
      public int? SlotId { get; set; }
      public string? Suggested { get; set; }
      public SuggestionSource Source { get; set; }
      [Required]
      [StringLength(50)]
      public string? Word { get; set; }
      [Column(TypeName = "smalldatetime")]
      public DateTime? DateAdded { get; set; }
      public bool isDeleted { get; set; }
    }

    public class Vote
    {
      [Key]
      public int? Id { get; set; }
      [Required]
      public int FormId { get; set; }      
      public VoteType type { get; set; }
      [Column(TypeName = "smalldatetime")]
      public DateTime? DateVoted { get; set; }
      public int UserId { get; set; }
    }

    public enum VoteType{
      Yes,
      No
    }

    public enum Priority{
      Low,
      Medium, 
      High
    }

    public enum SuggestionSource{
      Formula,
      LemmaCopy,
      NN
    }

    public class User
    {
      [Key]
      public int? id { get; set; }
      [Required]
      [StringLength(50)]
      public string? Username { get; set; }
      [Required]
      public byte[] passwordHash { get; set; }
      [Required]
      [StringLength(50)]
      public string? Name { get; set; }
      public userRole role { get; set; }

    }
    public enum userRole
    {
      admin,
      editor
    }
  }
}