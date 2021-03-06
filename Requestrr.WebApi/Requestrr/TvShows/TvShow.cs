﻿using System.Linq;

namespace Requestrr.WebApi.Requestrr.TvShows
{
    public class SearchedTvShow
    {
        public int TheTvDbId { get; set; }
        public string Title { get; set; }
        public string FirstAired { get; set; }
        public string Banner { get; set; }
    }

    public class TvShow
    {
        public int TheTvDbId { get; set; }
        public string DownloadClientId { get; set; }
        public string Title { get; set; }
        public string Quality { get; set; }
        public bool HasEnded { get; set; }
        public string PlexUrl { get; set; }
        public string EmbyUrl { get; set; }
        public string Overview { get; set; }
        public string Banner { get; set; }
        public string FirstAired { get; set; }
        public string Network { get; set; }
        public string Status { get; set; }
        public TvSeason[] Seasons { get; set; }
        public bool IsRequested { get; set; }

        public bool IsMultiSeasons()
        {
            return Seasons.Length > 1;
        }

        public bool AllSeasonsAlreadyRequested()
        {
            return Seasons.OfType<NormalTvSeason>().All(x => x.IsRequested);
        }

        public bool AllSeasonsAvailable()
        {
            return Seasons.OfType<NormalTvSeason>().All(x => x.IsAvailable);
        }
    }

    public class AllTvSeasons : TvSeason { }
    public class FutureTvSeasons : TvSeason { }
    public class NormalTvSeason : TvSeason { }

    public abstract class TvSeason
    {
        public bool IsAvailable { get; set; }
        public int SeasonNumber { get; set; }
        public bool IsRequested { get; set; }
    }

    public class TvEpisode
    {
        public int EpisodeNumber { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsRequested { get; set; }
    }
}