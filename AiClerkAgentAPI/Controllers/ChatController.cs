using AiClerkAgentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Caching.Memory;
using AiClerkAgentApi.Models;

namespace AiClerkAgentAPI.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatCompletionService _chatService;
        private readonly Kernel _kernel;
        private readonly IMemoryCache _cache;
        private readonly OpenAIPromptExecutionSettings _settings;
        private readonly ChatSettings _chatSettings;

        private readonly MemoryCacheEntryOptions _chatCacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30) 
        };

        public ChatController(IChatCompletionService chatService, Kernel kernel, IMemoryCache cache, ChatSettings chatSettings)
        {
            _chatService = chatService;
            _kernel = kernel;
            _cache = cache;
            _chatSettings = chatSettings;
            _settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            if (string.IsNullOrWhiteSpace(_chatSettings.SystemPrompt))
                throw new ArgumentException("SystemPrompt darf nicht leer sein.");
        }

        [HttpPost]
        public async Task<IActionResult> PostChatAsync([FromBody] ChatRequest request)
        {
            string conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString()
                : request.ConversationId;

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Die Nachricht darf nicht leer sein.");

            if (!_cache.TryGetValue(conversationId, out ChatHistory history))
            {
                history = new ChatHistory();
                history.AddSystemMessage(_chatSettings.SystemPrompt);
            }

            history.AddUserMessage(request.Message);

            var responses = await _chatService.GetChatMessageContentsAsync(history, _settings, _kernel);
            var reply = responses.First().Content ?? string.Empty;

            history.AddAssistantMessage(reply);
            _cache.Set(conversationId, history, _chatCacheOptions); 

            return Ok(new
            {
                ConversationId = conversationId,
                Reply = reply
            });
        }

        [HttpDelete("{conversationId}")]
        public IActionResult DeleteChatHistory(string conversationId)
        {
            _cache.Remove(conversationId);
            return NoContent();
        }
    }
}
