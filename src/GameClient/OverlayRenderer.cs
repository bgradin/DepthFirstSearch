using XNAControls;
using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CommonCode.Networking;
using System;
using System.Threading;

namespace GameClient
{
	class OverlayRenderer : GraphicsEngineComponent
	{
		public OverlayRenderer(GraphicEngine engine)
			: base(engine)
		{
		}

		protected override void LoadContent()
		{
			// This is used for the blacked-out regions of the screen
			black = new Texture2D(GraphicsDevice, 1, 1);
			black.SetData(new[] { Color.Black });

			bacteriaFew = Engine.AnglerGame.Content.Load<Texture2D>("bacteriaFew");
			bacteriaMedium = Engine.AnglerGame.Content.Load<Texture2D>("bacteriaMedium");
			bacteriaMany = Engine.AnglerGame.Content.Load<Texture2D>("bacteriaMany");

			lightCircle = Engine.Game.DrawRadialGradient(World.MainPlayer.Radius, System.Drawing.Color.Transparent, System.Drawing.Color.Black);

			font = new System.Drawing.Font("Arial", 12);

			timer = new Timer(RenderBacteriaCount, null, 0, 1000);
			RenderBacteriaCount();

			base.LoadContent();
		}

		public void RenderBacteriaCount(object state = null)
		{
			lock (timerSync)
			{
				if (World.MainPlayer.BacteriaCount > 0)
				{
					World.MainPlayer.BacteriaCount--;
					World.MainPlayer.SendToServer(ServerAction.ClientCollectItem, new ItemCollectData(0, -1));
				}

				bacteriaCount = Engine.Game.DrawText("Bacteria: " + (World.MainPlayer.BacteriaCount).ToString(), font, System.Drawing.Color.Cyan);
			}
		}

		public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
		{
			int screenWidth = GraphicsDevice.Viewport.Width;
			int screenHeight = GraphicsDevice.Viewport.Height;

			// Main player light circle
			Engine.SpriteBatch.Draw(lightCircle, Engine.AnglerGame.MainPlayer.Bounds, Color.White);

			// Draw items
			for (int i = World.CurrentMap.VisibleBounds.Left; i < World.CurrentMap.VisibleBounds.Right; i++)
			{
				for (int j = World.CurrentMap.VisibleBounds.Top; j < World.CurrentMap.VisibleBounds.Bottom; j++)
				{
					Texture2D texture = bacteriaFew;

					ItemTile tile = World.CurrentMap.GetTile(i, j, LAYERS.Item) as ItemTile;

					if (tile == null || tile.Type == ItemTileSpec.NONE)
						continue;

					if (tile.Quantity > Const.MIN_BACTERIA + (Const.MAX_BACTERIA - Const.MIN_BACTERIA) / 3)
						texture = bacteriaMedium;
					if (tile.Quantity > Const.MIN_BACTERIA + 2 * ((Const.MAX_BACTERIA - Const.MIN_BACTERIA) / 3))
						texture = bacteriaMany;

					Engine.SpriteBatch.Draw(texture, World.CurrentMap.PositionOnVisibleMap(tile.X, tile.Y),
						Color.White * (float)(((1.0 - minimumOpactiy) * Math.Sin((double)(gameTime.TotalGameTime.TotalMilliseconds) * tile.Speed * (Math.PI / 180)) + 1) / 2));
				}
			}

			Engine.SpriteBatch.Draw(bacteriaCount, new Vector2(GraphicsDevice.Viewport.Width - bacteriaCount.Width - 10, 10), Color.White);

			base.Draw(gameTime);
		}

		protected override void Dispose(bool disposing)
		{
			if (lightCircle != null)
				lightCircle.Dispose();

			if (black != null)
				black.Dispose();

			lock (timerSync)
			{
				if (timer != null)
				{
					timer.Change(0, Timeout.Infinite);
					timer.Dispose();
					timer = null;
				}
			}

			if (font != null)
				font.Dispose();

			base.Dispose(disposing);
		}

		Timer timer;
		System.Drawing.Font font;
		Texture2D lightCircle, black, bacteriaFew, bacteriaMedium, bacteriaMany, bacteriaCount;
		
		static readonly object timerSync = new object(); //lock any time timer is referenced and in the timer's callback function

		const double minimumOpactiy = 0.2;
	}
}
