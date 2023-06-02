using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;
using PartyMusic.Services;

namespace PartyMusic.Controllers;

[ApiController]
[Route("/api/ws")]
internal class WSController : ControllerBase
{
    private readonly ILogger<WSController> logger;
    private readonly WSConnectionsService wsService;
    private readonly IConfiguration config;
    
    const int MAX_WS_RECEIVE_BYTES_COUNT = 256; //todo mb it should be changed
    
    private WebSocketConnection? myWSConnection = null;
    
    
    public WSController(
        ILogger<WSController> logger,
        WSConnectionsService wsService,
        IConfiguration config
    ) {
        this.logger = logger;
        this.wsService = wsService;
        this.config = config;
    }
    
    [HttpGet("connect")]
    public async Task GetWs(string isPlayer = "no")
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var cancellationTokenSource = new CancellationTokenSource();

        wsService.LogToPlayer(this, "WS connected");
        
        myWSConnection = new()
        {
            WebSocket = webSocket,
            CancellationTokenSource = cancellationTokenSource,
        };

        if (isPlayer == "yes")
        {
            wsService.PlayerWSConnection = myWSConnection;
            await wsService.SendNewSongCommand();
        }
        
        wsService.WSConnections.Add(myWSConnection);

        await wsService.SendInitDataCommand(myWSConnection);
        
        while (!cancellationTokenSource.Token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var segments = new ArraySegment<byte>(new byte[MAX_WS_RECEIVE_BYTES_COUNT], 0, MAX_WS_RECEIVE_BYTES_COUNT);
            var receiveResult = await webSocket.ReceiveAsync(segments, cancellationTokenSource.Token);
            var receivedMessageCount = receiveResult.Count;
            var segmentsReal = segments[0..receivedMessageCount];
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(segmentsReal);
                Console.WriteLine("Received webhook: " + message);
                wsService.OnMessage(myWSConnection, this, message);
            }
            else
            {
                wsService.LogToPlayer(this,"Received webhook message type: " + receiveResult.MessageType);
            }
            
        }
        
        wsService.WSConnections.Remove(myWSConnection);

        if (wsService.PlayerWSConnection == myWSConnection)
        {
            wsService.PlayerWSConnection = null;
            // await playerWSConnection.WebSocket.CloseAsync();
            // playerWSConnection.CancellationTokenSource.Cancel();
            // playerWSConnection = null;
            // PlayerDisconnected();
        }

        myWSConnection = null;
        wsService.LogToPlayer(this, "WS disconnected");
    }

}
