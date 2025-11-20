using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public IndexModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<Expense> RecentExpenses { get; set; } = new();
    public int TotalExpenses { get; set; }
    public int PendingApprovals { get; set; }
    public decimal TotalAmount { get; set; }
    public int ApprovedCount { get; set; }

    public async Task OnGetAsync()
    {
        var dbError = _expenseService.GetDatabaseError();
        if (!string.IsNullOrEmpty(dbError))
        {
            ViewData["DatabaseError"] = dbError;
        }

        var expenses = await _expenseService.GetExpensesAsync();
        
        RecentExpenses = expenses.Take(10).ToList();
        TotalExpenses = expenses.Count;
        PendingApprovals = expenses.Count(e => e.StatusName == "Submitted");
        TotalAmount = expenses.Where(e => e.StatusName == "Approved").Sum(e => e.Amount);
        ApprovedCount = expenses.Count(e => e.StatusName == "Approved");
    }
}
