using System.Diagnostics;

namespace Chezzler;

public static class Chezzler
{
    public const int MinValue = int.MinValue + 1;
    public const int MaxValue = int.MaxValue;
    private static readonly Random random = new();
    //public static readonly Dictionary<long, TTableEntry> tTable = new();
    private static int SearchDepth = 5;

    public static readonly int[][] MidgamePieceValues = new int[][]
    {
        new int[] //no piece
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
        },
        new int[] //pawn
        {
              0,   0,   0,   0,   0,   0,   0,   0,
            200, 200, 200, 200, 200, 200, 200, 200,
            180, 180, 180, 180, 180, 180, 180, 180,
            160, 160, 160, 160, 160, 160, 160, 160,
            130, 140, 140, 150, 150, 100,  90,  90,
            110, 120, 100, 130, 130,  90, 100, 100,
             90, 100, 100,  90,  90, 100, 100, 100,
              0,   0,   0,   0,   0,   0,   0,   0,
        },
        new int[] //rook
        {
            480, 490, 490, 490, 490, 490, 490, 480,
            510, 520, 520, 520, 520, 520, 520, 510,
            490, 500, 500, 500, 500, 500, 500, 490,
            490, 500, 500, 500, 500, 500, 500, 490,
            490, 500, 500, 500, 500, 500, 500, 490,
            490, 500, 500, 500, 500, 500, 500, 490,
            490, 500, 500, 500, 500, 500, 500, 490,
            480, 490, 490, 490, 490, 490, 490, 480,
        },
        new int[] //knight
        {
            240, 250, 260, 260, 260, 260, 250, 240,
            250, 260, 280, 280, 280, 280, 260, 250,
            260, 280, 300, 300, 300, 300, 280, 260,
            260, 280, 300, 300, 300, 300, 280, 260,
            260, 280, 300, 300, 300, 300, 280, 260,
            260, 280, 300, 300, 300, 300, 280, 260,
            250, 260, 280, 280, 280, 280, 260, 250,
            240, 250, 260, 260, 260, 260, 250, 240,
        },
        new int[] //bishop
        {
            280, 280, 260, 260, 260, 260, 280, 280,
            280, 300, 300, 280, 280, 300, 300, 280,
            260, 300, 300, 300, 300, 300, 300, 260,
            260, 280, 300, 320, 320, 300, 280, 260,
            260, 280, 320, 320, 320, 320, 280, 260,
            260, 320, 320, 320, 320, 320, 320, 260,
            300, 320, 320, 300, 300, 320, 320, 300,
            300, 300, 280, 280, 280, 280, 300, 300,
        },
        new int[] //queen
        {
            890, 890, 890, 890, 890, 890, 890, 880,
            890, 900, 900, 900, 900, 900, 900, 890,
            890, 900, 900, 900, 900, 900, 900, 890,
            890, 900, 900, 900, 900, 900, 900, 890,
            890, 900, 900, 900, 900, 900, 900, 890,
            890, 900, 900, 900, 900, 900, 900, 890,
            890, 900, 900, 900, 900, 900, 900, 890,
            880, 890, 890, 890, 890, 890, 890, 880,
        },
        new int[] //king
        {
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
              0,   0,   0,   0,   0,   0,   0,   0,
              0, 100,   0,   0,   0,   0, 100,   0,
            100, 200, 100,   0,   0, 100, 200, 100,
        },
    };

    public static readonly int[][] EndgamePieceValues = new int[][]
    {
        new int[] //no piece
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
        },
        new int[] //pawn
        {
              0,   0,   0,   0,   0,   0,   0,   0,
            250, 250, 250, 250, 250, 250, 250, 250,
            200, 200, 200, 200, 200, 200, 200, 200,
            160, 160, 160, 160, 160, 160, 160, 160,
            130, 130, 130, 130, 130, 130, 130, 130,
            110, 110, 110, 110, 110, 110, 110, 110,
            100, 100, 100, 100, 100, 100, 100, 100,
              0,   0,   0,   0,   0,   0,   0,   0,
        },
        new int[] //rook
        {
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500,
        },
        new int[] //knight
        {
            240, 250, 260, 260, 260, 260, 250, 240,
            250, 260, 280, 280, 280, 280, 260, 250,
            260, 280, 300, 300, 300, 300, 280, 260,
            260, 280, 300, 300, 300, 300, 280, 260,
            260, 280, 300, 300, 300, 300, 280, 260,
            260, 280, 300, 300, 300, 300, 280, 260,
            250, 260, 280, 280, 280, 280, 260, 250,
            240, 250, 260, 260, 260, 260, 250, 240,
        },
        new int[] //bishop
        {
            290, 290, 290, 290, 290, 290, 290, 290,
            290, 310, 310, 310, 310, 310, 310, 290,
            290, 310, 330, 330, 330, 330, 310, 290,
            290, 310, 330, 350, 350, 330, 310, 290,
            290, 310, 330, 350, 350, 330, 310, 290,
            290, 310, 330, 330, 330, 330, 310, 290,
            290, 310, 310, 310, 310, 310, 310, 290,
            290, 290, 290, 290, 290, 290, 290, 290,
        },
        new int[] //queen
        {
            900, 900, 900, 900, 900, 900, 900, 880,
            900, 900, 900, 900, 900, 900, 900, 900,
            900, 900, 900, 900, 900, 900, 900, 900,
            900, 900, 900, 900, 900, 900, 900, 900,
            900, 900, 900, 900, 900, 900, 900, 900,
            900, 900, 900, 900, 900, 900, 900, 900,
            900, 900, 900, 900, 900, 900, 900, 900,
            900, 900, 900, 900, 900, 900, 900, 900,
        },
        new int[] //king
        {
              0,  10,  20,  30,  30,  20,  10,   0,
             10,  20,  30,  40,  40,  30,  20,  10,
             20,  30,  40,  50,  50,  40,  30,  20,
             30,  40,  50,  60,  60,  50,  40,  30,
             30,  40,  50,  60,  60,  50,  40,  30,
             20,  30,  40,  50,  50,  40,  30,  20,
             10,  20,  30,  40,  40,  30,  20,  10,
              0,  10,  20,  30,  30,  20,  10,   0,
        },
    };

    public enum EntryType
    {
        ExactValue,
        LowerBound,
        UpperBound,
    }

    public struct TTableEntry
    {
        public int Value;
        public int Depth;
        public EntryType entryType;
    }

    public static Move GetBestMove(Board board)
    {
        var moves = new List<Move>();
        board.GetMoves(moves);
        board.RemoveIllegalMoves(moves);

        if (moves.Count == 0)
            return new Move() { isNullMove = true };

        //Shuffle(moves);
        SortMoves(moves, board);

        var bestMove = moves[0];
        var bestValue = MinValue;
        for (int i = 0; i < moves.Count; i++)
        {
            board.MakeMove(moves[i]);
            var value = -Negamax(board, MinValue, -bestValue, 0);
            board.UnmakeMove(moves[i]);
            if (value > bestValue)
            {
                bestValue = value;
                bestMove = moves[i];
            }
        }

        Debug.WriteLine("Evaluation: " + bestValue);
        return bestMove;
    }

    public static int Negamax(Board board, int alpha, int beta, int depth)
    {
        var originalAlpha = alpha;

        //check tTable
        //if (tTable.TryGetValue(board.Hash, out var entry)
        //&& entry.Depth >= depth)
        //{
        //    switch (entry.entryType)
        //    {
        //        case EntryType.UpperBound:
        //            beta = Math.Min(beta, entry.Value);
        //            break;

        //        case EntryType.LowerBound:
        //            alpha = Math.Max(alpha, entry.Value);
        //            break;

        //        case EntryType.ExactValue:
        //            return entry.Value;
        //    }
        //    if (alpha >= beta)
        //        return entry.Value;
        //}

        //no king = no bueno
        if (board.GetSquares(Piece.King | board.SideToMove).Count == 0)
            return MinValue + depth;

        //check for draws
        if (board.HalfMoveClock >= 100 || board.History.Contains(board.Hash) || board.PieceCount <= 2)
            return 0;

        //leaf node
        if (depth >= SearchDepth)
        {
            //Debug.WriteLine(board.ToFEN());
            return Quiesce(board, alpha, beta, depth);
            //return Evaluate(board);
        }

        //get all the moves in the position
        var moves = new List<Move>();
        board.GetMoves(moves);
        //board.RemoveIllegalMoves(moves);
        SortMoves(moves, board);

        //recursive search
        var movesTried = 0;
        var value = MinValue;
        for (int i = 0; i < moves.Count; i++)
        {
            board.MakeMove(moves[i]);
            if (!board.InCheck(board.SideToMove ^ 0b11000))
            {
                value = Math.Max(value, -Negamax(board, -beta, -alpha, depth + 1));
                movesTried++;
            }
            board.UnmakeMove(moves[i]);
            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
                break;
        }

        if (movesTried == 0 && alpha < beta)
            return board.InCheck(board.SideToMove) ? MinValue + depth : 0;

        //tTable store
        //var newEntry = new TTableEntry()
        //{
        //    Value = bestValue,
        //    Depth = depth,
        //};
        //if (bestValue <= originalAlpha)
        //    newEntry.entryType = EntryType.UpperBound;
        //else if (bestValue >= beta)
        //    newEntry.entryType = EntryType.LowerBound;
        //else
        //    newEntry.entryType = EntryType.ExactValue;
        //if (tTable.ContainsKey(board.Hash))
        //{
        //    if (depth > entry.Depth)
        //        tTable[board.Hash] = newEntry;
        //}
        //else
        //    tTable.Add(board.Hash, newEntry);

        return value;
    }

    public static int Quiesce(Board board, int alpha, int beta, int depth)
    {
        var originalAlpha = alpha;

        //check tTable
        //if (tTable.TryGetValue(board.Hash, out var entry))
        //{
        //    switch (entry.entryType)
        //    {
        //        case EntryType.UpperBound:
        //            beta = Math.Min(beta, entry.Value);
        //            break;

        //        case EntryType.LowerBound:
        //            alpha = Math.Max(alpha, entry.Value);
        //            break;

        //        case EntryType.ExactValue:
        //            return entry.Value;
        //    }
        //    if (alpha >= beta)
        //        return entry.Value;
        //}

        //no king == no bueno
        if (board.GetSquares(Piece.King | board.SideToMove).Count == 0)
            return MinValue + depth;

        //draw by insufficient material
        if (board.PieceCount <= 2)
            return 0;

        //assume we can get this value if we make a quiet move
        var standValue = Evaluate(board);
        if (standValue >= beta)
            return beta;
        alpha = Math.Max(standValue, alpha);
        
        //get all the moves in the position
        var moves = new List<Move>();
        board.GetMoves(moves, true);
        //board.RemoveIllegalMoves(moves);
        SortMoves(moves, board);

        //recursive search
        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            var value = MinValue;
            board.MakeMove(move);
            if (!board.InCheck(board.SideToMove ^ 0b11000))
            {
                value = -Quiesce(board, -beta, -alpha, depth + 1);
            }
            board.UnmakeMove(move);

            if (value >= beta)
                return beta;
            alpha = Math.Max(value, alpha);
        }

        //tTable store
        //var newEntry = new TTableEntry()
        //{
        //    Value = alpha,
        //    Depth = 0,
        //};
        //if (alpha <= originalAlpha)
        //    newEntry.entryType = EntryType.UpperBound;
        //else if (alpha >= beta)
        //    newEntry.entryType = EntryType.LowerBound;
        //else
        //    newEntry.entryType = EntryType.ExactValue;
        //if (!tTable.ContainsKey(board.Hash))
        //    tTable.Add(board.Hash, newEntry);

        return alpha;
    }

    public static int Evaluate(Board board)
    {
        var myEval = Evaluate(board, board.SideToMove);
        var opEval = Evaluate(board, board.SideToMove ^ 0b11000);

        var evaluation = myEval - opEval;

        if (evaluation != 0)
        {
            var whiteKingSquare = board.GetSquares(Piece.King | Piece.White)[0];
            var blackKingSquare = board.GetSquares(Piece.King | Piece.Black)[0];
            evaluation +=
                (14 - Board.ManhattanDistance(whiteKingSquare, blackKingSquare)) //king proximity
                * (32 - board.PieceCount) / 29 //proximity matters most in the endgame
                * 2000 / evaluation; //proximity is best when you're ahead, but only by a little bit
        }
        
        return evaluation;
    }

    public static int Evaluate(Board board, int side)
    {
        var kingFile = board.GetSquares(Piece.King | side)[0] % 8;
        var evaluation = 0;
        for (int piece = side | Piece.Pawn; piece <= (side | Piece.King); piece++)
        {
            var squares = board.GetSquares(piece);
            for (int i = 0; i < squares.Count; i++)
            {
                var adjustedSquare = squares[i];
                if (side == Piece.Black)
                    adjustedSquare = Board.InvertRank(adjustedSquare);
                if (kingFile < 4)
                    adjustedSquare = Board.InvertFile(adjustedSquare);
                evaluation +=
                    MidgamePieceValues[Piece.GetType(piece)][adjustedSquare] * (board.PieceCount - 2) / 29
                  + EndgamePieceValues[Piece.GetType(piece)][adjustedSquare] * (32 - board.PieceCount) / 29;
            }
        }

        //give points for keeping castling rights
        if (side == Piece.White)
        {
            if (board.WhiteCanCastleKingSide)
                evaluation += 25;
            if (board.WhiteCanCastleQueenSide)
                evaluation += 25;
        }
        else if (side == Piece.Black)
        {
            if (board.BlackCanCastleKingSide)
                evaluation += 25;
            if (board.BlackCanCastleQueenSide)
                evaluation += 25;
        }

        return evaluation;
    }

    public static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var r = random.Next(list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    public static void SortMoves(List<Move> moves, Board board)
    {
        var priorities = new int[moves.Count];
        for (int i = 0; i < moves.Count; i++)
        {
            priorities[i] = Move.GetPriority(moves[i], board);
        }

        for (int i = 0; i < moves.Count - 1; i++)
        {
            for (int j = i + 1; j < moves.Count; j++)
            {
                if (priorities[i] < priorities[j])
                {
                    (moves[i], moves[j]) = (moves[j], moves[i]);
                    (priorities[i], priorities[j]) = (priorities[j], priorities[i]);
                }
            }
        }
    }

    public static void RemoveNonCaptures(List<Move> moves)
    {
        for (int i = moves.Count - 1; i >= 0; i--)
        {
            if (moves[i].captures == 0)
                moves.RemoveAt(i);
        }
    }
}
