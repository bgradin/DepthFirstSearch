using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using CommonCode.Networking;

namespace GameClient
{
	class MinorPlayerRenderer : GraphicsEngineComponent
	{
		public MinorPlayerRenderer(GraphicEngine engine)
			: base(engine)
		{
		}

		public override void Update(GameTime gameTime)
		{
			for (int i = 1; i < World.Players.Count; i++)
				World.Players[i].UpdateMovement();

			base.Update(gameTime);
		}

		public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
		{
			int screenWidth = GraphicsDevice.Viewport.Width;
			int screenHeight = GraphicsDevice.Viewport.Height;
			Rectangle bounds = Engine.AnglerGame.MainPlayer.Bounds;
			int x = bounds.X < 0 ? 0 : bounds.X;
			int y = bounds.Y < 0 ? 0 : bounds.Y;
			Rectangle clipRectangle = new Rectangle(
				x,
				y,
				bounds.X + bounds.Width > screenWidth ? screenWidth - x : (bounds.X < 0 ? bounds.Width + bounds.X : bounds.Width),
				bounds.Y + bounds.Height > screenHeight ? screenHeight - y : (bounds.Y < 0 ? bounds.Height + bounds.Y : bounds.Height));
			GraphicsDevice.ScissorRectangle = clipRectangle;

			// Draw all of the players except the mainplayer
			for (int i = 1; i < World.Players.Count; i++)
			{
				if (World.Players[i].UserName == World.MainPlayer.UserName)
				{
					World.Players.RemoveAt(i--);
					continue;
				}

				Keys directionKey = Keys.E; // Start with some random key (this will change)
				if (World.Players[i].CurrentGraphicIndex == World.Players[i].BackGraphicIndex)
					directionKey = Keys.Up;
				if (World.Players[i].CurrentGraphicIndex == World.Players[i].FrontGraphicIndex)
					directionKey = Keys.Down;
				if (World.Players[i].CurrentGraphicIndex == World.Players[i].RightGraphicIndex)
					directionKey = Keys.Right;
				if (World.Players[i].CurrentGraphicIndex == World.Players[i].LeftGraphicIndex)
					directionKey = Keys.Left;

				// Handle recently pressed keys
				if ((!World.Players[i].MovementBlocked && World.Players[i].NextMoves.Count > 0 && World.Players[i].NextMoves.Peek() == directionKey)
					|| (World.Players[i].OffsetX == 0 && World.Players[i].OffsetY== 0))
				{
					Keys? key;
					int distance = World.Players[i].GetMove(World.CurrentMap, out key);

					if (distance != 0 && key != null)
					{
						Direction direction = (Direction)Enum.Parse(typeof(Direction), key.ToString());
						World.Players[i].StartMoving(direction, distance);
					}
				}

				if (World.Players[i].X >= World.CurrentMap.VisibleBounds.Left
					&& World.Players[i].X <= World.CurrentMap.VisibleBounds.Right
					&& World.Players[i].Y >= World.CurrentMap.VisibleBounds.Top
					&& World.Players[i].Y <= World.CurrentMap.VisibleBounds.Bottom)
				{
					Rectangle rect = World.CurrentMap.PositionOnVisibleMap(World.Players[i].X, World.Players[i].Y);
					rect = new Rectangle(rect.Left + World.Players[i].OffsetX, rect.Top + World.Players[i].OffsetY, Const.TILE_SIZE, Const.TILE_SIZE);
					Engine.SpriteBatch.Draw(FXCollection.Textures[World.Players[i].CurrentGraphicIndex], rect, Color.White);
				}
			}

			Engine.SpriteBatch.End();
			Engine.SpriteBatch.Begin(Engine.SortMode, Engine.BlendState, Engine.SamplerState, Engine.StencilState, Engine.RasterizerState, Engine.Effect);
			GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, screenWidth, screenHeight);

			base.Draw(gameTime);
		}
	}
}
