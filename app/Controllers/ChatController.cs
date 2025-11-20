using ExpenseManagement.Models;
using ExpenseManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Process a chat message and get AI response
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> ProcessMessage([FromBody] ChatRequest request)
    {
        if (!_chatService.IsEnabled())
        {
            return Ok(new ChatResponse
            {
                Success = false,
                Error = "Chat UI is not enabled",
                Response = "The chat feature is not currently enabled. Please contact your administrator."
            });
        }

        try
        {
            var response = await _chatService.ProcessChatMessageAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new ChatResponse
            {
                Success = false,
                Error = "Internal server error",
                Response = "I'm sorry, but I encountered an error. Please try again later."
            });
        }
    }

    /// <summary>
    /// Check if chat is enabled
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new { enabled = _chatService.IsEnabled() });
    }
}
