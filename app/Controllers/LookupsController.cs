// Controllers/LookupsController.cs
// prompt-008-create-api-endpoints
// REST API for lookup data (roles, categories, statuses)

using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Controllers;

/// <summary>Lookup data API (roles, categories, statuses)</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LookupsController : ControllerBase
{
    private readonly ExpenseRepository _repo;
    public LookupsController(ExpenseRepository repo) => _repo = repo;

    /// <summary>Get all roles.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _repo.GetRolesAsync();
        return Ok(roles);
    }

    /// <summary>Get all expense categories.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _repo.GetCategoriesAsync();
        return Ok(cats);
    }

    /// <summary>Get active expense categories only.</summary>
    [HttpGet("categories/active")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseCategory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveCategories()
    {
        var cats = await _repo.GetActiveCategoriesAsync();
        return Ok(cats);
    }

    /// <summary>Get all expense statuses.</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(IEnumerable<ExpenseStatus>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatuses()
    {
        var statuses = await _repo.GetStatusesAsync();
        return Ok(statuses);
    }
}
