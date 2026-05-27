using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.BO;

namespace ShopQuanAo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatbotController(ChatbotService chatbotService, UserManager<ApplicationUser> userManager)
        {
            _chatbotService = chatbotService;
            _userManager = userManager;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> AskAssistant([FromBody] ChatRequestDto request)
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                string clientIdentifier = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (string.IsNullOrEmpty(clientIdentifier))
                {
                    clientIdentifier = HttpContext.TraceIdentifier;
                }

                string botAnswer = await _chatbotService.ProcessChatAsync(request, userId, clientIdentifier);

                return Ok(new { answer = botAnswer });
            }
            catch (Exception ex)
            {
                return Ok(new { answer = "Bot đang bận xử lý, bạn hãy thử lại sau vài giây nhé! (" + ex.Message + ")" });
            }
        }
    }
}