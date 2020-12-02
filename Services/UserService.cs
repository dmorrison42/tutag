using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Text;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

using Tutag.Helpers;
using Tutag.Models;

namespace Tutag.Services
{
    public interface IUserService
    {
        string Authenticate(AuthenticateRequest model);
        Entities.User CurrentUser { get; }
        string[] Users { get; }
    }

    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        private readonly AuthenticationStateProvider _authProvider;
        static KafkaConnection<string, Models.AuthenticateRequest> _kafka;
        private static Dictionary<string, List<string>> _users = new Dictionary<string, List<string>>();

        public Entities.User CurrentUser
        {
            get
            {
                var authState = _authProvider.GetAuthenticationStateAsync();
                return authState.Wait(500)
                    ? authState.Result?.User as Tutag.Entities.User
                    : null;
            }
        }

        public string[] Users => _users.ContainsKey(CurrentUser?.RoomCode)
            ? _users[CurrentUser?.RoomCode].ToArray()
            : null;


        static UserService()
        {
            _kafka = new KafkaConnection<string, Models.AuthenticateRequest>("authenticate", "AuthenticationServer");
        }

        public UserService(IOptions<AppSettings> appSettings, AuthenticationStateProvider auth)
        {
            _appSettings = appSettings.Value;
            _authProvider = auth;
        }

        public string Authenticate(AuthenticateRequest model)
        {
            _kafka.Produce(model.RoomCode, model);

            var cancel = new CancellationTokenSource(1000);
            while (true)
            {
                var consumeResult = _kafka.Consumer.Consume(cancel.Token);
                var msg = consumeResult.Message.Value;

                if (!_users.ContainsKey(msg.RoomCode))
                {
                    _users[msg.RoomCode] = new List<string>();
                }

                if (!_users[msg.RoomCode].Contains(msg.Username))
                {
                    _users[msg.RoomCode].Add(msg.Username);
                }

                if (msg.RoomCode == model.RoomCode && msg.Username == model.Username)
                {
                    model.IsAdmin = _users[msg.RoomCode].FirstOrDefault() == model.Username;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.RoomCode)) return null;

            // authentication successful so generate jwt token
            return generateJwtToken(model);
        }

        // helper methods

        private string generateJwtToken(AuthenticateRequest user)
        {
            // generate token that is valid for 7 days
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim("user", user.Username.ToString()),
                    new Claim("room", user.RoomCode.ToString()),
                    new Claim("isAdmin", user.IsAdmin.ToString()),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}