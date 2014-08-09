using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace GameClient
{
	class Background : DrawableGameComponent
	{
		private GraphicsDevice m_Device;

		private Texture2D background;

		SpriteBatch sb;

		public Background(Game game, Texture2D texture)
			: base(game)
		{
			// Set the device object to be used for rendering
			m_Device = game.GraphicsDevice;

			background = texture;

			sb = new SpriteBatch(m_Device);
		}

		public override void Draw(GameTime gameTime)
		{
			int screenWidth = m_Device.Viewport.Width;
			int screenHeight = m_Device.Viewport.Height;
			int x = screenWidth / 2 - background.Width / 2;
			int y = screenHeight / 2 - background.Height / 2;
			int width = background.Width;
			int height = background.Height;
			double widthRatio = (double)width / (double)height;
			double heightRatio = 1 / widthRatio;

			// Scale image to fit the window as well as possible, but don't distort it
			if (screenWidth >= width && screenHeight >= height)
			{
				if (screenWidth > width)
				{
					x = 0;
					width = screenWidth;
					height = (int)Math.Floor(screenWidth * heightRatio);
					y = screenHeight / 2 - height / 2;
				}
				
				if (screenHeight > height)
				{
					y = 0;
					height = screenHeight;
					width = (int)Math.Floor(screenHeight * widthRatio);
					x = screenWidth / 2 - width / 2;
				}
			}
			else
			{
				if (screenWidth < width)
				{
					x = 0;
					width = screenWidth;
					height = (int)Math.Floor(screenWidth * heightRatio);
					y = screenHeight / 2 - height / 2;
				}
				
				if (screenHeight < height)
				{
					y = 0;
					height = screenHeight;
					width = (int)Math.Floor(screenHeight * widthRatio);
					x = screenWidth / 2 - width / 2;
				}
			}

			sb.Begin();
			sb.Draw(background, new Rectangle(x, y, width, height), Color.White);
			sb.End();

			base.Draw(gameTime);
		}
	}
}