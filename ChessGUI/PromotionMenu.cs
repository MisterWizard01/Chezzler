using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGUI;

public class PromotionMenu
{
    public Point Position { get; set; }
    public Point Dimensions { get; set; }
    public bool Visible { get; set; }
    public int MouseIndex { get; set; }

    public PromotionMenu(Point position, Point dimensions)
    {
        Position = position;
        Dimensions = dimensions;
        MouseIndex = -1;
    }

    public void Update(MouseState mouseState)
    {
        MouseIndex = -1;
        if (mouseState.X < Position.X
         || mouseState.Y < Position.Y
         || mouseState.X > Position.X + Dimensions.X
         || mouseState.Y > Position.Y + Dimensions.Y)
            return;

        MouseIndex = 4 * (mouseState.X - Position.X) / Dimensions.X;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Texture2D pieces, Rectangle[] pieceSources, int startIndex)
    {
        if (!Visible)
            return;

        var outline = new Rectangle(Position - new Point(2), Dimensions + new Point(4));
        spriteBatch.Draw(pixel, outline, Color.Sienna);
        spriteBatch.Draw(pixel, new Rectangle(Position, Dimensions), Color.BurlyWood);

        for (int i = 0; i < 4; i++)
        {
            var square = Dimensions / new Point(4, 1);
            spriteBatch.Draw(pieces, new Rectangle(Position + square * new Point(i, 0), square),
                pieceSources[startIndex + i], Color.White);
        }
    }
}
