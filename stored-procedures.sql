-- stored-procedures.sql
-- prompt-006-create-stored-procedures
-- CRUD stored procedures for the Expense Management System (Northwind DB)
-- All application data access MUST go through these procedures only.

SET NOCOUNT ON;
GO

-- ════════════════════════════════════════════════════════════════════════════
--  ROLES
-- ════════════════════════════════════════════════════════════════════════════

CREATE OR ALTER PROCEDURE dbo.GetRoles
AS
BEGIN
    SET NOCOUNT ON;
    SELECT RoleId, RoleName, Description
    FROM   dbo.Roles
    ORDER  BY RoleName;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetRoleById
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT RoleId, RoleName, Description
    FROM   dbo.Roles
    WHERE  RoleId = @RoleId;
END
GO

-- ════════════════════════════════════════════════════════════════════════════
--  USERS
-- ════════════════════════════════════════════════════════════════════════════

CREATE OR ALTER PROCEDURE dbo.GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  u.UserId, u.UserName, u.Email,
            u.RoleId, r.RoleName,
            u.ManagerId, m.UserName AS ManagerName,
            u.IsActive, u.CreatedAt
    FROM    dbo.Users u
    JOIN    dbo.Roles r ON r.RoleId = u.RoleId
    LEFT JOIN dbo.Users m ON m.UserId = u.ManagerId
    ORDER  BY u.UserName;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  u.UserId, u.UserName, u.Email,
            u.RoleId, r.RoleName,
            u.ManagerId, m.UserName AS ManagerName,
            u.IsActive, u.CreatedAt
    FROM    dbo.Users u
    JOIN    dbo.Roles r ON r.RoleId = u.RoleId
    LEFT JOIN dbo.Users m ON m.UserId = u.ManagerId
    WHERE   u.UserId = @UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.CreateUser
    @UserName  NVARCHAR(100),
    @Email     NVARCHAR(255),
    @RoleId    INT,
    @ManagerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Users (UserName, Email, RoleId, ManagerId, IsActive, CreatedAt)
    VALUES (@UserName, @Email, @RoleId, @ManagerId, 1, SYSUTCDATETIME());
    SELECT SCOPE_IDENTITY() AS UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.UpdateUser
    @UserId    INT,
    @UserName  NVARCHAR(100),
    @Email     NVARCHAR(255),
    @RoleId    INT,
    @ManagerId INT = NULL,
    @IsActive  BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    UserName  = @UserName,
           Email     = @Email,
           RoleId    = @RoleId,
           ManagerId = @ManagerId,
           IsActive  = @IsActive
    WHERE  UserId = @UserId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE dbo.DeleteUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET IsActive = 0 WHERE UserId = @UserId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ════════════════════════════════════════════════════════════════════════════
--  EXPENSE CATEGORIES
-- ════════════════════════════════════════════════════════════════════════════

CREATE OR ALTER PROCEDURE dbo.GetExpenseCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive
    FROM   dbo.ExpenseCategories
    ORDER  BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetActiveExpenseCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName
    FROM   dbo.ExpenseCategories
    WHERE  IsActive = 1
    ORDER  BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE dbo.CreateExpenseCategory
    @CategoryName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ExpenseCategories (CategoryName, IsActive)
    VALUES (@CategoryName, 1);
    SELECT SCOPE_IDENTITY() AS CategoryId;
END
GO

CREATE OR ALTER PROCEDURE dbo.UpdateExpenseCategory
    @CategoryId   INT,
    @CategoryName NVARCHAR(100),
    @IsActive     BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ExpenseCategories
    SET    CategoryName = @CategoryName,
           IsActive     = @IsActive
    WHERE  CategoryId = @CategoryId;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ════════════════════════════════════════════════════════════════════════════
--  EXPENSE STATUS
-- ════════════════════════════════════════════════════════════════════════════

CREATE OR ALTER PROCEDURE dbo.GetExpenseStatuses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StatusId, StatusName
    FROM   dbo.ExpenseStatus
    ORDER  BY StatusId;
END
GO

-- ════════════════════════════════════════════════════════════════════════════
--  EXPENSES
-- ════════════════════════════════════════════════════════════════════════════

CREATE OR ALTER PROCEDURE dbo.GetExpenses
    @StatusId   INT  = NULL,
    @UserId     INT  = NULL,
    @CategoryId INT  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.ExpenseId,
            e.UserId,    u.UserName,
            e.CategoryId, c.CategoryName,
            e.StatusId,   s.StatusName,
            e.AmountMinor,
            CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountGBP,
            e.Currency,
            e.ExpenseDate,
            e.Description,
            e.ReceiptFile,
            e.SubmittedAt,
            e.ReviewedBy,
            rv.UserName AS ReviewedByName,
            e.ReviewedAt,
            e.CreatedAt
    FROM    dbo.Expenses e
    JOIN    dbo.Users            u  ON  u.UserId     = e.UserId
    JOIN    dbo.ExpenseCategories c  ON  c.CategoryId = e.CategoryId
    JOIN    dbo.ExpenseStatus    s  ON  s.StatusId   = e.StatusId
    LEFT JOIN dbo.Users          rv ON  rv.UserId    = e.ReviewedBy
    WHERE   (@StatusId   IS NULL OR e.StatusId   = @StatusId)
      AND   (@UserId     IS NULL OR e.UserId     = @UserId)
      AND   (@CategoryId IS NULL OR e.CategoryId = @CategoryId)
    ORDER  BY e.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  e.ExpenseId,
            e.UserId,    u.UserName,
            e.CategoryId, c.CategoryName,
            e.StatusId,   s.StatusName,
            e.AmountMinor,
            CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS AmountGBP,
            e.Currency,
            e.ExpenseDate,
            e.Description,
            e.ReceiptFile,
            e.SubmittedAt,
            e.ReviewedBy,
            rv.UserName AS ReviewedByName,
            e.ReviewedAt,
            e.CreatedAt
    FROM    dbo.Expenses e
    JOIN    dbo.Users            u  ON  u.UserId     = e.UserId
    JOIN    dbo.ExpenseCategories c  ON  c.CategoryId = e.CategoryId
    JOIN    dbo.ExpenseStatus    s  ON  s.StatusId   = e.StatusId
    LEFT JOIN dbo.Users          rv ON  rv.UserId    = e.ReviewedBy
    WHERE   e.ExpenseId = @ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.CreateExpense
    @UserId      INT,
    @CategoryId  INT,
    @AmountMinor INT,
    @Currency    NVARCHAR(3)   = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- New expense always starts as Draft (StatusId = 1)
    DECLARE @DraftStatusId INT = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    INSERT INTO dbo.Expenses
        (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES
        (@UserId, @CategoryId, @DraftStatusId, @AmountMinor, @Currency, @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());

    SELECT SCOPE_IDENTITY() AS ExpenseId;
END
GO

CREATE OR ALTER PROCEDURE dbo.UpdateExpense
    @ExpenseId   INT,
    @CategoryId  INT,
    @AmountMinor INT,
    @Currency    NVARCHAR(3)    = 'GBP',
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Only allow editing of Draft expenses
    UPDATE dbo.Expenses
    SET    CategoryId  = @CategoryId,
           AmountMinor = @AmountMinor,
           Currency    = @Currency,
           ExpenseDate = @ExpenseDate,
           Description = @Description,
           ReceiptFile = @ReceiptFile
    WHERE  ExpenseId = @ExpenseId
      AND  StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE dbo.SubmitExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET    StatusId    = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted'),
           SubmittedAt = SYSUTCDATETIME()
    WHERE  ExpenseId = @ExpenseId
      AND  StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE dbo.ApproveExpense
    @ExpenseId   INT,
    @ReviewedBy  INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET    StatusId   = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved'),
           ReviewedBy = @ReviewedBy,
           ReviewedAt = SYSUTCDATETIME()
    WHERE  ExpenseId = @ExpenseId
      AND  StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE dbo.RejectExpense
    @ExpenseId  INT,
    @ReviewedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Expenses
    SET    StatusId   = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected'),
           ReviewedBy = @ReviewedBy,
           ReviewedAt = SYSUTCDATETIME()
    WHERE  ExpenseId = @ExpenseId
      AND  StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

CREATE OR ALTER PROCEDURE dbo.DeleteExpense
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Only allow deletion of Draft expenses
    DELETE FROM dbo.Expenses
    WHERE  ExpenseId = @ExpenseId
      AND  StatusId  = (SELECT StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft');

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- ════════════════════════════════════════════════════════════════════════════
--  DASHBOARD / REPORTING
-- ════════════════════════════════════════════════════════════════════════════

CREATE OR ALTER PROCEDURE dbo.GetExpenseSummary
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  s.StatusName,
            COUNT(*)                                           AS ExpenseCount,
            CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(12,2)) AS TotalGBP
    FROM    dbo.Expenses e
    JOIN    dbo.ExpenseStatus s ON s.StatusId = e.StatusId
    GROUP  BY s.StatusName
    ORDER  BY s.StatusName;
END
GO

CREATE OR ALTER PROCEDURE dbo.GetExpensesByCategory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  c.CategoryName,
            COUNT(*)                                           AS ExpenseCount,
            CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(12,2)) AS TotalGBP
    FROM    dbo.Expenses e
    JOIN    dbo.ExpenseCategories c ON c.CategoryId = e.CategoryId
    GROUP  BY c.CategoryName
    ORDER  BY TotalGBP DESC;
END
GO
