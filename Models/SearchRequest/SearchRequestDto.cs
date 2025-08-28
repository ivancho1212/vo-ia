namespace Voia.Api.Models.SearchRequest
{
    public class SearchRequestDto
    {
        public int BotId { get; set; }
        public string Query { get; set; } = string.Empty;
        public int Limit { get; set; } = 5;
    }
}
