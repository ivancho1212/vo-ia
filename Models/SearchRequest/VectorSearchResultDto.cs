namespace Voia.Api.Models.SearchRequest
{
    public class VectorSearchResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
