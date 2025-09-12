using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Voia.Api.Models.Bots
{
    // DTOs para deserializar la respuesta de /api/Bots/{id}/context

    public class FullBotContextDto
    {
        [JsonPropertyName("botId")]
        public int BotId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("messages")]
        public List<MessageDto> Messages { get; set; }

        [JsonPropertyName("training")]
        public TrainingDataDto Training { get; set; }

        [JsonPropertyName("capture")]
        public CaptureDataDto Capture { get; set; }
    }

    public class MessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class TrainingDataDto
    {
        [JsonPropertyName("documents")]
        public List<string> Documents { get; set; }

        [JsonPropertyName("urls")]
        public List<string> Urls { get; set; }

        [JsonPropertyName("customTexts")]
        public List<string> CustomTexts { get; set; }

        [JsonPropertyName("vectors")]
        public List<object> Vectors { get; set; }
    }

    public class CaptureDataDto
    {
        [JsonPropertyName("fields")]
        public List<CaptureFieldDto> Fields { get; set; }
    }

    public class CaptureFieldDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }
    }
}
