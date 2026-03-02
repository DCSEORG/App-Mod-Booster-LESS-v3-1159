// Controllers/UsersController.cs
// prompt-008-create-api-endpoints
// REST API for Users

using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Controllers;

/// <summary>Users REST API</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly ExpenseRepository _repo;
    public UsersController(ExpenseRepository repo) => _repo = repo;

    /// <summary>List all users.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _repo.GetUsersAsync();
        return Ok(users);
    }

    /// <summary>Get a user by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _repo.GetUserByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>Create a new user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var id = await _repo.CreateUserAsync(req);
        if (id < 0) return StatusCode(503, new { message = "Database unavailable – running in demo mode." });
        return CreatedAtAction(nameof(GetById), new { id }, new { userId = id });
    }

    /// <summary>Update an existing user.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var rows = await _repo.UpdateUserAsync(id, req);
        return rows == 0 ? NotFound() : Ok(new { rowsAffected = rows });
    }

    /// <summary>Deactivate (soft-delete) a user.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var rows = await _repo.DeleteUserAsync(id);
        return rows == 0 ? NotFound() : NoContent();
    }
}
