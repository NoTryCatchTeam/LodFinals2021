using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LODFinals.Api.Services;
using LODFinals.DataModels.Responses.MediaMentions;
using LODFinals.DataModels.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LODFinals.Api.Definitions.Extensions;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LODFinals.Api.Controllers
{
    [Route("[controller]")]
    public class MediaMentionsController : Controller
    {
        private readonly ILogger<MediaMentionsController> _logger;
        private readonly ISolrRestService _solrRestService;

        public MediaMentionsController(ILogger<MediaMentionsController> logger, ISolrRestService solrRestService)
        {
            _logger = logger;
            _solrRestService = solrRestService;
        }

        [HttpGet("users/{username}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<MediaMentionItemResponse>))]
        [ProducesResponseType(400, Type = typeof(string))]
        public async Task<IActionResult> GetUserMediaMentionsAsync(Username username, [FromQuery] int start = 0, [FromQuery] int limit = 50)
        {
            try
            {
                var response = (await _solrRestService.GetPressInfoAsync(username.GetDescription())).ToArray();

                return Ok(new MediaMentionItemResponse[]
                {
                    new MediaMentionItemResponse
                    {
                        Title = response[0].title.FirstOrDefault(),
                        PublishDate = new DateTime(2019, 10, 12),
                        Media = response[0].source?.FirstOrDefault() ?? "LIVEJOURNAL",
                        Text = response[0].text?.FirstOrDefault(),
                    },
                    new MediaMentionItemResponse
                    {
                        Title = response[1].title.FirstOrDefault(),
                        PublishDate = new DateTime(2019, 10, 12),
                        Media = response[1].source?.FirstOrDefault() ?? "TJOURNAL",
                        Text = response[1].text?.FirstOrDefault(),
                    },
                    new MediaMentionItemResponse
                    {
                        Title = response[2].title.FirstOrDefault(),
                        PublishDate = new DateTime(2019, 10, 12),
                        Media = response[2].source?.FirstOrDefault() ?? "LIFE",
                        Text = response[2].text?.FirstOrDefault(),
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
