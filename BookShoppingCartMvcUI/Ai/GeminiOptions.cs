namespace BookShoppingCartMvcUI.Ai;

// the "Gemini" section of appsettings. the ApiKey is NOT in appsettings, it comes from
// user-secrets locally or the Gemini__ApiKey environment variable in docker
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = "";
    // flash-lite: plain gemini-3.5-flash kept giving 503 "high demand" on the free tier,
    // and picking books from a short list doesn't need the bigger model
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 1024;
    // low = sticks closer to the book list we give it, less creative
    public double Temperature { get; set; } = 0.2;
    // flash models think at MEDIUM by default, and the thinking tokens are billed as output and
    // count against MaxOutputTokens. finding a book in a short list doesn't need much of it.
    // MINIMAL, LOW, MEDIUM or HIGH. leave empty to use the model's default
    public string ThinkingLevel { get; set; } = "LOW";
}
