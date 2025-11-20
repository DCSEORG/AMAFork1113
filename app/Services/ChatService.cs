using ExpenseManagement.Models;
using Azure.AI.OpenAI;
using Azure;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessChatMessageAsync(ChatRequest request);
    bool IsEnabled();
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IExpenseService _expenseService;
    private OpenAIClient? _openAIClient;
    private string? _deploymentName;
    private bool _isEnabled;

    public ChatService(
        IConfiguration configuration, 
        ILogger<ChatService> logger,
        IExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        _isEnabled = configuration.GetValue<bool>("EnableChatUI");

        if (_isEnabled)
        {
            InitializeOpenAI();
        }
    }

    public bool IsEnabled() => _isEnabled;

    private void InitializeOpenAI()
    {
        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            var apiKey = _configuration["OpenAI:ApiKey"];
            _deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("OpenAI configuration is incomplete. Chat UI will be disabled.");
                _isEnabled = false;
                return;
            }

            _openAIClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _logger.LogInformation("OpenAI client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI client");
            _isEnabled = false;
        }
    }

    public async Task<ChatResponse> ProcessChatMessageAsync(ChatRequest request)
    {
        if (!_isEnabled || _openAIClient == null)
        {
            return new ChatResponse
            {
                Success = false,
                Error = "Chat UI is not enabled or configured",
                Response = "I'm sorry, but the chat feature is not currently available. Please check the configuration."
            };
        }

        try
        {
            // Build system message with expense management context
            var systemMessage = BuildSystemMessage();

            // Get expense data for context
            var expenses = await _expenseService.GetExpensesAsync();
            var users = await _expenseService.GetUsersAsync();
            var categories = await _expenseService.GetCategoriesAsync();

            // Add data context to the last user message
            var contextMessage = $"\n\nCurrent expense data context:\n" +
                                $"Total expenses: {expenses.Count}\n" +
                                $"Users: {string.Join(", ", users.Select(u => u.UserName))}\n" +
                                $"Categories: {string.Join(", ", categories.Select(c => c.CategoryName))}\n" +
                                $"Recent expenses:\n{JsonSerializer.Serialize(expenses.Take(5), new JsonSerializerOptions { WriteIndented = true })}";

            var options = new ChatCompletionsOptions(_deploymentName, new[]
            {
                new ChatRequestSystemMessage(systemMessage)
            })
            {
                MaxTokens = 1000,
                Temperature = 0.7f,
                NucleusSamplingFactor = 0.95f
            };

            // Add history
            foreach (var msg in request.History)
            {
                if (msg.Role.ToLower() == "user")
                {
                    options.Messages.Add(new ChatRequestUserMessage(msg.Content));
                }
                else
                {
                    options.Messages.Add(new ChatRequestAssistantMessage(msg.Content));
                }
            }

            // Add current message with context
            options.Messages.Add(new ChatRequestUserMessage(request.Message + contextMessage));

            var response = await _openAIClient.GetChatCompletionsAsync(options);
            var assistantMessage = response.Value.Choices[0].Message.Content;

            return new ChatResponse
            {
                Success = true,
                Response = assistantMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResponse
            {
                Success = false,
                Error = ex.Message,
                Response = "I'm sorry, but I encountered an error processing your request. Please try again."
            };
        }
    }

    private string BuildSystemMessage()
    {
        return @"You are an AI assistant for an Expense Management System. You help users manage their expenses by:
1. Answering questions about expenses, approvals, and categories
2. Helping users understand their expense data
3. Providing guidance on submitting and approving expenses
4. Explaining expense status and policies

You have access to the current expense data through the context provided with each message.

When users ask about specific expenses, refer to the data provided. When they ask about actions (like creating or approving expenses), explain how to use the web interface to perform those actions.

Be helpful, concise, and professional. Format currency amounts in GBP (£) when discussing expenses.

Key features of the system:
- Draft expenses: Created but not yet submitted
- Submitted expenses: Pending manager approval
- Approved expenses: Approved by a manager
- Rejected expenses: Rejected by a manager

Users can:
- View their expenses
- Create new expense drafts
- Submit expenses for approval
- Edit draft expenses
- Delete draft expenses

Managers can:
- View all submitted expenses
- Approve or reject submitted expenses
- View expense history";
    }
}
