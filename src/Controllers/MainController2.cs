using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using YoutubeExplode;
using YoutubeExplode.Search;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Text;
using PartyMusic.Services;

namespace PartyMusic.Controllers;

[ApiController]
[Route("/api/[controller]")]
internal class MainController2 : ControllerBase
{
    private readonly ILogger<MainController2> logger;
    private readonly CoreService core;
    private readonly YoutubeService youtube;
    
    const int MAX_WS_RECEIVE_BYTES_COUNT = 100;

    private static readonly Regex videoFromUrlRegex =
        new (@"(youtube\.com\/watch.*([\?\&]v\=(?<videoId>[a-zA-Z0-9\-]*)))|(youtu\.be\/(?<videoId2>[a-zA-Z0-9\-]*))", RegexOptions.Compiled);
    
    private WebSocketConnection? myWSConnection = null;
    
    public MainController2(
        ILogger<MainController2> logger,
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

        Log("WS connected");
        
        myWSConnection = new()
        {
            WebSocket = webSocket,
            CancellationTokenSource = cancellationTokenSource,
        };

        if (isPlayer == "yes")
        {
            core.PlayerWSConnection = myWSConnection;
            PlayerConnected();
            if (core.SongsQueue.Any())
            {
                await SendToPlayer(new
                {
                    actionId = "new_song",
                    song = core.AllSongs[core.SongsQueue[0]]
                });
            }
        }
        
        core.WSConnections.Add(myWSConnection);
        
        await Task.WhenAll(
            SendToUser(myWSConnection, new
            {
                actionId = "update_songs",
                songs = core.SongsQueue.Select(x => core.AllSongs[x]),
            }),
            SendToUser(myWSConnection, new
            {
                actionId = "play_pause_song",
                play = core.Playing,
            }),
            SendToUser(myWSConnection, new
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
                Log("Received webhook: " + Encoding.UTF8.GetString(segmentsReal));
            }
            else
            {
                Log("Received webhook message type: " + receiveResult.MessageType);
            }
            
        }
        
        core.WSConnections.Remove(myWSConnection);

        if (core.PlayerWSConnection == myWSConnection)
        {
            // await playerWSConnection.WebSocket.CloseAsync();
            core.PlayerWSConnection.CancellationTokenSource.Cancel();
            core.PlayerWSConnection = null;
            PlayerDisconnected();
        }

        myWSConnection = null;
        Log("WS disconnected");
    }


    [HttpGet("/api/search")]
    public ValueTask<List<object>> Search(string query, int count = 10)
    {
        return youtube.SearchYoutube(query, count)
        .Select(x =>
        {
            var videoId = youtube.ExtractVideoId(x.Url);
            core.AllSongs[videoId] = new()
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
        await youtube.Download(id);
    }

    [HttpPost("/api/add-song-to-queue")]
    public async Task AddSongToQueue(string songId, string start = "no")
    {
        var song = core.AllSongs[songId];
        if (song == null)
        {
            throw new Exception("No such song found");
        }

        if (start == "yes" && core.SongsQueue.Any())
        {
            core.SongsQueue.Insert(1, songId);
        }
        else
        {
            core.SongsQueue.Add(songId);
        }

        if (core.SongsQueue.Count == 1)
        {
            await SendToPlayer(new
            {
                actionId = "new_song",
                song = core.AllSongs[core.SongsQueue[0]]
            });
        }

        await UpdateSongsAsync();
    }
    [HttpPost("/api/remove-song-from-queue")]
    public async Task RemoveSongFromQueue(int songId)
    {
        core.SongsQueue.RemoveAt(songId);
        UpdateSongsAsync();
    }
    
    
    [HttpPost("/api/restart-song")]
    public Task RestartSong()
    {
        return SendToPlayer(new
        {
            actionId = "restart_song",
        });
    }
    
    [HttpPost("/api/next-song")]
    public async Task NextSong()
    {
        if (core.SongsQueue.Count() < 2)
        {
            throw new Exception("Too few songs");
        }
        core.SongsQueue.RemoveAt(0);
        await SendToPlayer(new
        {
            actionId = "new_song",
            song = core.AllSongs[core.SongsQueue[0]]
        });
        await UpdateSongsAsync();
    }
    
    [HttpPost("/api/play-pause-song")]
    public Task PlayPauseSong(string? play)
    {
        core.Playing = (play == "yes") ? true : (play == "no") ? false : !core.Playing;
        return SendToAllUsers(new
        {
            actionId = "play_pause_song",
            play = core.Playing,
        });
    }
    
    [HttpPost("/api/set-volume")]
    public Task SetVolume(double volume = .5)
    {
        core.Volume = volume;
        return SendToAllUsers(new
        {
            actionId = "set_volume",
            volume = volume < 0 ? 0 : volume > 1 ? 1 : volume,
        });
    }

    private Task UpdateSongsAsync()
    {
        return SendToAllUsers(new
        {
            actionId = "update_songs",
            songs = core.SongsQueue.Select(x => core.AllSongs[x]),
        });
    }

   

    private Task SendToPlayer(object o)
    {
        AssertPlayerIsNotNull();
        return SendToUser(core.PlayerWSConnection!, o);
    }
    
    private async Task SendToAllUsers(object o)
    {
        await Task.WhenAll(core.WSConnections.Select(conn => SendToUser(conn, o)));
    }
    
    private static Task SendToUser(WebSocketConnection conn, object o)
    {
        if (conn.WebSocket.State != WebSocketState.Open)
        {
            return Task.CompletedTask;
        }
        string message = JsonSerializer.Serialize(o);
        var segments = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        return conn!.WebSocket.SendAsync(segments, WebSocketMessageType.Text, true, new());
    }

    private void AssertPlayerIsNotNull()
    {
        if (core.PlayerWSConnection == null)
        {
            throw new Exception("Player is not set.");
        }
    }
    

    private async Task Log(string message)
    {
        Console.WriteLine(message);
        
        if (core.PlayerWSConnection == null)
        {
            return;
        }

        if (core.PlayerWSConnection.WebSocket.State != WebSocketState.Open)
        {
            core.PlayerWSConnection = null;
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
        await core.PlayerWSConnection.WebSocket.SendAsync(segments, WebSocketMessageType.Text, true, new());
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

