namespace BookShoppingCartMvcUI.Repositories;

public interface IGenreRepository
{
    Task AddGenre(Genre genre);
    Task UpdateGenre(Genre genre);
    Task<Genre?> GetGenreById(int id);
    Task DeleteGenre(Genre genre);
    Task<IEnumerable<Genre>> GetGenres();
    // all genres by name with their book count
    Task<List<AdminGenreRowModel>> GetGenresWithBookCount();
    Task<bool> GenreExists(int id);
    // case insensitive. exceptId so a genre can keep its own name when editing
    Task<bool> GenreNameTaken(string name, int exceptId = 0);
    Task<bool> HasBooks(int genreId);
}
