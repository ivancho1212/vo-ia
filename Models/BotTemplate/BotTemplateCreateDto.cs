using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

public class BotTemplateCreateDto
{
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [Required]
    [JsonPropertyName("iaProviderId")]
    public int IaProviderId { get; set; }

    [Required]
    [JsonPropertyName("aiModelConfigId")]
    public int AiModelConfigId { get; set; }

    [JsonPropertyName("defaultStyleId")]
    public int? DefaultStyleId { get; set; }
}
