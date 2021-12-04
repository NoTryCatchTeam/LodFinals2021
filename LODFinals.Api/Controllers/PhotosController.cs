using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LODFinals.Api.Controllers
{
    [Route("[controller]")]
    public class PhotosController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<PhotosController> _logger;

        public PhotosController(IWebHostEnvironment webHostEnvironment, ILogger<PhotosController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpGet("{username}")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(204)]
        [ProducesResponseType(400, Type = typeof(string))]
        public async Task<IActionResult> GetUserPhotosAsync(string username, [FromQuery] int start = 0, [FromQuery] int end = 50)
        {
            try
            {
                var imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                if (!Directory.Exists(imagesPath))
                {
                    return NoContent();
                }

                var imagesUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}" + "/images/";

                var searchedFiles = Directory
                    .GetFiles(imagesPath)
                    .Select(path => new FileInfo(path))
                    .Where(file => Regex.IsMatch(file.Name, @$"^{username}_"))
                    .OrderBy(file => file.Name)
                    .Skip(start)
                    .Take(end)
                    .Select(file => imagesUrl + file.Name)
                    .ToArray();

                if (searchedFiles.Any() != true)
                {
                    return NoContent();
                }

                return Ok(searchedFiles);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Ошибка получения картинок");
                return BadRequest("Ошибка получения картинок");
            }
        }
    }
}
