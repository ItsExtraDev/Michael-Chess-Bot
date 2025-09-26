﻿using Michael.src.Helpers;
using System.Numerics;


namespace Michael.src.MoveGen
{

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

        private static bool cachedInit;
        public static ulong enemyAttacks; //Stores the enemy attacks for the current position, used to check for legal moves.
        private static int friendlyKingSquare; //Stores the square of the friendly king, used to check for legal moves.
        private static bool IsInDoubleCheck;
        private static bool IsInCheck; //Stores whether the current position is in check or not.
        private static ulong checkRayMask;
        private static ulong DiagonalPinMask;
        private static ulong OrthogonalPinMask;
        private static ulong DiagonalPinMovementMask;
        private static ulong OrthogonalPinMovementMask;
        //If we are generating moves for a qsearch, the only legal moves are captures and king moves.
        private static ulong movementMask;

        private static ulong lastHash;

        public static bool InCheck(Board boardInstance)
        {
            board = boardInstance;
            Init();

            return IsInCheck;
        }

        public static void GenerateLegalMoves(Board boardInstance, ref Span<Move> legalMoves, bool generateCapturesOnly)
        {
            board = boardInstance; //Set the board to the current board instance.
            Init(); //Initialize all the necessary variables for move generation.
            CurrentMoveIndex = 0;

            // In captures-only (qsearch):
            // - If NOT in check: allow only true captures (opp pieces + EP square)
            // - If IN check: allow all legal evasions (captures + interpositions), so don't restrict by target occupancy
            if (generateCapturesOnly)
            {
                if (IsInCheck)
                {
                    // In check: allow all legal moves (we must try every evasion)
                    movementMask = ulong.MaxValue;
                }
                else
                {
                    // Not in check: only captures
                    ulong epMask = board.EnPassantSquare != -1 ? (1ul << board.EnPassantSquare) : 0ul;
                    movementMask = board.ColoredBitboards[board.ColorToMove ^ 1] | epMask;
                }
            }
            else
            {
                movementMask = ulong.MaxValue;
            }

            GenerateLegalKingMoves(ref legalMoves); // Generate all the legal king moves and add them to the legal moves array.
            if (!IsInDoubleCheck)
            {
                GenerateLegalPawnMoves(ref legalMoves); // Generate all the legal pawn moves and add them to the legal moves array.
                GenerateLegalKnightMoves(ref legalMoves); // Generate all the legal knight moves and add them to the legal moves array.
                GenerateLegalSlidingMoves(ref legalMoves); // Generate all the legal sliding moves (rooks, bishops, and queens) and add them to the legal moves array.
            }

            //Convert the array to a span and slice to the amount of legal moves in the position and return as an array.
            //This is done to no return 218 moves when there are less than that in the position.
            legalMoves = legalMoves.Slice(0, CurrentMoveIndex).ToArray();
        }

        /// <summary>
        /// Generates all the legal moves for a pawn piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal pawn moves</param>
        private static void GenerateLegalPawnMoves(ref Span<Move> legalMoves)
        {
            int BitboardIndex = BitboardHelper.GetBitboardIndex(Piece.Pawn, board.ColorToMove);
            ulong pawns = board.PiecesBitboards[BitboardIndex];
            ulong pawnsPush = pawns & ~DiagonalPinMask;
            ulong pawnsCapture = pawns & ~OrthogonalPinMask;
            int moveDir = board.ColorToMove == Piece.White ? 1 : -1;

            //The rank each color need to get to in order to promote a pawn
            int finalRank = board.ColorToMove == Piece.White ? 7 : 0;
            //pawns can move up one rank, only to empty squares.
            ulong oneRankPush = BitboardHelper.ShiftBitboard(pawnsPush, 8 * moveDir);

            ulong piecesToCapture = board.ColoredBitboards[board.ColorToMove ^ 1];
            if (board.EnPassantSquare != 0)
            {
                piecesToCapture |= (1ul << board.EnPassantSquare);
            }
            ulong CaptureLeft = ((BitboardHelper.ShiftBitboard(pawnsCapture, 8 * moveDir) >> 1) & piecesToCapture) & PrecomputeMoveData.NotHFile & checkRayMask & movementMask;
            ulong CaptureRight = ((BitboardHelper.ShiftBitboard(pawnsCapture, 8 * moveDir) << 1) & piecesToCapture) & PrecomputeMoveData.NotAFile & checkRayMask & movementMask;
            oneRankPush &= board.ColoredBitboards[2];
            ulong twoRankPush = (BitboardHelper.ShiftBitboard(oneRankPush, 8 * moveDir)) & board.ColoredBitboards[2] & checkRayMask;

            //Make sure a pawn can push 2 squares only if on the starting square
            twoRankPush &= (board.ColorToMove == Piece.White ? BitboardHelper.Rank4 : BitboardHelper.Rank5) & movementMask;
            oneRankPush &= checkRayMask & movementMask;


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
                    continue;
                }
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
                    continue;
                }
               else if (board.EnPassantSquare == targetSquare && BoardHelper.Rank(targetSquare) > 0 && BoardHelper.Rank(targetSquare) < 7)
                    legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, MoveFlag.EnPassant);
                else legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
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
                    continue;
                }
                 else if (board.EnPassantSquare == targetSquare && BoardHelper.Rank(targetSquare) > 0 && BoardHelper.Rank(targetSquare) < 7)
                   legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare, MoveFlag.EnPassant);
                 else legalMoves[CurrentMoveIndex++] = new Move(startingSquare, targetSquare);
            }
        }


        /// <summary>
        /// Generates all the legal moves for a knight piece and return to the given legalMoves array.
        /// </summary>
        /// <param name="legalMoves">the array to return the legal knight moves</param>
        public static void GenerateLegalKnightMoves(ref Span<Move> legalMoves)
        {
            ulong knightBitboard = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Knight, board.ColorToMove)] & ~DiagonalPinMask & ~OrthogonalPinMask; ;
            while (knightBitboard != 0)
            {
                int knightSquare = BitboardHelper.PopLSB(ref knightBitboard); // Get the square of the knight piece.
                ulong attacks = KnightMoves[knightSquare] & enemyBitboardAndEmptySquares & checkRayMask & movementMask; // Get the precomputed moves for the knight from that square.
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
        public static void GenerateLegalKingMoves(ref Span<Move> legalMoves)
        {
            if (friendlyKingSquare == 64)
                return;

            ulong kingAttacks = KingMoves[friendlyKingSquare] & enemyBitboardAndEmptySquares & ~enemyAttacks; // Get the precomputed moves for the king from that square.
            if (!IsInCheck)
            {
                kingAttacks &= movementMask;
            }

            while (kingAttacks != 0)
            {
                int targetSquare = BitboardHelper.PopLSB(ref kingAttacks);
                legalMoves[CurrentMoveIndex++] = new Move(friendlyKingSquare, targetSquare);
            }

            //Generate castling moves if the king is not in check and the rook is not moved.
            int ourCastlingMask = board.ColorToMove == Piece.White ? 0b0011 : 0b1100;
            if (movementMask == ulong.MaxValue && !IsInCheck && (board.CasltingRight & ourCastlingMask) != 0 && BoardHelper.File(friendlyKingSquare) == 4)
            {
                if (board.IsWhiteToMove && BoardHelper.Rank(friendlyKingSquare) == 0 ||
                    !board.IsWhiteToMove && BoardHelper.Rank(friendlyKingSquare) == 7)
                {
                    if (board.ColorToMove == Piece.White && Piece.PieceType(board.Squares[7]) == Piece.Rook ||
                    board.ColorToMove == Piece.Black && Piece.PieceType(board.Squares[63]) == Piece.Rook)
                    {
                        //Generate castling moves for the king.
                        if ((GameState.CanWhiteCastleShort(board.CurrentGameState) && board.ColorToMove == Piece.White) ||
                            (GameState.CanBlackCastleShort(board.CurrentGameState) && board.ColorToMove == Piece.Black))
                        {
                            //King side castling
                            if ((board.Squares[friendlyKingSquare + 1] == Piece.None) && (board.Squares[friendlyKingSquare + 2] == Piece.None))
                            {
                                if ((enemyAttacks & 1ul << friendlyKingSquare + 1) == 0 && (enemyAttacks & 1ul << friendlyKingSquare + 2) == 0)
                                {
                                    legalMoves[CurrentMoveIndex++] = new Move(friendlyKingSquare, friendlyKingSquare + 2, MoveFlag.CastleShort);
                                }
                            }
                        }
                    }
                    if (board.ColorToMove == Piece.White && Piece.PieceType(board.Squares[0]) == Piece.Rook ||
                        board.ColorToMove == Piece.Black && Piece.PieceType(board.Squares[56]) == Piece.Rook)
                    {
                        if ((GameState.CanWhiteCastleLong(board.CurrentGameState) && board.ColorToMove == Piece.White) ||
                            (GameState.CanBlackCastleLong(board.CurrentGameState) && board.ColorToMove == Piece.Black))
                        {
                            //Queen side castling
                            if ((board.Squares[friendlyKingSquare - 1] == Piece.None) && (board.Squares[friendlyKingSquare - 2] == Piece.None) && (board.Squares[friendlyKingSquare - 3] == Piece.None) &&
                                    (enemyAttacks & 1ul << friendlyKingSquare - 1) == 0 && (enemyAttacks & 1ul << friendlyKingSquare - 2) == 0)
                            {
                                legalMoves[CurrentMoveIndex++] = new Move(friendlyKingSquare, friendlyKingSquare - 2, MoveFlag.CastleLong);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates all legal sliding moves for rooks, bishops and queens.
        /// </summary>
        private static void GenerateLegalSlidingMoves(ref Span<Move> legalMoves)
        {
            // Precompute bitboards only once
            ulong rooksAndQueens = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Rook, board.ColorToMove)] |
                                   board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove)];

            ulong bishopsAndQueens = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Bishop, board.ColorToMove)] |
                                     board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove)];

            // Orthogonal (rook) sliders
            GenerateSlidingMovesByBitboard(
                rooksAndQueens & ~DiagonalPinMask & ~OrthogonalPinMask,
                ref legalMoves,
                true);

            // Orthogonal pinned
            GenerateSlidingMovesByBitboard(
                rooksAndQueens & ~DiagonalPinMask & OrthogonalPinMask,
                ref legalMoves,
                true,
                OrthogonalPinMovementMask);

            // Diagonal (bishop) sliders
            GenerateSlidingMovesByBitboard(
                bishopsAndQueens & ~OrthogonalPinMask & ~DiagonalPinMask,
                ref legalMoves,
                false);

            // Diagonal pinned
            GenerateSlidingMovesByBitboard(
                bishopsAndQueens & ~OrthogonalPinMask & DiagonalPinMask,
                ref legalMoves,
                false,
                DiagonalPinMovementMask);
        }

        private static void GenerateSlidingMovesByBitboard(
            ulong pieces, ref Span<Move> legalMoves,
            bool isRook, ulong pinMask = ulong.MaxValue)
        {
            // Precompute blockers and friendlies once
            ulong blockers = ~board.ColoredBitboards[2];
            ulong friendly = board.ColoredBitboards[board.ColorToMove];

            while (pieces != 0)
            {
                int from = BitboardHelper.PopLSB(ref pieces);

                // directly mask friendly squares and pins
                ulong attacks = (isRook
                                    ? Magic.GetRookAttacks(from, blockers)
                                    : Magic.GetBishopAttacks(from, blockers))
                                & checkRayMask
                                & movementMask
                                & pinMask
                                & ~friendly; // friendly removed here

                while (attacks != 0)
                {
                    int to = BitboardHelper.PopLSB(ref attacks);
                    legalMoves[CurrentMoveIndex++] = new Move(from, to);
                }
            }
        }



        private static void Init()
        {
            if (board.CurrentHash == lastHash)
            {
                // nothing changed, keep previous precomputed masks
                return;
            }
            lastHash = board.CurrentHash;

            enemyBitboardAndEmptySquares = ~board.ColoredBitboards[board.ColorToMove];
            IsInCheck = false;
            IsInDoubleCheck = false;
            ulong kingBB = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.King, board.ColorToMove)];
            friendlyKingSquare = Math.Min(63, BitOperations.TrailingZeroCount(kingBB));
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
            ulong bishops = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Bishop, board.ColorToMove ^ 1)] | board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove ^ 1)];
            ulong rooks = board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Rook, board.ColorToMove ^ 1)] | board.PiecesBitboards[BitboardHelper.GetBitboardIndex(Piece.Queen, board.ColorToMove ^ 1)];
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

            ulong blockers = ~board.ColoredBitboards[2] ^ 1ul << friendlyKingSquare;

            ulong BishopAttacks;
            ulong BishopTunnel;

            while (bishops != 0)
            {
                int square = BitboardHelper.PopLSB(ref bishops);

                BishopAttacks = Magic.GetBishopAttacks(square, blockers);
                BishopTunnel = BoardHelper.GetAttackTunnel(square, friendlyKingSquare, false);

                if ((BishopAttacks & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= BishopTunnel;
                }

                ulong diagonalPin = BishopTunnel & ~board.ColoredBitboards[2];
                if (BitOperations.PopCount(diagonalPin) == 1)
                {
                    DiagonalPinMask |= diagonalPin;
                    DiagonalPinMovementMask |= BishopTunnel;
                    DiagonalPinMovementMask |= 1ul << square;
                }
                attacks |= BishopAttacks;
            }

            ulong rookAttacks;
            ulong rookTunnel;
            while (rooks != 0)
            {
                int square = BitboardHelper.PopLSB(ref rooks);

                rookAttacks = Magic.GetRookAttacks(square, blockers);
                rookTunnel = BoardHelper.GetAttackTunnel(square, friendlyKingSquare, true);

                if ((rookAttacks & 1ul << friendlyKingSquare) != 0)
                {
                    IsInDoubleCheck = IsInCheck;
                    IsInCheck = true;
                    checkRayMask |= 1ul << square;
                    checkRayMask |= rookTunnel;
                }

                ulong orthogonalPin = rookTunnel & ~board.ColoredBitboards[2];
                if (BitOperations.PopCount(orthogonalPin) == 1)
                {
                    OrthogonalPinMovementMask |= rookTunnel;
                    OrthogonalPinMask |= orthogonalPin;
                    OrthogonalPinMovementMask |= 1ul << square;
                }

                attacks |= rookAttacks;
            }

            attacks |= KingMoves[Math.Min(63, BitOperations.TrailingZeroCount(king))];
            if (checkRayMask == 0)
                checkRayMask = ulong.MaxValue;

            return attacks;
        }

        /// <summary>
        /// Generates all the legal moves in the position and returns the number of legal moves generated to a certain depth.
        /// used for perft testing, debbuging and move generation validation and to help optimize.
        /// </summary>
        /// <returns></returns>
        public static ulong Perft(Board b, int depth)
        {
            if (depth == 1)
            {
                return (ulong)b.GetLegalMoves().Length; // If we are at depth 1, return the number of legal moves in the position.
            }

            ulong numPosition = 0; // Initialize the number of positions to 0.

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
