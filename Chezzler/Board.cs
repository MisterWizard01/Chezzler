namespace Chezzler;

public class Board
{
    public static readonly int[] Directions = { 1, -7, -8, -9, -1, 7, 8, 9, };
    public static readonly int[][] DistanceToEdge = new int[64][];
    public static readonly long[][] ZobristTable = new long[Piece.King | Piece.Black + 1][];
    public static long ZobristBlackToPlay;
    public static readonly long[] ZobristEnPassantFile = new long[8];
    public static readonly long[] ZobristCastlingRights = new long[16];

    /// <summary>
    /// An array of pieces mapped by their location.
    /// </summary>
    private int[] _board;

    /// <summary>
    /// An array of lists of squares indexed by the piece on them.
    /// WARNING: _pieceList[0] is not guaranteed to be an accurate list of empty squares.
    /// </summary>
    private readonly List<int>[] _pieceList;
    public int PieceCount { get; private set; }
    //public int WhiteCount { get; private set; }
    //public int BlackCount { get; private set; }
    public int SideToMove { get; set; }

    /// <summary>
    /// A bitfield representing the castling rights of both sides.
    /// 1 = can castle, 0 = can't castle. Order is KQkq.
    /// </summary>
    public int CastlingRights { get; set; }

    /// <summary>
    /// The square on which a pawn can be captured en passant
    /// </summary>
    public int EnPassantSquare { get; set; }

    /// <summary>
    /// Number of plies since the last pawn move or capture.
    /// Used for determining draw by 50-move rule.
    /// </summary>
    public int HalfMoveClock { get; set; }

    /// <summary>
    /// The 1-based move number. (White's ply + black's reply = 1 move)
    /// </summary>
    public int MoveCounter { get; set; }

    /// <summary>
    /// The Zobrist Hash of the position.
    /// </summary>
    public long Hash { get; private set; }

    /// <summary>
    /// A count of how long each position has been visited this game.
    /// Indexed by its Zobrist hash.
    /// </summary>
    public List<long> History { get; set; }

    public bool WhiteCanCastleKingSide => (CastlingRights & 0b1000) > 0;
    public bool WhiteCanCastleQueenSide => (CastlingRights & 0b0100) > 0;
    public bool BlackCanCastleKingSide => (CastlingRights & 0b0010) > 0;
    public bool BlackCanCastleQueenSide => (CastlingRights & 0b0001) > 0;


    public Board()
    {
        _board = new int[64];
        _pieceList = new List<int>[Piece.Black + Piece.King + 1];
        for (int i = 0; i < _pieceList.Length; i++)
        {
            _pieceList[i] = new List<int>(8);
        }
        PieceCount = 0;
        //WhiteCount = 0;
        //BlackCount = 0;
        SideToMove = 0;
        CastlingRights = 0b1111;
        EnPassantSquare = -1;
        HalfMoveClock = 0;
        MoveCounter = 1;
        Hash = 0;
        History = new();
    }
    
    public Board(Board toCopy)
    {
        _board = new int[64];
        Array.Copy(toCopy._board, _board, 64);
        _pieceList = new List<int>[Piece.Black + Piece.King + 1];
        for (int i = 0; i < _pieceList.Length; i++)
        {
            _pieceList[i] = new List<int>();
            for (int j = 0; j < toCopy._pieceList[i].Count; j++)
            {
                _pieceList[i].Add(toCopy._pieceList[i][j]);
            }
        }
        PieceCount = toCopy.PieceCount;
        //WhiteCount = toCopy.WhiteCount;
        //BlackCount = toCopy.BlackCount;
        SideToMove = toCopy.SideToMove;
        CastlingRights = toCopy.CastlingRights;
        EnPassantSquare = toCopy.EnPassantSquare;
        HalfMoveClock = toCopy.HalfMoveClock;
        MoveCounter = toCopy.MoveCounter;
        Hash = toCopy.Hash;
        History = new();
        foreach(var hash in toCopy.History)
        {
            History.Add(hash);
        }
    }

    public static void ComputeDistanceToEdge()
    {
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int right = 7 - file;
                int up = rank;
                int left = file;
                int down = 7 - rank;

                DistanceToEdge[file + rank * 8] = new int[]
                {
                    right,
                    Math.Min(right, up),
                    up,
                    Math.Min(left, up),
                    left,
                    Math.Min(left, down),
                    down,
                    Math.Min(right, down),
                };
            }
        }
    }

    public static void SetupZobrist(Random random)
    {
        ZobristTable[0] = new long[64];
        for (int i = 0; i < 64; i++)
        {
            ZobristTable[0][i] = 0;
        }
        for (int i = Piece.Pawn + Piece.White; i <= Piece.King + Piece.Black; i++)
        {
            ZobristTable[i] = new long[64];
            for (int j = 0; j < 64; j++)
            {
                ZobristTable[i][j] = random.NextInt64(long.MinValue, long.MaxValue);
            }
        }

        ZobristBlackToPlay = random.NextInt64(long.MinValue, long.MaxValue);
        for (int i = 0; i < ZobristEnPassantFile.Length; i++)
        {
            ZobristEnPassantFile[i] = random.NextInt64(long.MinValue, long.MaxValue);
        }
        for (int i = 0; i < ZobristCastlingRights.Length; i++)
        {
            ZobristCastlingRights[i] = random.NextInt64(long.MinValue, long.MaxValue);
        }
    }

    public static char Rank(int square) => (char)(7 - square / 8 + '1');
    public static char File(int square) => (char) (square % 8 + 'a');
    public static string RankAndFile(int square) => new(new char[] { File(square), Rank(square) });
    public static int Square(string rankAndFile)
    {
        if (rankAndFile == null || rankAndFile.Length != 2)
        {
            return -1;
        }

        var file = rankAndFile[0] - 'a';
        var rank = rankAndFile[1] - '1';
        if (file < 0 || file > 7
        || rank < 0 || rank > 7)
        {
            return -1;
        }
        return file + (7 - rank) * 8;
    }

    public int PieceOn(int square) => _board[square];
    public int PieceOn(string square) => PieceOn(Square(square));

    public void PlacePiece(int piece, int square)
    {
        if (piece <= 0)
            return;
        _pieceList[piece].Add(square);
        _board[square] = piece;
        PieceCount++;
        //if (Piece.GetSide(piece) == Piece.White)
        //    WhiteCount++;
        //else if (Piece.GetSide(piece) == Piece.Black)
        //    BlackCount++;
        Hash ^= ZobristTable[piece][square];
    }
    public int RemovePiece(int square)
    {
        var piece = _board[square];
        if (piece <= 0)
            return piece;
        _board[square] = 0;
        _pieceList[piece].Remove(square);
        PieceCount--;
        //if (Piece.GetSide(piece) == Piece.White)
        //    WhiteCount--;
        //else if (Piece.GetSide(piece) == Piece.Black)
        //    BlackCount--;
        Hash ^= ZobristTable[piece][square];
        return piece;
    }

    /// <summary>
    /// Returns a list of all the squares that have the given piece on them.
    /// </summary>
    public List<int> GetSquares(int piece) => _pieceList[piece];

    public List<int>[] GetSquares() => _pieceList;

    /// <summary>
    /// Adds to moves all semilegal moves in the current position.
    /// </summary>
    public void GetMoves(List<Move> moves, bool capturesOnly = false)
    {
        for (int i = 0; i < _pieceList[Piece.Queen | SideToMove].Count; i++)
            AddSlidingMoves(moves, _pieceList[Piece.Queen | SideToMove][i], 0b11111111, 7, capturesOnly);
        for (int i = 0; i < _pieceList[Piece.Rook | SideToMove].Count; i++)
            AddSlidingMoves(moves, _pieceList[Piece.Rook | SideToMove][i], 0b01010101, 7, capturesOnly);
        for (int i = 0; i < _pieceList[Piece.Bishop | SideToMove].Count; i++)
            AddSlidingMoves(moves, _pieceList[Piece.Bishop | SideToMove][i], 0b10101010, 7, capturesOnly);
        for (int i = 0; i < _pieceList[Piece.Knight | SideToMove].Count; i++)
            AddKnightMoves(moves, _pieceList[Piece.Knight | SideToMove][i], capturesOnly);
        for (int i = 0; i < _pieceList[Piece.Pawn | SideToMove].Count; i++)
            AddPawnMoves(moves, _pieceList[Piece.Pawn | SideToMove][i], capturesOnly);
        AddSlidingMoves(moves, _pieceList[Piece.King | SideToMove][0], 0b11111111, 1, capturesOnly);
        if (capturesOnly || InCheck(SideToMove))
            return;
        AddCastlingMoves(moves);

    }

    /// <summary>
    /// Adds to moves all semilegal moves in the current position which start on fromSquare.
    /// </summary>
    public void GetMoves(List<Move> moves, int fromSquare, bool capturesOnly = false)
    {
        var piece = _board[fromSquare];

        if (Piece.GetSide(piece) != SideToMove) return;

        switch (Piece.GetType(piece)) {
            case Piece.Pawn:
                AddPawnMoves(moves, fromSquare, capturesOnly);
                break;
            case Piece.Rook:
                AddSlidingMoves(moves, fromSquare, 0b01010101, 7, capturesOnly);
                break;
            case Piece.Knight:
                AddKnightMoves(moves, fromSquare, capturesOnly);
                break;
            case Piece.Bishop:
                AddSlidingMoves(moves, fromSquare, 0b10101010, 7, capturesOnly);
                break;
            case Piece.Queen:
                AddSlidingMoves(moves, fromSquare, 0b11111111, 7, capturesOnly);
                break;
            case Piece.King:
                AddSlidingMoves(moves, fromSquare, 0b11111111, 1, capturesOnly);
                if (!capturesOnly && !InCheck(SideToMove) && (fromSquare == 60 || fromSquare == 4))
                    AddCastlingMoves(moves);
                break;
        }
    }

    public void AddPawnMoves(List<Move> moves, int square, bool capturesOnly = false)
    {
        var nRank = square / 8; //numerical rank (0-7, top to bottom)
        var nFile = square % 8; //numerical file (0-7, left to right)

        var direction = SideToMove / 4 - 3;

        //capturing
        if (nFile > 0)
        {
            var toSquare = square - 1 + 8 * direction;
            var toCapture = PieceOn(toSquare);
            if (Piece.GetType(toCapture) != 0
            && Piece.GetSide(toCapture) != SideToMove
            || toSquare == EnPassantSquare)
            {
                CheckPromotions(moves, new Move(square, toSquare, this));
            }
        }
        if (nFile < 7)
        {
            var toSquare = square + 1 + 8 * direction;
            var toCapture = PieceOn(toSquare);
            if (Piece.GetType(toCapture) != 0
            && Piece.GetSide(toCapture) != SideToMove
            || toSquare == EnPassantSquare)
            {
                CheckPromotions(moves, new Move(square, toSquare, this));
            }
        }

        if (capturesOnly)
            return;

        //quiet moves
        if (PieceOn(square + 8 * direction) == 0)
        {
            CheckPromotions(moves, new Move(square, square + 8 * direction, this));

            //double step movement
            if ((SideToMove == Piece.White && nRank > 5 || SideToMove == Piece.Black && nRank < 2)
            && PieceOn(square + 16 * direction) == 0)
            {
                moves.Add(new Move(square, square + 16 * direction, this));
            }
        }
    }

    public void CheckPromotions(List<Move> moves, Move original)
    {
        if (original.toSquare < 8 || original.toSquare > 55)
        {
            moves.Add(new Move(original.fromSquare, original.toSquare, this)
            {
                promotesTo = Piece.Queen,
            });
            moves.Add(new Move(original.fromSquare, original.toSquare, this)
            {
                promotesTo = Piece.Rook,
            });
            moves.Add(new Move(original.fromSquare, original.toSquare, this)
            {
                promotesTo = Piece.Knight,
            });
            moves.Add(new Move(original.fromSquare, original.toSquare, this)
            {
                promotesTo = Piece.Bishop,
            });
        }
        else
        {
            moves.Add(original);
        }
    }

    public void AddKnightMoves(List<Move> moves, int square, bool capturesOnly = false)
    {
        var destinations = GetKnightHops(square);

        for (int i = 0; i < destinations.Count; i++)
        {
            var toCapture = PieceOn(destinations[i]);
            if (Piece.GetSide(toCapture) != SideToMove && (!capturesOnly || toCapture > 0))
                moves.Add(new Move(square, destinations[i], this));
        }
    }

    public static List<int> GetKnightHops(int square)
    {
        var destinations = new List<int>();
        var rank = square / 8;
        var file = square % 8;

        if (rank > 1)
        {
            if (file > 0)
                destinations.Add(square - 17); //-1,-2
            if (file < 7)
                destinations.Add(square - 15); //+1,-2
        }
        if (rank > 0)
        {
            if (file > 1)
                destinations.Add(square - 10); //-2,-1
            if (file < 6)
                destinations.Add(square - 6); //+2,-1
        }

        if (rank < 6)
        {
            if (file > 0)
                destinations.Add(square + 15); //-1,+2
            if (file < 7)
                destinations.Add(square + 17); //+1,+2
        }
        if (rank < 7)
        {
            if (file > 1)
                destinations.Add(square + 6); //-2,+1
            if (file < 6)
                destinations.Add(square + 10); //+2,+1
        }
        return destinations;
    }

    public void AddSlidingMoves(List<Move> moves, int square, int directions, int maxDist = 7, bool capturesOnly = false)
    {
        for (int direction = 0; direction < Directions.Length; direction++)
        {
            if ((directions & (1 << direction)) == 0)
                continue;

            for (int distance = 1; distance <= Math.Min(maxDist, DistanceToEdge[square][direction]); distance++)
            {
                var destination = square + Directions[direction] * distance;
                var toCapture = PieceOn(destination);

                if (Piece.GetSide(toCapture) == SideToMove) //friendly piece
                    break;

                if (!capturesOnly || toCapture > 0)
                    moves.Add(new Move(square, destination, this));

                if (toCapture != 0) //enemy piece
                    break;
            }
        }
    }

    public void AddCastlingMoves(List<Move> moves)
    {
        if (SideToMove == Piece.White)
        {
            if (WhiteCanCastleKingSide
            && PieceOn(61) == 0 && PieceOn(62) == 0
            && !IsAttacked(61, Piece.Black) && !IsAttacked(62, Piece.Black))
            {
                moves.Add(new Move(60, 62, this) { isKingSideCastle = true });
            }

            if (WhiteCanCastleQueenSide
            && PieceOn(57) == 0 && PieceOn(58) == 0 && PieceOn(59) == 0
            && !IsAttacked(58, Piece.Black) && !IsAttacked(59, Piece.Black))
            {
                moves.Add(new Move(60, 58, this) { isQueenSideCastle = true });
            }
        }
        else if (SideToMove == Piece.Black)
        {
            if (BlackCanCastleKingSide
            && PieceOn(5) == 0 && PieceOn(6) == 0
            && !IsAttacked(5, Piece.White) && !IsAttacked(6, Piece.White))
            {
                moves.Add(new Move(4, 6, this) { isKingSideCastle = true });
            }

            if (BlackCanCastleQueenSide
            && PieceOn(1) == 0 && PieceOn(2) == 0 && PieceOn(3) == 0
            && !IsAttacked(2, Piece.White) && !IsAttacked(3, Piece.White))
            {
                moves.Add(new Move(4, 2, this) { isQueenSideCastle = true });
            }
        }
    }

    public void MakeMove(Move move)
    {
        if (move.isNullMove)
            return;

        //add this position to the history
        if (!History.Contains(Hash))
            History.Add(Hash);

        RemovePiece(move.toSquare);
        PlacePiece(RemovePiece(move.fromSquare), move.toSquare);

        //handle castling
        if (move.IsCastle)
        {
            int castlingRook;
            if (SideToMove == Piece.Black)
            {
                if (move.isQueenSideCastle)
                    castlingRook = 0;
                else
                    castlingRook = 7;
            }
            else
            {
                if (move.isQueenSideCastle)
                    castlingRook = 56;
                else
                    castlingRook = 63;
            }
            PlacePiece(RemovePiece(castlingRook), (move.fromSquare + move.toSquare) / 2);
        }

        //moving the king removes castling rights
        Hash ^= ZobristCastlingRights[CastlingRights];
        if (Piece.GetType(move.piece) == Piece.King)
        {
            CastlingRights &= SideToMove * 9 / 8 - 6;
            //is equivalent to
            //if (SideToMove == Piece.White)
            //    CastlingRights &= 0b0011;
            //else
            //    CastlingRights &= 0b1100;
        }
        //moving rooks also gives up castling rights
        var changedSquare = -1;
        if (Piece.GetType(move.piece) == Piece.Rook)
            changedSquare = move.fromSquare;
        if (Piece.GetType(move.captures) == Piece.Rook)
            changedSquare = move.toSquare;
        CastlingRights &= changedSquare switch
        {
            0 => 0b1110,
            7 => 0b1101,
            56 => 0b1011,
            63 => 0b0111,
            _ => 0b1111,
        };
        Hash ^= ZobristCastlingRights[CastlingRights];

        if (EnPassantSquare > -1)
            Hash ^= ZobristEnPassantFile[EnPassantSquare % 8];

        var oldEnPassantSquare = EnPassantSquare;
        EnPassantSquare = -1;
        if (Piece.GetType(move.piece) == Piece.Pawn)
        {
            //capturing en passant
            if (move.toSquare == oldEnPassantSquare)
                RemovePiece(oldEnPassantSquare - 2 * SideToMove + 24);

            //double stepping a pawn creates en passant square
            var fromRank = Rank(move.fromSquare);
            var toRank = Rank(move.toSquare);
            if (Math.Abs(fromRank - toRank) == 2)
            {
                EnPassantSquare = (move.fromSquare + move.toSquare) / 2;
                Hash ^= ZobristEnPassantFile[EnPassantSquare % 8];
            }

            //handle promotions
            if (move.promotesTo > 0)
            {
                RemovePiece(move.toSquare);
                PlacePiece(move.promotesTo | Piece.GetSide(move.piece), move.toSquare);
            }
        }

        //end of turn cleanup
        if (Piece.GetType(move.piece) == Piece.Pawn || move.IsCapture)
            HalfMoveClock = 0;
        else
            HalfMoveClock++;

        if (SideToMove == Piece.Black)
            MoveCounter++;

        SideToMove ^= 0b11000;
        Hash ^= ZobristBlackToPlay;
    }

    public void UnmakeMove(Move move)
    {
        if (move.isNullMove)
            return;
        
        PlacePiece(RemovePiece(move.toSquare), move.fromSquare);
        if (!move.isEnPassant)
            PlacePiece(move.captures, move.toSquare);
        else
            PlacePiece(move.captures, move.toSquare + SideToMove * 2 - 24);

        //handle castling
        if (move.IsCastle)
        {
            int castlingRook;
            if (SideToMove == Piece.White)
            {
                if (move.isQueenSideCastle)
                    castlingRook = 0;
                else
                    castlingRook = 7;
            }
            else
            {
                if (move.isQueenSideCastle)
                    castlingRook = 56;
                else
                    castlingRook = 63;
            }
            PlacePiece(RemovePiece((move.fromSquare + move.toSquare) / 2), castlingRook);
        }

        //restore lossy data
        Hash ^= ZobristCastlingRights[CastlingRights];
        CastlingRights = move.prevCastlingRights;
        Hash ^= ZobristCastlingRights[CastlingRights];

        if (EnPassantSquare > -1)
            Hash ^= ZobristEnPassantFile[EnPassantSquare % 8];
        EnPassantSquare = move.prevEnPassantSquare;
        if (EnPassantSquare > -1)
            Hash ^= ZobristEnPassantFile[EnPassantSquare % 8];

        HalfMoveClock = move.prevHalfMoveClock;

        //undo promotion
        if (move.IsPromotion)
        {
            RemovePiece(move.fromSquare);
            PlacePiece(move.piece, move.fromSquare);
        }

        //end of turn cleanup
        if (SideToMove == Piece.White)
            MoveCounter--;

        SideToMove ^= 0b11000;
        Hash ^= ZobristBlackToPlay;

        //remove this position from the history
        History.Remove(Hash);
    }

    public bool IsAttacked(int square, int attackingSide)
    {
        //knights
        var knightHops = GetKnightHops(square);
        for (int i = 0; i < knightHops.Count; i++)
        {
            var piece = PieceOn(knightHops[i]);
            if (Piece.GetSide(piece) == attackingSide && Piece.GetType(piece) == Piece.Knight)
                return true;
        }

        //pawns
        var file = square % 8;
        if (file > 0)
        {
            var fromSquare = square + attackingSide * -2 + 23;
            if (fromSquare >= 0 && fromSquare < 64)
            {
                var piece = PieceOn(fromSquare);
                if (Piece.GetType(piece) == Piece.Pawn
                && Piece.GetSide(piece) == attackingSide)
                    return true;
            }
        }
        if (file < 7)
        {
            var fromSquare = square + attackingSide * -2 + 25;
            if (fromSquare >= 0 && fromSquare < 64)
            {
                var piece = PieceOn(fromSquare);
                if (Piece.GetType(piece) == Piece.Pawn
                && Piece.GetSide(piece) == attackingSide)
                    return true;
            }
        }

        //sliding pieces
        for (int direction = 0; direction < Directions.Length; direction++)
        {
            for (int distance = 1; distance <= DistanceToEdge[square][direction]; distance++)
            {
                var destination = square + Directions[direction] * distance;
                var piece = PieceOn(destination);

                if (Piece.GetSide(piece) == (attackingSide ^ 0b11000)) //friendly piece
                    break;

                var pieceType = Piece.GetType(piece);
                if (pieceType == Piece.Queen
                || (pieceType == Piece.Rook && direction % 2 == 0)
                || (pieceType == Piece.Bishop && direction % 2 == 1)
                || (pieceType == Piece.King && distance == 1))
                    return true;

                if (piece > 0)
                    break;
            }
        }

        return false;
    }

    public void RemoveIllegalMoves(List<Move> moves)
    {
        for (int i = moves.Count - 1; i >= 0; i--)
        {
            var move = moves[i];
            MakeMove(move);
            if (InCheck(SideToMove ^ 0b11000))
                moves.RemoveAt(i);
            UnmakeMove(move);
        }
    }

    public bool InCheck(int side)
    {
        return IsAttacked(_pieceList[Piece.King | side][0], side ^ 0b11000);
    }

    public static int ManhattanDistance(int square1, int square2)
    {
        return Math.Abs(square1 % 8 - square2 % 8) + Math.Abs(square1 / 8 - square2 / 8);
    }

    public static int InvertRank(int square)
    {
        var rank = square / 8;
        var file = square % 8;
        var newRank = 7 - rank;
        return newRank * 8 + file;
    }

    public static int InvertFile(int square)
    {
        var rank = square / 8;
        var file = square % 8;
        var newFile = 7 - file;
        return rank * 8 + newFile;
    }

    public string ToFEN()
    {
        var fen = "";

        //piece placement
        for (int i = 0; i < 8; i++)
        {
            var blanks = 0;
            for (int j = 0; j < 8; j++)
            {
                var square = i * 8 + j;
                var piece = PieceOn(square);
                if (piece > 0)
                {
                    if (blanks > 0)
                        fen += blanks.ToString();
                    fen += Piece.Piece2Char[piece];
                    blanks = 0;
                }
                else
                    blanks++;
            }
            if (i < 7)
            {
                if (blanks > 0)
                    fen += blanks;
                fen += "/";
            }
        }

        //side to move
        fen += (SideToMove == Piece.White ? " w " : " b ");

        //castling rights
        if (WhiteCanCastleKingSide)
            fen += "K";
        if (WhiteCanCastleQueenSide)
            fen += "Q";
        if (BlackCanCastleKingSide)
            fen += "k";
        if (BlackCanCastleQueenSide)
            fen += "q";

        //en passant square
        if (EnPassantSquare == -1)
            fen += " -";
        else
            fen += " " + RankAndFile(EnPassantSquare);

        //halfmove clock
        fen += " " + HalfMoveClock;

        //fullmove counter
        fen += " " + MoveCounter;

        return fen;
    }

    public static Board FromFEN(string fen)
    {
        var board = new Board();
        var fenParts = fen.Split(' ', StringSplitOptions.TrimEntries);

        //piece placement
        var square = 0;
        for (int i = 0; i < fenParts[0].Length; i++)
        {
            if (fenParts[0][i] == '\\' || fenParts[0][i] == '/') continue;
            
            if (char.IsNumber(fenParts[0][i]))
            {
                square += int.Parse(fenParts[0][i].ToString());
                continue;
            }

            var piece = Piece.Char2Piece(fenParts[0][i]);
            board._board[square] = piece;
            board._pieceList[piece].Add(square);
            board.PieceCount++;
            //if (Piece.GetSide(piece) == Piece.White)
            //    board.WhiteCount++;
            //else if (Piece.GetSide(piece) == Piece.Black)
            //    board.BlackCount++;
            board.Hash ^= ZobristTable[piece][square];
            square++;
        }

        //side to move
        board.SideToMove = fenParts[1].ToLower() switch
        {
            "w" => Piece.White,
            "b" => Piece.Black,
            _ => 0,
        };

        //castling rights
        board.CastlingRights = 0b0000;
        for (int i = 0; i < fenParts[2].Length; i++)
        {
            board.CastlingRights |= (fenParts[2][i]) switch
            {
                'K' => 0b1000,
                'Q' => 0b0100,
                'k' => 0b0010,
                'q' => 0b0001,
                _ => 0,
            };
        }

        //en passant targeting square
        board.EnPassantSquare = Square(fenParts[3]);

        //halfmove clock
        board.HalfMoveClock = int.TryParse(fenParts[4], out int hmc) ? hmc : 0;

        //fullmove counter
        board.MoveCounter = int.TryParse(fenParts[5], out int mc) ? mc : 1;

        return board;
    }

    public override string ToString()
    {
        var result = "";
        for (int i = 0; i < _board.Length; i++)
        {
            //result += ' ';
            result += Piece.Piece2Char[_board[i]];
            //result += Piece.Piece2Figurine[_board[i]];
            if (i % 8 == 7)
            {
                result += '\n';
            }
        }

        result += "Castling Rights: " + CastlingRights + "\n";
        result += "En Passant Square: " + EnPassantSquare + "\n";
        result += "Move " + MoveCounter + ", (" + HalfMoveClock + ")\n";
        return result;
    }
}
