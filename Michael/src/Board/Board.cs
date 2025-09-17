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
        /// <summary>
        /// This project uses bitboards for move generation and board evaluation.
        /// Bitboards are an efficient way to represent the board and pieces, 
        /// with bitboards each piece type has its own bitboard. This allows us to
        /// avoid looping through the entire board — instead, we analyze binary numbers 
        /// to determine occupied squares and possible attacks.
        ///
        /// Each index in the array corresponds to a different piece type and color:
        /// index 0 = white pawn, index 1 = white knight, etc.
        ///
        /// More information can be found at: https://www.chessprogramming.org/Bitboards
        /// </summary>
        public ulong[] PiecesBitboards = new ulong[12];
        public ulong[] ColoredBitboards = new ulong[3];

        /// <summary>
        /// Each index in the array correlates to the corresponding square in the board.
        /// Unlike bitboards, which are very fast to show which square is occupied and by
        /// which color, this array is mostly used to determaine which piece is occuping the square,
        /// which is not possible with regular bitboards.
        /// </summary>
        public int[] Squares = new int[64];

        //Current turn of the game.
        public int ColorToMove;

        // Halfmove clock for the 50-move rule (counts halfmoves since last capture or pawn move)
        public int HalfmoveClock = 0;

        //The game state used to track the current state of the game.
        public int CurrentGameState;

        //Contains the history of game states for undo functionality.
        public List<int> GameStateHistory = new List<int>();
        public List<Move> moveHistory = new List<Move>();
        public List<int> HalfmoveClockHistory = new List<int>();

        private Move[] MoveDir;

        public int EnPassantSquare = 0;

        public bool IsWhiteToMove => ColorToMove == Piece.White;

        private static bool InCheck;
        private static bool hasCachedCheck = false;

        public int CasltingRight;

        public int plyCount = 0;

        //Create the array before the game starts to avoid allocating memory every time we need to generate legal moves.
        Move[] legalMoves;

        // Zobrist
        public ulong CurrentHash;
        public Dictionary<ulong, int> repetitionCounts = new Dictionary<ulong, int>();

        /// <summary>
        /// Instantiates a board and automatically loads the starting position.
        /// The position can be changed by passing a custom FEN string.
        /// </summary>
        /// <param name="fenString">The FEN string representing the position (defaults to starting position).</param>
        public Board(string fenString = FEN.StartingFEN)
        {
            LoadFen(fenString);
            CurrentHash = Zobrist.ComputeHash(this);
            repetitionCounts[CurrentHash] = 1;
        }

        public bool IsInCheck()
        {
            if (hasCachedCheck)
            {
                return InCheck;
            }
            InCheck = MoveGenerator.InCheck(this);
            hasCachedCheck = true;
            return InCheck;
        }

        public bool IsCheckmate()
            => IsInCheck() && GetLegalMoves().Length == 0;

        public bool IsInStalemate()
            => !IsInCheck() && GetLegalMoves().Length == 0;

        public bool IsThreefoldRepetition()
        {
            return repetitionCounts.TryGetValue(CurrentHash, out int count) && count >= 3;
        }

        public bool IsDraw()
            => IsInStalemate() || IsFiftyMoveRuleDraw() || IsThreefoldRepetition();

        public bool IsFiftyMoveRuleDraw()
            => HalfmoveClock >= 100;


        /// <summary>
        /// Sets up the board state based on a given FEN string.
        /// </summary>
        /// <param name="fenString">The FEN string representing the position.</param>
        private void LoadFen(string fenString)
        {
            FEN.LoadFEN(this, fenString);
            CurrentGameState = GameState.MakeGameState(Piece.None, Piece.None, EnPassantSquare, CasltingRight); // Initialize the game state
            CurrentHash = Zobrist.ComputeHash(this);
            repetitionCounts[CurrentHash] = 1;
            HalfmoveClockHistory.Add(HalfmoveClock);
        }

        /// <summary>
        /// Gets all the legal moves in the current position and returns them as an array of moves.
        /// </summary>
        /// <returns>An array of all the legal moves in the position</returns>
        public Move[] GetLegalMoves(bool GenerateCapturesOnly = false)
        {
            Span<Move> legalMovesSpan = stackalloc Move[MoveGenerator.MaxLegalMoves];
            MoveGenerator.GenerateLegalMoves(this, ref legalMovesSpan, GenerateCapturesOnly);

            return legalMovesSpan.ToArray();
        }

        public void MakeNullMove()
        {
            ColorToMove ^= 1; // Switch the turn to the other player (0 for white, 1 for black)

            // Update Zobrist hash
            CurrentHash = Zobrist.ComputeHash(this);
            if (!repetitionCounts.ContainsKey(CurrentHash))
                repetitionCounts[CurrentHash] = 1;
            else
                repetitionCounts[CurrentHash]++;
        }
        public void UndoNullMove()
        {
            ColorToMove ^= 1;

            // Remove the hash of the current position (the one after the move)
            if (repetitionCounts.ContainsKey(CurrentHash))
            {
                repetitionCounts[CurrentHash]--;
                if (repetitionCounts[CurrentHash] == 0)
                    repetitionCounts.Remove(CurrentHash);
            }

            CurrentHash = Zobrist.ComputeHash(this);
        }

        /// <summary>
        /// Makes a move on the board. updates bitboards and square array.
        /// </summary>
        /// <param name="move"></param>
        public void MakeMove(Move move)
        {
            // --- locals & refs for speed ---
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

            int capturedPiece = Squares[to]; // may be Piece.None
            bool isCapture = (capturedPiece != Piece.None);

            // --- move piece on piece table + squares array ---
            Squares[from] = Piece.None;
            Squares[to] = movingPiece;

            // Move bitboards for moving piece, color and empty squares.
            BitboardHelper.MovePiece(ref movingBitboard, from, to);
            BitboardHelper.MovePiece(ref colorBB, from, to);
            BitboardHelper.MovePiece(ref emptyBB, from, to); // empty squares: remove from `from`, add to `to`

            // --- update castling rights when king moves or rook moves from a corner ---
            // Castling rights bit layout assumed unchanged: 0..3 used.
            // Precompute masks for clearing rights:
            const int WHITE_KINGSIDE = 1 << 0;
            const int WHITE_QUEENSIDE = 1 << 1;
            const int BLACK_KINGSIDE = 1 << 2;
            const int BLACK_QUEENSIDE = 1 << 3;

            if (movingPieceType == Piece.King)
            {
                // Clear both castling rights for the moving color
                int clearMask = (movingColor == Piece.White) ? (WHITE_KINGSIDE | WHITE_QUEENSIDE)
                                                             : (BLACK_KINGSIDE | BLACK_QUEENSIDE);
                CasltingRight &= ~clearMask;
            }
            else if (movingPieceType == Piece.Rook)
            {
                // If rook moved from a corner, clear that specific right.
                switch (from)
                {
                    case 0: CasltingRight &= ~WHITE_QUEENSIDE; break; // white a1 (queen side)
                    case 7: CasltingRight &= ~WHITE_KINGSIDE; break; // white h1 (king side)
                    case 56: CasltingRight &= ~BLACK_QUEENSIDE; break; // black a8
                    case 63: CasltingRight &= ~BLACK_KINGSIDE; break; // black h8
                }
            }

            // --- handle en-passant capture ---
            if (move.MoveFlag == MoveFlag.EnPassant)
            {
                // captured pawn is behind the target square relative to mover
                int epCapturedSquare = to + (movingColor == Piece.White ? -8 : 8);

                // Remove pawn from arrays/bitboards
                Squares[epCapturedSquare] = Piece.None;
                int capturedPawnIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, oppColor);
                ref ulong capturedPawnBB = ref PiecesBitboards[capturedPawnIndex];

                BitboardHelper.ToggleBit(ref capturedPawnBB, epCapturedSquare); // remove pawn
                BitboardHelper.ToggleBit(ref oppColorBB, epCapturedSquare);
                BitboardHelper.ToggleBit(ref emptyBB, epCapturedSquare); // that square becomes empty
                isCapture = true; // treat as capture for halfmove clock
                capturedPiece = Piece.CreatePiece(Piece.Pawn, oppColor); // for game state
            }
            else if (isCapture)
            {
                // --- handle normal capture (non-en-passant) ---
                int capType = Piece.PieceType(capturedPiece);
                int capIndex = BitboardHelper.GetBitboardIndex(capType, oppColor);
                ref ulong capBB = ref PiecesBitboards[capIndex];

                BitboardHelper.ToggleBit(ref capBB, to);      // remove from piece-bitboard
                BitboardHelper.ToggleBit(ref oppColorBB, to); // remove from colored bitboard
                BitboardHelper.ToggleBit(ref emptyBB, to);    // that square becomes empty (we later put moving piece there)

                // If captured rook from corner, update castling rights
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

            // --- promotions ---
            if (move.IsPromotion())
            {
                // moving pawn must be removed from its pawn bitboard, and promotion piece added
                int pawnIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, movingColor);
                ref ulong pawnBB = ref PiecesBitboards[pawnIndex];
                BitboardHelper.ToggleBit(ref pawnBB, to); // remove pawn from target (it was moved there)
                                                          // Clear the moved piece's bit at 'to' if still set (shouldn't be, but safe)
                if (BitboardHelper.IsBitSet(movingBitboard, to))
                    BitboardHelper.ToggleBit(ref movingBitboard, to);

                // Add promoted piece
                int promoteType = (int)move.MoveFlag; // as in your original code: flag holds promotion type
                int promoteIndex = BitboardHelper.GetBitboardIndex(promoteType, movingColor);
                ref ulong promoteBB = ref PiecesBitboards[promoteIndex];
                BitboardHelper.ToggleBit(ref promoteBB, to);

                int promotePiece = Piece.CreatePiece(promoteType, movingColor);
                Squares[to] = promotePiece; // overwrite with promoted piece
            }
            else if (move.IsCastle())
            {
                // Handle rook move during castling: compute rook source/target relative to king target
                int rookFrom = (move.MoveFlag == MoveFlag.CastleShort) ? to + 1 : to - 2;
                int rookTo = (move.MoveFlag == MoveFlag.CastleShort) ? to - 1 : to + 1;

                int rookIndex = BitboardHelper.GetBitboardIndex(Piece.Rook, movingColor);
                ref ulong rookBB = ref PiecesBitboards[rookIndex];

                int rookPiece = Squares[rookFrom];
                Squares[rookFrom] = Piece.None;
                Squares[rookTo] = rookPiece;

                BitboardHelper.MovePiece(ref rookBB, rookFrom, rookTo);
                BitboardHelper.MovePiece(ref colorBB, rookFrom, rookTo);
                BitboardHelper.MovePiece(ref emptyBB, rookFrom, rookTo);

                // Clear castling rights for this color
                int clrMask = (movingColor == Piece.White) ? (WHITE_KINGSIDE | WHITE_QUEENSIDE)
                                                           : (BLACK_KINGSIDE | BLACK_QUEENSIDE);
                CasltingRight &= ~clrMask;
            }

            // --- en-passant target square (double pawn push) ---
            if (move.MoveFlag == MoveFlag.DoublePawnPush)
            {
                EnPassantSquare = to + (movingColor == Piece.White ? -8 : 8);
            }
            else
            {
                EnPassantSquare = 0; // keep your original sentinel (0 = none)
            }

            // --- halfmove clock ---
            if (movingPieceType == Piece.Pawn || isCapture)
                HalfmoveClock = 0;
            else
                HalfmoveClock++;

            // --- history / state / ply / repetition ---
            GameStateHistory.Add(CurrentGameState);
            CurrentGameState = GameState.MakeGameState(capturedPiece, movingPiece, EnPassantSquare, CasltingRight);
            moveHistory.Add(move);

            ColorToMove = oppColor; // switch side
            HalfmoveClockHistory.Add(HalfmoveClock);
            plyCount++;
            hasCachedCheck = false;

            // Update Zobrist / repetitions
            CurrentHash = Zobrist.ComputeHash(this); // kept as full recompute for correctness with current Zobrist implementation
            if (!repetitionCounts.ContainsKey(CurrentHash))
                repetitionCounts[CurrentHash] = 1;
            else
                repetitionCounts[CurrentHash]++;
        }


        public void UndoMove(Move move)
        {
            // Remove the hash of the current position (the one after the move)
            if (repetitionCounts.ContainsKey(CurrentHash))
            {
                repetitionCounts[CurrentHash]--;
                if (repetitionCounts[CurrentHash] == 0)
                    repetitionCounts.Remove(CurrentHash);
            }

            int movingPiece = GameState.MovingPiece(CurrentGameState);
            int movingPieceType = Piece.PieceType(movingPiece);
            int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, ColorToMove ^ 1);
            ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
            int capturedPiece = GameState.CapturedPiece(CurrentGameState);

            Squares[move.TargetSquare] = Piece.None; // Clear the target square
            Squares[move.StartingSquare] = movingPiece; // Place the piece back on the starting square
            BitboardHelper.MovePiece(ref movingBitboard, move.TargetSquare, move.StartingSquare); // Update the bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove ^ 1], move.TargetSquare, move.StartingSquare); // Update the colored bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[2], move.StartingSquare, move.TargetSquare); // Add the target square back to the empty squares bitboard

            if (move.MoveFlag == MoveFlag.EnPassant)
            {
                // Handle en passant logic
                int enPassantSquare = move.TargetSquare + (ColorToMove == Piece.Black ? -8 : 8); // Calculate the square that the pawn would have been on if it had not been captured
                Squares[enPassantSquare] = Piece.CreatePiece(Piece.Pawn, ColorToMove); // Remove the captured pawn from the board
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, ColorToMove);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref capturedBitboard, enPassantSquare); // Remove the captured pawn from its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove], enPassantSquare); // Remove from the colored bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], enPassantSquare); // Remove from the colored bitboard
            }
            else if (capturedPiece != Piece.None)
            {
                // If a piece was captured, restore it to the board and its bitboard
                int capturedPieceType = Piece.PieceType(capturedPiece);
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(capturedPieceType, ColorToMove);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.TargetSquare);
                BitboardHelper.ToggleBit(ref capturedBitboard, move.TargetSquare); // Restore the captured piece to its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove], move.TargetSquare); // Restore to the colored bitboard
                Squares[move.TargetSquare] = capturedPiece; // Clear the target square
            }

            if (move.IsPromotion())
            {
                // Handle promotion logic
                int promotionPieceType = move.MoveFlag; // The piece type to promote to
                int promotionBitboardIndex = BitboardHelper.GetBitboardIndex(promotionPieceType, ColorToMove ^ 1);
                ref ulong promotionBitboard = ref PiecesBitboards[promotionBitboardIndex];
                BitboardHelper.ToggleBit(ref promotionBitboard, move.TargetSquare); // Add the promoted piece to its bitboard
                //BitboardHelper.ToggleBit(ref movingBitboard, move.StartingSquare); // remove the moving pawn from its bitboard
                int movingPawn = Piece.CreatePiece(Piece.Pawn, ColorToMove ^ 1); // Create the moving pawn piece
                Squares[move.StartingSquare] = movingPawn; // Place the promoted piece on the target square
            }
            else if (move.IsCastle())
            {
                // Handle castling logic
                int rookStartSquare = move.MoveFlag == MoveFlag.CastleShort ? move.TargetSquare + 1 : move.TargetSquare - 2; // Determine the rook's starting square based on castling type
                int rookTargetSquare = move.TargetSquare + (move.MoveFlag == MoveFlag.CastleShort ? -1 : 1); // Determine the rook's target square
                int rookPiece = Squares[rookTargetSquare]; // Get the rook piece
                Squares[rookTargetSquare] = Piece.None; // Clear the rook's starting square
                Squares[rookStartSquare] = rookPiece; // Place the rook on its target square
                int rookBitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Rook, ColorToMove ^ 1);
                ref ulong rookBitboard = ref PiecesBitboards[rookBitboardIndex];
                BitboardHelper.MovePiece(ref rookBitboard, rookStartSquare, rookTargetSquare); // Update the rook's bitboard
                BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove ^ 1], rookStartSquare, rookTargetSquare); // Update the colored bitboard
                BitboardHelper.MovePiece(ref ColoredBitboards[2], rookStartSquare, rookTargetSquare); // Update the colored bitboard
            }

            CurrentGameState = GameStateHistory.ElementAt(GameStateHistory.Count - 1); // Restore the previous game state from history
            GameStateHistory.RemoveAt(GameStateHistory.Count - 1); // Remove the last game state from history
            moveHistory.RemoveAt(moveHistory.Count - 1); // Remove the last move from history
            EnPassantSquare = GameState.GetEnPassantSquare(CurrentGameState);
            ColorToMove ^= 1; // Switch the turn back to the previous player (0 for white, 1 for black)
            CasltingRight = GameState.GetCastlingRights(CurrentGameState);
            HalfmoveClockHistory.RemoveAt(HalfmoveClockHistory.Count - 1);
            HalfmoveClock = HalfmoveClockHistory.Last();
            plyCount--; // Decrement the ply count for the current turn
            hasCachedCheck = false;

            // Now recompute hash for the restored position
            CurrentHash = Zobrist.ComputeHash(this);
        }
    }
}
