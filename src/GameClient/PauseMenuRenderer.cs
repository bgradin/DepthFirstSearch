using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SD = System.Drawing;
using CommonCode.Networking;
using XNAControls;

namespace GameClient
{
	public class PauseMenuRenderer : GraphicsEngineComponent
	{
		public PauseMenuRenderer(GraphicEngine engine)
			: base(engine)
		{
		}

		protected override void LoadContent()
		{
			int screenWidth = GraphicsDevice.Viewport.Width;
			int screenHeight = GraphicsDevice.Viewport.Height;

			black = new Texture2D(GraphicsDevice, 1, 1);
			black.SetData(new[] { Color.Black });

			bones = Engine.AnglerGame.Content.Load<Texture2D>("fishBones");

			pauseMenu = new XNAMenu(Game, new Rectangle(screenWidth / 2 - 150, screenHeight / 2 - 100, 280, 150))
			{
				ForeColor = SD.Color.FromArgb(165, 165, 165),
				HighlightColor = SD.Color.White,
				BackgroundColor = SD.Color.FromArgb(125, 0, 0, 0),
				BorderColor = SD.Color.FromArgb(200, 50, 50, 50),
				Font = new SD.Font("Calibri", 20),
				RenderingHint = SD.Text.TextRenderingHint.SingleBitPerPixelGridFit,
				SelectionEmphasisTexture = bones,
				ItemHeight = 40,
				Visible = false
			};
			pauseMenu.SelectionChanged += (o, e) => { FXCollection.SoundEffects[SoundEffects.Blip].Play(); };

			pauseMenu.AddMenuItem("Toggle Music", (o, e) =>
			{
				if (Engine.AnglerGame.GamePaused)
				{
					FXCollection.SoundEffects[SoundEffects.Select].Play();

					if (!FXCollection.Songs[Songs.InGame].Playing)
						FXCollection.Songs[Songs.InGame].Play();
					else
					{
						foreach (LoopedSoundPlayer player in FXCollection.Songs)
						{
							if (player.Playing)
								player.Stop();
						}
					}
				}
			});
			pauseMenu.AddMenuItem("Log Out", (o, e) => 
				{
					if (Engine.AnglerGame.GamePaused)
					{
						FXCollection.SoundEffects[SoundEffects.Select].Play();

						pauseMenu.SelectedIndex = 0;
						Engine.AnglerGame.GamePaused = false;
						World.MainPlayer.SendToServer(ServerAction.ClientLogout);
					}
				});
			pauseMenu.AddMenuItem("Quit Game", (o, e) =>
			{
				if (Engine.AnglerGame.GamePaused)
				{
					FXCollection.SoundEffects[SoundEffects.Select].Play();
					System.Threading.Tasks.Task.Factory.StartNew(() =>
						{
							System.Threading.Thread.Sleep(300); // Make sure the select sound plays
							Engine.Game.Exit();
						});
				}
			});

			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			KeyboardState state = Keyboard.GetState();

			if (pauseMenu.Visible = Engine.AnglerGame.GamePaused)
			{
				if (state.IsKeyDown(Keys.Escape))
					pauseMenu.SelectedIndex = 0;

				pauseMenu.Update(gameTime);
			}

			previousState = state;

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			if (Engine.AnglerGame.GamePaused)
			{
				Engine.SpriteBatch.Draw(black, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White * 0.5f);

				Engine.SpriteBatch.End();
				pauseMenu.SpriteBatch = Engine.SpriteBatch;
				pauseMenu.Draw(gameTime);
				Engine.SpriteBatch.Begin(Engine.SortMode, Engine.BlendState, Engine.SamplerState, Engine.StencilState, Engine.RasterizerState);
			}

			base.Draw(gameTime);
		}

		Texture2D black, bones;
		XNAMenu pauseMenu;
		KeyboardState previousState;
	}
}
