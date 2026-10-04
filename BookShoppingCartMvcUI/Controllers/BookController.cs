using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class BookController : Controller
{
    public const long MaxImageBytes = 1 * 1024 * 1024;
    public static readonly IReadOnlyList<string> AllowedImageExtensions = [".jpeg", ".jpg", ".png"];

    private readonly IBookRepository _bookRepo;
    private readonly IGenreRepository _genreRepo;
    private readonly IFileService _fileService;
    private readonly ILogger<BookController> _logger;

    public BookController(IBookRepository bookRepo, IGenreRepository genreRepo, IFileService fileService, ILogger<BookController> logger)
    {
        _bookRepo = bookRepo;
        _genreRepo = genreRepo;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<IActionResult> Index([FromQuery] AdminBookQuery query)
    {
        return View(await _bookRepo.GetAdminBooks(query));
    }

    public async Task<IActionResult> AddBook()
    {
        return View(new BookDTO { GenreList = await GenreSelectList() });
    }

    [HttpPost]
    public async Task<IActionResult> AddBook(BookDTO bookToAdd)
    {
        await ValidateBook(bookToAdd);
        if (!ModelState.IsValid)
        {
            bookToAdd.GenreList = await GenreSelectList();
            return View(bookToAdd);
        }

        string? newImage = null;
        try
        {
            if (bookToAdd.ImageFile != null)
                newImage = await _fileService.SaveFile(bookToAdd.ImageFile, AllowedImageExtensions);

            // create the stock row too so the book can be sold straight away
            await _bookRepo.AddBook(new Book
            {
                BookName = bookToAdd.BookName!.Trim(),
                AuthorName = bookToAdd.AuthorName!.Trim(),
                Image = newImage,
                GenreId = bookToAdd.GenreId,
                Price = bookToAdd.Price,
                Stock = new Stock { Quantity = bookToAdd.InitialStock }
            });
            TempData["successMessage"] = $"\"{bookToAdd.BookName.Trim()}\" was added.";
            return RedirectToAction(nameof(AddBook));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adding a book failed");
            DeleteImageQuietly(newImage);
            TempData["errorMessage"] = "The book could not be saved. Please try again.";
            bookToAdd.GenreList = await GenreSelectList();
            return View(bookToAdd);
        }
    }

    public async Task<IActionResult> UpdateBook(int id)
    {
        var book = await _bookRepo.GetBookById(id);
        if (book == null)
        {
            TempData["errorMessage"] = $"No book with id {id} was found.";
            return RedirectToAction(nameof(Index));
        }
        return View(new BookDTO
        {
            Id = book.Id,
            BookName = book.BookName,
            AuthorName = book.AuthorName,
            GenreId = book.GenreId,
            Price = book.Price,
            Image = book.Image,
            GenreList = await GenreSelectList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBook(BookDTO bookToUpdate)
    {
        var book = await _bookRepo.GetBookById(bookToUpdate.Id);
        if (book == null)
        {
            TempData["errorMessage"] = $"No book with id {bookToUpdate.Id} was found.";
            return RedirectToAction(nameof(Index));
        }
        // take the image from the db, not the form - otherwise someone could edit the field and delete another file
        bookToUpdate.Image = book.Image;
        // stock is changed on the stock page
        ModelState.Remove(nameof(BookDTO.InitialStock));

        await ValidateBook(bookToUpdate);
        if (!ModelState.IsValid)
        {
            bookToUpdate.GenreList = await GenreSelectList();
            return View(bookToUpdate);
        }

        string? newImage = null;
        try
        {
            var oldImage = book.Image;
            if (bookToUpdate.ImageFile != null)
            {
                newImage = await _fileService.SaveFile(bookToUpdate.ImageFile, AllowedImageExtensions);
                book.Image = newImage;
            }
            book.BookName = bookToUpdate.BookName!.Trim();
            book.AuthorName = bookToUpdate.AuthorName!.Trim();
            book.GenreId = bookToUpdate.GenreId;
            book.Price = bookToUpdate.Price;
            await _bookRepo.UpdateBook(book);

            if (newImage != null)
                DeleteImageQuietly(oldImage);
            TempData["successMessage"] = $"\"{book.BookName}\" was updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Updating book {BookId} failed", bookToUpdate.Id);
            DeleteImageQuietly(newImage);
            TempData["errorMessage"] = "The book could not be saved. Please try again.";
            bookToUpdate.GenreList = await GenreSelectList();
            return View(bookToUpdate);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _bookRepo.GetBookById(id);
        if (book == null)
        {
            TempData["errorMessage"] = $"No book with id {id} was found.";
            return RedirectToAction(nameof(Index));
        }

        // can't delete a book that's been ordered (db blocks it too since OrderDetail -> Book is Restrict),
        // this just shows a nicer message
        var inUse = $"\"{book.BookName}\" has been ordered, so it is kept for order history. Set its stock to 0 to stop selling it.";
        if (await _bookRepo.HasOrders(id))
        {
            TempData["errorMessage"] = inUse;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _bookRepo.DeleteBook(book);
        }
        catch (DbUpdateException ex)
        {
            // someone ordered it between the check and the delete
            _logger.LogWarning(ex, "Deleting book {BookId} was refused by the database", id);
            TempData["errorMessage"] = inUse;
            return RedirectToAction(nameof(Index));
        }
        DeleteImageQuietly(book.Image);
        TempData["successMessage"] = $"\"{book.BookName}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    // extra checks the attributes can't do
    private async Task ValidateBook(BookDTO book)
    {
        if (book.GenreId > 0 && !await _genreRepo.GenreExists(book.GenreId))
            ModelState.AddModelError(nameof(BookDTO.GenreId), "Please choose a genre.");

        if (book.ImageFile != null)
        {
            var extension = Path.GetExtension(book.ImageFile.FileName);
            if (!AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(BookDTO.ImageFile), $"Only {string.Join(", ", AllowedImageExtensions)} images are allowed.");
            else if (book.ImageFile.Length > MaxImageBytes)
                ModelState.AddModelError(nameof(BookDTO.ImageFile), "The image cannot be larger than 1 MB.");
            else if (book.ImageFile.Length == 0)
                ModelState.AddModelError(nameof(BookDTO.ImageFile), "The image file is empty.");
            else if (!ImageSignature.Matches(book.ImageFile))
                ModelState.AddModelError(nameof(BookDTO.ImageFile), "That file isn't a valid JPEG or PNG image.");
        }
    }

    private async Task<IEnumerable<SelectListItem>> GenreSelectList() =>
        (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString()
        }).ToList();

    // delete the old cover, doesn't matter if the file is already gone
    private void DeleteImageQuietly(string? image)
    {
        if (string.IsNullOrWhiteSpace(image))
            return;
        try
        {
            _fileService.DeleteFile(image);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete image {Image}", image);
        }
    }
}
