using Michael.src.Helpers;
using System.Numerics;

namespace Michael.src.MoveGen
{

    /*
     * LEFT TODO FOR MOVEGEN:
     * EN PASSANT
     * CASTLE
     * MATE
     * STALEMATE
     * DRAW REPETION
     * DRAW 50 MOVES
     * PIN
     * IS IN CHECK
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

        public static Move[] GenerateLegalMoves(Board boardInstance)
        {
            board = boardInstance; //Set the board to the current board instance.
            Init(); //Initialize all the necessary variables for move generation.
            Move[] legalMoves = new Move[MaxLegalMoves]; // Create an array to hold the legal moves.

            GenerateLegalPawnMoves(ref legalMoves); // Generate all the legal pawn moves and add them to the legal moves array.
            GenerateLegalKnightMoves(ref legalMoves); // Generate all the legal knight moves and add them to the legal moves array.
            GenerateLegalKingMoves(ref legalMoves); // Generate all the legal king moves and add them to the legal moves array.
            GenerateLegalSlidingMoves(ref legalMoves); // Generate all the legal sliding moves (rooks, bishops, and queens) and add them to the legal moves array.

            //Convert the array to a span and slice to the amount of legal moves in the position and return as an array.
            //This is done to no return 218 moves when there are less than that in the position.
            return legalMoves.AsSpan().Slice(0, CurrentMoveIndex).ToArray(); 
        }

        // /// <summary>
        // /// Generates all the legal moves for a pawn piece and return to the given legalMoves array.
        // /// </summary>
        // /// <param name="legalMoves">the array to return the legal pawn moves</param>
        public static void GenerateLegalPawnMoves(ref Move[] legalMoves)
        {
            ulong pawnBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Pawn, board.ColorToMove)];

            int moveDirection = board.ColorToMove == Piece.White ? 1 : -1; // Determine the move direction based on the color to move.
            ulong oneRankPush = BitboardHelper.ShiftBitboard(pawnBitboard, 8 * moveDirection) & board.ColoredBitboards[2];
            ulong twoRankPush = BitboardHelper.ShiftBitboard(oneRankPush, 8 * moveDirection) & board.ColoredBitboards[2] & checkRayMask;
            ulong middleRank = board.ColorToMove == Piece.White ? BitboardHelper.Rank4 : BitboardHelper.Rank5; // Determine the middle rank based on the color to move.
            ulong promotionRank = board.ColorToMove == Piece.White ? BitboardHelper.Rank8 : BitboardHelper.Rank1; // Determine the promotion rank based on the color to move.
            //Allow double pawn push only if the move ends on the middle rank.
            twoRankPush &= middleRank;
            ulong[] Captures = (board.ColorToMove == Piece.White ? WhitePawnAttacks : BlackPawnAttacks);
            ulong pawnPushPromotion = oneRankPush & promotionRank & checkRayMask; // Get the squares where the pawn can promote.
            oneRankPush &= checkRayMask &= ~promotionRank; // Remove the promotion squares from the one rank push.

            while (oneRankPush != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref oneRankPush); // Get the square of the pawn piece.
                int startingSquare = targetSquare - (8 * moveDirection); // Calculate the starting square of the pawn.

                legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
            }
            while (twoRankPush != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref twoRankPush); // Get the square of the pawn piece.
                int startingSquare = targetSquare - (16 * moveDirection); // Calculate the starting square of the pawn.
                legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, MoveFlag.DoublePawnPush);
            }

            while (pawnPushPromotion != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref pawnPushPromotion); // Get the square of the pawn piece.
                int startingSquare = targetSquare - (8 * moveDirection); // Calculate the starting square of the pawn.

                for (int pt = 2; pt <= 5; pt++) // Iterate through all possible promotions (Knight, Bishop, Rook, Queen).
                {
                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, pt);
                }
            }

            while (pawnBitboard != 0)
            {
                int startingSquare = BitboardHelper.PopLSB(ref pawnBitboard); // Get the square of the pawn piece.
                ulong attacks = Captures[startingSquare] & board.ColoredBitboards[board.ColorToMove ^ 1] & checkRayMask;

                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks); // Get the target square of the pawn capture.
                                                                           //Promotion generation
                    if ((promotionRank & 1ul << targetSquare) != 0)
                    {
                        for (int pt = 2; pt <= 5; pt++) // Iterate through all possible promotions (Knight, Bishop, Rook, Queen).
                        {
                            legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, pt);
                        }
                        continue;
                    }
                    // Add the move to the legal moves array.
                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
                }
            }


        }

        /// <summary>
        /// Generates all the legal moves for a knight piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal knight moves</param>
        public static void GenerateLegalKnightMoves(ref Move[] legalMoves)
        {
            ulong knightBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Knight, board.ColorToMove)];
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

            //TODO remove after legal move generation is implemented.
            if (king == 0)
                return;

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
        public static void GenerateLegalSlidingMoves(ref Move[] legalMoves)
        {
            ulong orthogonalSlidesBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Rook, board.ColorToMove)] |
                                           board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove)];
            ulong diagonalSlidesBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Bishop, board.ColorToMove)] |
                                           board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove)];

            while (orthogonalSlidesBitboard != 0)
            {
                int square = BitboardHelper.PopLSB(ref orthogonalSlidesBitboard); // Get the square of the sliding piece.
                ulong attacks = Magic.GetRookAttacks(square, ~board.ColoredBitboards[2]) & enemyBitboardAndEmptySquares & checkRayMask; // Get the precomputed moves for the sliding piece from that square.
                // Iterate through all possible moves and add them to the legal moves array.
                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks);
                    legalMoves[CurrentMoveIndex++] = new Move(square, targetSquare);
                }
            }
            while (diagonalSlidesBitboard != 0)
            {
                int square = BitboardHelper.PopLSB(ref diagonalSlidesBitboard); // Get the square of the sliding piece.
                ulong attacks = Magic.GetBishopAttacks(square, ~board.ColoredBitboards[2]) & enemyBitboardAndEmptySquares & checkRayMask; // Get the precomputed moves for the sliding piece from that square.
                // Iterate through all possible moves and add them to the legal moves array.
                while (attacks != 0)
                {
                    int targetSquare = BitboardHelper.PopLSB(ref attacks);
                    legalMoves[CurrentMoveIndex++] = new Move(square, targetSquare);
                }
            }
        }

        //Sets up all the necessary variables for move generation.
        //only called from the start of generateLegalMoves. NOT from main.
        private static void Init()
        {
            CurrentMoveIndex = 0; // Reset the current move index to 0.
            enemyBitboardAndEmptySquares = ~board.ColoredBitboards[board.ColorToMove]; // Get the enemy bitboard and empty squares.
            IsInCheck = false;
            IsInDoubleCheck = false;
            friendlyKingSquare = BitOperations.TrailingZeroCount(board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.King, board.ColorToMove)]);
            checkRayMask = 0;

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
                Console.WriteLine(square + " " + friendlyKingSquare);
                ulong blockers = (~board.ColoredBitboards[2] & Magic.GetBishopAttackMask(square)) ^ 1ul << friendlyKingSquare;

                if ((Magic.GetBishopAttacks(square, blockers) & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false);
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
