// using Microsoft.AspNetCore.Mvc;
// using System.Net.WebSockets;
// using System.Text;
//
//
// namespace PartyMusic.Controllers;
//
// [ApiController]
// [Route("/api/[controller]")]
// public class WSController : ControllerBase
// {
//     private (WebSocket socket, Task task, CancellationTokenSource cancellationTokenSource) socketData {get; set;} = new();
//     private const int MAX_WS_RECEIVE_BYTES_COUNT = 100;
//
//     private async Task ExecuteWsData()
//     {
//         for (var i = 0; (i < 20 && socketData.socket.State == WebSocketState.Open && !socketData.cancellationTokenSource.IsCancellationRequested); i++)
//         {
//             var segments = new ArraySegment<byte>(Encoding.UTF8.GetBytes("hello world"));
//             await socketData.socket.SendAsync(segments, WebSocketMessageType.Text, true, new());
//             await Task.Delay(1000);
//         }
//
//         if (socketData.socket.State != WebSocketState.Open || socketData.cancellationTokenSource.IsCancellationRequested)
//         {
//             System.Console.WriteLine("WS was cancelled from out");
//         }
//         else
//         {
//             await socketData.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, new());
//         }
//         socketData.cancellationTokenSource.Cancel(false);
//         socketData.task.Dispose();
//         System.Console.WriteLine($"WS was cancelled");
//     }
//
//
//     [HttpGet("/ws")]
//     public async Task GetWs()
//     {
//         if (!HttpContext.WebSockets.IsWebSocketRequest)
//         {
//             HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
//             return;
//         }
//
//         using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
//         var cancellationTokenSource = new CancellationTokenSource();
//
//         System.Console.WriteLine($"WS connected (HashCode: {this.GetHashCode()})");
//
//         var task = Task.Run(() => { ExecuteWsData(); }, cancellationTokenSource.Token);
//
//         socketData = (webSocket, task, cancellationTokenSource);
//
//         while (!cancellationTokenSource.Token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
//         {
//             var segments = new ArraySegment<byte>(new byte[MAX_WS_RECEIVE_BYTES_COUNT], 0, MAX_WS_RECEIVE_BYTES_COUNT);
//             var receiveResult = await webSocket.ReceiveAsync(segments, cancellationTokenSource.Token);
//             var receivedMessageCount = receiveResult.Count;
//             var segmentsReal = segments[0..receivedMessageCount];
//             if (receiveResult.MessageType == WebSocketMessageType.Text)
//             {
//                 System.Console.WriteLine("Received webhook: " + Encoding.UTF8.GetString(segmentsReal));
//             }
//             else
//             {
//                 System.Console.WriteLine("Received webhook message type: " + receiveResult.MessageType);
//             }
//             
//         }
//         
//         System.Console.WriteLine($"WS disconnected");
//     }
// }
//
