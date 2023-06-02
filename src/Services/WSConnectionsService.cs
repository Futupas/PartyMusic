using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;

namespace PartyMusic.Services;

internal class WSConnectionsService
{
    private WebSocketConnection? _playerWSConnection = null;

    private readonly ILogger<WSConnectionsService> logger;
    private readonly WSConnectionsService wsService;
    private readonly IConfiguration config;
    private readonly SongsService songs;
    
    public WSConnectionsService(
        ILogger<WSConnectionsService> logger,
        WSConnectionsService wsService,
        IConfiguration config,
        SongsService songs
    )
    {
        this.logger = logger;
        this.wsService = wsService;
        this.config = config;
        this.songs = songs;
    }
    
    public WebSocketConnection? PlayerWSConnection
    {
        get { return _playerWSConnection; }
        set
        {
            _playerWSConnection = value;
            if (value is null)
            {
                PlayerDisconnected();
            }
            else
            {
                PlayerConnected();
            }
        }
    }

    public List<WebSocketConnection> WSConnections { get; } = new();

    public void OnMessage(WebSocketConnection connection, ControllerBase controller, string message)
    {
        //
    }
    
    
    public Task LogToPlayer(string ip, string message)
    {
        return SendToPlayer(new SimpleWSMessageModel()
        {
            ActionId = "log",
            Data = new()
            {
                { "ip", ip },
                { "message", message },
            }
        });
    }
    public Task LogToPlayer(ControllerBase controller, string message)
    {
        return LogToPlayer(controller.Request.HttpContext.Connection.RemoteIpAddress!.ToString(), message);
    }

    public Task SendNewSongCommand()
    {
        if (songs.SongsQueue.Any())
        {
            return SendToPlayer(new SimpleWSMessageModel()
            {
                ActionId = "new_song",
                Data = new() { { "songs", songs.AllSongs[songs.SongsQueue[0]] } },
            });
        }
        return Task.CompletedTask;
    }
    public Task SendInitDataCommand(WebSocketConnection conn)
    {
        return wsService.SendToUser(conn, new SimpleWSMessageModel()
        {
            ActionId = "init",
            Data = new()
            {
                { "songs", songs.SongsQueue.Select(x => songs.AllSongs[x]) },
                { "play", songs.Playing },
                { "volume", songs.Volume },
            },
        });
    }

    public Task SendToPlayer(WSMessageModelBase message)
    {
        AssertPlayerIsNotNull();
        return SendToUser(wsService.PlayerWSConnection!, message);
    }
    
    public async Task SendToAllUsers(WSMessageModelBase message)
    {
        await Task.WhenAll(WSConnections.Select(conn => SendToUser(conn, message)));
    }
    
    private void AssertPlayerIsNotNull()
    {
        if (_playerWSConnection == null)
        {
            throw new Exception("Player is not set.");
        }
    }
    public async Task SendToUser(WebSocketConnection conn, WSMessageModelBase message)
    {
        if (conn.WebSocket.State != WebSocketState.Open)
        {
            // WSConnections.Remove(conn); //todo think later
            return;
        }
        string messageStr = JsonSerializer.Serialize(message);
        var segments = new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageStr));
        await conn!.WebSocket.SendAsync(segments, WebSocketMessageType.Text, true, new());
    }

    public void PlayerConnected()
    {
        //todo implement
        Console.WriteLine("Player Connected");
    }
    
    public void PlayerDisconnected()
    {
        //todo implement
        Console.WriteLine("Player Disconnected");
    }
}
