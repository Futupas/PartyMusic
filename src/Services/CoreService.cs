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
}
