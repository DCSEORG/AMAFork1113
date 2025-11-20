using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages.Expenses;

public class CreateModel : PageModel
{
    private readonly IExpenseService _expenseService;

    public CreateModel(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    public List<User> Users { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty]
    public CreateExpenseRequest NewExpense { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        var dbError = _expenseService.GetDatabaseError();
        if (!string.IsNullOrEmpty(dbError))
        {
            ViewData["DatabaseError"] = dbError;
        }

        Users = await _expenseService.GetUsersAsync();
        Categories = await _expenseService.GetCategoriesAsync();

        // Set default values
        if (Users.Any())
        {
            NewExpense.UserId = Users.First().UserId;
        }
        if (Categories.Any())
        {
            NewExpense.CategoryId = Categories.First().CategoryId;
        }
        NewExpense.ExpenseDate = DateTime.Now.Date;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Users = await _expenseService.GetUsersAsync();
            Categories = await _expenseService.GetCategoriesAsync();
            return Page();
        }

        var expenseId = await _expenseService.CreateExpenseAsync(NewExpense);
        
        if (expenseId > 0)
        {
            Message = "Expense created successfully!";
            return RedirectToPage("/Index");
        }

        ModelState.AddModelError("", "Failed to create expense");
        Users = await _expenseService.GetUsersAsync();
        Categories = await _expenseService.GetCategoriesAsync();
        return Page();
    }
}
