﻿using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.Extensions;
using Requestrr.WebApi.Requestrr.ChatClients;
using Requestrr.WebApi.Requestrr.ChatClients.Discord;
using Requestrr.WebApi.Requestrr.DownloadClients;
using Requestrr.WebApi.Requestrr.DownloadClients.Ombi;
using Requestrr.WebApi.Requestrr.DownloadClients.Radarr;
using Requestrr.WebApi.Requestrr.DownloadClients.Sonarr;
using Requestrr.WebApi.Requestrr.Movies;
using Requestrr.WebApi.Requestrr.Notifications;
using Requestrr.WebApi.Requestrr.TvShows;

namespace Requestrr.WebApi.Requestrr
{
    public class RequestrrBot
    {
        private DiscordSocketClient _client;
        private MovieNotificationEngine _movieNotificationEngine;
        private TvShowNotificationEngine _tvShowNotificationEngine;
        private CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RequestrrBot> _logger;
        private readonly DiscordSettingsProvider _discordSettingsProvider;
        private readonly ConcurrentBag<Func<Task>> _refreshQueue = new ConcurrentBag<Func<Task>>();
        private DiscordSettings _currentSettings = new DiscordSettings();
        private MovieNotificationsRepository _movieNotificationRequestRepository = new MovieNotificationsRepository();
        private TvShowNotificationsRepository _tvShowNotificationRequestRepository = new TvShowNotificationsRepository();

        private Ombi _ombiDownloadClient;
        private Radarr _radarrDownloadClient;
        private Sonarr _sonarrDownloadClient;
        private ModuleInfo _moduleInfo = null;

        public RequestrrBot(IServiceProvider serviceProvider, ILogger<RequestrrBot> logger, DiscordSettingsProvider discordSettingsProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _discordSettingsProvider = discordSettingsProvider;
            _ombiDownloadClient = new Ombi(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<Ombi>>(), serviceProvider.Get<OmbiSettingsProvider>());
            _radarrDownloadClient = new Radarr(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<Radarr>>(), serviceProvider.Get<RadarrSettingsProvider>());
            _sonarrDownloadClient = new Sonarr(serviceProvider.Get<IHttpClientFactory>(), serviceProvider.Get<ILogger<Sonarr>>(), serviceProvider.Get<SonarrSettingsProvider>());
        }

        public async void Start()
        {
            _commandService = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
            });

            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Connected += Connected;
            _commandService.CommandExecuted += CommandExecutedAsync;
            _client.MessageReceived += MessageReceivedAsync;

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var newSettings = _discordSettingsProvider.Provide();

                        if (!_currentSettings.Equals(newSettings) || _client.ConnectionState == ConnectionState.Disconnected)
                        {
                            _logger.LogWarning("Bot changes detected: restarting bot");
                            _currentSettings = newSettings;
                            await RestartBot(newSettings);
                            _logger.LogWarning("Bot changes detected: bot restarted");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex.Message);
                    }

                    await Task.Delay(5000);
                }
            });
        }

        private async Task RestartBot(DiscordSettings discordSettings)
        {
            await _client.StopAsync();
            await _client.LogoutAsync();

            if (_moduleInfo != null)
            {
                await _commandService.RemoveModuleAsync(_moduleInfo);
            }

            await ApplyBotConfigurationAsync(discordSettings);

            await _client.LoginAsync(TokenType.Bot, discordSettings.BotToken);
            await _client.StartAsync();
        }

        private async Task ApplyBotConfigurationAsync(DiscordSettings discordSettings)
        {
            await _client.SetGameAsync(discordSettings.StatusMessage);

            _moduleInfo = await _commandService.CreateModuleAsync(string.Empty, x =>
            {
                x.AddCommand("ping", async (commandContext, noidea, serviceProvider, commandInfo) =>
                {
                    using (var command = new DiscordPingWorkFlow((SocketCommandContext)commandContext, _client, serviceProvider.Get<DiscordSettingsProvider>()))
                    {
                        await command.HandlePingAsync();
                    }
                }, c => c.WithName("ping").WithRunMode(RunMode.Async));

                x.AddCommand("help", async (commandContext, noidea, serviceProvider, commandInfo) =>
                {
                    using (var command = new DiscordHelpWorkFlow((SocketCommandContext)commandContext, _client, serviceProvider.Get<DiscordSettingsProvider>()))
                    {
                        await command.HandleHelpAsync();
                    }
                }, c => c.WithName("help").WithRunMode(RunMode.Async));

                if (discordSettings.MovieDownloadClient != DownloadClient.Disabled)
                {
                    x.AddCommand(discordSettings.MovieCommand, async (commandContext, message, serviceProvider, commandInfo) =>
                    {
                        using (var command = new DiscordMovieRequestingWorkFlow(
                        (SocketCommandContext)commandContext,
                        _client,
                        GetMovieClient<IMovieSearcher>(discordSettings),
                        GetMovieClient<IMovieRequester>(discordSettings),
                        serviceProvider.Get<DiscordSettingsProvider>(),
                        _movieNotificationRequestRepository))
                        {
                            await command.HandleMovieRequestAsync(message[0].ToString());
                        }
                    }, c => c.WithName("movie").WithSummary($"The correct usage of this command is: ```{discordSettings.CommandPrefix}{discordSettings.MovieCommand} name of movie```").WithRunMode(RunMode.Async).AddParameter<string>("movieName", p => p.WithIsRemainder(true).WithIsOptional(false)));
                }

                if (discordSettings.TvShowDownloadClient != DownloadClient.Disabled)
                {
                    x.AddCommand(discordSettings.TvShowCommand, async (commandContext, message, serviceProvider, commandInfo) =>
                    {
                        using (var command = new DiscordTvShowsRequestingWorkFlow(
                           (SocketCommandContext)commandContext,
                           _client,
                            GetTvShowClient<ITvShowSearcher>(discordSettings),
                            GetTvShowClient<ITvShowRequester>(discordSettings),
                           serviceProvider.Get<DiscordSettingsProvider>(),
                           _tvShowNotificationRequestRepository))
                        {
                            await command.HandleTvShowRequestAsync(message[0].ToString());
                        }
                    }, c => c.WithName("tv").WithSummary($"The correct usage of this command is: ```{discordSettings.CommandPrefix}{discordSettings.TvShowCommand} name of tv show```").WithRunMode(RunMode.Async).AddParameter<string>("tvShowName", p => p.WithIsRemainder(true).WithIsOptional(false)));
                }
            });
        }

        private T GetMovieClient<T>(DiscordSettings settings) where T : class
        {
            if (settings.MovieDownloadClient == DownloadClient.Radarr)
            {
                return _radarrDownloadClient as T;
            }
            else if (settings.MovieDownloadClient == DownloadClient.Ombi)
            {
                return _ombiDownloadClient as T;
            }
            else
            {
                throw new Exception($"Invalid configured movie download client {settings.MovieDownloadClient}");
            }
        }

        private T GetTvShowClient<T>(DiscordSettings settings) where T : class
        {
            if (settings.TvShowDownloadClient == DownloadClient.Sonarr)
            {
                return _sonarrDownloadClient as T;
            }
            else if (settings.TvShowDownloadClient == DownloadClient.Ombi)
            {
                return _ombiDownloadClient as T;
            }
            else
            {
                throw new Exception($"Invalid configured tv show download client {settings.TvShowDownloadClient}");
            }
        }

        private Task LogAsync(LogMessage log)
        {
            switch (log.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(log.Exception, $"[Discord] {log.Message}");

                    if (_client.ConnectionState == ConnectionState.Connected)
                    {
                        _logger.LogCritical($"[Discord] Disconnecting from Discord due to error");
                        _client.StopAsync().ContinueWith(x => _client.LogoutAsync());
                    }

                    break;
                case LogSeverity.Error:
                    _logger.LogError(log.Exception, $"[Discord] {log.Message}");
                    if (_client.ConnectionState == ConnectionState.Connected)
                    {
                        _logger.LogError($"[Discord] Disconnecting from Discord due to error");
                        _client.StopAsync().ContinueWith(x => _client.LogoutAsync());
                    }
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(log.Exception, $"[Discord] {log.Message}");
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(log.Exception, $"[Discord] {log.Message}");
                    break;
                case LogSeverity.Debug:
                    _logger.LogDebug(log.Exception, $"[Discord] {log.Message}");
                    break;
                case LogSeverity.Verbose:
                    _logger.LogTrace(log.Exception, $"[Discord] {log.Message}");
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task Connected()
        {
            try
            {
                if (_movieNotificationEngine != null)
                {
                    await _movieNotificationEngine.StopAsync();
                }

                if (_currentSettings.MovieDownloadClient != DownloadClient.Disabled)
                {
                    _movieNotificationEngine = new MovieNotificationEngine(GetMovieClient<IMovieSearcher>(_currentSettings), new UserMovieNotifier(_client), _logger, _movieNotificationRequestRepository);
                    _movieNotificationEngine.Start();
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning("Error while starting movie notification engine: " + ex.Message);
            }

            try
            {
                if (_tvShowNotificationEngine != null)
                {
                    await _tvShowNotificationEngine.StopAsync();
                }

                if (_currentSettings.TvShowDownloadClient != DownloadClient.Disabled)
                {
                    _tvShowNotificationEngine = new TvShowNotificationEngine(GetTvShowClient<ITvShowSearcher>(_currentSettings), new UserTvShowNotifier(_client), _logger, _tvShowNotificationRequestRepository);
                    _tvShowNotificationEngine.Start();
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning("Error while starting tv show notification engine: " + ex.Message);
            }
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var argPos = 0;

            if (!string.IsNullOrWhiteSpace(_currentSettings.CommandPrefix) && !message.HasStringPrefix(_currentSettings.CommandPrefix, ref argPos)) return;

            var context = new SocketCommandContext(_client, message);

            await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
                return;

            if (result.IsSuccess)
                return;

            if (result.Error == CommandError.BadArgCount)
            {
                await context.Channel.SendMessageAsync(command.Value.Summary);
                return;
            }

            await context.Channel.SendMessageAsync("An unexpected error occurred while trying to process your request.");
        }
    }
}