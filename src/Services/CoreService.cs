using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;

namespace PartyMusic.Services;

internal class CoreService
{
    public List<string> SongsQueue { get; } = new();
    public Dictionary<string, SongModel> AllSongs { get; } = new(); //todo This is a very bad decision 'cause it will use lots of memory. 

    public WebSocketConnection? PlayerWSConnection { get; set; } = null;
    public List<WebSocketConnection> WSConnections { get; } = new();

    public bool Playing { get; set; } = false;
    public double Volume { get; set; } = 50.0;
    
    
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

    public void AssertPlayerIsNotNull()
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
