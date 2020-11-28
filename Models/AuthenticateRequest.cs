using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Tutag.Models
{
    public class AuthenticateRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string RoomCode { get; set; }

        [JsonIgnore]
        public bool IsAdmin { get; set; } = false;
    }
}