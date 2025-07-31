public class ConversationPreviewDto
{
    public string Type { get; set; }           // "message" o "image"
    public string Content { get; set; }        // Texto del mensaje o nombre de archivo
    public string Url { get; set; }            // Solo para imágenes
    public DateTime Timestamp { get; set; }
}
