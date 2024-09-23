using System.Diagnostics.CodeAnalysis;

namespace Chezzler;

public struct Move
{
    public enum Disambiguation : int
    {
        None = 0,
        File = 1,
        Rank = 2,
        Both = 3,
    }

    public int fromSquare;
    public int toSquare;

    public int piece;
    public int captures;
    public bool isEnPassant;
    public int prevEnPassantSquare;
    public int promotesTo;
    public bool isQueenSideCastle;
    public bool isKingSideCastle;
    public int prevCastlingRights;
    public bool isCheck;
    public bool isCheckmate;
    public Disambiguation disambiguation;
    public bool isNullMove;
    public int prevHalfMoveClock;

    public bool IsCapture => captures > 0;
    public bool IsPromotion => promotesTo > 0;
    public bool IsCastle => isQueenSideCastle || isKingSideCastle;


    public Move(int fromSquare, int toSquare, Board? board = null)
    {
        this.fromSquare = fromSquare;
        this.toSquare = toSquare;

        piece = 0;
        captures = 0;
        isEnPassant = false;
        isQueenSideCastle = false;
        isKingSideCastle = false;

        prevEnPassantSquare = -1;
        prevCastlingRights = 0;
        prevHalfMoveClock = 0;

        if (board != null)
        {
            piece = board.PieceOn(fromSquare);
            captures = board.PieceOn(toSquare);

            if (toSquare == board.EnPassantSquare
            && Piece.GetType(piece) == Piece.Pawn)
            {
                captures = Piece.Pawn | (board.SideToMove ^ 0b11000);
                isEnPassant = true;
            }
            prevEnPassantSquare = board.EnPassantSquare;

            if (Piece.GetType(piece) == Piece.King)
            {
                isQueenSideCastle = toSquare - fromSquare == -2;
                isKingSideCastle = toSquare - fromSquare == 2;
            }
            prevCastlingRights = board.CastlingRights;

            prevHalfMoveClock = board.HalfMoveClock;
        }

        promotesTo = 0;
        isCheck = false;
        isCheckmate = false;
        disambiguation = Disambiguation.None;
        isNullMove = fromSquare < 0 || fromSquare > 63
            || toSquare < 0 || toSquare > 63
            || fromSquare == toSquare;

    }

    public static int GetPriority(Move move, Board board)
    {
        var adjustedFromSquare = (move.piece & Piece.Black) > 0 ? Board.InvertRank(move.fromSquare) : move.fromSquare;
        var adjustedToSquare = (move.piece & Piece.Black) > 0 ? Board.InvertRank(move.toSquare) : move.toSquare;
        var adjustedCaptureSqaure = (move.captures & Piece.Black) > 0 ? Board.InvertRank(move.toSquare) : move.toSquare;
        var pieceValue = Chezzler.MidgamePieceValues[Piece.GetType(move.piece)];
        var priority = pieceValue[adjustedToSquare] - pieceValue[adjustedFromSquare];
        if (move.captures > 0)
            priority += 10 * Chezzler.MidgamePieceValues[Piece.GetType(move.captures)][adjustedCaptureSqaure] - pieceValue[move.toSquare];
        if (move.promotesTo > 0)
            priority += Chezzler.MidgamePieceValues[Piece.GetType(move.promotesTo)][adjustedToSquare];

        //if (board.IsAttacked(move.toSquare, Piece.GetSide(move.piece) ^ 0b11000))
        //    priority -= Chezzler.PieceValues[move.piece];

        return priority;
    }

    public static Move FromString(string? moveString)
    {
        if (string.IsNullOrEmpty(moveString) || moveString.Length < 2)
            return new Move() { isNullMove = true };

        var firstLetterCapital = char.IsUpper(moveString[0]);
        moveString = moveString.ToLower();
        var result = new Move();

        //check(mate)
        var checkIndicator = moveString[^1];
        if (checkIndicator == '#')
        {
            result.isCheckmate = true;
            result.isCheck = true;
            moveString = moveString[..^1];
        }
        else if (checkIndicator == '+')
        {
            result.isCheck = true;
            moveString = moveString[..^1];
        }

        //castling
        if (moveString == "o-o" || moveString == "0-0")
        {
            result.isKingSideCastle = true;
            result.piece = Piece.King;
            return result;
        }
        if (moveString == "o-o-o" || moveString == "0-0-0")
        {
            result.isQueenSideCastle = true;
            result.piece = Piece.King;
            return result;
        }

        //promotion
        if (moveString[^2] == '=')
        {
            result.promotesTo = Piece.Char2Piece(moveString[^1], false);
            moveString = moveString[..^2];
        }

        //destination
        result.toSquare = Board.Square(moveString[^2..]);
        if (result.toSquare < 0 || result.toSquare > 63)
        {
            result.isNullMove = true;
            return result;
        }
        moveString = moveString[..^2];

        //quiet pawn move is done
        if (moveString.Length == 0)
        {
            result.piece = Piece.Pawn;
            return result;
        }

        //capture
        if (moveString[^1] == 'x')
        {
            result.captures = Piece.Any;
            moveString = moveString[..^1];
        }

        //disambiguation
        if (moveString.Length == 2)
        {
            if (char.IsLetter(moveString[1]))
                result.disambiguation = Disambiguation.File;
            else if (char.IsNumber(moveString[1]))
                result.disambiguation = Disambiguation.Rank;
        }
        else if (moveString.Length == 3)
        {
            result.disambiguation = Disambiguation.Both;
            result.fromSquare = Board.Square(moveString[1..]);
        }

        //piece
        var p = moveString[0];
        result.piece = Piece.Char2Piece(p, false);
        if (result.piece == 0 || result.IsCapture && p == 'b' && !firstLetterCapital)
            result.piece = Piece.Pawn;

        return result;
    }

    public static Move FromString(string? moveString, Board board)
    {
        if (string.IsNullOrEmpty(moveString) || moveString.Length < 2)
            return new Move() { isNullMove = true };
        moveString = moveString.ToLower();

        //castling
        if (moveString.StartsWith("0-0-0")
        || moveString.StartsWith("o-o-o"))
        {
            if (board.SideToMove == Piece.White)
                return new Move(60, 58, board) { isQueenSideCastle = true };
            else
                return new Move(4, 2, board) { isQueenSideCastle = true };
        }
        else if (moveString.StartsWith("0-0")
        || moveString.StartsWith("o-o"))
        {
            if (board.SideToMove == Piece.White)
                return new Move(60, 62, board) { isKingSideCastle = true };
            else
                return new Move(4, 6, board) { isKingSideCastle = true };
        }

        switch (moveString[0])
        {
            case 'a':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'g':
            case 'h':
                if (char.IsNumber(moveString[1]))
                {
                    var toSquare = Board.Square(moveString[..2]);
                    return new Move(toSquare + board.SideToMove == Piece.White ? 8 : -8, toSquare, board);
                }
                if (moveString[1] == 'x')
                {
                    var toSquare = Board.Square(moveString.Substring(2, 2));
                    var captureDirection = moveString[2] - moveString[0];
                    var fromSquare = toSquare
                        + board.SideToMove == Piece.White ? 8 : -8
                        - captureDirection;
                    return new Move(fromSquare, toSquare, board);
                }
                break;

            case 'b':
                break;

            case 'k':
                var possibleFromSquares = board.GetSquares(Piece.King + board.SideToMove);
                foreach (var square in possibleFromSquares)
                {
                    var moves = new List<Move>();
                    board.AddSlidingMoves(moves, square, 0b11111111, 1);
                }
                break;
            case 'n':
                break;
            case 'q':
                break;
            case 'r':
                break;

            default:
                break;
        }
        return new Move() { isNullMove = true };
    }

    public override string ToString()
    {
        if (isNullMove) return "-";
        if (isKingSideCastle) return "O-O";
        if (isQueenSideCastle) return "O-O-O";

        var result = "";

        var isPawn = Piece.GetType(piece) == Piece.Pawn;
        if (!isPawn)
            result += char.ToUpper(Piece.Piece2Char[piece]); //pieces are always capitalized for the moves
        if (isPawn && IsCapture || ((int)disambiguation & 1) > 0)
            result += Board.File(fromSquare);
        if (((int)disambiguation & 2) > 0)
            result += Board.Rank(fromSquare);
        if (IsCapture)
            result += "x";
        result += Board.RankAndFile(toSquare);
        if (IsPromotion)
            result += "=" + Piece.Piece2Char[promotesTo];

        return result;
    }

    public string GetDescription()
    {
        if (isNullMove)
            return "Null move\n";
        var result = "";
        result += "From Square: " + fromSquare + "(" + Board.RankAndFile(fromSquare) + ")\n";
        result += "To Square: " + toSquare + "(" + Board.RankAndFile(toSquare) + ")\n";
        result += "Piece: " + piece + "(" + Piece.Piece2Figurine[piece] + ")\n";
        if (IsCapture)
            result += "Caputres: " + captures + "(" + Piece.Piece2Figurine[captures] + ")\n";
        if (IsPromotion)
            result += "Promotes to: " + promotesTo + "(" + Piece.Piece2Figurine[promotesTo] + ")\n";
        if (isKingSideCastle)
            result += "Kingside Castle\n";
        if (isQueenSideCastle)
            result += "Queenside Castle\n";
        if (isCheckmate)
            result += "Checkmate\n";
        else if (isCheck)
            result += "Check\n";

        return result;
    }

    public override bool Equals(object? obj)
    {
        return obj is Move move &&
               fromSquare == move.fromSquare &&
               toSquare == move.toSquare &&
               promotesTo == move.promotesTo;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(fromSquare, toSquare, Piece.GetType(promotesTo));
    }

    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);
}
