using AiClerkAgentAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Caching.Memory;
using AiClerkAgentApi.Models;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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

        public ChatController( IChatCompletionService chatService, Kernel kernel, IMemoryCache cache)
        {
            _chatService = chatService;
            _kernel = kernel;
            _cache = cache;
            _settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
        }

        [HttpPost]
        public async Task<IActionResult> PostChatAsync([FromBody] ChatRequest request)
        {
            // Generiere eine neue ConversationId auf Serverseite, falls nicht vorhanden
            string conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString()
                : request.ConversationId;

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Die Nachricht darf nicht leer sein.");
            }

            // Versuche, bestehende Chatgeschichte zu laden
            if (!_cache.TryGetValue(conversationId, out ChatHistory history))
            {
                // Neue Session: ChatHistory anlegen und System-Prompt setzen
                history = new ChatHistory();
                history.AddSystemMessage("Du bist ein effizienter Chat-Assistent, der mit einer einzigen Nachfrage zielgerichtet hilft.");
            }

            // Neue User-Nachricht zur History hinzufügen
            history.AddUserMessage(request.Message);

            // Anfrage an Semantic Kernel / OpenAI schicken
            var responses = await _chatService.GetChatMessageContentsAsync(history, _settings, _kernel);
            var reply = responses.First().Content ?? string.Empty;

            // Assistant-Nachricht zur History hinzufügen
            history.AddAssistantMessage(reply);

            // Aktualisierte History im Cache speichern
            _cache.Set(conversationId, history);

            // Define the response payload including the conversationId for the client
            var result = new
            {
                ConversationId = conversationId,
                Reply = reply
            };

            return Ok(result);
        }

        [HttpDelete("{conversationId}")]
        public IActionResult DeleteChatHistory(string conversationId)
        {
            // 1) Cache-Eintrag löschen
            _cache.Remove(conversationId);
            return NoContent();
        }
    }
}