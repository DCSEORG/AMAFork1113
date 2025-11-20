using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class ApprovalsModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public ApprovalsModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public List<User> Managers { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        var dbError = _expenseService.GetDatabaseError();
        if (!string.IsNullOrEmpty(dbError))
        {
            ViewData["DatabaseError"] = dbError;
        }

        // Get submitted expenses (statusId = 2)
        PendingExpenses = await _expenseService.GetExpensesAsync(null, 2);
        
        var users = await _expenseService.GetUsersAsync();
        Managers = users.Where(u => u.RoleName == "Manager").ToList();
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId, int managerId)
    {
        var request = new ApproveExpenseRequest
        {
            ExpenseId = expenseId,
            ManagerId = managerId,
            Approved = true
        };

        var success = await _expenseService.ApproveExpenseAsync(request);
        
        if (success)
        {
            Message = "Expense approved successfully!";
        }
        else
        {
            Message = "Failed to approve expense.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId, int managerId)
    {
        var request = new ApproveExpenseRequest
        {
            ExpenseId = expenseId,
            ManagerId = managerId,
            Approved = false
        };

        var success = await _expenseService.ApproveExpenseAsync(request);
        
        if (success)
        {
            Message = "Expense rejected successfully!";
        }
        else
        {
            Message = "Failed to reject expense.";
        }

        return RedirectToPage();
    }
}
