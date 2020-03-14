﻿using System.Threading.Tasks;
using Requestrr.WebApi.Requestrr.Notifications;

namespace Requestrr.WebApi.Requestrr.TvShows.SeasonsRequestWorkflows
{
    public class FutureSeasonsRequestingWorkflow
    {
        private readonly TvShowUserRequester _user;
        private readonly ITvShowSearcher _searcher;
        private readonly ITvShowRequester _requester;
        private readonly ITvShowUserInterface _userInterface;
        private readonly TvShowNotificationsRepository _notificationRequestRepository;

        public FutureSeasonsRequestingWorkflow(
            TvShowUserRequester user,
            ITvShowSearcher searcher,
            ITvShowRequester requester,
            ITvShowUserInterface userInterface,
            TvShowNotificationsRepository tvShowNotificationsRepository)
        {
            _user = user;
            _searcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationRequestRepository = tvShowNotificationsRepository;
        }

        public async Task HandleRequestAsync(TvShow tvShow, FutureTvSeasons selectedSeason)
        {
            await _userInterface.DisplayTvShowDetailsAsync(tvShow);
            
            if (tvShow.IsRequested)
            {
                await RequestNotificationsForSeasonAsync(tvShow, selectedSeason);
            }
            else
            {
                var wasRequested = await _userInterface.GetTvShowRequestConfirmation(selectedSeason);

                if (wasRequested)
                {
                    await _requester.RequestTvShowAsync(_user.Username, tvShow, selectedSeason);
                    _notificationRequestRepository.AddSeasonNotification(_user.UserId, tvShow.TheTvDbId, selectedSeason);
                    await _userInterface.DisplayRequestSuccessForSeasonAsync(selectedSeason);
                }
            }
        }

        private async Task RequestNotificationsForSeasonAsync(TvShow tvShow, FutureTvSeasons selectedSeason)
        {
            if (_notificationRequestRepository.HasSeasonNotification(_user.UserId, tvShow.TheTvDbId, selectedSeason))
            {
                await _userInterface.WarnAlreadyNotifiedForSeasonsAsync(tvShow, selectedSeason);
            }
            else
            {
                var isRequested = await _userInterface.AskForSeasonNotificationRequestAsync(tvShow, selectedSeason);

                if (isRequested)
                {
                    _notificationRequestRepository.AddSeasonNotification(_user.UserId, tvShow.TheTvDbId, selectedSeason);
                    await _userInterface.DisplayNotificationSuccessForSeasonAsync(selectedSeason);
                }
            }
        }
    }
}