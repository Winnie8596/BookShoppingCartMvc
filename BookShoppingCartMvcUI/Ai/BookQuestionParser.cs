using System.Globalization;
using System.Text.RegularExpressions;

namespace BookShoppingCartMvcUI.Ai;

// turns "a programming book under RM50" into keywords + a price filter.
// price and stock are hard rules so we do them here in c#, not in the ai
public static class BookQuestionParser
{
    public const int MaxKeywords = 8;

    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private const string Amount = @"(?:rm\s*)?(\d+(?:\.\d+)?)";

    // "between RM10 and RM20", "from 10 to 20", "RM10 - RM20" (without the words both need "rm",
    // otherwise "top 10 to 20" would count)
    private static readonly Regex Range = new(
        $@"(?:between|from)\s*{Amount}\s*(?:and|to|-)\s*{Amount}|rm\s*(\d+(?:\.\d+)?)\s*(?:to|-)\s*rm\s*(\d+(?:\.\d+)?)",
        Options);
    private static readonly Regex Below = new($@"(?:under|below|less than|cheaper than|up to|at most|max(?:imum)?|within|<=?)\s*{Amount}", Options);
    private static readonly Regex Above = new($@"(?:over|above|more than|at least|min(?:imum)?|>=?)\s*{Amount}", Options);
    // a single price with no "under/over", e.g. "costs RM20" or "a RM20 book". needs "rm" or "cost/priced"
    // in front, so "top 10" or "the 3 best" stay plain numbers
    private static readonly Regex Around = new(@"(?:\brm\s*|\b(?:costs?|costing|priced(?:\s+at)?)\s+(?:rm\s*)?)(\d+(?:\.\d+)?)\b", Options);
    // any price left over, we just don't want "rm20" as a keyword
    private static readonly Regex AnyPrice = new($@"\b{Amount}\b", Options);
    private static readonly Regex InStock = new(@"\b(?:in stock|available)\b", Options);
    private static readonly Regex Word = new(@"[a-z0-9.#+]+", Options);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "of", "to", "in", "on", "for", "with", "about", "by", "at", "from",
        "i", "im", "me", "my", "you", "your", "we", "our", "it", "its", "this", "that", "these", "those",
        "is", "are", "was", "be", "do", "does", "did", "have", "has", "had", "can", "could", "would", "should", "will",
        "what", "which", "who", "any", "some", "something", "anything", "there", "here", "please", "thanks", "hi", "hello",
        "book", "books", "novel", "novels", "read", "reading", "recommend", "recommendation", "suggest", "suggestion",
        "looking", "look", "find", "want", "need", "like", "love", "good", "best", "great", "nice", "help", "learn",
        "learning", "related", "sell", "store", "shop", "catalog", "stock", "available", "price", "priced", "cost",
        "costs", "cheap", "budget", "under", "below", "over", "above", "than", "less", "more", "between", "rm", "get"
    };

    public static AssistantBookQuery Parse(string? question)
    {
        var text = question ?? "";
        decimal? minPrice = null;
        decimal? maxPrice = null;

        var range = Range.Match(text);
        if (range.Success)
        {
            // groups 1-2 = the "between/from" form, 3-4 = the "RM10 - RM20" form
            var first = range.Groups[1].Success ? 1 : 3;
            var a = ParseAmount(range.Groups[first].Value);
            var b = ParseAmount(range.Groups[first + 1].Value);
            minPrice = Math.Min(a, b);
            maxPrice = Math.Max(a, b);
            text = text.Remove(range.Index, range.Length);
        }
        else
        {
            var below = Below.Match(text);
            if (below.Success)
            {
                maxPrice = ParseAmount(below.Groups[1].Value);
            }
            var above = Above.Match(text);
            if (above.Success)
            {
                minPrice = ParseAmount(above.Groups[1].Value);
            }

            // nobody means exactly RM20.00, so take 10% either side (RM19.99 is a "RM20 book")
            var around = Around.Match(text);
            if (!below.Success && !above.Success && around.Success)
            {
                var amount = ParseAmount(around.Groups[1].Value);
                minPrice = Math.Round(amount * 0.9m, 2);
                maxPrice = Math.Round(amount * 1.1m, 2);
            }
        }

        var inStockOnly = InStock.IsMatch(text);
        text = AnyPrice.Replace(text, " ");

        var keywords = Word.Matches(text.ToLowerInvariant())
            .Select(m => Normalize(m.Value))
            .Where(w => w.Length > 1 && !StopWords.Contains(w) && !w.All(c => char.IsDigit(c) || c == '.'))
            .Distinct()
            .Take(MaxKeywords)
            .ToList();

        return new AssistantBookQuery(keywords, minPrice, maxPrice, inStockOnly);
    }

    private static decimal ParseAmount(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static string Normalize(string word)
    {
        // keep ".net", but "books." at the end of a sentence becomes "books"
        if (word != ".net")
        {
            word = word.Trim('.');
        }
        // very rough plural -> singular so "thrillers" finds the Thriller genre
        if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss"))
        {
            word = word[..^1];
        }
        return word;
    }
}
