using PartyMusic.Models.Core;

namespace PartyMusic.Services;

internal class SongsService
{
    public List<string> SongsQueue { get; } = new();
    
    public Dictionary<string, SongModel> AllSongs { get; } = new(); //todo This is a very bad decision 'cause it will use lots of memory. 

    public bool Playing { get; set; } = false;
    
    /// <summary> In range [0..1] </summary>
    public double Volume { get; set; }  = .5;

    public SongsService(
        IConfiguration config
    ) {
        
    }
}
