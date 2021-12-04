using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LODFinals.DataModels.Responses.MediaMentions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LODFinals.Api.Controllers
{
    [Route("[controller]")]
    public class MediaMentionsController : Controller
    {
        private readonly ILogger<MediaMentionsController> _logger;

        public MediaMentionsController(ILogger<MediaMentionsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("users/{username}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<MediaMentionItemResponse>))]
        [ProducesResponseType(400, Type = typeof(string))]
        public async Task<IActionResult> GetUserMediaMentionsAsync(string username, [FromQuery] int start = 0, [FromQuery] int limit = 50)
        {
            try
            {
                return Ok(new MediaMentionItemResponse[]
                {
                    new MediaMentionItemResponse
                    {
                        Title = "\"Сколько вы зарабатываете?\" О Юрии Дуде и поколении YouTube",
                        PublishDate = new DateTime(2019, 10, 12),
                        Media = "LIVEJOURNAL"
                    },
                    new MediaMentionItemResponse
                    {
                        Title = "\"Сколько вы зарабатываете?\" О Юрии Дуде и поколении YouTube",
                        PublishDate = new DateTime(2019, 10, 12),
                        Media = "LIVEJOURNAL"
                    },
                    new MediaMentionItemResponse
                    {
                        Title = "\"Сколько вы зарабатываете?\" О Юрии Дуде и поколении YouTube",
                        PublishDate = new DateTime(2019, 10, 12),
                        Media = "LIVEJOURNAL"
                    },
                });
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Ошибка получения упоминаний в СМИ по пользователю {username}");
                return BadRequest("Ошибка получения упоминаний в СМИ");
            }
        }
    }
}
