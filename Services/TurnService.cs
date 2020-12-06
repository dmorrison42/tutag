using System;
using System.Collections.Generic;
using System.Linq;

using Tutag.Helpers;

namespace Tutag.Services
{
    public interface ITurnService
    {
        Models.Turn CurrentTurn { get; }

        event EventHandler OnUpdate;

        void CreateTurn(string prompt);
    }

    public class TurnService : ITurnService
    {
        private readonly IUserService _userService;
        static KafkaConnection<string, Models.Turn> _kafkaTurns;
        static Dictionary<string, Models.Turn> _turns = new Dictionary<string, Models.Turn>();

        private string RoomCode => _userService?.CurrentUser?.RoomCode;
        private bool IsAdmin => _userService?.CurrentUser?.IsAdmin == true;

        public Models.Turn CurrentTurn => _turns.ContainsKey(RoomCode)
            ? _turns[RoomCode]
            : null;

        private static event EventHandler<string> OnGlobalUpdate;
        public event EventHandler OnUpdate;

        static TurnService()
        {
            _kafkaTurns = new KafkaConnection<string, Models.Turn>("turns",
                nameof(VoteService), (result) =>
                {
                    var room = result.Message.Key;
                    var turn = result.Message.Value;
                    _turns[room] = new Models.Turn(turn.Prompt, turn.ParentId, (int)result.Offset.Value);
                    OnGlobalUpdate.Invoke(null, room);
                });
        }

        public TurnService(IUserService userService)
        {
            _userService = userService;

            OnGlobalUpdate += (o, e) =>
            {
                if (RoomCode == e)
                {
                    OnUpdate.Invoke(this, new EventArgs());
                }
            };
        }

        public void CreateTurn(string prompt)
        {
            if (_userService.CurrentUser?.IsAdmin != true)
            {
                throw new AccessViolationException("Not an admin stop making turns");
            }

            var parent = CurrentTurn?.Id ?? -1;

            _kafkaTurns.Produce(RoomCode, new Models.Turn(prompt, parent));
        }
    }
}