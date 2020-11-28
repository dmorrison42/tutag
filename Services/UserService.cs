using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Text;

using Confluent.Kafka;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Tutag.Helpers;
using Tutag.Models;

namespace Tutag.Services
{
    public interface IUserService
    {
        string Authenticate(AuthenticateRequest model);
    }

    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        static IProducer<string, string> _producer = null;
        static IConsumer<string, string> _consumer = null;
        static string _topic = "authenticate";
        private static string _bootstrapServers = "localhost:9092";
        private static CancellationTokenSource _cancel = new CancellationTokenSource();
        private static Dictionary<string, List<string>> _users = new Dictionary<string, List<string>>();

        static UserService()
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                ClientId = "AuthenticationServer",
            };
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = System.Guid.NewGuid().ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe(_topic);
        }

        public UserService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public string Authenticate(AuthenticateRequest model)
        {
            _producer.Produce(_topic, new Message<string, string>
            {
                Key = model.RoomCode,
                Value = JObject.FromObject(model).ToString(),
            });

            var cancel = new CancellationTokenSource(1000);
            while (true)
            {
                var consumeResult = _consumer.Consume(cancel.Token);
                var msg = JObject.Parse(consumeResult.Message.Value).ToObject<Models.AuthenticateRequest>();

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