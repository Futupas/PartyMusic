using System.Net.WebSockets;
using System.Text;
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
    
    const int MAX_WS_RECEIVE_BYTES_COUNT = 100;

    private WebSocketConnection? myWSConnection = null;
    
    public MainController(
        ILogger<MainController> logger,
        CoreService core,
        YoutubeService youtube
    ) {
        this.logger = logger;
        this.core = core;
        this.youtube = youtube;
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
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                core.Log(this, "Received webhook: " + Encoding.UTF8.GetString(segmentsReal));
            }
            else
            {
                core.Log(this, "Received webhook message type: " + receiveResult.MessageType);
            }
            
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


    [HttpGet("/api/search")]
    public ValueTask<List<object>> Search(string query, int count = 10)
    {
        return core.Search(query, count);
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
}

