namespace Chezzler;

/* To Do:
 * determine fromSquare with move parser
 * random opponent
 * handle checks and checkmate
 * prevent castling thru check
 * 3fold repetition
 * 
 * optimize board as list of pieces
 * optimize checks with pins and attacked sqaures?
 */

public class Program
{
    public static void Main(string[] args)
    {
        Random random = new Random();
        Board.ComputeDistanceToEdge();
        Board board = Board.FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        //Board board = Board.FromFEN("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 0 1");
        //Board board = Board.FromFEN("8/8/8/2pP4/8/8/8/8 w KQkq c6 0 1");
        Console.OutputEncoding = System.Text.Encoding.Unicode;

        //while (true)
        //{
        //    var s = Console.ReadLine();
        //    var move = Move.FromString(s, board);

        //    Console.WriteLine(move.GetDescription());
        //}

        while (true)
        {
            Console.WriteLine(board);
            var legalMoves = new List<Move>();
            board.GetMoves(legalMoves);

            //for (int i = 0; i < legalMoves.Length; i++)
            //{
            //    Console.WriteLine(legalMoves[i]);
            //}

            if (legalMoves.Count == 0)
            {
                Console.WriteLine("Game over, starting new game.");
                Console.ReadKey(true);
                board = Board.FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
                continue;
            }

            var move = legalMoves[random.Next(legalMoves.Count)];
            Console.WriteLine(move);

            if (move.IsCastle)
                Console.ReadKey(true);

            board.MakeMove(move);
            Console.WriteLine();
        }

        //while (true)
        //{
        //    Console.WriteLine(board.ToString());
        //    Console.WriteLine();
        //    Console.WriteLine("Your Move: ");

        //    Move move;
        //    do
        //    {
        //        var moveString = Console.ReadLine();

        //        if (moveString is null)
        //        {
        //            Console.WriteLine("Null move.");
        //        }

        //        move = Move.FromString(moveString);
        //        if (move.isNullMove)
        //        {
        //            Console.WriteLine("Not a valid move.");
        //        }

        //        if (!board.GetLegalMoves.Contains(move))
        //        {
        //            Console.WriteLine("Illegal move.");
        //            move = null;
        //        }
        //    } while (move.isNullMove);

        //    board.MakeMove(move);
        //}
    }
}