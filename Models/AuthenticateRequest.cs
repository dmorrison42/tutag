using System.ComponentModel.DataAnnotations;

namespace Tutag.Models
{
    public class AuthenticateRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string RoomCode { get; set; }
    }
}