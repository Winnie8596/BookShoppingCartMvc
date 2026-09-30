using BookShoppingCartMvcUI.Ai;
using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BookShoppingCartMvcUI.Controllers
{
    // guests can use it too, it only reads the public catalog
    public class AssistantController : Controller
    {
        private readonly BookAssistant _assistant;

        public AssistantController(BookAssistant assistant)
        {
            _assistant = assistant;
        }

        public IActionResult Index() => View();

        // returns json for the page to show. the antiforgery token is checked by the global filter
        [HttpPost]
        [EnableRateLimiting(AiServiceCollectionExtensions.AssistantRateLimit)]
        public async Task<IActionResult> Ask(string? question)
        {
            var reply = await _assistant.AskAsync(question, HttpContext.RequestAborted);

            var books = reply.Books.Select(b => new
            {
                id = b.Id,
                title = b.Title,
                author = b.Author,
                genre = b.Genre,
                price = Money.Format(b.Price),
                stock = StockLevel.Status(b.Quantity),
                url = Url.Action("Details", "Home", new { id = b.Id })
            });

            return reply.Status switch
            {
                AssistantReplyStatus.Answered => Ok(new { answer = reply.Message, books }),
                AssistantReplyStatus.Invalid => BadRequest(new { error = reply.Message }),
                _ => StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = reply.Message, books })
            };
        }
    }
}
