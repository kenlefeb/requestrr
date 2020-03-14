﻿namespace Requestrr.WebApi.Requestrr.DownloadClients.Ombi
{
    public class OmbiSettingsProvider
    {
        public OmbiSettings Provide()
        {
            dynamic settings = SettingsFile.Read();

            return new OmbiSettings
            {
                ApiKey = settings.DownloadClients.Ombi.ApiKey,
                ApiUsername = settings.DownloadClients.Ombi.ApiUsername,
                Hostname = settings.DownloadClients.Ombi.Hostname,
                Port = settings.DownloadClients.Ombi.Port,
                UseSSL = (bool)settings.DownloadClients.Ombi.UseSSL
            };
        }
    }
}