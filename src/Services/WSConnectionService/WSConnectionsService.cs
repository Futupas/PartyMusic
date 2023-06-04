using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;

namespace PartyMusic.Services;

/// <summary> This is the main class with the majority of logic </summary>
internal partial class WSConnectionsService
{
    private WebSocketConnection? _playerWSConnection = null;

    private readonly ILogger<WSConnectionsService> logger;
    private readonly IConfiguration config;
    private readonly SongsService songs;
    private readonly YoutubeService youtube;
    
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

    
    
   
    public WSConnectionsService(
        ILogger<WSConnectionsService> logger,
        IConfiguration config,
        SongsService songs,
        YoutubeService youtube
    ) {
        this.logger = logger;
        this.config = config;
        this.songs = songs;
        this.youtube = youtube;
    }

    public void PlayerConnected()
    {
        //todo implement
        logger.LogInformation("Player Connected");
    }
    
    public void PlayerDisconnected()
    {
        //todo implement
        logger.LogWarning("Player Disconnected");
    }
}
