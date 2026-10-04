namespace BookShoppingCartMvcUI.Models.DTOs;

// what we pulled out of the customer's question with plain c#, before any ai is involved
public record AssistantBookQuery(
    IReadOnlyList<string> Keywords,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false);

// the only book fields the assistant gets to see. no image, no review text, no exact stock count
public record AssistantBook(
    int Id,
    string? Title,
    string? Author,
    string? Genre,
    decimal Price,
    int Quantity,
    double? AverageRating,
    int ReviewCount);
