using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using YoutubeExplode;
using YoutubeExplode.Search;
using System.Text.Json;
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

    private static readonly List<string> SongsQueue = new();
    private static readonly Dictionary<string, SongModel> AllSongs = new(); //todo This is a very bad decision 'cause it will use lots of memory. 

    private WebSocketConnection? myWSConnection = null;
    private static WebSocketConnection? playerWSConnection = null;
    private static List<WebSocketConnection> wsConnections = new();
    
    
    [HttpGet("/api/ws")]
    public async Task GetWs(string isPlayer = "no")
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
            AllSongs[videoId] = new()
            {
                Id = videoId,
                Title = x.Title,
                Duration = (int?)x.Duration?.TotalSeconds,
            };
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

    [HttpPost("/api/add-song-to-queue")]
    public async Task AddSongToQueue(string songId, string start = "no")
    {
        var song = AllSongs[songId];
        if (song == null)
        {
            throw new Exception("No such song found");
        }

        if (start == "yes" && SongsQueue.Any())
        {
            SongsQueue.Insert(1, songId);
        }
        else
        {
            SongsQueue.Add(songId);
        }

        UpdateSongsAsync();
    }
    [HttpPost("/api/remove-song-from-queue")]
    public async Task RemoveSongFromQueue(int songId)
    {
        SongsQueue.RemoveAt(songId);
        UpdateSongsAsync();
    }

    private Task UpdateSongsAsync()
    {
        return Task.WhenAll(wsConnections.Select(conn =>
        {
            if (conn.WebSocket.State != WebSocketState.Open)
            {
                return Task.CompletedTask;
            }

            string message = JsonSerializer.Serialize(new
            {
                actionId = "update_songs",
                songs = SongsQueue.Select(x => AllSongs[x]),
            });
            var segments = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            return conn.WebSocket.SendAsync(segments, WebSocketMessageType.Text, true, new());
        }));
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
        
        message = JsonSerializer.Serialize(new
        {
            actionId = "add_log",
            level = "log",
            message,
        });
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

