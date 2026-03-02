// Pages/Index.cshtml.cs – Dashboard page model
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly ExpenseRepository _repo;

    public List<ExpenseSummary>   Summary           { get; private set; } = new();
    public List<CategorySummary>  CategoryBreakdown { get; private set; } = new();
    public List<Expense>          RecentExpenses    { get; private set; } = new();
    public string                 ErrorMessage      { get; private set; } = "";

    public IndexModel(ExpenseRepository repo) => _repo = repo;

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
            ErrorMessage = $"Could not connect to the database. Showing demo data. " +
                           $"({Path.GetFileName(ex.StackTrace?.Split('\n').FirstOrDefault() ?? "")}: {ex.Message})";
            RecentExpenses = await _repo.GetExpensesAsync();
        }
    }
}
