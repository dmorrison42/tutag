using System;
using System.Collections.Generic;
using System.Linq;

using Tutag.Helpers;

namespace Tutag.Services
{
    public interface IVoteService
    {
        IReadOnlyList<Models.Action> Actions { get; }
        int CountVotes(int actionId);
        int GetMyVote(int actionId);

        event EventHandler OnUpdate;

        void CastVote(int actionId, int value);
        void CreateAction(string action);
    }

    public class VoteService : IVoteService
    {
        private readonly IUserService _userService;
        private readonly ITurnService _turnService;
        static KafkaConnection<string, Models.Action> _kafkaActions;
        static KafkaConnection<string, Models.VoteEvent> _kafkaVotes;
        private static Dictionary<(string, int), List<Models.Action>> _actions =
            new Dictionary<(string, int), List<Models.Action>>();
        private static Dictionary<(string, int), Dictionary<string, int>> _votes =
            new Dictionary<(string, int), Dictionary<string, int>>();

        private int Turn => _turnService.CurrentTurn?.Id ?? -1;
        private string RoomCode => _userService?.CurrentUser?.RoomCode;
        private string Username => _userService?.CurrentUser?.Username;
        public IReadOnlyList<Models.Action> Actions => _actions.ContainsKey((RoomCode, Turn))
            ? _actions[(RoomCode, Turn)]
            : new List<Models.Action>();
        private Dictionary<string, int> Votes(int actionId) => _votes.ContainsKey((RoomCode, actionId))
            ? _votes[(RoomCode, actionId)]
            : new Dictionary<string, int>();
        public int GetMyVote(int actionId) => Votes(actionId).GetValueOrDefault(Username);
        public int CountVotes(int actionId) => Votes(actionId).Values.Sum();
        private static event EventHandler<string> OnGlobalUpdate;
        public event EventHandler OnUpdate;

        static VoteService()
        {
            _kafkaActions = new KafkaConnection<string, Models.Action>("actions",
                nameof(VoteService), (result) =>
                {
                    var room = result.Message.Key;
                    var action = result.Message.Value;
                    if (!_actions.ContainsKey((room, action.Turn)))
                    {
                        _actions[(room, action.Turn)] = new List<Models.Action>();
                    }
                    _actions[(room, action.Turn)].Add(new Models.Action(action.Turn, action.Description, (int)result.Offset.Value));
                    OnGlobalUpdate.Invoke(null, room);
                });

            _kafkaVotes = new KafkaConnection<string, Models.VoteEvent>("voting",
                nameof(VoteService), (result) =>
                {
                    var room = result.Message.Key;
                    var vote = result.Message.Value;
                    if (!_votes.ContainsKey((room, vote.ActionId)))
                    {
                        _votes[(room, vote.ActionId)] = new Dictionary<string, int>();
                    }
                    _votes[(room, vote.ActionId)][vote.Username] = vote.Vote;
                    OnGlobalUpdate.Invoke(null, room);
                });
        }

        public VoteService(IUserService userService, ITurnService turnService)
        {
            _userService = userService;
            _turnService = turnService;

            OnGlobalUpdate += (o, e) =>
            {
                if (RoomCode == e)
                {
                    OnUpdate.Invoke(this, new EventArgs());
                }
            };
        }

        public void CreateAction(string action)
        {
            if (_userService.CurrentUser?.IsAdmin != true)
            {
                throw new AccessViolationException("Not an admin stop making events");
            }

            _kafkaActions.Produce(RoomCode, new Models.Action(Turn, action));
        }

        public void CastVote(int actionId, int value)
        {
            _kafkaVotes.Produce(RoomCode, new Models.VoteEvent(Username, actionId, value));
        }
    }
}