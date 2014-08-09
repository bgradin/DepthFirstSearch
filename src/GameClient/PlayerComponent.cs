using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CommonCode.Networking;
using System;

namespace GameClient
{
	public class PlayerComponent : AnglerGameDrawableComponent
	{
		public Player Player { get; set; }

		public int ScreenX { get; private set; }
		public int ScreenY { get; private set; }

		public Rectangle Bounds { get; private set; }

		public PlayerComponent(AnglerGame game, Player player)
			: base(game)
		{
			DrawOrder = player == World.MainPlayer ? (int)RenderOrder.OverlayLayer : (int)RenderOrder.NPCLayer;
			Player = player;
		}

		public static PlayerComponent CreateAndAdd(AnglerGame game, Player player)
		{
			PlayerComponent newPlayer = new PlayerComponent(game, player);
			game.Components.Add(newPlayer);
			return newPlayer;
		}

		public override void Update(GameTime gameTime)
		{
			Player.UpdateMovement();

			// Figure out how to draw the screen (it's based off of the main player)
			if (Player == World.MainPlayer)
			{
				// Calculate screen and map dimensions, in pixels
				int screenWidth = GraphicsDevice.Viewport.Width;
				int screenHeight = GraphicsDevice.Viewport.Height;
				int adjustedMapWidth = World.CurrentMap.Width * Const.TILE_SIZE;
				int adjustedMapHeight = World.CurrentMap.Height * Const.TILE_SIZE;

				// Calculate player x coordinate
				ScreenX = screenWidth / 2 - Const.TILE_SIZE / 2;
				if (adjustedMapWidth < screenWidth) // If the map is smaller than the screen
					ScreenX = (screenWidth - adjustedMapWidth) / 2 + Player.ActualX;
				else
				{
					int rightGap = ScreenX - Player.ActualX;
					int leftGap = (screenWidth - ScreenX) - ((World.CurrentMap.Width * Const.TILE_SIZE) - Player.ActualX);
					// Note: since the map is larger than the screen, we don't have to worry about having a gap at right and left at the same time

					if (rightGap > 0)
						ScreenX -= rightGap;

					if (leftGap > 0)
						ScreenX += leftGap;
				}

				// Calculate player y coordinate
				ScreenY = screenHeight / 2 - Const.TILE_SIZE / 2;
				if (adjustedMapHeight < screenHeight) // If the map is smaller than the screen
					ScreenY = (screenHeight - adjustedMapHeight) / 2 + Player.ActualY;
				else
				{
					int topGap = ScreenY - Player.ActualY;
					int bottomGap = (screenHeight - ScreenY) - ((World.CurrentMap.Height * Const.TILE_SIZE) - Player.ActualY);
					// Note: since the map is larger than the screen, we don't have to worry about having a gap at top and bottom at the same time

					if (topGap > 0)
						ScreenY -= topGap;

					if (bottomGap > 0)
						ScreenY += bottomGap;
				}

				Bounds = new Rectangle(ScreenX - Player.Radius + Const.TILE_SIZE / 2, ScreenY - Player.Radius + Const.TILE_SIZE / 2, Player.Radius * 2, Player.Radius * 2);

				// Calculate window of visible tiles on the map
				int minXTile = (int)Math.Floor(((double)(Player.ActualX - ScreenX) / Const.TILE_SIZE));
				if (minXTile < 0)
					minXTile = 0;

				int maxXTile = (int)Math.Floor((double)(screenWidth - ScreenX + (Player.X * Const.TILE_SIZE) + Player.OffsetX) / Const.TILE_SIZE) + 1;
				if (maxXTile > World.CurrentMap.Width)
					maxXTile = World.CurrentMap.Width;

				int minYTile = (int)Math.Floor(((double)(Player.ActualY - ScreenY) / Const.TILE_SIZE));
				if (minYTile < 0)
					minYTile = 0;

				int maxYTile = (int)Math.Floor((double)(screenHeight - ScreenY + (Player.Y * Const.TILE_SIZE) + Player.OffsetY) / Const.TILE_SIZE) + 1;
				if (maxYTile > World.CurrentMap.Height)
					maxYTile = World.CurrentMap.Height;

				World.CurrentMap.VisibleBounds = new Rectangle(minXTile, minYTile, maxXTile - minXTile, maxYTile - minYTile);

				// Calculate upper left corner coordinates
				int upperLeftX = ScreenX - (Player.X - minXTile) * Const.TILE_SIZE - Player.OffsetX;
				int upperLeftY = ScreenY - (Player.Y - minYTile) * Const.TILE_SIZE - Player.OffsetY;
				World.CurrentMap.UpperLeftCorner = new System.Drawing.Point(upperLeftX, upperLeftY);
			}

			base.Update(gameTime);
		}

		protected override void LoadContent()
		{
			spritebatch = new SpriteBatch(AnglerGame.GraphicsDeviceManager.GraphicsDevice);

			base.LoadContent();
		}

		public override void Draw(GameTime gameTime)
		{
			spritebatch.Begin();
			spritebatch.Draw(FXCollection.Textures[Player.CurrentGraphicIndex], new Rectangle(ScreenX, ScreenY, Const.TILE_SIZE, Const.TILE_SIZE), Color.White);
			spritebatch.End();
			base.Draw(gameTime);
		}

		SpriteBatch spritebatch;
	}
}
