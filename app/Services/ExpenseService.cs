using ExpenseManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null);
    Task<Expense?> GetExpenseByIdAsync(int expenseId);
    Task<int> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> UpdateExpenseAsync(UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(int expenseId);
    Task<bool> SubmitExpenseAsync(int expenseId);
    Task<bool> ApproveExpenseAsync(ApproveExpenseRequest request);
    Task<List<User>> GetUsersAsync();
    Task<List<Category>> GetCategoriesAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
    string? GetDatabaseError();
}

public class ExpenseService : IExpenseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpenseService> _logger;
    private string? _databaseError;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetDatabaseError() => _databaseError;

    private async Task<SqlConnection> GetConnectionAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string not configured");
        }

        var connection = new SqlConnection(connectionString);
        
        try
        {
            await connection.OpenAsync();
            _databaseError = null;
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to database");
            _databaseError = $"Database connection failed: {ex.Message}";
            throw;
        }
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                SELECT e.ExpenseId, e.UserId, u.UserName, e.CategoryId, c.CategoryName,
                       e.StatusId, s.StatusName, e.AmountMinor, e.Currency, e.ExpenseDate,
                       e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy,
                       r.UserName as ReviewedByName, e.ReviewedAt, e.CreatedAt
                FROM Expenses e
                JOIN Users u ON e.UserId = u.UserId
                JOIN ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN Users r ON e.ReviewedBy = r.UserId
                WHERE (@UserId IS NULL OR e.UserId = @UserId)
                  AND (@StatusId IS NULL OR e.StatusId = @StatusId)
                ORDER BY e.CreatedAt DESC", connection);

            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(new Expense
                {
                    ExpenseId = reader.GetInt32("ExpenseId"),
                    UserId = reader.GetInt32("UserId"),
                    UserName = reader.GetString("UserName"),
                    CategoryId = reader.GetInt32("CategoryId"),
                    CategoryName = reader.GetString("CategoryName"),
                    StatusId = reader.GetInt32("StatusId"),
                    StatusName = reader.GetString("StatusName"),
                    Amount = reader.GetInt32("AmountMinor") / 100m,
                    Currency = reader.GetString("Currency"),
                    ExpenseDate = reader.GetDateTime("ExpenseDate"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                    ReceiptFile = reader.IsDBNull("ReceiptFile") ? null : reader.GetString("ReceiptFile"),
                    SubmittedAt = reader.IsDBNull("SubmittedAt") ? null : reader.GetDateTime("SubmittedAt"),
                    ReviewedBy = reader.IsDBNull("ReviewedBy") ? null : reader.GetInt32("ReviewedBy"),
                    ReviewedByName = reader.IsDBNull("ReviewedByName") ? null : reader.GetString("ReviewedByName"),
                    ReviewedAt = reader.IsDBNull("ReviewedAt") ? null : reader.GetDateTime("ReviewedAt"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expenses, using dummy data");
            return GetDummyExpenses(userId, statusId);
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                SELECT e.ExpenseId, e.UserId, u.UserName, e.CategoryId, c.CategoryName,
                       e.StatusId, s.StatusName, e.AmountMinor, e.Currency, e.ExpenseDate,
                       e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy,
                       r.UserName as ReviewedByName, e.ReviewedAt, e.CreatedAt
                FROM Expenses e
                JOIN Users u ON e.UserId = u.UserId
                JOIN ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN Users r ON e.ReviewedBy = r.UserId
                WHERE e.ExpenseId = @ExpenseId", connection);

            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Expense
                {
                    ExpenseId = reader.GetInt32("ExpenseId"),
                    UserId = reader.GetInt32("UserId"),
                    UserName = reader.GetString("UserName"),
                    CategoryId = reader.GetInt32("CategoryId"),
                    CategoryName = reader.GetString("CategoryName"),
                    StatusId = reader.GetInt32("StatusId"),
                    StatusName = reader.GetString("StatusName"),
                    Amount = reader.GetInt32("AmountMinor") / 100m,
                    Currency = reader.GetString("Currency"),
                    ExpenseDate = reader.GetDateTime("ExpenseDate"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                    ReceiptFile = reader.IsDBNull("ReceiptFile") ? null : reader.GetString("ReceiptFile"),
                    SubmittedAt = reader.IsDBNull("SubmittedAt") ? null : reader.GetDateTime("SubmittedAt"),
                    ReviewedBy = reader.IsDBNull("ReviewedBy") ? null : reader.GetInt32("ReviewedBy"),
                    ReviewedByName = reader.IsDBNull("ReviewedByName") ? null : reader.GetString("ReviewedByName"),
                    ReviewedAt = reader.IsDBNull("ReviewedAt") ? null : reader.GetDateTime("ReviewedAt"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expense by ID, using dummy data");
            return GetDummyExpenses(null, null).FirstOrDefault(e => e.ExpenseId == expenseId);
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                INSERT INTO Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, 
                                     ExpenseDate, Description, ReceiptFile, CreatedAt)
                VALUES (@UserId, @CategoryId, 1, @AmountMinor, 'GBP', 
                        @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);", connection);

            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            return (int)(await command.ExecuteScalarAsync() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create expense");
            return -1;
        }
    }

    public async Task<bool> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                UPDATE Expenses 
                SET CategoryId = @CategoryId,
                    AmountMinor = @AmountMinor,
                    ExpenseDate = @ExpenseDate,
                    Description = @Description,
                    ReceiptFile = @ReceiptFile
                WHERE ExpenseId = @ExpenseId AND StatusId = 1", connection);

            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update expense");
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(
                "DELETE FROM Expenses WHERE ExpenseId = @ExpenseId AND StatusId = 1", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expense");
            return false;
        }
    }

    public async Task<bool> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                UPDATE Expenses 
                SET StatusId = 2, SubmittedAt = SYSUTCDATETIME()
                WHERE ExpenseId = @ExpenseId AND StatusId = 1", connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit expense");
            return false;
        }
    }

    public async Task<bool> ApproveExpenseAsync(ApproveExpenseRequest request)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                UPDATE Expenses 
                SET StatusId = @StatusId,
                    ReviewedBy = @ManagerId,
                    ReviewedAt = SYSUTCDATETIME()
                WHERE ExpenseId = @ExpenseId AND StatusId = 2", connection);

            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@StatusId", request.Approved ? 3 : 4);
            command.Parameters.AddWithValue("@ManagerId", request.ManagerId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve/reject expense");
            return false;
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(@"
                SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName, 
                       u.ManagerId, m.UserName as ManagerName, u.IsActive
                FROM Users u
                JOIN Roles r ON u.RoleId = r.RoleId
                LEFT JOIN Users m ON u.ManagerId = m.UserId
                WHERE u.IsActive = 1", connection);

            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32("UserId"),
                    UserName = reader.GetString("UserName"),
                    Email = reader.GetString("Email"),
                    RoleId = reader.GetInt32("RoleId"),
                    RoleName = reader.GetString("RoleName"),
                    ManagerId = reader.IsDBNull("ManagerId") ? null : reader.GetInt32("ManagerId"),
                    ManagerName = reader.IsDBNull("ManagerName") ? null : reader.GetString("ManagerName"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users, using dummy data");
            return GetDummyUsers();
        }
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(
                "SELECT CategoryId, CategoryName, IsActive FROM ExpenseCategories WHERE IsActive = 1", 
                connection);

            var categories = new List<Category>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = reader.GetInt32("CategoryId"),
                    CategoryName = reader.GetString("CategoryName"),
                    IsActive = reader.GetBoolean("IsActive")
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get categories, using dummy data");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        try
        {
            using var connection = await GetConnectionAsync();
            using var command = new SqlCommand(
                "SELECT StatusId, StatusName FROM ExpenseStatus", connection);

            var statuses = new List<ExpenseStatus>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32("StatusId"),
                    StatusName = reader.GetString("StatusName")
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statuses, using dummy data");
            return GetDummyStatuses();
        }
    }

    // Dummy data methods
    private List<Expense> GetDummyExpenses(int? userId, int? statusId)
    {
        var allExpenses = new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                Amount = 25.40m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi from airport to client site",
                ReceiptFile = "/receipts/alice/taxi_oct20.jpg",
                SubmittedAt = DateTime.Now.AddDays(-4),
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                Amount = 14.25m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-30),
                Description = "Client lunch meeting",
                ReceiptFile = "/receipts/alice/lunch_sep15.jpg",
                SubmittedAt = DateTime.Now.AddDays(-29),
                ReviewedBy = 2,
                ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-28),
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 1,
                StatusName = "Draft",
                Amount = 7.99m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-1),
                Description = "Office stationery",
                CreatedAt = DateTime.Now.AddDays(-1)
            }
        };

        return allExpenses
            .Where(e => !userId.HasValue || e.UserId == userId.Value)
            .Where(e => !statusId.HasValue || e.StatusId == statusId.Value)
            .ToList();
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleId = 1,
                RoleName = "Employee",
                ManagerId = 2,
                ManagerName = "Bob Manager",
                IsActive = true
            },
            new User
            {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleId = 2,
                RoleName = "Manager",
                IsActive = true
            }
        };
    }

    private List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new Category { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }
}
