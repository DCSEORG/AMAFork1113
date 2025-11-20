using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages.Expenses;

public class ListModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public ListModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? FilterUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterStatusId { get; set; }

    public async Task OnGetAsync()
    {
        var dbError = _expenseService.GetDatabaseError();
        if (!string.IsNullOrEmpty(dbError))
        {
            ViewData["DatabaseError"] = dbError;
        }

        Expenses = await _expenseService.GetExpensesAsync(FilterUserId, FilterStatusId);
        Users = await _expenseService.GetUsersAsync();
        Statuses = await _expenseService.GetStatusesAsync();
    }
}
