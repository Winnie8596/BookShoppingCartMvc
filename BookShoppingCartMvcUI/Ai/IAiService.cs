namespace BookShoppingCartMvcUI.Ai;

// the only thing the rest of the app knows about the ai provider.
// system prompt = our rules + the book list, user prompt = the customer's question
public interface IAiService
{
    Task<string> GenerateResponseAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

// thrown when the provider can't give us an answer (down, timeout, no key, bad reply).
// callers catch this one type instead of HttpRequestException, JsonException etc
public class AiServiceException : Exception
{
    public AiServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
