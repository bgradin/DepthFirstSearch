using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CommonCode.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using XNAControls;
using SD = System.Drawing;

namespace GameClient
{
	class ChatRenderer : GraphicsEngineComponent
	{
		public XNATextBox MessageBox { get; set; }

		public ChatRenderer(GraphicEngine engine)
			: base (engine)
		{
		}

		protected override void LoadContent()
		{
			font = new System.Drawing.Font("Arial", 12);

			chatBackground = Engine.Game.DrawRectangle(new SD.Size(500, 170), SD.Color.FromArgb(125, 0, 0, 0), SD.Color.FromArgb(200, 50, 50, 50));

			chatBounds = new Rectangle(20, GraphicsDevice.Viewport.Height - 220, 500, 170);

			textRenderingSpriteBatch = new SpriteBatch(GraphicsDevice);

			carat = Engine.Game.DrawText(">", font, SD.Color.White);

			Texture2D transparent = new Texture2D(GraphicsDevice, 1, lineHeight);
			Color[] transparentPixels = new Color[lineHeight];
			for (int i = 0; i < transparentPixels.Length; i++)
				transparentPixels[i] = Color.Transparent;
			transparent.SetData(transparentPixels);
			Texture2D[] textboxTextures = new Texture2D[4]
				{
					transparent,
					transparent,
					transparent,
					transparent
				};
			MessageBox = new XNATextBox(Game, new Microsoft.Xna.Framework.Rectangle(chatBounds.X + 15, GraphicsDevice.Viewport.Height - 20 - lineHeight, chatBackground.Width - 10, lineHeight), textboxTextures, "Arial", 12.0f);
			MessageBox.TextColor = SD.Color.White;
			Engine.Game.Components.Remove(MessageBox); // since it's automatically added
			Engine.AnglerGame.KeyboardDispatcher.Subscriber = MessageBox;
			MessageBox.Initialize();
			MessageBox.MaxChars = 100;

			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			MessageBox.Update(gameTime);

			base.Update(gameTime);
		}

		public Texture2D GenerateTexture(ChatMessage message)
		{
			// Get username and text textures
			Texture2D username = Engine.Game.DrawText(message.Username + ":", font, message.UsernameColor);
			Texture2D text = Engine.Game.DrawText(message.Message, font, message.MessageColor, chatBounds.Width - 10 - username.Width);

			// Create new render target
			chatRenderTarget = new RenderTarget2D(GraphicsDevice, chatBounds.Width, text.Height > username.Height ? text.Height : username.Height);
			GraphicsDevice.SetRenderTarget(chatRenderTarget);
			GraphicsDevice.Clear(Color.Transparent);

			// Draw textures
			textRenderingSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
			textRenderingSpriteBatch.Draw(username, new Rectangle(0, 0, username.Width, username.Height), Color.White);
			textRenderingSpriteBatch.Draw(text, new Rectangle(username.Width, 0, text.Width, text.Height), Color.White);
			textRenderingSpriteBatch.End();

			// Reset device
			GraphicsDevice.SetRenderTarget(null);

			return (Texture2D)chatRenderTarget;
		}

		public override void Draw(GameTime gameTime)
		{
			// Draw chat message box
			if (Engine.AnglerGame.ShowChatMessages)
			{
				Engine.SpriteBatch.Draw(chatBackground, chatBounds, Color.White);

				if (Engine.AnglerGame.CurrentChatMessages.Count != 0)
				{
					int i = chatBounds.Y + chatBounds.Height;

					foreach (Texture2D messageTexture in Engine.AnglerGame.CurrentChatMessages.Values)
					{
						i -= messageTexture.Height;
						int sourceRectHeight = i > chatBounds.Y ? 0 : chatBounds.Y + 1 - i;

						Engine.SpriteBatch.Draw(messageTexture,
							new Rectangle(chatBounds.X + 10, i + sourceRectHeight, messageTexture.Width, messageTexture.Height - sourceRectHeight),
							new Rectangle(0, sourceRectHeight, messageTexture.Width, messageTexture.Height - sourceRectHeight),
							Color.White);

						if (i < chatBounds.Y)
							break;
					}
				}
			}
			else
			{
				if (Engine.AnglerGame.CurrentChatMessages.Count != 0)
				{
					// Get a list of textures of message sent since the latest send date, and the opacity at which to draw them
					DateTime latestDate = DateTime.Now.Subtract(messageDuration);
					KeyValuePair<Texture2D, float>[] currentMessages = Engine.AnglerGame.CurrentChatMessages
						.Where(i => i.Key > latestDate)
						.Select(i => new KeyValuePair<Texture2D, float>(
							i.Value,
							i.Key > latestDate.Add(messageFadeDuration) ? 1.0f : (float)((i.Key - latestDate).TotalMilliseconds / messageFadeDuration.TotalMilliseconds)))
						.ToArray();

					int y = chatBounds.Y + chatBounds.Height;
					foreach (var pair in currentMessages)
					{
						y -= pair.Key.Height;
						int sourceRectHeight = y > chatBounds.Y ? 0 : chatBounds.Y + 1 - y;

						Engine.SpriteBatch.Draw(pair.Key,
							new Rectangle(chatBounds.X + 10, y + sourceRectHeight, pair.Key.Width, pair.Key.Height - sourceRectHeight),
							new Rectangle(0, sourceRectHeight, pair.Key.Width, pair.Key.Height - sourceRectHeight),
							Color.White * pair.Value);

						if (y < chatBounds.Y)
							break;
					}
				}
			}

			// Draw chat prompt
			if (MessageBox.Visible = Engine.AnglerGame.ShowChatPrompt)
			{
				if (!MessageBox.Selected)
				{
					MessageBox.Selected = true;
					Engine.AnglerGame.KeyboardDispatcher.Subscriber = MessageBox;
				}

				Engine.SpriteBatch.Draw(carat, new Rectangle(chatBounds.X + 3, GraphicsDevice.Viewport.Height - 44, carat.Width, carat.Height), Color.White);

				Engine.SpriteBatch.End();
				MessageBox.SpriteBatch = Engine.SpriteBatch;
				MessageBox.Draw(gameTime);
				Engine.SpriteBatch.Begin(Engine.SortMode, Engine.BlendState, Engine.SamplerState, Engine.StencilState, Engine.RasterizerState);
			}
			else if (MessageBox.Selected)
			{
				MessageBox.Selected = false;
				Engine.AnglerGame.KeyboardDispatcher.Subscriber = null;
			}

			base.Draw(gameTime);
		}

		protected override void Dispose(bool disposing)
		{
			if(font != null)
				font.Dispose();
			base.Dispose(disposing);
		}

		RenderTarget2D chatRenderTarget;
		SpriteBatch textRenderingSpriteBatch;
		Texture2D chatBackground, carat;
		const int lineHeight = 30;
		Rectangle chatBounds;
		TimeSpan messageDuration = new TimeSpan(0, 0, 10);
		TimeSpan messageFadeDuration = new TimeSpan(0, 0, 1);
		System.Drawing.Font font;
	}
}
