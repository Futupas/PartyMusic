using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;

namespace PartyMusic.Services;

public class CoreService
{
    private readonly ILogger<CoreService> logger;
    private readonly YoutubeService youtube;
    public List<string> SongsQueue { get; } = new();
    public Dictionary<string, SongModel> AllSongs { get; } = new(); //todo This is a very bad decision 'cause it will use lots of memory. 

    public WebSocketConnection? PlayerWSConnection { get; set; } = null;
    public List<WebSocketConnection> WSConnections { get; } = new();

    public bool Playing { get; set; } = false;
    public double Volume { get; set; } = .5;

    public CoreService(
        ILogger<CoreService> logger,
        YoutubeService youtube
    )
    {
        this.logger = logger;
        this.youtube = youtube;
    }
    
    
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

        if (SongsQueue.Count == 1)
        {
            await SendToPlayer(new
            {
                actionId = "new_song",
                song = AllSongs[SongsQueue[0]]
            });
        }

        await UpdateSongsAsync();
    }
    
    public async Task NextSong()
    {
        if (SongsQueue.Count() < 2)
        {
            throw new Exception("Too few songs");
        }
        SongsQueue.RemoveAt(0);
        await SendToPlayer(new
        {
            actionId = "new_song",
            song = AllSongs[SongsQueue[0]]
        });
        await UpdateSongsAsync();
    }
    
    public ValueTask<List<object>> Search(string query, int count = 10)
    {
        return youtube.SearchYoutube(query, count)
            .Select(x =>
            {
                var videoId = youtube.ExtractVideoId(x.Url);
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
                    Exists = File.Exists(@$"wwwroot/data/{videoId}.mp3")
                };
            })
            .ToListAsync();
    }
    
    public Task UpdateSongsAsync()
    {
        return SendToAllUsers(new
        {
            actionId = "update_songs",
            songs = SongsQueue.Select(x => AllSongs[x]),
        });
    }
    
    public Task SendToPlayer(object o)
    {
        AssertPlayerIsNotNull();
        return SendToUser(PlayerWSConnection!, o);
    }
    
    public async Task SendToAllUsers(object o)
    {
        await Task.WhenAll(WSConnections.Select(conn => SendToUser(conn, o)));
    }
    
    public Task SendToUser(WebSocketConnection conn, object o)
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
        if (PlayerWSConnection == null)
        {
            throw new Exception("Player is not set.");
        }
    }
    

    public async Task Log(string message)
    {
        Console.WriteLine(message);
        
        if (PlayerWSConnection == null)
        {
            return;
        }

        if (PlayerWSConnection.WebSocket.State != WebSocketState.Open)
        {
            PlayerWSConnection = null;
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
        await PlayerWSConnection.WebSocket.SendAsync(segments, WebSocketMessageType.Text, true, new());
    }
    public Task Log(ControllerBase controller, string message)
    {
        message = controller.Request.HttpContext.Connection.RemoteIpAddress!.ToString() + ": " + message;
        return Log(message);
    }
    public void PlayerConnected()
    {
        Console.WriteLine("PlayerConnected");
    }
    public void PlayerDisconnected()
    {
        Console.WriteLine("PlayerDisconnected");
    }
}
