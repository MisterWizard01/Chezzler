using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Chezzler;
using System.Diagnostics;
using System.Collections.Generic;

namespace ChessGUI;

/* TO DO:
 * flip board?
 */

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Random _random;

    private Texture2D _pixel, _circle, _pieces;
    private SpriteFont _arial;
    private Rectangle[] _pieceSources;
    private Point boardPosition;
    private Point boardDimensions;
    private Point squareSize;

    private Board board;
    private List<Move> highlightedMoves;
    private List<Move> moveHistory;
    private int historyIndex;
    private int historyScroll;

    private MouseState mouseState;
    private MouseState prevMouseState;
    private KeyboardState keyState;
    private KeyboardState prevKeyState;
    private int selectedPiece;
    private int selectedSquare;
    private bool isHeld;
    private PromotionMenu promoMenu;
    private Move playerMove;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _random = new Random(1234567890);
    }

    protected override void Initialize()
    {
        _pixel = new(GraphicsDevice, 1, 1);
        _pixel.SetData(new Color[] { Color.White });

        var canvasSize = 100;
        var radius = 25;
        _circle = new(GraphicsDevice, canvasSize, canvasSize);
        var colorData = new Color[canvasSize * canvasSize];
        for (int x = 0; x < canvasSize; x++)
        {
            for (int y = 0; y < canvasSize; y++)
            {
                var xOff = x - canvasSize / 2;
                var yOff = y - canvasSize / 2;
                colorData[x + y * canvasSize] = xOff * xOff + yOff * yOff < radius * radius ? Color.White : Color.Transparent;
            }
        }
        _circle.SetData(colorData);

        _pieceSources = new Rectangle[Piece.Black | Piece.King + 1];
        _pieceSources[0] = new Rectangle();
        _pieceSources[Piece.White | Piece.King] = new Rectangle(151, 68, 180, 180);
        _pieceSources[Piece.White | Piece.Queen] = new Rectangle(337, 68, 180, 180);
        _pieceSources[Piece.White | Piece.Rook] = new Rectangle(524, 68, 180, 180);
        _pieceSources[Piece.White | Piece.Bishop] = new Rectangle(711, 68, 180, 180);
        _pieceSources[Piece.White | Piece.Knight] = new Rectangle(897, 68, 180, 180);
        _pieceSources[Piece.White | Piece.Pawn] = new Rectangle(1084, 68, 180, 180);
        _pieceSources[Piece.Black | Piece.King] = new Rectangle(151, 294, 180, 180);
        _pieceSources[Piece.Black | Piece.Queen] = new Rectangle(337, 294, 180, 180);
        _pieceSources[Piece.Black | Piece.Rook] = new Rectangle(524, 294, 180, 180);
        _pieceSources[Piece.Black | Piece.Bishop] = new Rectangle(711, 294, 180, 180);
        _pieceSources[Piece.Black | Piece.Knight] = new Rectangle(897, 294, 180, 180);
        _pieceSources[Piece.Black | Piece.Pawn] = new Rectangle(1084, 294, 180, 180);

        var width = Window.ClientBounds.Width;
        var height = Window.ClientBounds.Height;
        boardDimensions = new Point(Math.Min(width, height) * 8 / 10);
        boardPosition = (Window.ClientBounds.Size - boardDimensions) / new Point(2, 2);
        squareSize = boardDimensions / new Point(8, 8);

        Board.ComputeDistanceToEdge();
        Board.SetupZobrist(_random);
        board = Board.FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"); //starting position
        //board = Board.FromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
        //board = Board.FromFEN("rn2kbn1/ppp1p1pr/8/7p/qP1N4/2P4P/3P1PP1/2B1K2R w q - 8 20");

        //endgame tests
        //board = Board.FromFEN("3qk2r/8/8/8/8/8/8/4K3 w  - 0 1");
        //board = Board.FromFEN("r3k2r/8/8/8/8/8/8/4K3 w  - 0 1");
        //board = Board.FromFEN("3qk3/8/8/8/8/8/8/4K3 w  - 0 1");
        //board = Board.FromFEN("4k2r/8/8/8/8/8/8/4K3 w  - 0 1");
        //board = Board.FromFEN("2b1kb2/8/8/8/8/8/8/4K3 w  - 0 1");
        //board = Board.FromFEN("1n2kb2/8/8/8/8/8/8/4K3 w  - 0 1");

        highlightedMoves = new List<Move>();
        moveHistory = new List<Move>();

        var menuDimensions = squareSize * new Point(4, 1);
        var menuPosition = boardPosition - new Point(menuDimensions.X + 10, 0);
        promoMenu = new PromotionMenu(menuPosition, menuDimensions);

        base.Initialize();

        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pieces = Content.Load<Texture2D>("chess pieces");
        _arial = Content.Load<SpriteFont>("arial");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        mouseState = Mouse.GetState();
        var mouseSquareCoords = (mouseState.Position - boardPosition) / squareSize;
        var mouseSquare = mouseSquareCoords.X + mouseSquareCoords.Y * 8;
        keyState = Keyboard.GetState();

        if (promoMenu.Visible)
        {
            promoMenu.Update(mouseState);
            if (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed)
            {
                promoMenu.Visible = false;
                selectedPiece = -1;
                if (promoMenu.MouseIndex > -1)
                {
                    playerMove.promotesTo = promoMenu.MouseIndex + 2;
                    DoMove(playerMove);
                }
            }
        }
        else
        {
            //AI plays
            if (keyState.IsKeyDown(Keys.Space) && prevKeyState.IsKeyUp(Keys.Space))
            {
                var startTime = DateTime.Now;
                //var move = Chezzler.Chezzler.GetBestMove(new Board(board));
                var move = Chezzler.Chezzler.GetBestMove(board);
                Debug.WriteLine("Search complete in " + Math.Floor((DateTime.Now - startTime).TotalMilliseconds) + " ms.");
                DoMove(move);
            }

            //human plays
            if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
                DoClick(mouseSquare, mouseSquareCoords);
            if (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed)
                DoRelease(mouseSquare);
        }

        //navigation
        if (keyState.IsKeyDown(Keys.Left) && prevKeyState.IsKeyUp(Keys.Left)
        && historyIndex > 0)
        {
            board.UnmakeMove(moveHistory[historyIndex - 1]);
            historyIndex--;
            if (historyIndex < historyScroll * 2 + 1)
                historyScroll = Math.Max(0, historyScroll - 1);
            highlightedMoves.Clear();
        }
        if (keyState.IsKeyDown(Keys.Right) && prevKeyState.IsKeyUp(Keys.Right)
        && historyIndex < moveHistory.Count)
        {
            board.MakeMove(moveHistory[historyIndex]);
            historyIndex++;
            if (historyIndex > historyScroll * 2 + 40)
                historyScroll++;
            highlightedMoves.Clear();
        }

        prevMouseState = mouseState;
        prevKeyState = keyState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var square = x + y * 8;
                var destination = new Rectangle(
                    boardPosition.X + x * squareSize.X,
                    boardPosition.Y + y * squareSize.Y,
                    squareSize.X, squareSize.Y);
                _spriteBatch.Draw(_pixel, destination, (x + y) % 2 == 0 ? Color.BurlyWood : Color.Sienna);

                //previous move
                if (historyIndex > 0)
                {
                    var lastMove = moveHistory[historyIndex - 1];
                    if (lastMove.fromSquare == square || lastMove.toSquare == square)
                        _spriteBatch.Draw(_pixel, destination, Color.Green * 0.5f);
                }

                //pieces
                if (square != selectedSquare || isHeld == false)
                    _spriteBatch.Draw(_pieces, destination, _pieceSources[board.PieceOn(square)], Color.White);

                //legal moves
                for (int i = 0; i < highlightedMoves.Count; i++)
                {
                    if (highlightedMoves[i].toSquare == square && highlightedMoves[i].piece == selectedPiece)
                    {
                        _spriteBatch.Draw(_circle, destination, Color.Green);
                        break;
                    }
                }
            }
        }

        //move history
        var historyX = boardPosition.X + boardDimensions.X + 10;
        var textMargin = 2;
        for (int i = 0; i < Math.Min(20, moveHistory.Count / 2 + 1); i++)
        {
            var moveNumber = historyScroll + i;
            var y = boardPosition.Y + i * 20;

            //move number
            _spriteBatch.DrawString(_arial, (moveNumber + 1) + ".",
            new Vector2(historyX, y), Color.Black);

            //white's move
            var firstIndexToDraw = moveNumber * 2;
            if (firstIndexToDraw < moveHistory.Count)
            {
                var firstStringToDraw = moveHistory[firstIndexToDraw].ToString();
                if (firstIndexToDraw == historyIndex - 1)
                    _spriteBatch.Draw(_pixel, new Rectangle(
                    new Point(historyX + 30 - textMargin, y - textMargin),
                    _arial.MeasureString(firstStringToDraw).ToPoint() + new Point(2 * textMargin)), Color.Green * 0.5f);
                _spriteBatch.DrawString(_arial, firstStringToDraw, new Vector2(historyX + 30, y), Color.Black);
            }

            //black's move
            var secondIndexToDraw = moveNumber * 2 + 1;
            if (secondIndexToDraw < moveHistory.Count)
            {
                var secondStringToDraw = moveHistory[secondIndexToDraw].ToString();
                if (secondIndexToDraw == historyIndex - 1)
                    _spriteBatch.Draw(_pixel, new Rectangle(
                    new Point(historyX + 100 - textMargin, y - textMargin),
                    _arial.MeasureString(secondStringToDraw).ToPoint() + new Point(2 * textMargin)), Color.Green * 0.5f);
                _spriteBatch.DrawString(_arial, secondStringToDraw, new Vector2(historyX + 100, y), Color.Black);
            }
        }

        //rank and file indicators
        for (int i = 0; i < 8; i++)
        {
            _spriteBatch.DrawString(_arial, (i + 1).ToString(), boardPosition.ToVector2() + new Vector2(-15,
                boardDimensions.Y - squareSize.Y * (i + 0.5f)), Color.Black);
            _spriteBatch.DrawString(_arial, ((char)('a' + i)).ToString(), boardPosition.ToVector2() + new Vector2(
                squareSize.X * (i + 0.5f), boardDimensions.Y + 10), Color.Black);
        }

        //promotion menu
        promoMenu.Draw(_spriteBatch, _pixel, _pieces, _pieceSources, Piece.Rook | board.SideToMove);

        //held piece
        if (isHeld)
        {
            _spriteBatch.Draw(_pieces, new Rectangle(mouseState.Position - squareSize / new Point(2), squareSize),
                _pieceSources[selectedPiece], Color.White);
        }

        //info
        var spacer = 18;
        _spriteBatch.DrawString(_arial, "Move " + board.MoveCounter, new Vector2(5, boardPosition.Y + spacer), Color.Black);
        _spriteBatch.DrawString(_arial, (board.SideToMove == Piece.White ? "White" : "Black") + " to move.", new Vector2(5, boardPosition.Y + spacer * 2), Color.Black);
        _spriteBatch.DrawString(_arial, "Castling Rights: " + board.CastlingRights, new Vector2(5, boardPosition.Y + spacer * 3), Color.Black);
        _spriteBatch.DrawString(_arial, "En Passant Square: " + board.EnPassantSquare, new Vector2(5, boardPosition.Y + spacer * 4), Color.Black);
        _spriteBatch.DrawString(_arial, "Halfmove Clock: " + board.HalfMoveClock, new Vector2(5, boardPosition.Y + spacer * 5), Color.Black);
        _spriteBatch.DrawString(_arial, "History Length: " + board.History.Count, new Vector2(5, boardPosition.Y + spacer * 6), Color.Black);
        _spriteBatch.DrawString(_arial, board.Hash.ToString(), new Vector2(5, boardPosition.Y + spacer * 7), Color.Black);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    public bool MouseOnBoard()
    {
        return mouseState.X >= boardPosition.X
            && mouseState.Y >= boardPosition.Y
            && mouseState.X < boardPosition.X + boardDimensions.X
            && mouseState.Y < boardPosition.Y + boardDimensions.Y;
    }

    public void DoClick(int mouseSquare, Point mouseSquareCoords)
    {
        if (!MouseOnBoard())
        {
            highlightedMoves.Clear();
            selectedPiece = 0;
            selectedSquare = -1;
            return;
        }

        if (selectedSquare > -1)
        {
            playerMove = new Move(selectedSquare, mouseSquare, board);
            if (selectedPiece > 0)
            {
                if (highlightedMoves.Contains(playerMove))
                {
                    DoMove(playerMove);
                    return;
                }
                var promoMove = playerMove;
                promoMove.promotesTo = Piece.Queen;
                if (highlightedMoves.Contains(playerMove))
                {
                    OpenPromoMenu();
                    return;
                }
            }
        }

        selectedPiece = board.PieceOn(mouseSquare);
        //heldOffest = boardPosition + mouseSquareCoords * squareSize - mouseState.Position;
        selectedSquare = mouseSquare;
        highlightedMoves.Clear();
        board.GetMoves(highlightedMoves, selectedSquare);
        board.RemoveIllegalMoves(highlightedMoves);
        isHeld = true;
    }

    public void DoRelease(int mouseSquare)
    {
        isHeld = false;
        if (!MouseOnBoard())
        {
            return;
        }

        if (selectedSquare > -1)
        {
            playerMove = new Move(selectedSquare, mouseSquare, board);
            if (selectedPiece > 0)
            {
                if (highlightedMoves.Contains(playerMove))
                {
                    DoMove(playerMove);
                    return;
                }
                var promoMove = playerMove;
                promoMove.promotesTo = Piece.Queen;
                if (highlightedMoves.Contains(promoMove))
                {
                    OpenPromoMenu();
                    return;
                }
            }
        }
        promoMenu.Visible = false;
    }

    public void DoMove(Move move)
    {
        if (move.isNullMove)
            return;

        board.MakeMove(move);
        if (historyIndex < moveHistory.Count)
            moveHistory.RemoveRange(historyIndex, moveHistory.Count - historyIndex);
        moveHistory.Add(move);
        historyIndex++;
        if (historyIndex > historyScroll * 2 + 40)
            historyScroll++;
        highlightedMoves.Clear();
        selectedPiece = 0;
        selectedSquare = -1;
    }

    public void OpenPromoMenu()
    {
        promoMenu.Visible = true;
    }
}