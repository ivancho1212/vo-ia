using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models
{
using System.ComponentModel.DataAnnotations.Schema;

public class BotStyle
{
    public int Id { get; set; }
    public int BotId { get; set; }
    public string Theme { get; set; }
    public string PrimaryColor { get; set; }
    public string SecondaryColor { get; set; }
    public string FontFamily { get; set; }
    public string AvatarUrl { get; set; }
    public string Position { get; set; }
    public string CustomCss { get; set; }
    public DateTime UpdatedAt { get; set; }
}

}
