namespace PartyMusic.Models.Core;

public class SongModel
{
    public required String Id { get; set; }
    public int? Duration { get; set; }
    public required string Title { get; set; }
}
