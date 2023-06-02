using System.Text.Json.Serialization;

namespace PartyMusic.Models.WebSocketMessages;

public record SimpleWSMessageModel : WSMessageModelBase
{
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; init; } = new();
}
