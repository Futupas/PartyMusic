using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;

namespace PartyMusic.Services;

internal partial class WSConnectionsService
{
    
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
        return SendToUser(conn, new SimpleWSMessageModel()
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
        return SendToUser(PlayerWSConnection!, message);
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

}
