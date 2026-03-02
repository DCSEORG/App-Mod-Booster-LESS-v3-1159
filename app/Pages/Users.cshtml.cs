// Pages/Users.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Data;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class UsersModel : PageModel
{
    private readonly ExpenseRepository _repo;

    public List<User> AllUsers    { get; private set; } = new();
    public List<Role> Roles       { get; private set; } = new();
    public User?      EditUser    { get; private set; }
    public bool       ShowForm    { get; private set; }
    public string     ErrorMessage { get; private set; } = "";

    public UsersModel(ExpenseRepository repo) => _repo = repo;

    public async Task OnGetAsync(int? userId = null, string action = "")
    {
        try
        {
            AllUsers = await _repo.GetUsersAsync();
            Roles    = await _repo.GetRolesAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }

        if (action == "create")
        {
            ShowForm = true;
            return;
        }

        if (action == "edit" && userId.HasValue)
        {
            EditUser = AllUsers.FirstOrDefault(u => u.UserId == userId);
            ShowForm = true;
        }
    }

    public async Task<IActionResult> OnPostAsync(
        string  formAction,
        int?    userId,
        string? userName,
        string? email,
        int     roleId,
        int?    managerId,
        bool    isActive = true)
    {
        switch (formAction)
        {
            case "save":
                if (userId.HasValue && userId > 0)
                {
                    await _repo.UpdateUserAsync(userId.Value, new UpdateUserRequest
                    {
                        UserName  = userName ?? "",
                        Email     = email ?? "",
                        RoleId    = roleId,
                        ManagerId = managerId,
                        IsActive  = isActive,
                    });
                }
                else
                {
                    await _repo.CreateUserAsync(new CreateUserRequest
                    {
                        UserName  = userName ?? "",
                        Email     = email ?? "",
                        RoleId    = roleId,
                        ManagerId = managerId,
                    });
                }
                break;

            case "delete":
                if (userId.HasValue) await _repo.DeleteUserAsync(userId.Value);
                break;
        }

        return RedirectToPage("/Users");
    }
}
