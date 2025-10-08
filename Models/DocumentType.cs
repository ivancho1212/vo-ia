using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
    [Table("document_types")]
    public class DocumentType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Abbreviation { get; set; }
        public string? Description { get; set; }
    }
}