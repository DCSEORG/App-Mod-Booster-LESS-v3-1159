// Pages/Expenses.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class ExpensesModel : PageModel
{
    private readonly ExpenseRepository _repo;

    public List<Expense>         Expenses         { get; private set; } = new();
    public List<User>            Users            { get; private set; } = new();
    public List<ExpenseCategory> Categories       { get; private set; } = new();
    public List<ExpenseStatus>   Statuses         { get; private set; } = new();
    public Expense?              DetailExpense    { get; private set; }
    public Expense?              EditExpense      { get; private set; }
    public bool                  ShowForm         { get; private set; }
    public int?                  FilterStatusId   { get; private set; }
    public int?                  FilterUserId     { get; private set; }
    public int?                  FilterCategoryId { get; private set; }
    public string                ErrorMessage     { get; private set; } = "";

    public ExpensesModel(ExpenseRepository repo) => _repo = repo;

    public async Task OnGetAsync(
        int?   statusId   = null,
        int?   userId     = null,
        int?   categoryId = null,
        int?   expenseId  = null,
        string action     = "")
    {
        FilterStatusId   = statusId;
        FilterUserId     = userId;
        FilterCategoryId = categoryId;

        await LoadLookupsAsync();

        if (action == "create")
        {
            ShowForm = true;
            return;
        }

        if (action == "edit" && expenseId.HasValue)
        {
            EditExpense = await _repo.GetExpenseByIdAsync(expenseId.Value);
            ShowForm    = true;
            return;
        }

        if (expenseId.HasValue)
        {
            DetailExpense = await _repo.GetExpenseByIdAsync(expenseId.Value);
            return;
        }

        Expenses = await _repo.GetExpensesAsync(statusId, userId, categoryId);
    }

    public async Task<IActionResult> OnPostAsync(
        string  formAction,
        int?    expenseId,
        int     userId,
        int     categoryId,
        decimal amountGbp,
        string? expenseDate,
        string? description,
        string? receiptFile,
        int?    reviewedBy)
    {
        await LoadLookupsAsync();

        switch (formAction)
        {
            case "save":
                if (expenseId.HasValue && expenseId > 0)
                {
                    await _repo.UpdateExpenseAsync(expenseId.Value, new UpdateExpenseRequest
                    {
                        CategoryId  = categoryId,
                        AmountGBP   = amountGbp,
                        ExpenseDate = expenseDate ?? DateTime.Today.ToString("yyyy-MM-dd"),
                        Description = description,
                        ReceiptFile = receiptFile,
                    });
                }
                else
                {
                    await _repo.CreateExpenseAsync(new CreateExpenseRequest
                    {
                        UserId      = userId,
                        CategoryId  = categoryId,
                        AmountGBP   = amountGbp,
                        ExpenseDate = expenseDate ?? DateTime.Today.ToString("yyyy-MM-dd"),
                        Description = description,
                        ReceiptFile = receiptFile,
                    });
                }
                break;

            case "submit":
                if (expenseId.HasValue) await _repo.SubmitExpenseAsync(expenseId.Value);
                break;

            case "approve":
                if (expenseId.HasValue && reviewedBy.HasValue)
                    await _repo.ApproveExpenseAsync(expenseId.Value, reviewedBy.Value);
                break;

            case "reject":
                if (expenseId.HasValue && reviewedBy.HasValue)
                    await _repo.RejectExpenseAsync(expenseId.Value, reviewedBy.Value);
                break;

            case "delete":
                if (expenseId.HasValue) await _repo.DeleteExpenseAsync(expenseId.Value);
                break;
        }

        return RedirectToPage("/Expenses");
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            Users      = await _repo.GetUsersAsync();
            Categories = await _repo.GetActiveCategoriesAsync();
            Statuses   = await _repo.GetStatusesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
