// Controllers/ExpensesController.cs
// prompt-008-create-api-endpoints
// REST API for Expenses

using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Controllers;

/// <summary>Expenses REST API</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseRepository _repo;
    public ExpensesController(ExpenseRepository repo) => _repo = repo;

    /// <summary>List all expenses, optionally filtered by status, user, or category.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Expense>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? statusId   = null,
        [FromQuery] int? userId     = null,
        [FromQuery] int? categoryId = null)
    {
        var expenses = await _repo.GetExpensesAsync(statusId, userId, categoryId);
        return Ok(expenses);
    }

    /// <summary>Get a single expense by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Expense), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var expense = await _repo.GetExpenseByIdAsync(id);
        return expense is null ? NotFound() : Ok(expense);
    }

    /// <summary>Create a new draft expense.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = await _repo.CreateExpenseAsync(req);
        if (id < 0) return StatusCode(503, new { message = "Database unavailable – running in demo mode." });
        return CreatedAtAction(nameof(GetById), new { id }, new { expenseId = id });
    }

    /// <summary>Update a draft expense.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var rows = await _repo.UpdateExpenseAsync(id, req);
        return rows == 0 ? NotFound(new { message = "Expense not found or not in Draft status." }) : Ok(new { rowsAffected = rows });
    }

    /// <summary>Submit a draft expense for manager review.</summary>
    [HttpPost("{id:int}/submit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(int id)
    {
        var rows = await _repo.SubmitExpenseAsync(id);
        return rows == 0 ? NotFound(new { message = "Expense not found or not in Draft status." }) : Ok(new { rowsAffected = rows });
    }

    /// <summary>Approve a submitted expense.</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id, [FromBody] ReviewExpenseRequest req)
    {
        var rows = await _repo.ApproveExpenseAsync(id, req.ReviewedBy);
        return rows == 0 ? NotFound(new { message = "Expense not found or not in Submitted status." }) : Ok(new { rowsAffected = rows });
    }

    /// <summary>Reject a submitted expense.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id, [FromBody] ReviewExpenseRequest req)
    {
        var rows = await _repo.RejectExpenseAsync(id, req.ReviewedBy);
        return rows == 0 ? NotFound(new { message = "Expense not found or not in Submitted status." }) : Ok(new { rowsAffected = rows });
    }

    /// <summary>Delete a draft expense.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var rows = await _repo.DeleteExpenseAsync(id);
        return rows == 0 ? NotFound(new { message = "Expense not found or not in Draft status." }) : NoContent();
    }

    /// <summary>Get expense summary by status.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _repo.GetExpenseSummaryAsync();
        return Ok(summary);
    }

    /// <summary>Get expense totals grouped by category.</summary>
    [HttpGet("by-category")]
    [ProducesResponseType(typeof(IEnumerable<CategorySummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory()
    {
        var summary = await _repo.GetExpensesByCategoryAsync();
        return Ok(summary);
    }
}
