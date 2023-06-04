using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;

namespace PartyMusic.Services;

internal partial class WSConnectionsService
{
    
    public async Task OnMessage(WebSocketConnection connection, ControllerBase controller, string message)
    {
        if (await HandlePlayPause(connection, controller, message)) return;
        if (await HandleRestart(connection, controller, message)) return;
        if (await HandleNextSong(connection, controller, message)) return;
        if (await HandleSetVolume(connection, controller, message)) return;
    }

    private async Task<bool> HandlePlayPause(WebSocketConnection connection, ControllerBase controller, string message)
    {
        if (message == "song-play")
        {
            songs.Playing = true;
        }
        else if (message == "song-pause")
        {
            songs.Playing = false;
        }
        else if (message == "song-toggle-play-pause")
        {
            songs.Playing = !songs.Playing;
        }
        else
        {
            return false;
        }

        await SendToAllUsers(new SimpleWSMessageModel()
        {
            ActionId = "song-play-pause",
            Data = new() { { "play", songs.Playing } },
        });
        return true;
    }
    
    private async Task<bool> HandleNextSong(WebSocketConnection connection, ControllerBase controller, string message)
    {
        if (message != "song-next")
        {
            return false;
        }
        
        if (songs.SongsQueue.Count() < 2)
        {
            throw new Exception("Too few songs");
        }
        songs.SongsQueue.RemoveAt(0);
        await SendToPlayer(new SimpleWSMessageModel()
        {
            ActionId = "new_song",
            Data = new() { { "song", songs.AllSongs[songs.SongsQueue[0]] } }
        });
        await UpdateSongsAsync();
        return true;
    }
    private async Task<bool> HandleSetVolume(WebSocketConnection connection, ControllerBase controller, string message)
    {
        if (!message.StartsWith("volume-set"))
        {
            return false;
        }
        
        // Message is volume-set-0.345

        var volume = double.Parse(message.Split('-')[2].Trim());
        
        
        await SendToAllUsers(new SimpleWSMessageModel()
        {
            ActionId = "volume-set",
            Data = new() { { "volume", volume } },
        });
        
        return true;
    }

    private async Task<bool> HandleRestart(WebSocketConnection connection, ControllerBase controller, string message)
    {
        if (message != "song-restart")
        {
            return false;
        }

        var volume = double.Parse(message.Split('-')[2].Trim());
        
        
        await SendToAllUsers(new SimpleWSMessageModel()
        {
            ActionId = "song-restart",
        });
        
        return true;
    }
    
    private Task UpdateSongsAsync()
    {
        return SendToAllUsers(new SimpleWSMessageModel()
        {
            ActionId = "update_songs",
            Data = new() { { "songs", songs.SongsQueue.Select(x => songs.AllSongs[x]) } },
        });
    }
}
