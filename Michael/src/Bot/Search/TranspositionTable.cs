using Michael.src.MoveGen;

namespace Michael.src.Bot.Search
{
    public static class TranspositionTable
    {
        private const int TableSize = 8_388_608; // ~8 million entries
        private static TTEntry[] table = new TTEntry[TableSize];

        public static void Store(ulong key, TTEntry entry)
        {
            int index = (int)(key % TableSize);
            if (table[index].Depth <= entry.Depth)
                table[index] = entry;
        }

        public static bool TryGet(ulong key, out TTEntry entry)
        {
            int index = (int)(key % TableSize);
            entry = table[index];
            if (entry.ZobristKey == key)
                return true;
            return false;
        }
    }


    public enum NodeType { Exact, Alpha, Beta }

    public struct TTEntry
    {
        public ulong ZobristKey; // Unique board key
        public int Depth;        // Search depth at which this entry was stored
        public int Score;        // Evaluation score
        public NodeType Type;    // Node type for alpha-beta
        public Move BestMove;    // Best move found at this position
    }
}