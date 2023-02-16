using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class Message
    {
        public string text { get; set; }
        public string bot_id { get; set; }
        public List<Mention> attachments { get; set; }
        
       
        public Message(string _text, string _botId)
        {
            bot_id = _botId;
            text = _text;
        }

        public Message(string _text)
        {
            text = _text;
            bot_id = "6cac06cf66a381ec605c8fff25";
        }
    }

    public class Mention
    {
        public string type { get; set; }
        public int[][] loci { get; set; }
        public string[] user_ids{ get; set; }
    }
}