using AiClerkAgentAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AiClerkAgentAPI.Controllers
{
    [Route("api/system-prompt")]
    [ApiController]
    public class SystemPromptController : ControllerBase
    {
        private readonly ChatSettings _chatSettings;

        public SystemPromptController(ChatSettings chatSettings)
        {
            _chatSettings = chatSettings;
        }
        [HttpGet]
        public IActionResult Get() =>
            Ok(new { Prompt = _chatSettings.SystemPrompt });

        [HttpPost]
        public IActionResult Set([FromBody] ChatSettings req)
        {
            if (string.IsNullOrWhiteSpace(req.SystemPrompt))
                return BadRequest("Prompt must not be empty.");

            _chatSettings.SystemPrompt = req.SystemPrompt;
            return Ok(new { Message = "System prompt has been updated." });
        }
    }
}
