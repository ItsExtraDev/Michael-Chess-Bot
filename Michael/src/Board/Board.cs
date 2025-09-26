using Michael.src.Helpers;
using Michael.src.MoveGen;

namespace Michael.src
{
    /// <summary>
    /// Represents the main board class in the chess engine.
    /// 
    /// Manages all board-related state, including:
    /// - Piece bitboards
    /// - Turn tracking
    /// - Check status
    /// - Castling rights, en passant, etc.
    ///
    /// Provides core methods such as:
    /// - <c>MakeMove</c>: Executes a given move.
    /// - <c>GetLegalMoves</c>: Returns all legal moves for the current position.
    /// 
    /// This class serves as the central interface for querying and updating board state.
    /// </summary>
    public class Board
    {

        public ulong[] PiecesBitboards = new ulong[12];
        public ulong[] ColoredBitboards = new ulong[3];
        public int[] Squares = new int[64];
        public int ColorToMove;
        public int HalfmoveClock = 0;
        public int CurrentGameState;
        public List<int> GameStateHistory = new List<int>();
        public List<int> HalfmoveClockHistory = new List<int>();
        public int EnPassantSquare = 0;
        public bool IsWhiteToMove => ColorToMove == Piece.White;
        private bool InCheck;
        private bool hasCachedCheck = false;
        public int CasltingRight;
        public int plyCount = 0;
        private ulong LastHash;
        public ulong CurrentHash;
        private List<ulong> hashHistory = new();
        public Dictionary<ulong, int> repetitionCounts = new Dictionary<ulong, int>();

        public Board(string fenString = FEN.StartingFEN)
        {
            LoadFen(fenString);
            CurrentHash = Zobrist.ComputeHash(this);
            repetitionCounts[CurrentHash] = 1;
            GameStateHistory.Clear();
        }

        public bool IsInCheck()
        {
            if (hasCachedCheck)
                return InCheck;
            InCheck = MoveGenerator.InCheck(this);
            hasCachedCheck = true;
            return InCheck;
        }

        public bool IsCheckmate() => IsInCheck() && GetLegalMoves().Length == 0;
        public bool IsInStalemate() => false;//!IsInCheck() && GetLegalMoves().Length == 0;
        public bool IsThreefoldRepetition() =>
            repetitionCounts.TryGetValue(CurrentHash, out int count) && count >= 3;
        public bool IsDraw() =>
            IsInStalemate() || IsFiftyMoveRuleDraw() || IsThreefoldRepetition();
        public bool IsFiftyMoveRuleDraw() => HalfmoveClock >= 100;

        private void LoadFen(string fenString)
        {
            FEN.LoadFEN(this, fenString);
            CurrentGameState = GameState.MakeGameState(Piece.None, Piece.None, EnPassantSquare, CasltingRight);
            CurrentHash = Zobrist.ComputeHash(this);
            repetitionCounts[CurrentHash] = 1;
            HalfmoveClockHistory.Add(HalfmoveClock);
        }

        public Move[] GetLegalMoves(bool GenerateCapturesOnly = false)
        {
            Span<Move> legalMovesSpan = stackalloc Move[MoveGenerator.MaxLegalMoves];
            MoveGenerator.GenerateLegalMoves(this, ref legalMovesSpan, GenerateCapturesOnly);
            return legalMovesSpan.ToArray();
        }

        public void MakeNullMove()
        {
            ColorToMove ^= 1;
            LastHash = CurrentHash;
            CurrentHash = Zobrist.ComputeHash(this);
            if (!repetitionCounts.ContainsKey(CurrentHash))
                repetitionCounts[CurrentHash] = 1;
            else
                repetitionCounts[CurrentHash]++;
        }

        public void UndoNullMove()
        {
            ColorToMove ^= 1;
            if (repetitionCounts.ContainsKey(CurrentHash))
            {
                repetitionCounts[CurrentHash]--;
                if (repetitionCounts[CurrentHash] == 0)
                    repetitionCounts.Remove(CurrentHash);
            }
            CurrentHash = LastHash;
        }

        public void MakeMove(Move move)
        {
            int from = move.StartingSquare;
            int to = move.TargetSquare;
            int movingPiece = Squares[from];
            int movingPieceType = Piece.PieceType(movingPiece);
            int movingColor = ColorToMove;
            int oppColor = movingColor ^ 1;

            int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, movingColor);
            ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
            ref ulong colorBB = ref ColoredBitboards[movingColor];
            ref ulong oppColorBB = ref ColoredBitboards[oppColor];
            ref ulong emptyBB = ref ColoredBitboards[2];

            int capturedPiece = Squares[to];
            bool isCapture = (capturedPiece != Piece.None);

            // move on squares
            Squares[from] = Piece.None;
            Squares[to] = movingPiece;

            // update bitboards
            BitboardHelper.MovePiece(ref movingBitboard, from, to);
            BitboardHelper.MovePiece(ref colorBB, from, to);

            // empty squares: from empty, to occupied
            BitboardHelper.ToggleBit(ref emptyBB, from);
            BitboardHelper.ToggleBit(ref emptyBB, to);

            // castling rights update
            const int WHITE_KINGSIDE = 1 << 0;
            const int WHITE_QUEENSIDE = 1 << 1;
            const int BLACK_KINGSIDE = 1 << 2;
            const int BLACK_QUEENSIDE = 1 << 3;

            if (movingPieceType == Piece.King)
            {
                int clearMask = (movingColor == Piece.White) ? (WHITE_KINGSIDE | WHITE_QUEENSIDE)
                                                             : (BLACK_KINGSIDE | BLACK_QUEENSIDE);
                CasltingRight &= ~clearMask;
            }
            else if (movingPieceType == Piece.Rook)
            {
                switch (from)
                {
                    case 0: CasltingRight &= ~WHITE_QUEENSIDE; break;
                    case 7: CasltingRight &= ~WHITE_KINGSIDE; break;
                    case 56: CasltingRight &= ~BLACK_QUEENSIDE; break;
                    case 63: CasltingRight &= ~BLACK_KINGSIDE; break;

                }

            }

            // promotion
            if (move.IsPromotion())
            {
                int pawnIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, movingColor);
                ref ulong pawnBB = ref PiecesBitboards[pawnIndex];
                if ((pawnBB & (1UL << to)) != 0)
                    BitboardHelper.ToggleBit(ref pawnBB, to);

                int promoteType = (int)move.MoveFlag;
                int promoteIndex = BitboardHelper.GetBitboardIndex(promoteType, movingColor);
                ref ulong promoteBB = ref PiecesBitboards[promoteIndex];
                BitboardHelper.ToggleBit(ref promoteBB, to);
                Squares[to] = Piece.CreatePiece(promoteType, movingColor);
            }
            if (isCapture)
            {
                int capType = Piece.PieceType(capturedPiece);
                int capIndex = BitboardHelper.GetBitboardIndex(capType, oppColor);
                ref ulong capBB = ref PiecesBitboards[capIndex];
                BitboardHelper.ToggleBit(ref capBB, to);
                BitboardHelper.ToggleBit(ref oppColorBB, to);
                BitboardHelper.ToggleBit(ref emptyBB, to);

                if (capType == Piece.Rook)
                {
                    switch (to)
                    {
                        case 0: CasltingRight &= ~WHITE_QUEENSIDE; break;
                        case 7: CasltingRight &= ~WHITE_KINGSIDE; break;
                        case 56: CasltingRight &= ~BLACK_QUEENSIDE; break;
                        case 63: CasltingRight &= ~BLACK_KINGSIDE; break;
                    }
                }
            }
            // en-passant
            else if (move.MoveFlag == MoveFlag.EnPassant)
            {
                int epCapturedSquare = to + (movingColor == Piece.White ? -8 : 8);
                Squares[epCapturedSquare] = Piece.None;
                int capturedPawnIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, oppColor);
                ref ulong capturedPawnBB = ref PiecesBitboards[capturedPawnIndex];
                BitboardHelper.ToggleBit(ref capturedPawnBB, epCapturedSquare);
                BitboardHelper.ToggleBit(ref oppColorBB, epCapturedSquare);
                BitboardHelper.ToggleBit(ref emptyBB, epCapturedSquare);
                isCapture = true;
                capturedPiece = Piece.CreatePiece(Piece.Pawn, oppColor);
            }
            else if (move.IsCastle())
            {
                int rookFrom = (move.MoveFlag == MoveFlag.CastleShort) ? to + 1 : to - 2;
                int rookTo = (move.MoveFlag == MoveFlag.CastleShort) ? to - 1 : to + 1;
                int rookIndex = BitboardHelper.GetBitboardIndex(Piece.Rook, movingColor);
                ref ulong rookBB = ref PiecesBitboards[rookIndex];
                int rookPiece = Squares[rookFrom];
                Squares[rookFrom] = Piece.None;
                Squares[rookTo] = rookPiece;
                BitboardHelper.MovePiece(ref rookBB, rookFrom, rookTo);
                BitboardHelper.MovePiece(ref colorBB, rookFrom, rookTo);
                BitboardHelper.ToggleBit(ref emptyBB, rookFrom);
                BitboardHelper.ToggleBit(ref emptyBB, rookTo);
                int clrMask = (movingColor == Piece.White) ? (WHITE_KINGSIDE | WHITE_QUEENSIDE)
                                                           : (BLACK_KINGSIDE | BLACK_QUEENSIDE);
                CasltingRight &= ~clrMask;
            }

            EnPassantSquare = 0;
            if (move.MoveFlag == MoveFlag.DoublePawnPush)
                EnPassantSquare = to + (movingColor == Piece.White ? -8 : 8);

            HalfmoveClock++;
            if (movingPieceType == Piece.Pawn || isCapture)
                HalfmoveClock = 0;

            GameStateHistory.Add(CurrentGameState);
            CurrentGameState = 0;
            CurrentGameState = GameState.MakeGameState(capturedPiece, movingPiece, EnPassantSquare, CasltingRight);
            ColorToMove = oppColor;
            HalfmoveClockHistory.Add(HalfmoveClock);
            plyCount++;
            hasCachedCheck = false;
            hashHistory.Add(CurrentHash);
            CurrentHash = Zobrist.ComputeHash(this);
            if (!repetitionCounts.ContainsKey(CurrentHash))
                repetitionCounts[CurrentHash] = 1;
            else
                repetitionCounts[CurrentHash]++;
        }
     

        public void UndoMove(Move move)
        {
            if (repetitionCounts.ContainsKey(CurrentHash))
            {
                repetitionCounts[CurrentHash]--;
                if (repetitionCounts[CurrentHash] == 0)
                    repetitionCounts.Remove(CurrentHash);
            }

            int movingPiece = GameState.MovingPiece(CurrentGameState);
            int movingPieceType = Piece.PieceType(movingPiece);
            int capturedPiece = GameState.CapturedPiece(CurrentGameState);

            ColorToMove ^= 1; // flip back first so ColorToMove = side that moved
            int moverColor = ColorToMove;

            // undo promotion separately
            if (move.IsPromotion())
            {
                int promoteType = move.MoveFlag;
                int promoteIndex = BitboardHelper.GetBitboardIndex(promoteType, moverColor);

                ref ulong promoteBB = ref PiecesBitboards[promoteIndex];
                if ((promoteBB & (1UL << move.TargetSquare)) != 0)
                    BitboardHelper.ToggleBit(ref promoteBB, move.TargetSquare);

                int pawnIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, moverColor);
                ref ulong pawnBB = ref PiecesBitboards[pawnIndex];
                BitboardHelper.ToggleBit(ref pawnBB, move.StartingSquare);

                Squares[move.StartingSquare] = Piece.CreatePiece(Piece.Pawn, moverColor);
                Squares[move.TargetSquare] = Piece.None;

                BitboardHelper.ToggleBit(ref ColoredBitboards[moverColor], move.TargetSquare);
                BitboardHelper.ToggleBit(ref ColoredBitboards[moverColor], move.StartingSquare);
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.StartingSquare);
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.TargetSquare);
            }
            else
            {
                int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, moverColor);

                ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
                Squares[move.TargetSquare] = Piece.None;
                Squares[move.StartingSquare] = movingPiece;
                BitboardHelper.MovePiece(ref movingBitboard, move.TargetSquare, move.StartingSquare);
                BitboardHelper.MovePiece(ref ColoredBitboards[moverColor], move.TargetSquare, move.StartingSquare);
                BitboardHelper.MovePiece(ref ColoredBitboards[2], move.StartingSquare, move.TargetSquare);
            }

            CurrentGameState = GameStateHistory.Last();
            GameStateHistory.RemoveAt(GameStateHistory.Count - 1);
            EnPassantSquare = GameState.GetEnPassantSquare(CurrentGameState);
            CasltingRight = GameState.GetCastlingRights(CurrentGameState);
            HalfmoveClockHistory.RemoveAt(HalfmoveClockHistory.Count - 1);
            HalfmoveClock = HalfmoveClockHistory.Last();
            plyCount--;
            hasCachedCheck = false;
            CurrentHash = hashHistory.Last();
            hashHistory.RemoveAt(hashHistory.Count - 1);

            if (move.MoveFlag == MoveFlag.EnPassant)
            {
                int enPassantSquare = move.TargetSquare + (moverColor == Piece.White ? -8 : 8);
                Squares[enPassantSquare] = Piece.CreatePiece(Piece.Pawn, moverColor ^ 1);
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, moverColor ^ 1);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref capturedBitboard, enPassantSquare);
                BitboardHelper.ToggleBit(ref ColoredBitboards[moverColor ^ 1], enPassantSquare);
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], enPassantSquare);
                return;
            }
            if (capturedPiece != Piece.None)
            {
                int capturedPieceType = Piece.PieceType(capturedPiece);
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(capturedPieceType, moverColor ^ 1);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.TargetSquare);
                BitboardHelper.ToggleBit(ref capturedBitboard, move.TargetSquare);
                BitboardHelper.ToggleBit(ref ColoredBitboards[moverColor ^ 1], move.TargetSquare);
                Squares[move.TargetSquare] = capturedPiece;
                return;
            }
            if (move.IsCastle())
            {
                int rookStartSquare = move.MoveFlag == MoveFlag.CastleShort ? move.TargetSquare + 1 : move.TargetSquare - 2;
                int rookTargetSquare = move.TargetSquare + (move.MoveFlag == MoveFlag.CastleShort ? -1 : 1);
                int rookPiece = Squares[rookTargetSquare];
                Squares[rookTargetSquare] = Piece.None;
                Squares[rookStartSquare] = rookPiece;
                int rookBitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Rook, moverColor);
                ref ulong rookBitboard = ref PiecesBitboards[rookBitboardIndex];
                BitboardHelper.MovePiece(ref rookBitboard, rookStartSquare, rookTargetSquare);
                BitboardHelper.MovePiece(ref ColoredBitboards[moverColor], rookStartSquare, rookTargetSquare);
                BitboardHelper.MovePiece(ref ColoredBitboards[2], rookStartSquare, rookTargetSquare);
            }
        }
    }
}
