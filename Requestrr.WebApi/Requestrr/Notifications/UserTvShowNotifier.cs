﻿using System.Threading.Tasks;
using Discord.WebSocket;
using Requestrr.WebApi.Requestrr.ChatClients;
using Requestrr.WebApi.Requestrr.ChatClients.Discord;
using Requestrr.WebApi.Requestrr.TvShows;

namespace Requestrr.WebApi.Requestrr.Notifications
{
    public class UserTvShowNotifier
    {
        private readonly DiscordSocketClient _discordClient;
        public UserTvShowNotifier(
            DiscordSocketClient discordClient)
        {
            _discordClient = discordClient;
        }

        public async Task NotifyAsync(string userId, TvShow tvShow, int seasonNumber)
        {
            var user = _discordClient.GetUser(ulong.Parse(userId));
            var channel = await user.GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync($"The first episode of **season {seasonNumber}** of **{tvShow.Title}** that you requested has finished downloading and will be available in a few minutes!", false, DiscordTvShowsRequestingWorkFlow.GenerateTvShowDetailsAsync(tvShow, user));
        }
    }
}