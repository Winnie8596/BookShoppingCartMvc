namespace BookShoppingCartMvcUI.Ai;

// used when no ai provider is set up, so the app still starts and the assistant
// just says it's unavailable
public class DisabledAiService : IAiService
{
    public Task<string> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        throw new AiServiceException("No AI provider is configured.");
    }
}
