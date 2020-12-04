namespace Tutag.Models
{
    class Action
    {
        public Action(int turn, string action)
        {
            Turn = turn;
            Description = action;
        }

        public int Turn { set; get; }
        public string Description { set; get; }
    }
}