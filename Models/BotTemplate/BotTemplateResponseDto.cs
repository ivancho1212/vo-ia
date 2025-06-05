using System;
using System.Collections.Generic;

namespace Voia.Api.Models.DTOs
{
   public class BotTemplateResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int IaProviderId { get; set; }
    public int AiModelConfigId { get; set; }  // Añadido
    public int? DefaultStyleId { get; set; }
    public DateTime? CreatedAt { get; set; }  // Cambiado a nullable
    public DateTime? UpdatedAt { get; set; }  // Cambiado a nullable
}

}
