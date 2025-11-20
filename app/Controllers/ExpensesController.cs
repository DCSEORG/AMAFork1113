using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by user or status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses(
        [FromQuery] int? userId = null, 
        [FromQuery] int? statusId = null)
    {
        try
        {
            var expenses = await _expenseService.GetExpensesAsync(userId, statusId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return StatusCode(500, new { error = "Failed to retrieve expenses" });
        }
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        try
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
                return NotFound(new { error = "Expense not found" });

            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to retrieve expense" });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _expenseService.CreateExpenseAsync(request);
            if (expenseId <= 0)
                return BadRequest(new { error = "Failed to create expense" });

            return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, new { expenseId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = "Failed to create expense" });
        }
    }

    /// <summary>
    /// Update an existing expense (only drafts can be updated)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            if (id != request.ExpenseId)
                return BadRequest(new { error = "Expense ID mismatch" });

            var success = await _expenseService.UpdateExpenseAsync(request);
            if (!success)
                return BadRequest(new { error = "Failed to update expense. Only draft expenses can be updated." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to update expense" });
        }
    }

    /// <summary>
    /// Delete an expense (only drafts can be deleted)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var success = await _expenseService.DeleteExpenseAsync(id);
            if (!success)
                return BadRequest(new { error = "Failed to delete expense. Only draft expenses can be deleted." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to delete expense" });
        }
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        try
        {
            var success = await _expenseService.SubmitExpenseAsync(id);
            if (!success)
                return BadRequest(new { error = "Failed to submit expense. Only draft expenses can be submitted." });

            return Ok(new { message = "Expense submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to submit expense" });
        }
    }

    /// <summary>
    /// Approve or reject an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] ApproveExpenseRequest request)
    {
        try
        {
            if (id != request.ExpenseId)
                return BadRequest(new { error = "Expense ID mismatch" });

            var success = await _expenseService.ApproveExpenseAsync(request);
            if (!success)
                return BadRequest(new { error = "Failed to approve/reject expense. Only submitted expenses can be approved or rejected." });

            return Ok(new { message = request.Approved ? "Expense approved successfully" : "Expense rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to approve/reject expense" });
        }
    }
}
