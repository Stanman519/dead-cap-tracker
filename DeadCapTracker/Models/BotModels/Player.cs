using DeadCapTracker.Models.MFL;
using DeadCapTracker.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class PlayerParent
    {
        public string version { get; set; }
        public string encoding { get; set; }
        public PlayersSingle players { get; set; }
    }
    public class PlayersParent
    {
        public string version { get; set; }
        public string encoding { get; set; }
        public PlayersMulti players { get; set; }
    }
    public class PlayersMulti
    {
        public string timestamp { get; set; }
        public string since { get; set; }
        [JsonConverter(typeof(SingleOrArrayConverter<Player>))]
        public List<Player> player { get; set; } = new List<Player>();
    }

    public class PlayersSingle
    {
        public string timestamp { get; set; }
        public string since { get; set; }
        public Player player { get; set; }
    }
    public class Player
    {
        public string position { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public string team { get; set; }
    }
    
}