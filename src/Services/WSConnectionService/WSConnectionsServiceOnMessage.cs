using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;

namespace PartyMusic.Services;

internal partial class WSConnectionsService
{
    
    public void OnMessage(WebSocketConnection connection, ControllerBase controller, string message)
    {
        //
    }

}
