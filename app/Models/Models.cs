// Models/Expense.cs – domain models used by API and Razor Pages

namespace ExpenseManagement.Models;

public class Expense
{
    public int     ExpenseId       { get; set; }
    public int     UserId          { get; set; }
    public string  UserName        { get; set; } = "";
    public int     CategoryId      { get; set; }
    public string  CategoryName    { get; set; } = "";
    public int     StatusId        { get; set; }
    public string  StatusName      { get; set; } = "";
    public int     AmountMinor     { get; set; }           // pence
    public decimal AmountGBP       { get; set; }           // £
    public string  Currency        { get; set; } = "GBP";
    public DateTime ExpenseDate    { get; set; }
    public string? Description     { get; set; }
    public string? ReceiptFile     { get; set; }
    public DateTime? SubmittedAt   { get; set; }
    public int?    ReviewedBy      { get; set; }
    public string? ReviewedByName  { get; set; }
    public DateTime? ReviewedAt    { get; set; }
    public DateTime CreatedAt      { get; set; }
}

public class CreateExpenseRequest
{
    public int     UserId      { get; set; }
    public int     CategoryId  { get; set; }
    public decimal AmountGBP   { get; set; }   // converted to pence server-side
    public string  ExpenseDate { get; set; } = "";
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
}

public class UpdateExpenseRequest
{
    public int     CategoryId  { get; set; }
    public decimal AmountGBP   { get; set; }
    public string  ExpenseDate { get; set; } = "";
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
}

public class ReviewExpenseRequest
{
    public int ReviewedBy { get; set; }
}

public class User
{
    public int     UserId      { get; set; }
    public string  UserName    { get; set; } = "";
    public string  Email       { get; set; } = "";
    public int     RoleId      { get; set; }
    public string  RoleName    { get; set; } = "";
    public int?    ManagerId   { get; set; }
    public string? ManagerName { get; set; }
    public bool    IsActive    { get; set; }
    public DateTime CreatedAt  { get; set; }
}

public class CreateUserRequest
{
    public string  UserName  { get; set; } = "";
    public string  Email     { get; set; } = "";
    public int     RoleId    { get; set; }
    public int?    ManagerId { get; set; }
}

public class UpdateUserRequest
{
    public string  UserName  { get; set; } = "";
    public string  Email     { get; set; } = "";
    public int     RoleId    { get; set; }
    public int?    ManagerId { get; set; }
    public bool    IsActive  { get; set; }
}

public class Role
{
    public int    RoleId      { get; set; }
    public string RoleName    { get; set; } = "";
    public string? Description { get; set; }
}

public class ExpenseCategory
{
    public int    CategoryId   { get; set; }
    public string CategoryName { get; set; } = "";
    public bool   IsActive     { get; set; }
}

public class ExpenseStatus
{
    public int    StatusId   { get; set; }
    public string StatusName { get; set; } = "";
}

public class ExpenseSummary
{
    public string  StatusName    { get; set; } = "";
    public int     ExpenseCount  { get; set; }
    public decimal TotalGBP      { get; set; }
}

public class CategorySummary
{
    public string  CategoryName  { get; set; } = "";
    public int     ExpenseCount  { get; set; }
    public decimal TotalGBP      { get; set; }
}
