namespace Voia.Api.Models.Messages
{
    public class ConversationItemDto
{
    public string Type { get; set; }           // "message" | "file"
    public string Text { get; set; }           // solo para mensajes
    public string FileName { get; set; }       // solo para archivos
    public string FileType { get; set; }
    public string FileUrl { get; set; }

    public DateTime Timestamp { get; set; }

    // NUEVOS CAMPOS:
    public int? FromId { get; set; }           // userId o botId
    public string FromName { get; set; }
    public string FromRole { get; set; }       // "user", "bot", "admin"
    public string FromAvatarUrl { get; set; }  // si lo tienes
}


}
