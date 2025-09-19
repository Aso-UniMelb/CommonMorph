using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace common_morph_backend.Controllers
{
  // [ApiController]
  [Route("[controller]")]
  public class LLMController : Controller
  {

    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    // private readonly HttpClient _httpClient = new HttpClient();
    private readonly IConfiguration _configuration;
    public Dictionary<string, Provider> providedBy = new Dictionary<string, Provider>();

    public LLMController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
      _context = context;
      _httpClientFactory = httpClientFactory;
      _configuration = configuration;
      providedBy = new Dictionary<string, Provider>
      {
        { "OpenAI", new Provider { Url = "https://api.openai.com/v1/chat/completions", APIKey = _configuration["OpenAI"] } },
        { "Groq", new Provider { Url = "https://api.groq.com/openai/v1/chat/completions", APIKey = _configuration["Groq"] } },
        { "OpenRouter", new Provider { Url = "https://openrouter.ai/api/v1/chat/completions", APIKey = _configuration["OpenRouter"] } },
        { "GoogleAIStudio", new Provider { Url = "https://generativelanguage.googleapis.com/v1beta/models/", APIKey = _configuration["GoogleAIStudio"] } }
      };
    }

    public class Provider
    {
      public string Url { get; set; }
      public string APIKey { get; set; }
    }

    public class Sample
    {
      public string lemma { get; set; }
      public string stem1 { get; set; }
      public string stem2 { get; set; }
      public string stem3 { get; set; }
      public string form { get; set; }
    }

    [HttpPost("getQuestionFromLLM")]
    public async Task<IActionResult> getQuestionFromLLM(string prompt)
    {
      var results = await GetFromLLM(prompt, new Dictionary<string, Provider>
      {
        {"gemini-2.0-flash", providedBy["GoogleAIStudio"] },
        // {"gemini-2.5-flash-preview-04-17", providedBy["GoogleAIStudio"] }, // SLOW
        { "gpt-4.1-mini", providedBy["OpenAI"] },
        // { "gpt-4o-2024-11-20", providedBy["OpenAI"] }, //"chatgpt-4o-latest",
        { "meta-llama/llama-4-maverick-17b-128e-instruct", providedBy["Groq"] },
        // { "llama-3.3-70b-versatile", providedBy["Groq"] },
        { "deepseek-r1-distill-llama-70b", providedBy["Groq"] },
        // { "qwen-qwq-32b", providedBy["Groq"] }, //"qwen-2.5-32b",
        // { "gemma2-9b-it", providedBy["Groq"] },
        // { "google/gemini-2.5-pro-exp-03-25:free", providedBy["OpenRouter"] },
      });
      return Ok(results);
    }

    [HttpPost("getSuggestionFromLLM")]
    public async Task<IActionResult> getSuggestionFromLLM(Sample curLemma, List<Sample> samples)
    {
      // 0. check if the database already has the suggestions
      // 1. generate prompt
      string prompt = suggestionPrompt(curLemma, samples);
      // 2. get suggestions from LLMs
      var Suggestions = await GetFromLLM(prompt, new Dictionary<string, Provider>
      {
        { "gpt-4.1-mini", providedBy["OpenAI"] },
        { "llama-3.3-70b-versatile", providedBy["Groq"] },
      });
      // 3. return the suggestions to frontend
      return Ok(Suggestions);
    }

    private string suggestionPrompt(Sample curLemma, List<Sample> samples)
    {
      // This prompt does not need to be translated because it is not visible to the speakers.
      var prompt = new StringBuilder();
      prompt.Append(@$"In the language YYY, what is the correct inflected form of the lemma ""{curLemma.lemma}""");
      if (curLemma.stem1 != null)
      {
        prompt.Append(@$", given that its stem1 is ""{curLemma.stem1}""");
        if (curLemma.stem2 != null)
          prompt.Append(@$", and stem2 is ""{curLemma.stem2}""");
        if (curLemma.stem3 != null)
          prompt.Append(@$", and stem3 is ""{curLemma.stem3}""");
        prompt.Append(@$" for a specific grammatical feature set? As reference examples, under the same grammatical features:");
      }
      // samples for few-shot learning
      foreach (var sample in samples)
      {
        prompt.Append(@$"- The lemma {sample.lemma}");
        // if stems are provided
        if (sample.stem1 != null)
        {
          prompt.Append(@$"(with stem1: ""{sample.stem1}""");
          if (sample.stem2 != null)
            prompt.Append(@$", stem2: ""{sample.stem2}""");
          if (sample.stem3 != null)
            prompt.Append(@$", stem3: ""{sample.stem3}"")");
          prompt.Append(")");
        }
        // inflected form:
        prompt.Append(@$" yields the form ""{sample.form}"". ");
        // finishing the prompt with the following instructions
        prompt.Append("Do not explain anything to me. Just give me one final inflected word.");

      }
      return prompt.ToString();
    }

    private async Task<Dictionary<string, string>> GetFromLLM(string prompt, Dictionary<string, Provider> models)
    {
      var tasks = models.Select(async model =>
      {
        var httpClient = _httpClientFactory.CreateClient();
        var httpContent = new StringContent("");
        string url = "";

        if (model.Key.StartsWith("gemini"))
        {
          var requestBody = new
          {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.15 }
          };
          httpContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
          // "https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key=" + apiKey;
          url = model.Value.Url + model.Key + ":generateContent?key=" + model.Value.APIKey;
        }
        else
        {
          var body = new
          {
            model = model.Key,
            messages = new[] { new { role = "user", content = prompt } }
          };
          httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.Value.APIKey}");
          httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
          url = model.Value.Url;
        }
        try
        {
          var response = await httpClient.PostAsync(url, httpContent);
          var responseContent = await response.Content.ReadAsStringAsync();
          if (!response.IsSuccessStatusCode)
            return new KeyValuePair<string, string>(model.Key, "Error" + StatusCode((int)response.StatusCode, responseContent));
          using var doc = JsonDocument.Parse(responseContent);
          var reply = "";
          if (model.Key.StartsWith("gemini"))
          {
            reply = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
          }
          else
          {
            reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            reply = Regex.Replace(reply, @"<think>.+?</think>", "", RegexOptions.Singleline);
          }
          return new KeyValuePair<string, string>(model.Key, reply.Trim());
        }
        catch (Exception ex)
        {
          return new KeyValuePair<string, string>(model.Key, "Error: " + ex.Message);
        }
      });
      var results = await Task.WhenAll(tasks);
      return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    // [HttpPost("getQuestionFromGemini")]
    // public async Task<string> getQuestionFromGemini(string prompt)
    // {
    //   string apiKey = _configuration["GoogleAIStudio"];
    //   string apiUrl = "https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key=" + apiKey;
    //   using (HttpClient client = new HttpClient())
    //   {
    //     var requestBody = new
    //     {
    //       contents = new[] { new { parts = new[] { new { text = prompt } } } }
    //     };

    //     var jsonContent = JsonSerializer.Serialize(requestBody);
    //     var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    //     var response = await client.PostAsync(apiUrl, httpContent);
    //     return await response.Content.ReadAsStringAsync();
    //   }
    // }
  }
}