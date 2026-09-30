using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace BookShoppingCartMvcUI.Ai;

// calls the gemini generateContent REST endpoint.
// docs: https://ai.google.dev/api/generate-content
public class GeminiAiService : IAiService
{
    public const string BaseAddress = "https://generativelanguage.googleapis.com/";

    // gemini json is camelCase, and we leave out nulls (the system instruction has no role)
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiAiService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var body = new GeminiRequest(
            new GeminiContent(null, [new GeminiPart(systemPrompt)]),
            [new GeminiContent("user", [new GeminiPart(userPrompt)])],
            new GeminiGenerationConfig(
                _options.Temperature,
                _options.MaxOutputTokens,
                string.IsNullOrWhiteSpace(_options.ThinkingLevel) ? null : new GeminiThinkingConfig(_options.ThinkingLevel)));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent")
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        // key goes in a header, not ?key= in the url, because urls end up in logs
        request.Headers.Add("x-goog-api-key", _options.ApiKey);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 400 bad request, 401/403 bad key, 429 free tier limit hit, 5xx google is down
                _logger.LogWarning("Gemini returned {StatusCode}", (int)response.StatusCode);
                throw new AiServiceException($"Gemini returned status {(int)response.StatusCode}.");
            }

            var reply = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
            var text = ReadText(reply);

            // this is what google bills for. thinking tokens are charged like output tokens
            _logger.LogInformation(
                "Gemini {Model} used {PromptTokens} prompt, {OutputTokens} output and {ThinkingTokens} thinking tokens",
                _options.Model, reply?.UsageMetadata?.PromptTokenCount, reply?.UsageMetadata?.CandidatesTokenCount,
                reply?.UsageMetadata?.ThoughtsTokenCount);
            return text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // the customer closed the page, that's not a gemini failure
            throw;
        }
        catch (TaskCanceledException ex)
        {
            // HttpClient.Timeout ran out
            throw new AiServiceException("Gemini did not answer in time.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException("Could not reach Gemini.", ex);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException("Gemini sent a reply we could not read.", ex);
        }
    }

    private string ReadText(GeminiResponse? reply)
    {
        if (reply?.PromptFeedback?.BlockReason is string blockReason)
        {
            throw new AiServiceException($"Gemini blocked the prompt ({blockReason}).");
        }

        var candidate = reply?.Candidates?.FirstOrDefault();
        var text = string.Concat(
            candidate?.Content?.Parts?
                .Where(p => p.Thought != true)
                .Select(p => p.Text) ?? []);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AiServiceException($"Gemini sent an empty reply ({candidate?.FinishReason ?? "no candidates"}).");
        }
        if (candidate?.FinishReason == "MAX_TOKENS")
        {
            _logger.LogWarning("Gemini reply was cut off at {MaxOutputTokens} tokens", _options.MaxOutputTokens);
        }
        return text.Trim();
    }

    // just the parts of the gemini json we use

    private record GeminiRequest(
        GeminiContent SystemInstruction,
        List<GeminiContent> Contents,
        GeminiGenerationConfig GenerationConfig);

    private record GeminiGenerationConfig(double Temperature, int MaxOutputTokens, GeminiThinkingConfig? ThinkingConfig);

    private record GeminiThinkingConfig(string ThinkingLevel);

    private record GeminiContent(string? Role, List<GeminiPart>? Parts);

    private record GeminiPart(string? Text, bool? Thought = null);

    private record GeminiResponse(
        List<GeminiCandidate>? Candidates,
        GeminiPromptFeedback? PromptFeedback,
        GeminiUsage? UsageMetadata);

    private record GeminiCandidate(GeminiContent? Content, string? FinishReason);

    private record GeminiPromptFeedback(string? BlockReason);

    private record GeminiUsage(int? PromptTokenCount, int? CandidatesTokenCount, int? ThoughtsTokenCount, int? TotalTokenCount);
}
