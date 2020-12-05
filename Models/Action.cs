using Newtonsoft.Json;

namespace Tutag.Models
{
    public class Action
    {
        public Action(int turn, string action, int actionId = -1)
        {
            Turn = turn;
            Description = action;
            ActionId = actionId;
        }

        public int Turn { set; get; }
        public string Description { set; get; }
        [JsonIgnore]
        public int ActionId { get; }
    }
}