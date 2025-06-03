public class AiModelConfigDto
{
    public int Id { get; set; }
    public string ModelName { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? FrequencyPenalty { get; set; }
    public decimal? PresencePenalty { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int IaProviderId { get; set; }
    public string IaProviderName { get; set; }  // <-- para mostrar el nombre del proveedor
}
