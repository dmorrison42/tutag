using Newtonsoft.Json;

namespace Tutag.Models
{
    public class Turn
    {
        public Turn(string prompt, int parentId, int id = -1)
        {
            Prompt = prompt;
            ParentId = parentId;
            Id = id;
        }

        public string Prompt { set; get; }

        public int ParentId { set; get; }

        [JsonIgnore]
        public int Id { get; }
    }
}