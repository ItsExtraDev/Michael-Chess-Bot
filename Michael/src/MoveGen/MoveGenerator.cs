using Michael.src.Helpers;
using System.Numerics;

namespace Michael.src.MoveGen
{

    /*
     * LEFT TODO FOR MOVEGEN:
     * EN PASSANT
     * CASTLE
     * DRAW REPETION
     * DRAW 50 MOVES
     * COMPLETE PERFT TESTING
     * GET AT LEAST 50M NPS ON STARTPOSS
     */

    /// <summary>
    /// Provides methods for generating legal moves for a given board position.
    /// Handles all legal move generation, like move generation, legal validation, capture logic etc.
    /// generateLegalMoves is the main method and should be called only from the board.GetLegalMoves() Function
    /// </summary>
    public static class MoveGenerator
    {
        //The board we are generating moves for.
        //Being passed from the board.GetLegalMoves() function.
        private static Board board;

        //Maximum number of legal moves in a position, used for move generation array.
        //This is possible in the position: R6R/3Q4/1Q4Q1/4Q3/2Q4Q/Q4Q2/pp1Q4/kBNN1KB1 w - - 0 1, composed by Nenad Petrovic.
        public const int MaxLegalMoves = 218;

        //Current index in the legal moves array, used to track the position in the array.
        private static int CurrentMoveIndex = 0;

        private static ulong enemyBitboardAndEmptySquares; //Stores the enemy bitboard and empty squares for legal move generation.

        //Stores all the legal moves the knight can make from a given square.
        //Precomputed before the game starts in the PrecomputeMoveData class.
        public static ulong[] WhitePawnAttacks = new ulong[64];
        public static ulong[] BlackPawnAttacks = new ulong[64];
        public static ulong[] KnightMoves = new ulong[64];
        public static ulong[] KingMoves = new ulong[64];

        public static ulong enemyAttacks; //Stores the enemy attacks for the current position, used to check for legal moves.
        private static int friendlyKingSquare; //Stores the square of the friendly king, used to check for legal moves.
        private static bool IsInDoubleCheck;
        private static bool IsInCheck; //Stores whether the current position is in check or not.
        private static ulong checkRayMask;
        private static ulong DiagonalPinMask;
        private static ulong OrthogonalPinMask;
        private static ulong DiagonalPinMovementMask;
        private static ulong OrthogonalPinMovementMask;

        public static bool InCheck(Board boardInstance)
        {
            board = boardInstance;
            Init();

            return IsInCheck;
        }

        public static Move[] GenerateLegalMoves(Board boardInstance)
        {
            board = boardInstance; //Set the board to the current board instance.
            Init(); //Initialize all the necessary variables for move generation.
            Move[] legalMoves = new Move[MaxLegalMoves]; // Create an array to hold the legal moves.
            GenerateLegalKingMoves(ref legalMoves); // Generate all the legal king moves and add them to the legal moves array.
            if (!IsInDoubleCheck)
            {
                GenerateLegalPawnMoves(ref legalMoves); // Generate all the legal pawn moves and add them to the legal moves array.
                GenerateLegalKnightMoves(ref legalMoves); // Generate all the legal knight moves and add them to the legal moves array.
                GenerateLegalSlidingMoves(ref legalMoves); // Generate all the legal sliding moves (rooks, bishops, and queens) and add them to the legal moves array.
            }

            //Convert the array to a span and slice to the amount of legal moves in the position and return as an array.
            //This is done to no return 218 moves when there are less than that in the position.
            return legalMoves.AsSpan().Slice(0, CurrentMoveIndex).ToArray(); 
        }

        // /// <summary>
        // /// Generates all the legal moves for a pawn piece and return to the given legalMoves array.
        // /// </summary>
        // /// <param name="legalMoves">the array to return the legal pawn moves</param>
        private static void GenerateLegalPawnMoves(ref Move[] legalMoves)
        {
            int BitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, board.ColorToMove);
            ulong pawns = board.PiecesBitboards[BitboardIndex];
            ulong pawnsPush = pawns & ~DiagonalPinMask;
            ulong pawnsCapture = pawns & ~OrthogonalPinMask;
            int moveDir = board.ColorToMove == Piece.White ? 1 : -1;

            //The rank each color need to get to in order to promote a pawn
            int finalRank = board.ColorToMove == Piece.White ? 7 : 0;
            //pawns can move up one rank, only to empty squares.
            ulong oneRankPush = BitboardHelper.ShiftBitboard(pawnsPush, 8*moveDir);
            
            ulong CaptureLeft = ((BitboardHelper.ShiftBitboard(pawnsCapture, 8*moveDir) >> 1) & board.ColoredBitboards[board.ColorToMove ^ 1]) & PrecomputeMoveData.NotHFile & checkRayMask;
            ulong CaptureRight = ((BitboardHelper.ShiftBitboard(pawnsCapture, 8*moveDir) << 1) & board.ColoredBitboards[board.ColorToMove ^ 1]) & PrecomputeMoveData.NotAFile & checkRayMask;
            oneRankPush &= board.ColoredBitboards[2];
            ulong twoRankPush = (BitboardHelper.ShiftBitboard(oneRankPush, 8*moveDir)) & board.ColoredBitboards[2] & checkRayMask;

            //Make sure a pawn can push 2 squares only if on the starting square
            twoRankPush &= (board.ColorToMove == Piece.White ?  BitboardHelper.Rank4 : BitboardHelper.Rank5);
            oneRankPush &= checkRayMask;
            //One rank push
            while (oneRankPush != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref oneRankPush);
                int startingSquare = targetSquare - 8 * moveDir;
                if ((OrthogonalPinMask & 1ul << startingSquare) != 0)
                {
                    //If the pawn is pinned orthogonally, it can only move to the square it is pinned to.
                    if ((OrthogonalPinMovementMask & 1ul << targetSquare) == 0)
                        continue;
                }
                if (BoardHelper.Rank(targetSquare) == finalRank)
                {
                    for (int PromotionPT = 2; PromotionPT <= 5; PromotionPT++)
                    {
                        legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, PromotionPT);
                    }
                }
                else
                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
            }
            //Two rank push
            while (twoRankPush != 0)
            {

                int targetSquare = BitboardHelper.PopLSB(ref twoRankPush);
                int startingSquare = targetSquare - 16 * moveDir;
                if ((OrthogonalPinMask & 1ul << startingSquare) != 0)
                {
                    //If the pawn is pinned orthogonally, it can only move to the square it is pinned to.
                    if ((OrthogonalPinMovementMask & 1ul << targetSquare) == 0)
                        continue;
                }
                legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, MoveFlag.DoublePawnPush);
            }
            //Captures
            while (CaptureLeft != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref CaptureLeft);
                int startingSquare = targetSquare + (board.ColorToMove == Piece.White ? -7 : 9);

                if (((DiagonalPinMask & 1ul << startingSquare) != 0) && (DiagonalPinMovementMask & 1ul << targetSquare) == 0)
                    continue;

                if (BoardHelper.Rank(targetSquare) == finalRank)
                {
                    for (int PromotionPT = 2; PromotionPT <= 5; PromotionPT++)
                    {
                        legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, PromotionPT);
                    }
                }
                else
                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
            }
            while (CaptureRight != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref CaptureRight);
                int startingSquare = targetSquare + (board.ColorToMove == Piece.White ? -9 : 7);

                if (((DiagonalPinMask & 1ul << startingSquare) != 0) && (DiagonalPinMovementMask & 1ul << targetSquare) == 0)
                    continue;

                if (BoardHelper.Rank(targetSquare) == finalRank)
                {
                    for (int PromotionPT = 2; PromotionPT <= 5; PromotionPT++)
                    {
                        legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, PromotionPT);
                    }
                }
                else
                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
            }
        }


        /// <summary>
        /// Generates all the legal moves for a knight piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal knight moves</param>
        public static void GenerateLegalKnightMoves(ref Move[] legalMoves)
        {
            ulong knightBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Knight, board.ColorToMove)] & ~DiagonalPinMask & ~OrthogonalPinMask; ;
            while (knightBitboard != 0)
            {
                int knightSquare = BitboardHelper.PopLSB(ref knightBitboard); // Get the square of the knight piece.
                ulong attacks = KnightMoves[knightSquare] & enemyBitboardAndEmptySquares & checkRayMask; // Get the precomputed moves for the knight from that square.
                // Iterate through all possible moves and add them to the legal moves array.
                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks);
                    legalMoves[CurrentMoveIndex++] = new Move(knightSquare, targetSquare);
                }
            }
        }

        /// <summary>
        /// Generates all the legal moves for a king piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal king moves</param>
        public static void GenerateLegalKingMoves(ref Move[] legalMoves)
        {
            ulong king = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.King, board.ColorToMove)];

            int kingSquare = BitOperations.TrailingZeroCount(king); // Get the square of the king piece.

            ulong kingAttacks = KingMoves[kingSquare] & enemyBitboardAndEmptySquares & ~enemyAttacks; // Get the precomputed moves for the king from that square.
            while (kingAttacks != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref kingAttacks);
                legalMoves[CurrentMoveIndex++] = new Move(kingSquare, targetSquare);
            }
        }

        /// <summary>
        /// Generates all the legal moves for the sliding pieces (rooks, bishops, and queens) and returns to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal moves to</param>
        private static void GenerateLegalSlidingMoves(ref Move[] legalMoves)
        {
            ulong orthogonalPieces = (board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Rook, board.ColorToMove)]
                                    | board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove)]) & ~DiagonalPinMask;
            ulong diagonalPieces = (board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Bishop, board.ColorToMove)] |
                       board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove)]) & ~OrthogonalPinMask;
            

            while (orthogonalPieces != 0)
            {
                int startingSquare = BitboardHelper.PopLSB(ref orthogonalPieces);

                // Blockers are all pieces (both colors) masked with rook's blocker mask
                ulong blockers = ~board.ColoredBitboards[2] & Magic.GetRookAttackMask(startingSquare);

                // Calculate attacks given blockers
                ulong attacks = Magic.GetRookAttacks(startingSquare, blockers) & checkRayMask;

                if ((OrthogonalPinMask & 1ul << startingSquare) != 0)
                    attacks &= OrthogonalPinMovementMask;

                // Remove friendly squares from attacks
                attacks &= ~board.ColoredBitboards[board.ColorToMove];

                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks);

                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
                }
            }

            while (diagonalPieces != 0)
            {
                int startingSquare = BitboardHelper.PopLSB(ref diagonalPieces);

                // Blockers are all pieces (both colors) masked with bishop's blocker mask
                ulong blockers = ~board.ColoredBitboards[2] & Magic.GetBishopAttackMask(startingSquare);

                // Calculate attacks given blockers
                ulong attacks = Magic.GetBishopAttacks(startingSquare, blockers) & checkRayMask;

                if ((DiagonalPinMask & 1ul << startingSquare) != 0)
                    attacks &= DiagonalPinMovementMask;

                // Remove friendly squares from attacks
                attacks &= ~board.ColoredBitboards[board.ColorToMove];

                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks);

                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
                }
            }

        }


        private static void Init()
        {
            CurrentMoveIndex = 0;
            enemyBitboardAndEmptySquares = ~board.ColoredBitboards[board.ColorToMove];
            IsInCheck = false;
            IsInDoubleCheck = false;
            friendlyKingSquare = BitOperations.TrailingZeroCount(board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.King, board.ColorToMove)]);
            checkRayMask = 0;
            DiagonalPinMask = 0;
            DiagonalPinMovementMask = 0;
            OrthogonalPinMask = 0;
            OrthogonalPinMovementMask = 0;

            enemyAttacks = GetEnemyAttacks();
        }


        public static ulong GetEnemyAttacks()
        {
            ulong attacks = 0;

            ulong pawns = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Pawn, board.ColorToMove ^ 1)];
            ulong knights = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Knight, board.ColorToMove ^ 1)];
            ulong bishops = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Bishop, board.ColorToMove ^ 1)];
            ulong rooks = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Rook, board.ColorToMove ^ 1)];
            ulong queens = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove ^ 1)];
            ulong king = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.King, board.ColorToMove ^ 1)];

            while (pawns != 0)
            {
                int square = BitboardHelper.PopLSB(ref pawns);
                ulong[] attackUlong = board.ColorToMove == Piece.White ? BlackPawnAttacks : WhitePawnAttacks;

                if ((attackUlong[square] & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                }
                attacks |= attackUlong[square];
            }

            while (knights != 0)
            {
                int square = BitboardHelper.PopLSB(ref knights);

                if ((KnightMoves[square] & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                }

                attacks |= KnightMoves[square];
            }

            while (bishops != 0)
            {
                int square = BitboardHelper.PopLSB(ref bishops);
                ulong blockers = (~board.ColoredBitboards[2] & Magic.GetBishopAttackMask(square)) ^ 1ul << friendlyKingSquare;

                if ((Magic.GetBishopAttacks(square, blockers) & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false);
                }

                ulong diagonalPin = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false) & ~board.ColoredBitboards[2]);
                if (BitOperations.PopCount(diagonalPin) == 1)
                {
                    DiagonalPinMask |= diagonalPin;
                    DiagonalPinMovementMask = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false));
                    DiagonalPinMovementMask |= 1ul << square;
                }
                attacks |= Magic.GetBishopAttacks(square, blockers);
            }

            while (rooks != 0)
            {
                int square = BitboardHelper.PopLSB(ref rooks);
                ulong blockers = (~board.ColoredBitboards[2] & Magic.GetRookAttackMask(square)) ^ 1ul << friendlyKingSquare;

                if ((Magic.GetRookAttacks(square, blockers) & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true);
                }

                ulong orthogonalPin = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true) & ~board.ColoredBitboards[2]);
                if (BitOperations.PopCount(orthogonalPin) == 1)
                {
                    DiagonalPinMovementMask = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true));
                    OrthogonalPinMask |= orthogonalPin;
                    OrthogonalPinMovementMask |= 1ul << square;
                }

                attacks |= Magic.GetRookAttacks(square, blockers);
            }
            while (queens != 0)
            {
                int square = BitboardHelper.PopLSB(ref queens);

                ulong blockers = (~board.ColoredBitboards[2] & (Magic.GetRookAttackMask(square) | Magic.GetBishopAttackMask(square))) ^ 1ul << friendlyKingSquare;

                attacks |= Magic.GetBishopAttacks(square, blockers);
                attacks |= Magic.GetRookAttacks(square, blockers);

                if ((Magic.GetBishopAttacks(square, blockers) & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false);
                }
                if ((Magic.GetRookAttacks(square, blockers) & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true);
                }

                ulong diagonalPin = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false) & ~board.ColoredBitboards[2]);
                if (BitOperations.PopCount(diagonalPin) == 1)
                {
                    DiagonalPinMask |= diagonalPin;
                    DiagonalPinMovementMask = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false));
                    DiagonalPinMovementMask |= 1ul << square;
                }

                ulong orthogonalPin = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true) & ~board.ColoredBitboards[2]);
                if (BitOperations.PopCount(orthogonalPin) == 1)
                {
                    OrthogonalPinMask |= orthogonalPin;
                    OrthogonalPinMovementMask = (BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true));
                    OrthogonalPinMovementMask |= 1ul << square;
                }
            }

            attacks |= KingMoves[BitOperations.TrailingZeroCount(king)];
            if (checkRayMask == 0)
                checkRayMask = ulong.MaxValue;

            return attacks;
        }

        /// <summary>
        /// Generates all the legal moves in the position and returns the number of legal moves generated to a certain depth.
        /// used for perft testing, debbuging and move generation validation and to help optimize.
        /// </summary>
        /// <returns></returns>
        public static int Perft(Board b, int depth)
        {
            if (depth == 1)
            {
                return b.GetLegalMoves().Length; // If we are at depth 1, return the number of legal moves in the position.
            }

            int numPosition = 0; // Initialize the number of positions to 0.

            foreach (Move move in b.GetLegalMoves())
            {
                b.MakeMove(move);
                numPosition += Perft(b, depth - 1); // Recursively call perft on the new board and add the result to the number of positions.
                b.UndoMove(move); // Undo the move to return to the previous position.
            }

            return numPosition;
        }
    }
}
