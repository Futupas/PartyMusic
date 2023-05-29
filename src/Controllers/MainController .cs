using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;

namespace PartyMusic.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class MainController : ControllerBase
{
    // const string VLC_PATH = @"C:\Program Files\VideoLAN\VLC";
    const string VLC_PATH = @"/usr/lib/vlc";

    private readonly ILogger<WeatherForecastController> _logger;

    public MainController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "Test")]
    public async Task<string> Test()
    {
        string videoId = "wvK1VishIX0";
        await DownloadYouTubeAudio(videoId, "audio.mp3");

        // using (var mediaPlayer = new VlcMediaPlayer(new DirectoryInfo(@"C:\Program Files\VideoLAN\VLC")))
        using (var mediaPlayer = new VlcMediaPlayer(new DirectoryInfo(VLC_PATH)))
        {
            mediaPlayer.SetMedia(new FileInfo("audio.mp3"));
            mediaPlayer.Play();
            Thread.Sleep(30_000);
            mediaPlayer.Stop();
            // var mediaOptions = new[]
            // {
            //     $"--http-user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36"
            // };
            // // mediaPlayer.SetMedia(new Uri($"https://music.youtube.com/watch?v={videoId}"), mediaOptions);
            // mediaPlayer.SetMedia(new Uri($"https://music.youtube.com/watch?v=UBGLPVEbTzE"), mediaOptions);
            // mediaPlayer.Play();
            // Console.ReadLine(); // Wait for user input to stop the playback
            // mediaPlayer.Stop();
        }

        return "hello world";
    }




    static async Task DownloadYouTubeAudio(string videoId, string outputPath)
    {
        var youtube = new YoutubeClient();
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().FirstOrDefault();
        if (audioStreamInfo != null)
        {
            // using (var output = File.Create(outputPath))
            // {
            // }
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, outputPath);
        }
        else
        {
            Console.WriteLine("No audio stream found for the specified video.");
        }
    }
}

/*

using YoutubeExplode.Search;
using YoutubeExplode;

var youtube = new YoutubeClient();
// youtube.Search.GetVideosAsync("fff").CollectAsync(5);

await foreach (var result in youtube.Search.GetResultsAsync("nothing else matters").Take(10))
{
    // Use pattern matching to handle different results (videos, playlists, channels)
    switch (result)
    {
        case VideoSearchResult video:
        {
            var id = video.Id;
            var title = video.Title;
            var duration = video.Duration;
            Console.WriteLine($"Video {title}");
            break;
        }
        case PlaylistSearchResult playlist:
        {
            var id = playlist.Id;
            var title = playlist.Title;
            Console.WriteLine($"playlist {title}");
            break;
        }
        case ChannelSearchResult channel:
        {
            var id = channel.Id;
            var title = channel.Title;
            Console.WriteLine($"channel {title}");
            break;
        }
        default:
        {
            var title = result.Title;
            Console.WriteLine($"other {title}");
            break;
        }
    }
}


*/