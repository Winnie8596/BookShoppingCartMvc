using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

// no review id / user id in the routes, the repo always uses the logged in user
[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewRepository _reviewRepo;
    private readonly IHomeRepository _homeRepo;

    public ReviewController(IReviewRepository reviewRepo, IHomeRepository homeRepo)
    {
        _reviewRepo = reviewRepo;
        _homeRepo = homeRepo;
    }

    public async Task<IActionResult> Create(int bookId)
    {
        var book = await _homeRepo.GetBookDetails(bookId);
        if (book is null)
            return NotFound();
        if (await _reviewRepo.GetMyReview(bookId) is not null)
            return RedirectToAction(nameof(Edit), new { bookId });
        if (!await _reviewRepo.HasPurchased(bookId))
        {
            TempData["errorMessage"] = "Only customers who bought this book can review it.";
            return ToBook(bookId);
        }
        return Form(book, new ReviewInputModel { BookId = bookId }, isEdit: false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewInputModel input)
    {
        var book = await _homeRepo.GetBookDetails(input.BookId);
        if (book is null)
            return NotFound();
        if (!ModelState.IsValid)
            return Form(book, input, isEdit: false);

        var result = await _reviewRepo.Create(input);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return Form(book, input, isEdit: false);
        }
        TempData["successMessage"] = "Thank you! Your review has been posted.";
        return ToBook(input.BookId);
    }

    public async Task<IActionResult> Edit(int bookId)
    {
        var book = await _homeRepo.GetBookDetails(bookId);
        if (book is null)
            return NotFound();
        var review = await _reviewRepo.GetMyReview(bookId);
        if (review is null)
        {
            TempData["errorMessage"] = "You have not reviewed this book.";
            return ToBook(bookId);
        }
        return Form(book, review, isEdit: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReviewInputModel input)
    {
        var book = await _homeRepo.GetBookDetails(input.BookId);
        if (book is null)
            return NotFound();
        if (!ModelState.IsValid)
            return Form(book, input, isEdit: true);

        var result = await _reviewRepo.Update(input);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return Form(book, input, isEdit: true);
        }
        TempData["successMessage"] = "Your review has been updated.";
        return ToBook(input.BookId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int bookId, string? returnUrl = null)
    {
        var result = await _reviewRepo.Delete(bookId);
        if (result.Succeeded)
            TempData["successMessage"] = "Your review has been deleted.";
        else
            TempData["errorMessage"] = result.ErrorMessage;
        // only redirect to local urls (no open redirect)
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : ToBook(bookId);
    }

    private IActionResult Form(BookCardModel book, ReviewInputModel input, bool isEdit)
    {
        ViewData["BookName"] = book.BookName;
        ViewData["IsEdit"] = isEdit;
        return View("Form", input);
    }

    private IActionResult ToBook(int bookId) =>
        RedirectToAction(nameof(HomeController.Details), "Home", new { id = bookId });
}
