using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using YoutubeExplode;
// using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops;
using YoutubeExplode.Common;
using System.Linq;
using YoutubeExplode.Search;
using System.IO;
using System.Text.RegularExpressions;

namespace PartyMusic.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class MainController : ControllerBase
{
    private readonly ILogger<MainController> _logger;
    const int MAX_WS_RECEIVE_BYTES_COUNT = 100;

    private static readonly Regex videoFromUrlRegex =
        new (@"(youtube\.com\/watch.*([\?\&]v\=(?<videoId>[a-zA-Z0-9]*)))|(youtu\.be\/(?<videoId2>[a-zA-Z0-9]*))", RegexOptions.Compiled);

    private WebSocketConnection? myWSConnection = null;
    private static WebSocketConnection? playerWSConnection = null;
    private static List<WebSocketConnection> wsConnections = new();
    
    
    [HttpGet("/ws")]
    public async Task GetWs(string? isPlayer)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var cancellationTokenSource = new CancellationTokenSource();

        Log("WS connected");
        
        myWSConnection = new()
        {
            WebSocket = webSocket,
            CancellationTokenSource = cancellationTokenSource,
        };

        if (isPlayer == "yes")
        {
            playerWSConnection = myWSConnection;
            PlayerConnected();
        }
        
        wsConnections.Add(myWSConnection);

        while (!cancellationTokenSource.Token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var segments = new ArraySegment<byte>(new byte[MAX_WS_RECEIVE_BYTES_COUNT], 0, MAX_WS_RECEIVE_BYTES_COUNT);
            var receiveResult = await webSocket.ReceiveAsync(segments, cancellationTokenSource.Token);
            var receivedMessageCount = receiveResult.Count;
            var segmentsReal = segments[0..receivedMessageCount];
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                Log("Received webhook: " + Encoding.UTF8.GetString(segmentsReal));
            }
            else
            {
                Log("Received webhook message type: " + receiveResult.MessageType);
            }
            
        }
        
        wsConnections.Remove(myWSConnection);

        if (playerWSConnection == myWSConnection)
        {
            // await playerWSConnection.WebSocket.CloseAsync();
            await playerWSConnection.CancellationTokenSource.CancelAsync();
            playerWSConnection = null;
            PlayerDisconnected();
        }

        myWSConnection = null;
        Log("WS disconnected");
    }

    public MainController(ILogger<MainController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "Test")]
    public async Task<string> Test()
    {
        string videoId = "wvK1VishIX0";
        await DownloadYouTubeAudio(videoId, "audio.mp3");

        return "hello world";
    }

    
    [HttpGet("/api/search")]
    public ValueTask<List<object>> Search(string query, int count = 10)
    {
        return SearchYoutube(query, count)
        .Select(x =>
        {
            var videoId = ExtractVideoId(x.Url);
            return (object)new
            {
                x.Title,
                x.Url,
                Id = videoId,
                Duration = (int?)x.Duration?.TotalSeconds,
                Exists = System.IO.File.Exists(@$"wwwroot/data/{videoId}.mp3")
            };
        })
        .ToListAsync();
    }
    
    [HttpPost("/api/download")]
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
            LogStatic("No audio stream found for the specified video.");
        }
    }


    private static String ExtractVideoId(string url)
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
    static async Task DownloadYouTubeAudio(string videoId, string outputPath)
    {
        var youtube = new YoutubeClient();
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
        var audioStreamInfo = streamManifest.GetAudioOnlyStreams().FirstOrDefault();
        if (audioStreamInfo != null)
        {
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, outputPath);
        }
        else
        {
            LogStatic("No audio stream found for the specified video.");
        }
    }
    
    // static ValueTask<List<(string id, string title, int? duration)>> SearchYoutube(string query, int count = 10)
    // {
    //     return new YoutubeClient()
    //         .Search
    //         .GetVideosAsync(query)
    //         .Take(count)
    //         .Select(x => (x.Title, x.Url, (int?)x.Duration?.TotalSeconds))
    //         .Take(5)
    //         .ToListAsync();
    // }
    
    static IAsyncEnumerable<VideoSearchResult> SearchYoutube(string query, int count = 10)
    {
        return new YoutubeClient()
            .Search
            .GetVideosAsync(query)
            .Take(count);
    }
    

    private static async Task LogStatic(string message)
    {
        Console.WriteLine(message);
        
        if (playerWSConnection == null)
        {
            return;
        }

        if (playerWSConnection.WebSocket.State != WebSocketState.Open)
        {
            playerWSConnection = null;
            PlayerDisconnected();
            return;
        }
        
        var segments = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        await playerWSConnection.WebSocket.SendAsync(segments, WebSocketMessageType.Text, true, new());
    }
    private Task Log(string message)
    {
        message = $"Conn {this.GetHashCode()}: {message}";
        return LogStatic(message);
    }

    private static void PlayerConnected()
    {
        Console.WriteLine("PlayerConnected");
    }
    private static void PlayerDisconnected()
    {
        Console.WriteLine("PlayerDisconnected");
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