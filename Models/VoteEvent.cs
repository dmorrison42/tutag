namespace Tutag.Models
{
    class VoteEvent
    {
        public VoteEvent(string username, int actionId, int vote)
        {
            Username = username;
            ActionId = actionId;
            Vote = vote;
        }

        public string Username { set; get; }
        public int Vote { set; get; }
        public int ActionId { set; get; }
    }
}