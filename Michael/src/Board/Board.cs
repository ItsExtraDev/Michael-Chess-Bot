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


        public int EnPassantSquare = 0;

        private static bool InCheck;
        private static bool hasCachedCheck = false;

        public int CasltingRight;

        public int plyCount = 0;

        //Create the array before the game starts to avoid allocating memory every time we need to generate legal moves.
        Move[] legalMoves;

        // Zobrist
        public ulong CurrentHash;
        private Dictionary<ulong, int> repetitionCounts = new Dictionary<ulong, int>();

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
        }

        /// <summary>
        /// Gets all the legal moves in the current position and returns them as an array of moves.
        /// </summary>
        /// <returns>An array of all the legal moves in the position</returns>
        public Move[] GetLegalMoves(bool GenerateCapturesOnly = false)
        {
            legalMoves = MoveGenerator.GenerateLegalMoves(this, GenerateCapturesOnly);

            return legalMoves;
        }

        public void MakeNullMove()
        {
            ColorToMove ^= 1;
        }

        /// <summary>
        /// Makes a move on the board. updates bitboards and square array.
        /// </summary>
        /// <param name="move"></param>
        public void MakeMove(Move move)
        {
            int movingPiece = Squares[move.StartingSquare];
            int movingPieceType = Piece.PieceType(movingPiece);

            int movingBitboardIndex = BitboardHelper.GetBitboardIndex(movingPieceType, ColorToMove);
            ref ulong movingBitboard = ref PiecesBitboards[movingBitboardIndex];
            int CapturedPiece = Squares[move.TargetSquare];
            Squares[move.StartingSquare] = Piece.None; // Clear the starting square
            Squares[move.TargetSquare] = movingPiece; // Place the piece on the target square
            BitboardHelper.MovePiece(ref movingBitboard, move.StartingSquare, move.TargetSquare); // Update the bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove], move.StartingSquare, move.TargetSquare); // Update the colored bitboard
            BitboardHelper.MovePiece(ref ColoredBitboards[2], move.StartingSquare, move.TargetSquare); // Remove the starting square from the empty squares bitboard

            //Update castling rights
            if (movingPieceType == Piece.King)
            {
                int whiteCastlingMask = 1 << 0 | 1 << 1; // White castling rights mask
                int blackCastlingMask = 1 << 2 | 1 << 3; // Black castling rights mask
                int mask = ColorToMove == Piece.White ? whiteCastlingMask : blackCastlingMask; // Determine the castling rights mask based on color
                CasltingRight &= ~mask; // Remove the castling rights for the current player
            }
            else if (movingPieceType == Piece.Rook)
            {
                if (move.StartingSquare == 0) // A1 or H1 for white
                {
                    CasltingRight &= ~(1 << 1); // Remove white kingside castling right
                }
                else if (move.StartingSquare == 7)
                {
                    CasltingRight &= ~(1 << 0); // Remove white queenside castling right
                }
                else if (move.StartingSquare == 56) // A8 or H8 for black
                {
                    CasltingRight &= ~(1 << 3); // Remove black kingside castling right
                }
                else if (move.StartingSquare == 63)
                {
                    CasltingRight &= ~(1 << 2); // Remove black queenside castling right
                }
            }

            if (move.MoveFlag == MoveFlag.EnPassant)
            {
                // Handle en passant logic
                int enPassantSquare = move.TargetSquare + (ColorToMove == Piece.White ? -8 : 8); // Calculate the square that the pawn would have been on if it had not been captured

                Squares[enPassantSquare] = Piece.None; // Remove the captured pawn from the board
                int capturedPawnType = Piece.CreatePiece(Piece.Pawn, ColorToMove ^ 1); // Create the captured pawn piece
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, ColorToMove ^ 1);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref capturedBitboard, enPassantSquare); // Remove the captured pawn from its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove ^ 1], enPassantSquare); // Remove from the colored bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], enPassantSquare); // Remove from the colored bitboard
            }
            else if (CapturedPiece != Piece.None)
            {
                // If a piece was captured, remove it from the board and its bitboard
                int capturedPieceType = Piece.PieceType(CapturedPiece);                      //The color of the captured piece is always the opposite color of the moving player.
                int capturedBitboardIndex = BitboardHelper.GetBitboardIndex(capturedPieceType, ColorToMove ^ 1);
                ref ulong capturedBitboard = ref PiecesBitboards[capturedBitboardIndex];
                BitboardHelper.ToggleBit(ref capturedBitboard, move.TargetSquare); // Remove the captured piece from its bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[ColorToMove ^ 1], move.TargetSquare); // Remove from the colored bitboard
                BitboardHelper.ToggleBit(ref ColoredBitboards[2], move.TargetSquare); // Remove from the colored bitboard

                //Update castling rights if the captured piece was a rook
                if (capturedPieceType == Piece.Rook)
                {
                    if (move.TargetSquare == 0) // A1 or H1 for white
                    {
                        CasltingRight &= ~(1 << 1); // Remove white kingside castling right

                    }
                    else if (move.TargetSquare == 7)
                    {
                        CasltingRight &= ~(1 << 0); // Remove white queenside castling right
                    }
                    else if (move.TargetSquare == 56) // A8 or H8 for black
                    {
                        CasltingRight &= ~(1 << 3); // Remove black kingside castling right
                    }
                    else if (move.TargetSquare == 63)
                    {
                        CasltingRight &= ~(1 << 2); // Remove black queenside castling right
                    }
                }
            }
            if (move.IsPromotion())
            {
                // Handle promotion logic
                int promotionPieceType = move.MoveFlag; // The piece type to promote to
                int promotionPiece = Piece.CreatePiece(promotionPieceType, ColorToMove);
                int promotionBitboardIndex = BitboardHelper.GetBitboardIndex(promotionPieceType, ColorToMove);
                ref ulong promotionBitboard = ref PiecesBitboards[promotionBitboardIndex];
                BitboardHelper.ToggleBit(ref promotionBitboard, move.TargetSquare); // Add the promoted piece to its bitboard
                if (BitboardHelper.IsBitSet(movingBitboard, move.TargetSquare))
                    BitboardHelper.ToggleBit(ref movingBitboard, move.TargetSquare); // remove the moving pawn from its bitboard
                Squares[move.TargetSquare] = promotionPiece; // Place the promoted piece on the target square
            }
            else if (move.IsCastle())
            {
                // Handle castling logic
                int rookStartSquare = move.MoveFlag == MoveFlag.CastleShort ? move.TargetSquare + 1 : move.TargetSquare - 2; // Determine the rook's starting square based on castling type
                int rookTargetSquare = move.TargetSquare + (move.MoveFlag == MoveFlag.CastleShort ? -1 : 1); // Determine the rook's target square
                int rookPiece = Squares[rookStartSquare]; // Get the rook piece
                Squares[rookStartSquare] = Piece.None; // Clear the rook's starting square
                Squares[rookTargetSquare] = rookPiece; // Place the rook on its target square
                int rookBitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Rook, ColorToMove);
                ref ulong rookBitboard = ref PiecesBitboards[rookBitboardIndex];
                BitboardHelper.MovePiece(ref rookBitboard, rookStartSquare, rookTargetSquare); // Update the rook's bitboard
                BitboardHelper.MovePiece(ref ColoredBitboards[ColorToMove], rookStartSquare, rookTargetSquare); // Update the colored bitboard
                BitboardHelper.MovePiece(ref ColoredBitboards[2], rookStartSquare, rookTargetSquare); // Update the colored bitboard
                int whiteCastlingMask = 1 << 0 | 1 << 1; // White castling rights mask
                int blackCastlingMask = 1 << 2 | 1 << 3; // Black castling rights mask
                int mask = ColorToMove == Piece.White ? whiteCastlingMask : blackCastlingMask; // Determine the castling rights mask based on color
                CasltingRight &= ~mask; // Remove the castling rights for the current player

            }
            if (move.MoveFlag == MoveFlag.DoublePawnPush)
            {
                // Handle double pawn push logic
                EnPassantSquare = move.TargetSquare + (ColorToMove == Piece.White ? -8 : 8); // Set the en passant square
            }
            else
            {
                EnPassantSquare = 0; // Reset en passant square if not a double pawn push
            }
            // Update halfmove clock
            if (movingPieceType == Piece.Pawn || CapturedPiece != Piece.None)
            {
                HalfmoveClock = 0; // Reset on pawn move or capture
            }
            else
            {
                HalfmoveClock++; // Otherwise increment
            }
            //en passant logic, and caslting logic
            GameStateHistory.Add(CurrentGameState); // Add the current game state to history
            CurrentGameState = GameState.MakeGameState(CapturedPiece, movingPiece, EnPassantSquare, CasltingRight); // Update the game state with the captured piece and moving piece
            moveHistory.Add(move); // Add the move to the history
            ColorToMove ^= 1; // Switch the turn to the other player (0 for white, 1 for black)
            HalfmoveClockHistory.Add(HalfmoveClock); plyCount++; // Increment the ply count for the current turn
            hasCachedCheck = false;

            // Update Zobrist hash
            CurrentHash = Zobrist.ComputeHash(this);
            if (!repetitionCounts.ContainsKey(CurrentHash))
                repetitionCounts[CurrentHash] = 1;
            else
                repetitionCounts[CurrentHash]++;
        }

        public void UndoMove(Move move)
        {
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
                int capturedPawnType = Piece.CreatePiece(Piece.Pawn, ColorToMove); // Create the captured pawn piece
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
            EnPassantSquare = GameState.GetEnPassantSquare(CurrentGameState); // Restore the en passant square from the game state
            ColorToMove ^= 1; // Switch the turn back to the previous player (0 for white, 1 for black)
            CasltingRight = GameState.GetCastlingRights(CurrentGameState); // Restore the castling rights from the game state
            HalfmoveClock = HalfmoveClockHistory.Last();
            HalfmoveClockHistory.RemoveAt(HalfmoveClockHistory.Count - 1);
            plyCount--; // Decrement the ply count for the current turn
            hasCachedCheck = false;

            ulong previousHash = CurrentHash;
            if (repetitionCounts.ContainsKey(previousHash))
            {
                repetitionCounts[previousHash]--;
                if (repetitionCounts[previousHash] == 0)
                    repetitionCounts.Remove(previousHash);
            }
            CurrentHash = Zobrist.ComputeHash(this);
        }
    }
}
