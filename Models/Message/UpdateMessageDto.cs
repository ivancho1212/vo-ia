namespace Voia.Api.Models.Messages.DTOs
{
    /// <summary>
    /// DTO original para actualizar mensajes vía HTTP PUT.
    /// Usado por MessagesController para actualizar campos generales del mensaje.
    /// </summary>
    public class UpdateMessageDto
    {
        public int BotId { get; set; }
        public int? UserId { get; set; }
        public int? PublicUserId { get; set; }
        public int ConversationId { get; set; }
        public string? Sender { get; set; }
        public string? MessageText { get; set; }
        public int? TokensUsed { get; set; }
        public string? Source { get; set; }
        public int? ReplyToMessageId { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un mensaje después de que el archivo ha sido procesado.
    /// Arquitectura profesional: el cliente envía messageId temporal, y el servidor vincula el fileId.
    /// Usado por ChatHub.UpdateMessage() para notificar estado del upload.
    /// </summary>
    public class UpdateFileUploadStatusDto
    {
        /// <summary>
        /// ID temporal del mensaje generado por el cliente (UUID).
        /// Permite vincular el mensaje provisional con el archivo procesado.
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// ID de la conversación.
        /// </summary>
        public int ConversationId { get; set; }

        /// <summary>
        /// ID del archivo en la base de datos (FK a ChatUploadedFiles).
        /// Se envía cuando el upload completó exitosamente.
        /// </summary>
        public int? FileId { get; set; }

        /// <summary>
        /// URL pública del archivo (ej: /api/files/chat/123/inline).
        /// Permite que el cliente actualice la imagen con la URL del servidor.
        /// </summary>
        public string? FileUrl { get; set; }

        /// <summary>
        /// Estado del mensaje: "pending", "uploading", "sent", "failed".
        /// </summary>
        public string Status { get; set; } = "sent";

        /// <summary>
        /// Progreso del upload en porcentaje (0-100).
        /// Opcional: solo si se está enviando actualizaciones de progreso.
        /// </summary>
        public int? UploadProgress { get; set; }
    }
}
