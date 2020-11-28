using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.IdentityModel.Tokens.Jwt;

namespace Tutag.Entities
{
    public class User : GenericPrincipal
    {
        private JwtSecurityToken _token;

        public User(JwtSecurityToken token) : base(new ClaimsIdentity(token.Claims, "jtw"), new string[] { }) {
            _token = token;

            Username = token.Claims.First(x => x.Type == "user").Value;
            RoomCode = token.Claims.First(x => x.Type == "room").Value;
        }

        public string Username { get; }
        public string RoomCode { get; }
    }
}