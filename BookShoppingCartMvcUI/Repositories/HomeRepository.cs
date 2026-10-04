

using Microsoft.EntityFrameworkCore;
//get the book/genre data from database and give it to controller

namespace BookShoppingCartMvcUI.Repositories;

public class HomeRepository : IHomeRepository
{
    private readonly ApplicationDbContext _db;

    public HomeRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    // genre list for the filter, loaded on every catalog page so no tracking
    public async Task<IEnumerable<Genre>> Genres()
    {
        return await _db.Genres.AsNoTracking().ToListAsync();
    }

    // one book for the details page, same shape as the catalog cards
    public async Task<BookCardModel?> GetBookDetails(int bookId)
    {
        return await _db.Books
            .Where(b => b.Id == bookId)
            .Select(book => new BookCardModel
            {
                Id = book.Id,
                Image = book.Image,
                AuthorName = book.AuthorName,
                BookName = book.BookName,
                GenreId = book.GenreId,
                Price = book.Price,
                GenreName = book.Genre.GenreName,
                Quantity = book.Stock == null ? 0 : book.Stock.Quantity
            })
            .FirstOrDefaultAsync();
    }

    // related books = same genre or same author.
    // order: genre + author, then genre, then author. inside each group closest price first, then id
    public async Task<IEnumerable<BookCardModel>> GetRelatedBooks(BookCardModel book, int count = 4)
    {
        if (count < 1)
        {
            return Enumerable.Empty<BookCardModel>();
        }

        var author = (book.AuthorName ?? "").Trim().ToLower();
        bool hasAuthor = author != "";

        return await _db.Books
            .Where(b => b.Id != book.Id)
            .Select(b => new
            {
                Book = b,
                SameGenre = b.GenreId == book.GenreId,
                SameAuthor = hasAuthor && (b.AuthorName ?? "").Trim().ToLower() == author
            })
            .Where(x => x.SameGenre || x.SameAuthor)
            .OrderByDescending(x => x.SameGenre)
            .ThenByDescending(x => x.SameAuthor)
            // price distance without Math.Abs, the sqlite provider can't translate it for decimal
            .ThenBy(x => x.Book.Price >= book.Price ? x.Book.Price - book.Price : book.Price - x.Book.Price)
            .ThenBy(x => x.Book.Id)
            .Take(count)
            .Select(x => new BookCardModel
            {
                Id = x.Book.Id,
                Image = x.Book.Image,
                AuthorName = x.Book.AuthorName,
                BookName = x.Book.BookName,
                GenreId = x.Book.GenreId,
                Price = x.Book.Price,
                GenreName = x.Book.Genre.GenreName,
                Quantity = x.Book.Stock == null ? 0 : x.Book.Stock.Quantity
            })
            .ToListAsync();
    }

    // books for the ai assistant. a book matches if any keyword is in its title, author or genre.
    // title hits count most, then genre, then author. no keywords = best in-stock books for the price filter
    public async Task<List<AssistantBook>> FindBooksForAssistant(AssistantBookQuery query, int limit = 10)
    {
        if (limit < 1)
        {
            return new List<AssistantBook>();
        }

        var keywords = query.Keywords.Select(k => k.ToLower()).ToList();
        IQueryable<Book> books = _db.Books;

        if (query.MinPrice.HasValue)
        {
            books = books.Where(b => b.Price >= query.MinPrice.Value);
        }
        if (query.MaxPrice.HasValue)
        {
            books = books.Where(b => b.Price <= query.MaxPrice.Value);
        }
        if (query.InStockOnly)
        {
            books = books.Where(b => b.Stock != null && b.Stock.Quantity > 0);
        }
        if (keywords.Count > 0)
        {
            // keywords is sent as one json parameter, sql server reads it with OPENJSON
            books = books.Where(b => keywords.Any(k =>
                (b.BookName ?? "").ToLower().Contains(k) ||
                (b.AuthorName ?? "").ToLower().Contains(k) ||
                (b.Genre.GenreName ?? "").ToLower().Contains(k)));
        }

        // ef can't translate a query over an empty list, so no keywords = everything scores 0
        var scored = keywords.Count == 0
            ? books.Select(b => new { Book = b, Score = 0 })
            : books.Select(b => new
            {
                Book = b,
                Score = keywords.Count(k => (b.BookName ?? "").ToLower().Contains(k)) * 3
                    + keywords.Count(k => (b.Genre.GenreName ?? "").ToLower().Contains(k)) * 2
                    + keywords.Count(k => (b.AuthorName ?? "").ToLower().Contains(k))
            });

        return await scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Book.Stock != null && x.Book.Stock.Quantity > 0)
            .ThenByDescending(x => x.Book.Reviews.Average(r => (double?)r.Rating) ?? 0)
            .ThenBy(x => x.Book.Id)
            .Take(limit)
            .Select(x => new AssistantBook(
                x.Book.Id,
                x.Book.BookName,
                x.Book.AuthorName,
                x.Book.Genre.GenreName,
                x.Book.Price,
                x.Book.Stock == null ? 0 : x.Book.Stock.Quantity,
                x.Book.Reviews.Average(r => (double?)r.Rating),
                x.Book.Reviews.Count))
            .ToListAsync();
    }

    public async Task<(IEnumerable<BookCardModel> Books, int TotalCount)> GetBooks(
        string sTerm = "",
        int genreId = 0,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool inStockOnly = false,
        string sortBy = "relevance",
        int pageNumber = 1,
        int pageSize = 12,
        int? minRating = null)
    {
        
        // refactored code
        // In this code we are first building query, then rebuilding that query on the basis of filter. Query is translated into sql when we call .ToListAsync() method.

        // no Include/AsNoTracking needed - the Select pulls in Genre/Stock/Reviews and projections aren't tracked
        IQueryable<Book> bookQuery = _db.Books;

        if (!string.IsNullOrWhiteSpace(sTerm))
        {
            var term = sTerm.ToLower();
            bookQuery = bookQuery.Where(b =>
                (b.BookName ?? "").ToLower().Contains(term) ||
                (b.AuthorName ?? "").ToLower().Contains(term));
        }

        if (genreId > 0)
        {
            bookQuery = bookQuery.Where(b => b.GenreId == genreId);
        }

        if (minPrice.HasValue)
        {
            bookQuery = bookQuery.Where(b => b.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            bookQuery = bookQuery.Where(b => b.Price <= maxPrice.Value);
        }

        if (inStockOnly)
        {
            bookQuery = bookQuery.Where(b => b.Stock != null && b.Stock.Quantity > 0);
        }

        // avg rating, books with no reviews never match a min rating.
        // anything outside 1-5 is ignored (same as genreId 0)
        if (minRating is >= Review.MinRating and <= Review.MaxRating)
        {
            bookQuery = bookQuery.Where(b => b.Reviews.Average(r => (double?)r.Rating) >= minRating.Value);
        }

        var totalCount = await bookQuery.CountAsync();

        bookQuery = sortBy switch
        {
            "price_asc" => bookQuery.OrderBy(b => b.Price),
            "price_desc" => bookQuery.OrderByDescending(b => b.Price),
            "title_asc" => bookQuery.OrderBy(b => b.BookName),
            "title_desc" => bookQuery.OrderByDescending(b => b.BookName),
            "newest" => bookQuery.OrderByDescending(b => b.Id),
            // books with no reviews go last, then the other rating value, then id
            "rating_desc" => bookQuery
                .OrderByDescending(b => b.Reviews.Average(r => (double?)r.Rating) ?? 0)
                .ThenByDescending(b => b.Reviews.Count)
                .ThenBy(b => b.Id),
            "reviews_desc" => bookQuery
                .OrderByDescending(b => b.Reviews.Count)
                .ThenByDescending(b => b.Reviews.Average(r => (double?)r.Rating) ?? 0)
                .ThenBy(b => b.Id),
            _ => bookQuery.OrderBy(b => b.Id)
        };

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 12;
        }

        // long so a huge page number can't overflow
        long skip = (long)(pageNumber - 1) * pageSize;
        if (skip >= totalCount)
        {
            return (Enumerable.Empty<BookCardModel>(), totalCount);
        }

        var books = await bookQuery
            .Skip((int)skip)
            .Take(pageSize)
            .Select(book => new BookCardModel
            {
                Id = book.Id,
                Image = book.Image,
                AuthorName = book.AuthorName,
                BookName = book.BookName,
                GenreId = book.GenreId,
                Price = book.Price,
                GenreName = book.Genre.GenreName,
                Quantity = book.Stock == null ? 0 : book.Stock.Quantity,
                AverageRating = book.Reviews.Average(r => (double?)r.Rating),
                ReviewCount = book.Reviews.Count
            }).ToListAsync();

        return (books, totalCount);

    }
}
