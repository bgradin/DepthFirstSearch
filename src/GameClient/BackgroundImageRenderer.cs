using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GameClient
{
	class BackgroundImageRenderer : GraphicsEngineComponent
	{
		public BackgroundImageRenderer(GraphicEngine engine, Texture2D image)
			: base(engine)
		{
			m_image = image;
		}

		public static T CreateAndAdd<T>(GraphicEngine engine, Texture2D map) where T : BackgroundImageRenderer
		{
			T newComponent = (T)Activator.CreateInstance(typeof(T), new object[] { engine, map });
			engine.Components.Add(newComponent);
			return newComponent;
		}

		public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
		{
			int screenWidth = GraphicsDevice.Viewport.Width;
			int screenHeight = GraphicsDevice.Viewport.Height;
			int x = screenWidth / 2 - m_image.Width / 2;
			int y = screenHeight / 2 - m_image.Height / 2;
			int width = m_image.Width;
			int height = m_image.Height;
			double widthRatio = (double)width / (double)height;
			double heightRatio = 1 / widthRatio;

			// Scale image to fit the window as well as possible, but don't distort it
			if (screenWidth < width)
			{
				x = 0;
				width = screenWidth;
				height = (int)Math.Floor(screenWidth * heightRatio);
				y = screenHeight / 2 - height / 2;
			}
			else if (screenHeight < height)
			{
				y = 0;
				height = screenHeight;
				width = (int)Math.Floor(screenHeight * widthRatio);
				x = screenWidth / 2 - width / 2;
			}

			Engine.SpriteBatch.Draw(m_image, new Rectangle(x, y, width, height), Color.White);

			base.Draw(gameTime);
		}

		Texture2D m_image;
	}
}
