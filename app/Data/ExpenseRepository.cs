// Data/ExpenseRepository.cs
// All database access goes through stored procedures only.
// Returns dummy data when the database connection is unavailable.

using System.Data;
using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;

namespace ExpenseManagement.Data;

public class ExpenseRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseRepository> _logger;

    // Dummy data used when DB is unreachable
    private static readonly List<Expense> _dummyExpenses = new()
    {
        new Expense { ExpenseId=1, UserId=1, UserName="Alice Example",   CategoryId=1, CategoryName="Travel",   StatusId=2, StatusName="Submitted", AmountMinor=2540, AmountGBP=25.40m, Currency="GBP", ExpenseDate=DateTime.Today.AddDays(-5), Description="Taxi to client site",     CreatedAt=DateTime.UtcNow.AddDays(-5) },
        new Expense { ExpenseId=2, UserId=1, UserName="Alice Example",   CategoryId=2, CategoryName="Meals",    StatusId=3, StatusName="Approved",  AmountMinor=1425, AmountGBP=14.25m, Currency="GBP", ExpenseDate=DateTime.Today.AddDays(-15), Description="Client lunch",            CreatedAt=DateTime.UtcNow.AddDays(-15) },
        new Expense { ExpenseId=3, UserId=1, UserName="Alice Example",   CategoryId=3, CategoryName="Supplies", StatusId=1, StatusName="Draft",     AmountMinor=799,  AmountGBP=7.99m,  Currency="GBP", ExpenseDate=DateTime.Today.AddDays(-1),  Description="Office stationery",       CreatedAt=DateTime.UtcNow.AddDays(-1) },
        new Expense { ExpenseId=4, UserId=1, UserName="Alice Example",   CategoryId=4, CategoryName="Accommodation", StatusId=3, StatusName="Approved", AmountMinor=12300, AmountGBP=123.00m, Currency="GBP", ExpenseDate=DateTime.Today.AddDays(-60), Description="Hotel client visit", CreatedAt=DateTime.UtcNow.AddDays(-60) },
    };

    private static readonly List<User> _dummyUsers = new()
    {
        new User { UserId=1, UserName="Alice Example",  Email="alice@example.co.uk",         RoleId=1, RoleName="Employee", ManagerId=2, ManagerName="Bob Manager", IsActive=true, CreatedAt=DateTime.UtcNow.AddDays(-90) },
        new User { UserId=2, UserName="Bob Manager",    Email="bob.manager@example.co.uk",   RoleId=2, RoleName="Manager",  ManagerId=null,                         IsActive=true, CreatedAt=DateTime.UtcNow.AddDays(-90) },
    };

    private static readonly List<Role>            _dummyRoles      = new() { new Role { RoleId=1, RoleName="Employee", Description="Submit expenses" }, new Role { RoleId=2, RoleName="Manager", Description="Approve/reject expenses" } };
    private static readonly List<ExpenseCategory> _dummyCategories = new() { new ExpenseCategory { CategoryId=1, CategoryName="Travel", IsActive=true }, new ExpenseCategory { CategoryId=2, CategoryName="Meals", IsActive=true }, new ExpenseCategory { CategoryId=3, CategoryName="Supplies", IsActive=true }, new ExpenseCategory { CategoryId=4, CategoryName="Accommodation", IsActive=true }, new ExpenseCategory { CategoryId=5, CategoryName="Other", IsActive=true } };
    private static readonly List<ExpenseStatus>   _dummyStatuses   = new() { new ExpenseStatus { StatusId=1, StatusName="Draft" }, new ExpenseStatus { StatusId=2, StatusName="Submitted" }, new ExpenseStatus { StatusId=3, StatusName="Approved" }, new ExpenseStatus { StatusId=4, StatusName="Rejected" } };

    public ExpenseRepository(string connectionString, ILogger<ExpenseRepository> logger)
    {
        _connectionString = connectionString;
        _logger           = logger;
    }

    private bool HasConnection => !string.IsNullOrWhiteSpace(_connectionString);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SqlConnection CreateConnection() => new(_connectionString);

    private static SqlParameter Param(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    // ── Roles ─────────────────────────────────────────────────────────────────

    public async Task<List<Role>> GetRolesAsync()
    {
        if (!HasConnection) return _dummyRoles;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetRoles", conn) { CommandType = CommandType.StoredProcedure };
            var results = new List<Role>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(new Role { RoleId = reader.GetInt32(0), RoleName = reader.GetString(1), Description = reader.IsDBNull(2) ? null : reader.GetString(2) });
            return results;
        }
        catch (Exception ex) { _logger.LogError(ex, "GetRoles failed"); return _dummyRoles; }
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public async Task<List<User>> GetUsersAsync()
    {
        if (!HasConnection) return _dummyUsers;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetUsers", conn) { CommandType = CommandType.StoredProcedure };
            return await ReadUsers(cmd);
        }
        catch (Exception ex) { _logger.LogError(ex, "GetUsers failed"); return _dummyUsers; }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        if (!HasConnection) return _dummyUsers.FirstOrDefault(u => u.UserId == userId);
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetUserById", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(Param("@UserId", userId));
            var list = await ReadUsers(cmd);
            return list.FirstOrDefault();
        }
        catch (Exception ex) { _logger.LogError(ex, "GetUserById failed"); return null; }
    }

    public async Task<int> CreateUserAsync(CreateUserRequest req)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.CreateUser", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(new[] { Param("@UserName", req.UserName), Param("@Email", req.Email), Param("@RoleId", req.RoleId), Param("@ManagerId", req.ManagerId) });
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> UpdateUserAsync(int userId, UpdateUserRequest req)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.UpdateUser", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(new[] { Param("@UserId", userId), Param("@UserName", req.UserName), Param("@Email", req.Email), Param("@RoleId", req.RoleId), Param("@ManagerId", req.ManagerId), Param("@IsActive", req.IsActive) });
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> DeleteUserAsync(int userId)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.DeleteUser", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(Param("@UserId", userId));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task<List<User>> ReadUsers(SqlCommand cmd)
    {
        var list = new List<User>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new User
            {
                UserId      = reader.GetInt32(0),
                UserName    = reader.GetString(1),
                Email       = reader.GetString(2),
                RoleId      = reader.GetInt32(3),
                RoleName    = reader.GetString(4),
                ManagerId   = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsActive    = reader.GetBoolean(7),
                CreatedAt   = reader.GetDateTime(8),
            });
        }
        return list;
    }

    // ── Expense Categories ────────────────────────────────────────────────────

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        if (!HasConnection) return _dummyCategories;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpenseCategories", conn) { CommandType = CommandType.StoredProcedure };
            var list = new List<ExpenseCategory>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new ExpenseCategory { CategoryId = reader.GetInt32(0), CategoryName = reader.GetString(1), IsActive = reader.GetBoolean(2) });
            return list;
        }
        catch (Exception ex) { _logger.LogError(ex, "GetCategories failed"); return _dummyCategories; }
    }

    public async Task<List<ExpenseCategory>> GetActiveCategoriesAsync()
    {
        if (!HasConnection) return _dummyCategories.Where(c => c.IsActive).ToList();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetActiveExpenseCategories", conn) { CommandType = CommandType.StoredProcedure };
            var list = new List<ExpenseCategory>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new ExpenseCategory { CategoryId = reader.GetInt32(0), CategoryName = reader.GetString(1), IsActive = true });
            return list;
        }
        catch (Exception ex) { _logger.LogError(ex, "GetActiveCategories failed"); return _dummyCategories; }
    }

    // ── Expense Statuses ──────────────────────────────────────────────────────

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        if (!HasConnection) return _dummyStatuses;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpenseStatuses", conn) { CommandType = CommandType.StoredProcedure };
            var list = new List<ExpenseStatus>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new ExpenseStatus { StatusId = reader.GetInt32(0), StatusName = reader.GetString(1) });
            return list;
        }
        catch (Exception ex) { _logger.LogError(ex, "GetStatuses failed"); return _dummyStatuses; }
    }

    // ── Expenses ──────────────────────────────────────────────────────────────

    public async Task<List<Expense>> GetExpensesAsync(int? statusId = null, int? userId = null, int? categoryId = null)
    {
        if (!HasConnection) return _dummyExpenses;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpenses", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddRange(new[] { Param("@StatusId", statusId), Param("@UserId", userId), Param("@CategoryId", categoryId) });
            return await ReadExpenses(cmd);
        }
        catch (Exception ex) { _logger.LogError(ex, "GetExpenses failed"); return _dummyExpenses; }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        if (!HasConnection) return _dummyExpenses.FirstOrDefault(e => e.ExpenseId == expenseId);
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpenseById", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(Param("@ExpenseId", expenseId));
            var list = await ReadExpenses(cmd);
            return list.FirstOrDefault();
        }
        catch (Exception ex) { _logger.LogError(ex, "GetExpenseById failed"); return null; }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest req)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.CreateExpense", conn) { CommandType = CommandType.StoredProcedure };
        var amountMinor = (int)Math.Round(req.AmountGBP * 100);
        cmd.Parameters.AddRange(new[] { Param("@UserId", req.UserId), Param("@CategoryId", req.CategoryId), Param("@AmountMinor", amountMinor), Param("@Currency", "GBP"), Param("@ExpenseDate", req.ExpenseDate), Param("@Description", req.Description), Param("@ReceiptFile", req.ReceiptFile) });
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest req)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.UpdateExpense", conn) { CommandType = CommandType.StoredProcedure };
        var amountMinor = (int)Math.Round(req.AmountGBP * 100);
        cmd.Parameters.AddRange(new[] { Param("@ExpenseId", expenseId), Param("@CategoryId", req.CategoryId), Param("@AmountMinor", amountMinor), Param("@Currency", "GBP"), Param("@ExpenseDate", req.ExpenseDate), Param("@Description", req.Description), Param("@ReceiptFile", req.ReceiptFile) });
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.SubmitExpense", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(Param("@ExpenseId", expenseId));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.ApproveExpense", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(new[] { Param("@ExpenseId", expenseId), Param("@ReviewedBy", reviewedBy) });
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.RejectExpense", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.AddRange(new[] { Param("@ExpenseId", expenseId), Param("@ReviewedBy", reviewedBy) });
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        if (!HasConnection) return -1;
        await using var conn = CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("dbo.DeleteExpense", conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(Param("@ExpenseId", expenseId));
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<List<ExpenseSummary>> GetExpenseSummaryAsync()
    {
        if (!HasConnection) return new List<ExpenseSummary>();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpenseSummary", conn) { CommandType = CommandType.StoredProcedure };
            var list = new List<ExpenseSummary>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new ExpenseSummary { StatusName = reader.GetString(0), ExpenseCount = reader.GetInt32(1), TotalGBP = reader.GetDecimal(2) });
            return list;
        }
        catch (Exception ex) { _logger.LogError(ex, "GetExpenseSummary failed"); return new List<ExpenseSummary>(); }
    }

    public async Task<List<CategorySummary>> GetExpensesByCategoryAsync()
    {
        if (!HasConnection) return new List<CategorySummary>();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.GetExpensesByCategory", conn) { CommandType = CommandType.StoredProcedure };
            var list = new List<CategorySummary>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(new CategorySummary { CategoryName = reader.GetString(0), ExpenseCount = reader.GetInt32(1), TotalGBP = reader.GetDecimal(2) });
            return list;
        }
        catch (Exception ex) { _logger.LogError(ex, "GetExpensesByCategory failed"); return new List<CategorySummary>(); }
    }

    private static async Task<List<Expense>> ReadExpenses(SqlCommand cmd)
    {
        var list = new List<Expense>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Expense
            {
                ExpenseId      = reader.GetInt32(0),
                UserId         = reader.GetInt32(1),
                UserName       = reader.GetString(2),
                CategoryId     = reader.GetInt32(3),
                CategoryName   = reader.GetString(4),
                StatusId       = reader.GetInt32(5),
                StatusName     = reader.GetString(6),
                AmountMinor    = reader.GetInt32(7),
                AmountGBP      = reader.GetDecimal(8),
                Currency       = reader.GetString(9),
                ExpenseDate    = reader.GetDateTime(10),
                Description    = reader.IsDBNull(11) ? null : reader.GetString(11),
                ReceiptFile    = reader.IsDBNull(12) ? null : reader.GetString(12),
                SubmittedAt    = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                ReviewedBy     = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                ReviewedByName = reader.IsDBNull(15) ? null : reader.GetString(15),
                ReviewedAt     = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
                CreatedAt      = reader.GetDateTime(17),
            });
        }
        return list;
    }
}
