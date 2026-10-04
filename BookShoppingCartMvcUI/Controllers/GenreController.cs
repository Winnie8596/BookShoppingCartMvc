using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class GenreController : Controller
{
    private readonly IGenreRepository _genreRepo;
    private readonly ILogger<GenreController> _logger;

    public GenreController(IGenreRepository genreRepo, ILogger<GenreController> logger)
    {
        _genreRepo = genreRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _genreRepo.GetGenresWithBookCount());
    }

    public IActionResult AddGenre()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddGenre(GenreDTO genre)
    {
        await ValidateName(genre);
        if (!ModelState.IsValid)
        {
            return View(genre);
        }
        try
        {
            await _genreRepo.AddGenre(new Genre { GenreName = genre.GenreName.Trim() });
            TempData["successMessage"] = "The genre was added.";
            return RedirectToAction(nameof(AddGenre));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adding a genre failed");
            TempData["errorMessage"] = "The genre could not be added. Please try again.";
            return View(genre);
        }
    }

    public async Task<IActionResult> UpdateGenre(int id)
    {
        var genre = await _genreRepo.GetGenreById(id);
        if (genre is null)
            return GenreNotFound(id);
        return View(new GenreDTO { Id = genre.Id, GenreName = genre.GenreName });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateGenre(GenreDTO genreToUpdate)
    {
        var genre = await _genreRepo.GetGenreById(genreToUpdate.Id);
        if (genre is null)
            return GenreNotFound(genreToUpdate.Id);

        await ValidateName(genreToUpdate);
        if (!ModelState.IsValid)
        {
            return View(genreToUpdate);
        }
        try
        {
            genre.GenreName = genreToUpdate.GenreName.Trim();
            await _genreRepo.UpdateGenre(genre);
            TempData["successMessage"] = "The genre was updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Updating genre {GenreId} failed", genreToUpdate.Id);
            TempData["errorMessage"] = "The genre could not be updated. Please try again.";
            return View(genreToUpdate);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGenre(int id)
    {
        var genre = await _genreRepo.GetGenreById(id);
        if (genre is null)
            return GenreNotFound(id);

        // can't delete a genre that still has books (db blocks it too), just show a proper message
        var inUse = $"\"{genre.GenreName}\" still has books. Move them to another genre before deleting it.";
        if (await _genreRepo.HasBooks(id))
        {
            TempData["errorMessage"] = inUse;
            return RedirectToAction(nameof(Index));
        }
        try
        {
            await _genreRepo.DeleteGenre(genre);
            TempData["successMessage"] = $"\"{genre.GenreName}\" was deleted.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Deleting genre {GenreId} was refused by the database", id);
            TempData["errorMessage"] = inUse;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateName(GenreDTO genre)
    {
        if (!string.IsNullOrWhiteSpace(genre.GenreName) && await _genreRepo.GenreNameTaken(genre.GenreName, genre.Id))
            ModelState.AddModelError(nameof(GenreDTO.GenreName), "There is already a genre with this name.");
    }

    private IActionResult GenreNotFound(int id)
    {
        TempData["errorMessage"] = $"No genre with id {id} was found.";
        return RedirectToAction(nameof(Index));
    }
}
