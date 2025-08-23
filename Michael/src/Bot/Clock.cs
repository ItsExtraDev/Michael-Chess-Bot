namespace Michael.src.Bot
{
    public struct Clock
    {
        public int TimeLeftInMS { get; }
        //If there is no bonus time after X moves, set value to 0
        public int MovesToGo { get; }
        public int Incrament { get; }

        public Clock(int timeLeftInMS, int movesToGo, int incrament)
        {
            this.TimeLeftInMS = timeLeftInMS;
            this.MovesToGo = movesToGo;
            this.Incrament = incrament;
        }
    }
}
