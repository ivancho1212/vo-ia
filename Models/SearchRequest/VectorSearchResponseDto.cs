using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Voia.Api.Models.SearchRequest
{
    public class VectorSearchResponseDto
    {
        [JsonPropertyName("results")]
        public List<VectorSearchResultDto> Results { get; set; } = new();
    }
}
