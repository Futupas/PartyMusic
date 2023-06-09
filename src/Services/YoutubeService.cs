﻿using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Search;

namespace PartyMusic.Services;

public class YoutubeService
{
    private readonly IConfiguration config;
    private readonly ILogger<YoutubeService> logger;
    private static readonly Regex videoFromUrlRegex =
        new (@"(youtube\.com\/watch.*([\?\&]v\=(?<videoId>[a-zA-Z0-9\-]*)))|(youtu\.be\/(?<videoId2>[a-zA-Z0-9\-]*))", RegexOptions.Compiled);
    
    public YoutubeService(
        IConfiguration config,
        ILogger<YoutubeService> logger
    ) {
        this.config = config;
        this.logger = logger;
    }

    public async Task Download(string id)
    {
        if (!Directory.Exists("wwwroot/data"))
        {
            Directory.CreateDirectory("wwwroot/data");
        }
        
        var youtube = new YoutubeClient();
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(id);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().FirstOrDefault();
        if (audioStreamInfo != null)
        {
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, $@"wwwroot/data/{id}.mp3");
        }
        else
        {
            //todo log into console
            logger.LogWarning("No audio stream found for the specified video.");
        }
    }
    
    public IAsyncEnumerable<VideoSearchResult> SearchYoutube(string query, int count = 10)
    {
        return new YoutubeClient()
            .Search
            .GetVideosAsync(query)
            .Take(count);
    }
    
    public string ExtractVideoId(string url)
    {
        var simpleIds = videoFromUrlRegex.Match(url).Groups["videoId"];
        var shortenedIds = videoFromUrlRegex.Match(url).Groups["videoId2"];

        if (simpleIds.Success)
        {
            return simpleIds.Value;
        }
        
        if (shortenedIds.Success)
        {
            return simpleIds.Value;
        }

        throw new Exception("Video url not found.");
    }
}
