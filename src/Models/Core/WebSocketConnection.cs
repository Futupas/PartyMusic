using System.Net.WebSockets;

namespace PartyMusic.Models.Core;

public class WebSocketConnection
{
    public required WebSocket WebSocket { get; set; }
    public Task? Task { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
}
