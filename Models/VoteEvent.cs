namespace Tutag.Models
{
    class VoteEvent
    {
        public VoteEvent(string username, int vote)
        {
            Username = username;
            Vote = vote;
        }

        public string Username { set; get; }
        public int Vote { set; get; }
    }
}