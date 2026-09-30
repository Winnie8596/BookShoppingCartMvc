using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BookShoppingCartMvcUI.Ai;

// ai registrations in one place so a test can check them without starting the whole app
public static class AiServiceCollectionExtensions
{
    public const string AssistantRateLimit = "assistant";
    public const int AssistantRequestsPerMinute = 5;
    public const string TooManyRequestsMessage = "You're asking a bit fast. Please wait a minute and try again.";

    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<BookAssistant>();
        services.AddAssistantRateLimit();

        var section = configuration.GetSection(GeminiOptions.SectionName);
        var options = section.Get<GeminiOptions>() ?? new GeminiOptions();

        // no key (e.g. someone cloned the repo) -> the shop still works, the assistant is just off
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            services.AddTransient<IAiService, DisabledAiService>();
            return services;
        }

        services.Configure<GeminiOptions>(section);
        // typed client: IHttpClientFactory reuses the connections so we don't run out of sockets
        services.AddHttpClient<IAiService, GeminiAiService>(client =>
        {
            client.BaseAddress = new Uri(GeminiAiService.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        return services;
    }

    // every question is a paid (or free tier limited) api call, so one person can't hammer it.
    // counted per logged in user, or per ip address for guests
    private static void AddAssistantRateLimit(this IServiceCollection services)
    {
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, cancellationToken) =>
                new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = TooManyRequestsMessage }, cancellationToken));

            limiter.AddPolicy(AssistantRateLimit, http =>
            {
                var who = http.User.Identity?.IsAuthenticated == true
                    ? "user:" + http.User.Identity.Name
                    : "ip:" + http.Connection.RemoteIpAddress;
                return RateLimitPartition.GetFixedWindowLimiter(who, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = AssistantRequestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });
    }
}
