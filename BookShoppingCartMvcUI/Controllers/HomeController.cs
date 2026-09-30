using BookShoppingCartMvcUI.Models;
using BookShoppingCartMvcUI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BookShoppingCartMvcUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepository;

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepository)
        {
            _homeRepository = homeRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string sterm = "",
            int genreId = 0,
            double? minPrice = null,
            double? maxPrice = null,
            bool inStockOnly = false,
            string sortBy = "relevance",
            int pageNumber = 1,
            int? minRating = null)
        {
            const int pageSize = 12;
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            var (books, totalCount) = await _homeRepository.GetBooks(
                sterm, genreId, minPrice, maxPrice, inStockOnly, sortBy, pageNumber, pageSize, minRating);
            IEnumerable<Genre> genres = await _homeRepository.Genres();
            BookDisplayModel bookModel = new BookDisplayModel
            {
              Books=books,
              Genres=genres,
              STerm=sterm,
              GenreId=genreId,
              MinPrice = minPrice,
              MaxPrice = maxPrice,
              InStockOnly = inStockOnly,
              MinRating = minRating,
              SortBy = sortBy,
              PageNumber = pageNumber,
              PageSize = pageSize,
              TotalCount = totalCount
            };
            return View(bookModel);
        }

        public async Task<IActionResult> Details(int id, [FromServices] IReviewRepository reviewRepository)
        {
            var book = await _homeRepository.GetBookDetails(id);
            if (book is null)
            {
                return NotFound();
            }
            return View(new BookDetailsModel
            {
                Book = book,
                RelatedBooks = await _homeRepository.GetRelatedBooks(book),
                Reviews = await reviewRepository.GetBookReviews(id)
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // error pages get re-run with the original method (often POST), they don't change anything
        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // already logged by the exception middleware, user only sees the request id
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // hit by the status code pages middleware for empty error responses like 404
        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HttpStatus(int code)
        {
            // only meant to be used as an error page, 404 if opened directly
            if (HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>() is null)
                code = StatusCodes.Status404NotFound;
            Response.StatusCode = code;
            return View(nameof(HttpStatus), code);
        }
    }
}