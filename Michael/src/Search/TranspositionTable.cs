namespace Michael.src.Search
{
    public struct TTEntry
    {
        public ulong ZobristKey { get; set; }
        public int Evaluation { get; set; }
        public int Depth { get; set; }
        public NodeType NodeType { get; set; }
        public Move bestMove { get; set; }
    }

    public enum NodeType
    {
        Upperbound,
        Lowerbound,
        Exact
    }
}
