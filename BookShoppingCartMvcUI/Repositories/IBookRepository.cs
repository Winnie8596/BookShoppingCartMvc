namespace BookShoppingCartMvcUI.Repositories;

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
