using Newtonsoft.Json;

namespace Tutag.Models
{
    public class Action
    {
        public Action(int turn, string action, int id = -1)
        {
            Turn = turn;
            Description = action;
            Id = id;
        }

        public int Turn { set; get; }
        public string Description { set; get; }
        [JsonIgnore]
        public int Id { get; }
    }
}