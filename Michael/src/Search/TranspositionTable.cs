using System.Drawing;

namespace Michael.src.Search
{
    public static class TT
    {
        private static int TTIndex(ulong zobristKey)
            => (int)(zobristKey & (Searcher.TTSize - 1));

        /// <summary>
        /// Store an entry in the transposition table
        /// </summary>
        public static void StoreEntry(ref TTEntry[] TT, ulong hashKey, int depth, int eval, NodeType nodeType, Move bestMove)
        {
            TT[TTIndex(hashKey)] = new TTEntry
            {
                ZobristKey = hashKey,
                Depth = depth,
                Eval = eval,
                Type = nodeType,
                BestMove = bestMove
            };
        }

        /// <summary>
        /// Try to get an entry from the TT. Returns true only if the slot contains an entry
        /// whose ZobristKey matches the requested hashKey.
        /// </summary>
        public static bool TryGetEntry(TTEntry[] tt, ulong hashKey, out TTEntry entry)
        {
            entry = tt[TTIndex(hashKey)];
            // entry.ZobristKey must match the requested key
            if (entry.ZobristKey == hashKey)
            {
                return true;
            }
            // no valid entry for this key
            entry = default;
            return false;
        }
    }

    public enum NodeType
    {
        Exact,
        Alpha,
        Beta
    }

    public struct TTEntry
    {
        public ulong ZobristKey;
        public int Depth;
        public int Eval;
        public NodeType Type;
        public Move BestMove;
    }
}
