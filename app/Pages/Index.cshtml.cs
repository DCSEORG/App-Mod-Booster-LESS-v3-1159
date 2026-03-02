// Pages/Index.cshtml.cs – Dashboard page model
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ExpenseRepository _repo;
    private readonly ILogger<IndexModel> _logger;

    public List<ExpenseSummary>   Summary           { get; private set; } = new();
    public List<CategorySummary>  CategoryBreakdown { get; private set; } = new();
    public List<Expense>          RecentExpenses    { get; private set; } = new();
    public string                 ErrorMessage      { get; private set; } = "";

    public IndexModel(ExpenseRepository repo, ILogger<IndexModel> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Summary           = await _repo.GetExpenseSummaryAsync();
            CategoryBreakdown = await _repo.GetExpensesByCategoryAsync();
            var all           = await _repo.GetExpensesAsync();
            RecentExpenses    = all.Take(10).ToList();
        }
        catch (Exception ex)
        {
            // Log full details server-side; show only a safe message to the user
            _logger.LogError(ex, "Dashboard data load failed");
            ErrorMessage = "Could not connect to the database – showing demo data. " +
                           "Check that SQL_SERVER_FQDN is set and the managed identity has database access.";
            RecentExpenses = await _repo.GetExpensesAsync();
        }
    }
}
