using System.Net.WebSockets;

namespace PartyMusic.Models.Core;

public class WebSocketConnection
{
    public required WebSocket WebSocket { get; init; }
    public Task? Task { get; init; }
    public required CancellationTokenSource CancellationTokenSource { get; init; }
}
