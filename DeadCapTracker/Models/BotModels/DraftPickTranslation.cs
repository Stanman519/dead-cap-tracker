namespace DeadCapTracker.Models.BotModels
{
    public class DraftPickTranslation
    {
        public int CurrentOwner { get; set; }
        public int OriginalOwner { get; set; }
        public int Year { get; set; }
        public int Round { get; set; }
    }
}