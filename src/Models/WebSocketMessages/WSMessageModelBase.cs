using System.Text.Json.Serialization;

namespace PartyMusic.Models.WebSocketMessages;

public abstract record WSMessageModelBase
{
    [JsonPropertyName("actionId")]
    public required string ActionId { get; init; }
}
