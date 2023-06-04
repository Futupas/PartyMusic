using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartyMusic.Models.Core;
using PartyMusic.Models.WebSocketMessages;

namespace PartyMusic.Services;

internal partial class WSConnectionsService
{
    private WebSocketConnection? _playerWSConnection = null;

    private readonly ILogger<WSConnectionsService> logger;
    private readonly IConfiguration config;
    private readonly SongsService songs;
    private readonly YoutubeService youtube;
    private readonly HelpersService helper;
    
    public WSConnectionsService(
        ILogger<WSConnectionsService> logger,
        IConfiguration config,
        SongsService songs,
        YoutubeService youtube,
        HelpersService helper
    ) {
        this.logger = logger;
        this.config = config;
        this.songs = songs;
        this.youtube = youtube;
        this.helper = helper;
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
