using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize(Roles=nameof(Roles.Admin))]
    public class StockController : Controller
    {
        private readonly IStockRepository _stockRepo;

        public StockController(IStockRepository stockRepo)
        {
            _stockRepo = stockRepo;
        }

        public async Task<IActionResult> Index([FromQuery] InventoryQuery query)
        {
            return View(await _stockRepo.GetInventory(query));
        }

        public async Task<IActionResult> ManangeStock(int bookId)
        {
            var item = await _stockRepo.GetStockItem(bookId);
            if (item is null)
                return NotFound();
            return View(PageModel(item));
        }

        // set an exact count (stocktake)
        [HttpPost]
        public async Task<IActionResult> ManangeStock(StockDTO stock)
        {
            var item = await _stockRepo.GetStockItem(stock.BookId);
            if (item is null)
                return NotFound();
            if (!ModelState.IsValid)
                return View(PageModel(item));

            var result = await _stockRepo.SetStock(stock.BookId, stock.Quantity, stock.ExpectedQuantity);
            if (result.Succeeded)
            {
                TempData["successMessage"] = $"Stock of \"{item.BookName}\" is now {result.Quantity}.";
                return RedirectToAction(nameof(Index));
            }

            if (result.Status == StockChangeStatus.StockChanged)
            {
                ModelState.AddModelError(nameof(StockDTO.Quantity),
                    $"The stock changed from {stock.ExpectedQuantity} to {result.Quantity} while this page was open, probably because of a sale. " +
                    "Check the new count and save again.");
                // next save gets checked against the count they can see now
                ModelState.Remove(nameof(StockDTO.ExpectedQuantity));
            }
            else
            {
                ModelState.AddModelError(nameof(StockDTO.Quantity), "Quantity must be between 0 and 100,000.");
            }
            item.Quantity = result.Quantity;
            return View(PageModel(item));
        }

        // add/remove copies from the current count (delivery, damaged etc)
        [HttpPost]
        public async Task<IActionResult> AdjustStock(StockAdjustmentDTO adjustment)
        {
            var item = await _stockRepo.GetStockItem(adjustment.BookId);
            if (item is null)
                return NotFound();
            if (adjustment.Change == 0)
                ModelState.AddModelError(nameof(StockAdjustmentDTO.Change), "Enter a number other than 0.");
            if (!ModelState.IsValid)
                return View(nameof(ManangeStock), PageModel(item));

            int change = adjustment.Change!.Value;
            var result = await _stockRepo.AdjustStock(adjustment.BookId, change);
            if (result.Succeeded)
            {
                TempData["successMessage"] = change > 0
                    ? $"Added {change} to \"{item.BookName}\". Stock is now {result.Quantity}."
                    : $"Removed {-change} from \"{item.BookName}\". Stock is now {result.Quantity}.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(nameof(StockAdjustmentDTO.Change), result.Status switch
            {
                StockChangeStatus.BelowZero => $"Only {result.Quantity} in stock, so at most {result.Quantity} can be removed.",
                StockChangeStatus.AboveMaximum => $"Stock cannot go above 100,000. At most {(StockLevel.MaxQuantity - result.Quantity).ToString("N0", CultureInfo.InvariantCulture)} can be added.",
                _ => "The change must be between -100,000 and 100,000."
            });
            item.Quantity = result.Quantity;
            return View(nameof(ManangeStock), PageModel(item));
        }

        // both start at the current count. if the form is shown again it keeps what the admin typed,
        // because the input tag helpers use the posted value from ModelState first
        private static StockManageModel PageModel(StockDisplayModel item) => new()
        {
            BookId = item.BookId,
            BookName = item.BookName,
            AuthorName = item.AuthorName,
            CurrentQuantity = item.Quantity,
            Quantity = item.Quantity,
            ExpectedQuantity = item.Quantity
        };
    }
}
