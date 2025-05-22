public class BotTrainingConfigCreateDto
{    
    public int BotId { get; set; }
    public string TrainingType { get; set; } // "url", "form_data", etc.
    public string Data { get; set; }
}

