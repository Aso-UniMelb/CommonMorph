using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace common_morph_backend.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class LLMController : ControllerBase
  {
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    public Dictionary<string, Provider> providedBy = new Dictionary<string, Provider>();

    public LLMController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
      _context = context;
      _httpClientFactory = httpClientFactory;
      _configuration = configuration;
      providedBy = new Dictionary<string, Provider>
      {
        { "OpenAI", new Provider {
            Url = "https://api.openai.com/v1/chat/completions",
            APIKey = Environment.GetEnvironmentVariable("OpenAI") ?? _configuration["OpenAI"] ?? ""
           }
        },
        { "Groq", new Provider {
            Url = "https://api.groq.com/openai/v1/chat/completions",
            APIKey = Environment.GetEnvironmentVariable("Groq") ?? _configuration["Groq"] ?? ""
          }
        },
        { "OpenRouter", new Provider {
            Url = "https://openrouter.ai/api/v1/chat/completions",
            APIKey = Environment.GetEnvironmentVariable("OpenRouter") ?? _configuration["OpenRouter"] ?? ""
          }
        },
        { "GoogleAIStudio", new Provider {
            Url = "https://generativelanguage.googleapis.com/v1beta/models/",
            APIKey = Environment.GetEnvironmentVariable("GoogleAIStudio") ?? _configuration["GoogleAIStudio"] ?? ""
          }
        }
      };
    }

    public class Provider
    {
      public string Url { get; set; } = "";
      public string APIKey { get; set; } = "";
    }

    public class Sample
    {
      public string? lemma { get; set; }
      public string? stem1 { get; set; }
      public string? stem2 { get; set; }
      public string? stem3 { get; set; }
      public string? form { get; set; }
    }

    public class QuestionPromptRequest
    {
      public string prompt { get; set; } = "";
    }

    public class SuggestionRequest
    {
      public Sample? curLemma { get; set; }
      public List<Sample>? samples { get; set; }
      public string? lang { get; set; }
    }

    [HttpPost("getQuestionFromLLM")]
    public async Task<IActionResult> getQuestionFromLLM([FromBody] QuestionPromptRequest request)
    {
      if (string.IsNullOrWhiteSpace(request?.prompt))
      {
        return BadRequest(new { error = "Prompt is required" });
      }

      var modelsToQuery = new Dictionary<string, Provider>();

      // Google AI Studio
      if (!string.IsNullOrWhiteSpace(providedBy["GoogleAIStudio"].APIKey))
      {
        modelsToQuery["gemini-flash-lite-latest"] = providedBy["GoogleAIStudio"];
        modelsToQuery["gemini-flash-latest"] = providedBy["GoogleAIStudio"];
      }

      // Groq
      if (!string.IsNullOrWhiteSpace(providedBy["Groq"].APIKey))
      {
        modelsToQuery["groq/compound-mini"] = providedBy["Groq"];
        modelsToQuery["groq/compound"] = providedBy["Groq"];
      }

      // OpenRouter
      if (!string.IsNullOrWhiteSpace(providedBy["OpenRouter"].APIKey))
      {
        modelsToQuery["meta-llama/llama-3.3-70b-instruct"] = providedBy["OpenRouter"];
      }

      // OpenAI
      if (!string.IsNullOrWhiteSpace(providedBy["OpenAI"].APIKey))
      {
        modelsToQuery["gpt-4o-mini"] = providedBy["OpenAI"];
      }

      var results = await GetFromLLM(request.prompt, modelsToQuery);
      return Ok(results);
    }

    [HttpPost("getSuggestionFromLLM")]
    public async Task<IActionResult> getSuggestionFromLLM([FromBody] SuggestionRequest request)
    {
      if (request?.curLemma == null || string.IsNullOrWhiteSpace(request.curLemma.lemma))
      {
        return Ok(new Dictionary<string, string>());
      }

      string prompt = suggestionPrompt(request.curLemma, request.samples ?? new List<Sample>(), request.lang);
      var modelsToQuery = new Dictionary<string, Provider>();

      // Google AI Studio (Fast, robust)
      if (!string.IsNullOrWhiteSpace(providedBy["GoogleAIStudio"].APIKey))
      {
        modelsToQuery["gemini-flash-lite-latest"] = providedBy["GoogleAIStudio"];
      }

      // Groq
      if (!string.IsNullOrWhiteSpace(providedBy["Groq"].APIKey))
      {
        modelsToQuery["groq/compound-mini"] = providedBy["Groq"];
      }

      // OpenRouter
      if (!string.IsNullOrWhiteSpace(providedBy["OpenRouter"].APIKey))
      {
        modelsToQuery["meta-llama/llama-3.3-70b-instruct"] = providedBy["OpenRouter"];
      }

      // OpenAI
      if (!string.IsNullOrWhiteSpace(providedBy["OpenAI"].APIKey))
      {
        modelsToQuery["gpt-4o-mini"] = providedBy["OpenAI"];
      }

      var suggestions = await GetFromLLM(prompt, modelsToQuery);
      return Ok(suggestions);
    }

    private string suggestionPrompt(Sample curLemma, List<Sample> samples, string? langName = null)
    {
      var lang = !string.IsNullOrWhiteSpace(langName) ? langName : "the target language";
      var prompt = new StringBuilder();
      prompt.Append($"In the language {lang}, what is the correct inflected word form of the lemma \"{curLemma.lemma}\"");
      
      if (!string.IsNullOrWhiteSpace(curLemma.stem1))
      {
        prompt.Append($", given stem1 is \"{curLemma.stem1}\"");
        if (!string.IsNullOrWhiteSpace(curLemma.stem2))
          prompt.Append($", stem2 is \"{curLemma.stem2}\"");
        if (!string.IsNullOrWhiteSpace(curLemma.stem3))
          prompt.Append($", and stem3 is \"{curLemma.stem3}\"");
      }
      prompt.Append(" for the requested grammatical feature set?");

      if (samples != null && samples.Any())
      {
        prompt.Append(" Here are reference examples from the same variety under the exact same grammatical features:\n");
        foreach (var sample in samples)
        {
          if (string.IsNullOrWhiteSpace(sample.lemma) || string.IsNullOrWhiteSpace(sample.form)) continue;
          prompt.Append($"- Lemma \"{sample.lemma}\"");
          if (!string.IsNullOrWhiteSpace(sample.stem1))
          {
            prompt.Append($" (stem1: \"{sample.stem1}\"");
            if (!string.IsNullOrWhiteSpace(sample.stem2)) prompt.Append($", stem2: \"{sample.stem2}\"");
            if (!string.IsNullOrWhiteSpace(sample.stem3)) prompt.Append($", stem3: \"{sample.stem3}\"");
            prompt.Append(")");
          }
          prompt.Append($" -> Inflected form: \"{sample.form}\"\n");
        }
      }

      prompt.Append("\nDo not provide any explanation, markdown, or punctuation. Output ONLY the single final inflected word form.");
      return prompt.ToString();
    }

    private async Task<Dictionary<string, string>> GetFromLLM(string prompt, Dictionary<string, Provider> models)
    {
      var validModels = models.Where(m => m.Value != null && !string.IsNullOrWhiteSpace(m.Value.APIKey)).ToList();
      if (!validModels.Any())
      {
        return new Dictionary<string, string>();
      }

      var tasks = validModels.Select(async model =>
      {
        try
        {
          var httpClient = _httpClientFactory.CreateClient();
          httpClient.Timeout = TimeSpan.FromSeconds(15);
          HttpContent httpContent;
          string url;

          if (model.Key.StartsWith("gemini"))
          {
            var requestBody = new
            {
              contents = new[] { new { parts = new[] { new { text = prompt } } } },
              generationConfig = new { temperature = 0.15, maxOutputTokens = 256 }
            };
            httpContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            url = $"{model.Value.Url}{model.Key}:generateContent?key={model.Value.APIKey}";
          }
          else
          {
            var body = new
            {
              model = model.Key,
              messages = new[] { new { role = "user", content = prompt } },
              temperature = 0.15,
              max_tokens = 256
            };
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", model.Value.APIKey);
            httpContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            url = model.Value.Url;
          }

          var response = await httpClient.PostAsync(url, httpContent);
          var responseContent = await response.Content.ReadAsStringAsync();
          if (!response.IsSuccessStatusCode)
          {
            return new KeyValuePair<string, string>(model.Key, "");
          }

          using var doc = JsonDocument.Parse(responseContent);
          var reply = "";

          if (model.Key.StartsWith("gemini"))
          {
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && 
                candidates.GetArrayLength() > 0 && 
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textProp))
            {
              reply = textProp.GetString() ?? "";
            }
          }
          else
          {
            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var contentProp))
            {
              reply = contentProp.GetString() ?? "";
              reply = Regex.Replace(reply, @"<think>.*?</think>", "", RegexOptions.Singleline);
            }
          }

          reply = reply.Trim().Trim('"', '\'', '`', '\n', '\r');
          return new KeyValuePair<string, string>(model.Key, reply);
        }
        catch
        {
          return new KeyValuePair<string, string>(model.Key, "");
        }
      });

      var results = await Task.WhenAll(tasks);
      return results
        .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
  }
}