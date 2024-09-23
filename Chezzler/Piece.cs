namespace Chezzler;

public static class Piece
{
    public const int Pawn = 1;
    public const int Rook = 2;
    public const int Knight = 3;
    public const int Bishop = 4;
    public const int Queen = 5;
    public const int King = 6;
    public const int Any = 7;

    public const int White = 8;
    public const int Black = 16;

    public static readonly char[] Piece2Char = ".PRNBQK?.PRNBQK?.prnbqk?.PRNBQK?".ToCharArray();
    public static readonly char[] Piece2Figurine = ".PRNBQK?.♟♜♞♝♛♚?.♙♖♘♗♕♔?.PRNBQK?".ToCharArray();
    public static int Char2Piece(char ch, bool includeSide = true)
    {
        var side = includeSide ? (char.IsUpper(ch) ? White : Black) : 0;
        return (int)(side + char.ToUpper(ch) switch
        {
            'P' => Pawn,
            'R' => Rook,
            'N' => Knight,
            'B' => Bishop,
            'Q' => Queen,
            'K' => King,
            _ => 0,
        });
    }

    public static int GetSide(int piece)
    {
        return piece & 0b00011000;
    }
    public static int GetType(int piece)
    {
        return piece & 0b00000111;
    }
}