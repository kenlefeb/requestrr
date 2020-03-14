﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Requestrr.WebApi.config;
using Requestrr.WebApi.Requestrr.DownloadClients;

namespace Requestrr.WebApi.Controllers.DownloadClients
{
    [ApiController]
    [Authorize]
    [Route("/api/tvshows")]
    public class TvShowsDownloadClientController : ControllerBase
    {
        private readonly TvShowsSettings _tvShowsSettings;
        private readonly DownloadClientsSettings _downloadClientsSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public TvShowsDownloadClientController(
            IHttpClientFactory httpClientFactory,
            IOptionsSnapshot<TvShowsSettings> tvShowsSettingsAccessor,
            IOptionsSnapshot<DownloadClientsSettings> botClientsSettingsAccessor)
        {
            _tvShowsSettings = tvShowsSettingsAccessor.Value;
            _downloadClientsSettings = botClientsSettingsAccessor.Value;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet()]
        public async Task<IActionResult> GetAsync()
        {
            return Ok(new TvShowsSettingsModel
            {
                Client = _tvShowsSettings.Client,
                Sonarr = new SonarrSettings
                {
                    Hostname = _downloadClientsSettings.Sonarr.Hostname,
                    Port = _downloadClientsSettings.Sonarr.Port,
                    ApiKey = _downloadClientsSettings.Sonarr.ApiKey,
                    TvPath = _downloadClientsSettings.Sonarr.TvRootFolder,
                    TvProfile = _downloadClientsSettings.Sonarr.TvProfileId,
                    TvTags = _downloadClientsSettings.Sonarr.TvTags ?? Array.Empty<int>(),
                    TvLanguage = _downloadClientsSettings.Sonarr.TvLanguageId,
                    TvUseSeasonFolders = _downloadClientsSettings.Sonarr.TvUseSeasonFolders,
                    AnimePath = _downloadClientsSettings.Sonarr.AnimeRootFolder,
                    AnimeProfile = _downloadClientsSettings.Sonarr.AnimeProfileId,
                    AnimeTags = _downloadClientsSettings.Sonarr.AnimeTags ?? Array.Empty<int>(),
                    AnimeLanguage = _downloadClientsSettings.Sonarr.AnimeLanguageId,
                    AnimeUseSeasonFolders = _downloadClientsSettings.Sonarr.AnimeUseSeasonFolders,
                    UseSSL = _downloadClientsSettings.Sonarr.UseSSL,
                    Version = _downloadClientsSettings.Sonarr.Version
                },
                Ombi = new OmbiSettings
                {
                    Hostname = _downloadClientsSettings.Ombi.Hostname,
                    Port = _downloadClientsSettings.Ombi.Port,
                    ApiKey = _downloadClientsSettings.Ombi.ApiKey,
                    ApiUsername = _downloadClientsSettings.Ombi.ApiUsername,
                    UseSSL = _downloadClientsSettings.Ombi.UseSSL,
                    Version = _downloadClientsSettings.Ombi.Version
                },
                Command = _tvShowsSettings.Command
            });
        }

        [HttpPost("disable")]
        public async Task<IActionResult> SaveAsync()
        {
            _tvShowsSettings.Client = DownloadClient.Disabled;
            DownloadClientsSettingsRepository.SetDisabledClient(_tvShowsSettings);
            return Ok(new { ok = true });
        }
    }
}
