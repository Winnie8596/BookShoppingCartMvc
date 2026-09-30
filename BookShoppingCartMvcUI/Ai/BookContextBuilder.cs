using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BookShoppingCartMvcUI.Shared;

namespace BookShoppingCartMvcUI.Ai;

// writes the books we found as plain text for the prompt, one line per book
public static class BookContextBuilder
{
    private static readonly Regex Whitespace = new(@"\s+");
    private static readonly Regex MarkerLike = new(@"={3,}");

    public static string Build(IReadOnlyList<AssistantBook> books, IEnumerable<string?> genres, AssistantBookQuery? query = null)
    {
        var context = new StringBuilder();
        context.AppendLine("Genres in the store: " + string.Join(", ", genres.Select(OneLine)));
        if (query != null)
        {
            // tell the ai what we already filtered on, so it doesn't have to guess (or check prices itself)
            context.AppendLine("Search used for this question: " + DescribeSearch(query));
        }
        context.AppendLine();

        if (books.Count == 0)
        {
            context.AppendLine("Matching books: none. No book in the catalog matches this search.");
            return context.ToString();
        }

        context.AppendLine(query is { Keywords.Count: 0 } && !HasFilters(query)
            ? "Matching books (the question had no topic, so these are just well rated books from the catalog):"
            : "Matching books:");
        foreach (var book in books)
        {
            context.AppendLine(
                $"- \"{OneLine(book.Title)}\" by {OneLine(book.Author)} | genre: {OneLine(book.Genre)} | " +
                $"price: {Money.Format(book.Price)} | {StockLevel.Status(book.Quantity)} | {Rating(book)}");
        }
        return context.ToString();
    }

    private static string DescribeSearch(AssistantBookQuery query)
    {
        // the keywords themselves stay out, they're the customer's words and the question
        // only goes in the user prompt. the prices are numbers we parsed, so they're safe
        var parts = new List<string>
        {
            query.Keywords.Count > 0 ? "title, author or genre matches words from the question" : "no topic words"
        };

        if (query.MinPrice is double min && query.MaxPrice is double max)
        {
            parts.Add($"price {Money.Format(min)} to {Money.Format(max)}");
        }
        else if (query.MaxPrice is double below)
        {
            parts.Add($"price up to {Money.Format(below)}");
        }
        else if (query.MinPrice is double above)
        {
            parts.Add($"price from {Money.Format(above)}");
        }
        else
        {
            parts.Add("any price");
        }

        parts.Add(query.InStockOnly ? "in stock only" : "in stock or not");
        return string.Join("; ", parts);
    }

    private static bool HasFilters(AssistantBookQuery query) =>
        query.MinPrice.HasValue || query.MaxPrice.HasValue || query.InStockOnly;

    private static string Rating(AssistantBook book) => book.AverageRating is double average
        ? $"rating: {average.ToString("0.0", CultureInfo.InvariantCulture)}/5 from {book.ReviewCount} review(s)"
        : "no reviews yet";

    // titles are typed in by the admin, so squash any newlines to keep one book per line,
    // and break up "===" so a title can't pretend to be the end of the store data
    private static string OneLine(string? value) =>
        MarkerLike.Replace(Whitespace.Replace(value ?? "", " "), "=").Trim();
}
