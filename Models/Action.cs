using Newtonsoft.Json;

namespace Tutag.Models
{
    public class Action
    {
        public Action(int turn, string action, int actionId = -1)
        {
            Turn = turn;
            Description = action;
            Id = actionId;
        }

        public int Turn { set; get; }
        public string Description { set; get; }
        [JsonIgnore]
        public int Id { get; }
    }
}