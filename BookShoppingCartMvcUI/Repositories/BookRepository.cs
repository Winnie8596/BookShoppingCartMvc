using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories
{
    public interface IBookRepository
    {
        Task AddBook(Book book);
        Task DeleteBook(Book book);
        Task<Book?> GetBookById(int id);
        // admin book list, search/filter/sort/paging all done in sql
        Task<AdminBookListModel> GetAdminBooks(AdminBookQuery query);
        // true if the book is in any order
        Task<bool> HasOrders(int bookId);
        Task UpdateBook(Book book);
    }

    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;
        public BookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddBook(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBook(Book book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBook(Book book)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }

        // Edit and delete 
        public async Task<Book?> GetBookById(int id) => await _context.Books.FindAsync(id);

        public Task<bool> HasOrders(int bookId) => _context.OrderDetails.AnyAsync(od => od.BookId == bookId);

        public async Task<AdminBookListModel> GetAdminBooks(AdminBookQuery query)
        {
            var search = query.Search?.Trim();
            var sort = AdminBookQuery.Sorts.Contains(query.Sort) ? query.Sort! : AdminBookQuery.Sorts[0];
            var pageSize = query.PageSize < 1 ? AdminBookQuery.DefaultPageSize : Math.Min(query.PageSize, AdminBookQuery.MaxPageSize);
            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

            var books = _context.Books.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                // ToLower so it's case insensitive whatever the collation is
                var term = search.ToLower();
                books = books.Where(b => b.BookName!.ToLower().Contains(term) || b.AuthorName!.ToLower().Contains(term));
            }
            if (query.GenreId is int genreId)
                books = books.Where(b => b.GenreId == genreId);

            var model = new AdminBookListModel
            {
                Search = search,
                GenreId = query.GenreId,
                Sort = sort,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = await books.CountAsync(),
                Genres = await _context.Genres.AsNoTracking().OrderBy(g => g.GenreName).ToListAsync()
            };

            long skip = (long)(pageNumber - 1) * pageSize;
            if (skip >= model.TotalCount)
                return model;

    
            books = sort switch
            {
                "title" => books.OrderBy(b => b.BookName).ThenBy(b => b.Id),
                "price" => books.OrderBy(b => b.Price).ThenBy(b => b.Id),
                "price_desc" => books.OrderByDescending(b => b.Price).ThenBy(b => b.Id),
                "stock" => books.OrderBy(b => b.Stock == null ? 0 : b.Stock.Quantity).ThenBy(b => b.Id),
                _ => books.OrderByDescending(b => b.Id)
            };

            model.Books = await books
                .Skip((int)skip)
                .Take(pageSize)
                .Select(b => new AdminBookRowModel
                {
                    Id = b.Id,
                    BookName = b.BookName,
                    AuthorName = b.AuthorName,
                    GenreName = b.Genre.GenreName,
                    Price = b.Price,
                    Image = b.Image,
                    Quantity = b.Stock == null ? 0 : b.Stock.Quantity,
                    HasOrders = b.OrderDetail.Any()
                })
                .ToListAsync();
            return model;
        }
    }
}
