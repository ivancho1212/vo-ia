using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Voia.Api.Models.BotProfiles
{
    public class CreateBotProfile
    {
        public int BotId { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public string PersonalityTraits { get; set; }
        public string Language { get; set; }
        public string Tone { get; set; }
        public string Restrictions { get; set; }
    }
}