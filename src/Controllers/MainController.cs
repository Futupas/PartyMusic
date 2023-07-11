using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Services;

namespace PartyMusic.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class MainController : ControllerBase
{
    private readonly ILogger<MainController> logger;
    private readonly CoreService core;
    private readonly YoutubeService youtube;
    private readonly WifiAccessService wifiAccess;
    
    const int MAX_WS_RECEIVE_BYTES_COUNT = 100;

    private WebSocketConnection? myWSConnection = null;
    
    public MainController(
        ILogger<MainController> logger,
        CoreService core,
        YoutubeService youtube,
        WifiAccessService wifiAccess
    ) {
        this.logger = logger;
        this.core = core;
        this.youtube = youtube;
        this.wifiAccess = wifiAccess;
    }

    
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

        core.Log(this, "WS connected");
        
        myWSConnection = new()
        {
            WebSocket = webSocket,
            CancellationTokenSource = cancellationTokenSource,
        };

        if (isPlayer == "yes")
        {
            core.PlayerWSConnection = myWSConnection;
            core.PlayerConnected();
            if (core.SongsQueue.Any())
            {
                await core.SendToPlayer(new
                {
                    actionId = "new_song",
                    song = core.AllSongs[core.SongsQueue[0]]
                });
            }
        }
        
        core.WSConnections.Add(myWSConnection);
        
        await Task.WhenAll(
            core.SendToUser(myWSConnection, new
            {
                actionId = "update_songs",
                songs = core.SongsQueue.Select(x => core.AllSongs[x]),
            }),
            core.SendToUser(myWSConnection, new
            {
                actionId = "play_pause_song",
                play = core.Playing,
            }),
            core.SendToUser(myWSConnection, new
            {
                actionId = "set_volume",
                volume = core.Volume < 0 ? 0 : core.Volume > 100 ? 100 : core.Volume,
            })
        );
        
        
        while (!cancellationTokenSource.Token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var segments = new ArraySegment<byte>(new byte[MAX_WS_RECEIVE_BYTES_COUNT], 0, MAX_WS_RECEIVE_BYTES_COUNT);
            var receiveResult = await webSocket.ReceiveAsync(segments, cancellationTokenSource.Token);
            var receivedMessageCount = receiveResult.Count;
            var segmentsReal = segments[0..receivedMessageCount];

            OnWebSocketMessageReceive(myWSConnection, receiveResult.MessageType, segmentsReal);
        }
        
        core.WSConnections.Remove(myWSConnection);

        if (core.PlayerWSConnection == myWSConnection)
        {
            // await playerWSConnection.WebSocket.CloseAsync();
            core.PlayerWSConnection.CancellationTokenSource.Cancel();
            core.PlayerWSConnection = null;
            core.PlayerDisconnected();
        }

        myWSConnection = null;
        core.Log(this, "WS disconnected");
    }


    // [HttpGet("/api/search")]
    // public ValueTask<List<object>> Search(string query, int count = 10)
    // {
    //     return core.Search(query, count);
    // }
    
    [HttpPost("/api/download")]
    public async Task Download(string id)
    {
        await youtube.Download(id);
    }

    [HttpPost("/api/add-song-to-queue")]
    public async Task AddSongToQueue(string songId, string start = "no")
    {
        await core.AddSongToQueue(songId, start);
    }
    
    [HttpPost("/api/remove-song-from-queue")]
    public async Task RemoveSongFromQueue(int songId)
    {
        core.SongsQueue.RemoveAt(songId);
        await core.UpdateSongsAsync();
    }
    
    
    [HttpPost("/api/restart-song")]
    public Task RestartSong()
    {
        return core.SendToPlayer(new
        {
            actionId = "restart_song",
        });
    }
    
    [HttpPost("/api/next-song")]
    public async Task NextSong()
    {
        await core.NextSong();
    }
    
    [HttpPost("/api/play-pause-song")]
    public Task PlayPauseSong(string? play)
    {
        core.Playing = (play == "yes") ? true : (play == "no") ? false : !core.Playing;
        return core.SendToAllUsers(new
        {
            actionId = "play_pause_song",
            play = core.Playing,
        });
    }
    
    [HttpPost("/api/set-volume")]
    public Task SetVolume(double volume = .5)
    {
        core.Volume = volume;
        return core.SendToAllUsers(new
        {
            actionId = "set_volume",
            volume = volume < 0 ? 0 : volume > 1 ? 1 : volume,
        });
    }

    
    private async Task OnWebSocketMessageReceive(WebSocketConnection wsConnection, WebSocketMessageType messageType, ArraySegment<byte> message)
    {
        if (messageType != WebSocketMessageType.Text)
        {
            core.Log(this, "Received webhook message type: " + messageType);
            return;
        }
        
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(message));

        if (!data.TryGetValue("actionId", out var actionId) || actionId != "post")
        {
            throw new Exception("Bad WS Data: actionId is not present or it is incorrect.");
        }
        if (!data.TryGetValue("postType", out var postType) || string.IsNullOrEmpty(postType))
        {
            throw new Exception("Bad WS Data: postType is null or empty.");
        }
        if (!data.TryGetValue("requestId", out var requestId) || string.IsNullOrEmpty(requestId))
        {
            throw new Exception("Bad WS Data: requestId is null or empty.");
        }
        
        //Just 4 test

        switch (postType)
        {
            case "test":
                await core.SendToUser(wsConnection, new
                {
                    actionId,
                    requestId,
                    data = "Punks not dead!",
                    whereFrom = "test",
                });
                break;
            case "search-song":
                var query = data["query"];
                var count = int.Parse(data["count"]);
                
                await core.SendToUser(wsConnection, new
                {
                    actionId,
                    requestId,
                    data = await core.Search(query, count)
                });
                break;
            default:
                await core.SendToUser(wsConnection, new
                {
                    actionId,
                    requestId,
                    whereFrom = "default",
                });
                break;
        }
        
        
    }
    // [HttpGet("/api/search")]
    // public ValueTask<List<object>> Search(string query, int count = 10)
    // {
    //     return core.Search(query, count);
    // }
}

