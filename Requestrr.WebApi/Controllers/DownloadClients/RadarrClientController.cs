﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.config;
using Requestrr.WebApi.Requestrr.DownloadClients;
using Requestrr.WebApi.Requestrr.DownloadClients.Radarr;

namespace Requestrr.WebApi.Controllers.DownloadClients
{
    public class TestRadarrSettingsModel
    {
        [Required]
        public string Hostname { get; set; }
        [Required]
        public int Port { get; set; }
        [Required]
        public string ApiKey { get; set; }
        [Required]
        public bool UseSSL { get; set; }
        [Required]
        public string Version { get; set; }
    }

    public class SaveRadarrSettingsModel
    {
        [Required]
        public string Hostname { get; set; }
        [Required]
        public int Port { get; set; }
        [Required]
        public string ApiKey { get; set; }
        [Required]
        public string MovieMinAvailability { get; set; }
        [Required]
        public string MoviePath { get; set; }
        [Required]
        public int MovieProfile { get; set; }
        [Required]
        public int[] MovieTags { get; set; }
        [Required]
        public string AnimeMinAvailability { get; set; }
        [Required]
        public string AnimePath { get; set; }
        [Required]
        public int AnimeProfile { get; set; }
        [Required]
        public int[] AnimeTags { get; set; }
        public bool UseSSL { get; set; }
        [Required]
        public string Version { get; set; }
        [Required]
        public string Command { get; set; }
    }

    public class RadarrProfile
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }

    public class RadarrPath
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Path { get; set; }
    }

    public class RadarrTag
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }

    [ApiController]
    [Authorize]
    [Route("/api/movies/radarr")]
    public class RadarrClientController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Radarr> _logger;

        public RadarrClientController(
            IHttpClientFactory httpClientFactory,
            ILogger<Radarr> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestRadarrSettings([FromBody]TestRadarrSettingsModel model)
        {
            try
            {
                await Radarr.TestConnectionAsync(_httpClientFactory.CreateClient(), _logger, ConvertToRadarrSettings(model));

                return Ok(new { ok = true });
            }
            catch (System.Exception)
            {
                return BadRequest($"The specified settings are invalid");
            }
        }

        [HttpPost("rootpath")]
        public async Task<IActionResult> GetRadarrRootPaths([FromBody]TestRadarrSettingsModel model)
        {
            try
            {
                var paths = await Radarr.GetRootPaths(_httpClientFactory.CreateClient(), _logger, ConvertToRadarrSettings(model));

                return Ok(paths.Select(x => new RadarrPath
                {
                    Id = x.id,
                    Path = x.path
                }));
            }
            catch (System.Exception)
            {
                return BadRequest($"Could not load the paths from Radarr, check your settings.");
            }
        }

        [HttpPost("profile")]
        public async Task<IActionResult> GetRadarrProfiles([FromBody]TestRadarrSettingsModel model)
        {
            try
            {
                var profiles = await Radarr.GetProfiles(_httpClientFactory.CreateClient(), _logger, ConvertToRadarrSettings(model));

                return Ok(profiles.Select(x => new RadarrProfile
                {
                    Id = x.id,
                    Name = x.name
                }));
            }
            catch (System.Exception)
            {
                return BadRequest($"Could not load the profiles from Radarr, check your settings.");
            }
        }

        [HttpPost("tag")]
        public async Task<IActionResult> GetRadarrTags([FromBody]TestRadarrSettingsModel model)
        {
            try
            {
                var tags = await Radarr.GetTags(_httpClientFactory.CreateClient(), _logger, ConvertToRadarrSettings(model));

                return Ok(tags.Select(x => new RadarrTag
                {
                    Id = x.id,
                    Name = x.label
                }));
            }
            catch (System.Exception)
            {
                return BadRequest($"Could not load the tags from Radarr, check your settings.");
            }
        }

        [HttpPost()]
        public async Task<IActionResult> SaveAsync([FromBody]SaveRadarrSettingsModel model)
        {
            var movieSettings = new MoviesSettings
            {
                Client = DownloadClient.Radarr,
                Command = model.Command.Trim()
            };

            var radarrSetting = new RadarrSettings
            {
                Hostname = model.Hostname.Trim(),
                ApiKey = model.ApiKey.Trim(),
                Port = model.Port,
                MoviePath = model.MoviePath,
                MovieProfile = model.MovieProfile,
                MovieMinAvailability = model.MovieMinAvailability,
                MovieTags = model.MovieTags ?? Array.Empty<int>(),
                AnimePath = model.AnimePath,
                AnimeProfile = model.AnimeProfile,
                AnimeMinAvailability = model.AnimeMinAvailability,
                AnimeTags = model.AnimeTags ?? Array.Empty<int>(),
                UseSSL = model.UseSSL,
                Version = model.Version
            };

            DownloadClientsSettingsRepository.SetRadarr(movieSettings, radarrSetting);

            return Ok(new { ok = true });
        }

        private static Requestrr.DownloadClients.Radarr.RadarrSettings ConvertToRadarrSettings(TestRadarrSettingsModel model)
        {
            return new Requestrr.DownloadClients.Radarr.RadarrSettings
            {
                ApiKey = model.ApiKey.Trim(),
                Hostname = model.Hostname.Trim(),
                Port = model.Port,
                UseSSL = model.UseSSL,
                Version = model.Version
            };
        }
    }
}
