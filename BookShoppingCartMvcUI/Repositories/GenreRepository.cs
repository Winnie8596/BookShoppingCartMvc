using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories;

public class GenreRepository : IGenreRepository
{
    private readonly ApplicationDbContext _context;
    public GenreRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddGenre(Genre genre)
    {
        _context.Genres.Add(genre);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateGenre(Genre genre)
    {
        _context.Genres.Update(genre);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteGenre(Genre genre)
    {
        _context.Genres.Remove(genre);
        await _context.SaveChangesAsync();
    }

    public async Task<Genre?> GetGenreById(int id)
    {
        return await _context.Genres.FindAsync(id);
    }

    public async Task<IEnumerable<Genre>> GetGenres()
    {
        // for lists/dropdowns only, GetGenreById is still tracked for delete
        return await _context.Genres.AsNoTracking().OrderBy(g => g.GenreName).ToListAsync();
    }

    public async Task<List<AdminGenreRowModel>> GetGenresWithBookCount() =>
        await _context.Genres
            .OrderBy(g => g.GenreName)
            .Select(g => new AdminGenreRowModel(g.Id, g.GenreName, g.Books.Count))
            .ToListAsync();

    public Task<bool> GenreExists(int id) => _context.Genres.AnyAsync(g => g.Id == id);

    public Task<bool> GenreNameTaken(string name, int exceptId = 0)
    {
        var normalized = name.Trim().ToLower();
        return _context.Genres.AnyAsync(g => g.Id != exceptId && g.GenreName.ToLower() == normalized);
    }

    public Task<bool> HasBooks(int genreId) => _context.Books.AnyAsync(b => b.GenreId == genreId);
}
