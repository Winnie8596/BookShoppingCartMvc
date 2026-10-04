using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;
[Authorize(Roles = nameof(Roles.Admin))]
public class ReportsController : Controller
{
    private readonly IReportRepository _reportRepository;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportRepository reportRepository, ILogger<ReportsController> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<ActionResult> TopFiveSellingBooks(DateTime? sDate = null, DateTime? eDate = null)
    {
        try
        {
            // by default, get last 7 days record
            DateTime startDate = sDate ?? DateTime.UtcNow.AddDays(-7);
            DateTime endDate = eDate ?? DateTime.UtcNow;
            // the date picker sends midnight, so count the whole end day too.
            // the proc params are sql datetime, .997 is its last value before midnight
            DateTime endOfRange = eDate is null ? endDate : endDate.Date.AddDays(1).AddMilliseconds(-3);
            var topFiveSellingBooks = await _reportRepository.GetTopNSellingBooksByDate(startDate, endOfRange);
            var vm = new TopNSoldBooksVm(startDate, endDate, topFiveSellingBooks);
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Top selling books report failed");
            TempData["errorMessage"] = "The report could not be loaded. Please try again.";
            return RedirectToAction(nameof(AdminOperationsController.Dashboard), "AdminOperations");
        }
    }
}
