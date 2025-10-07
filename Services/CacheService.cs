using System.Text.RegularExpressions;
using System.Text.Json;

namespace common_morph_backend
{
  public class UMFeature
  {
    public string l { get; set; } // label
    public string f { get; set; } // feature
    public string d { get; set; } // dimension
  }

  public interface IMyCacheService
  {
    IReadOnlyList<UMFeature> UMFeatures { get; }
    string UM_Sort(string strTags);
  }

  public class MyCacheService : IMyCacheService
  {
    private readonly List<UMFeature> _umList;

    public MyCacheService(IWebHostEnvironment env)
    {
      var path = Path.Combine(env.ContentRootPath, "Services/UM.json");
      _umList = LoadUIMessages(path);
    }

    private List<UMFeature> LoadUIMessages(string filePath)
    {
      // read from json file
      var json = System.IO.File.ReadAllText(filePath);
      var um_data = JsonSerializer.Deserialize<List<UMFeature>>(json);
      var UM_list = new List<UMFeature>();
      foreach (var entry in um_data)
      {
        UM_list.Add(new UMFeature
        {
          l = entry.l,
          d = entry.d,
          f = entry.f,
        });
      }
      return UM_list;
    }

    public IReadOnlyList<UMFeature> UIMessages => _umList;

    public IReadOnlyList<UMFeature> UMFeatures => throw new NotImplementedException();

    public string UM_Sort(string strTags)
    {
      var tags = strTags.Split(new[] { ';', '+' }, StringSplitOptions.RemoveEmptyEntries).ToList();
      // Sort based on index in UM 
      tags.Sort((a, b) =>
      {
        int indexA = _umList.FindIndex(item => item.l == a);
        int indexB = _umList.FindIndex(item => item.l == b);
        return indexA.CompareTo(indexB);
      });
      string bundle = "";
      if (tags.Count > 0)
      {
        for (int i = 0; i < tags.Count - 1; i++)
        {
          var dim_i = _umList.First(item => item.l == tags[i]).d;
          var dim_next = _umList.First(item => item.l == tags[i + 1]).d;

          if (dim_i != dim_next)
            bundle += tags[i] + ";";
          else
            bundle += tags[i] + "+";
        }
        // Add the last element
        bundle += tags[tags.Count - 1];
      }
      // Remove trailing semicolons
      return bundle.TrimEnd(';');
    }
  }
}



