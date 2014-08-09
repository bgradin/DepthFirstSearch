using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CommonCode.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using XNAControls;
using SD = System.Drawing;

namespace GameClient
{
	public class AnglerGameDrawableComponent : DrawableGameComponent
	{
		public AnglerGame AnglerGame
		{
			get
			{
				if (!(Game is AnglerGame))
					throw new InvalidOperationException("Invalid game");

				return Game as AnglerGame;
			}
		}

		protected AnglerGameDrawableComponent(AnglerGame game)
			: base(game)
		{
		}
	}

	static class Songs
	{
		public static int PreGame;
		public static int InGame;
	}

	static class SoundEffects
	{
		public static int Select;
		public static int Collect;
		public static int Blip;
	}

	public class AnglerGame : Game
	{
		public PlayerComponent MainPlayer { get; set; }

		public GraphicsDeviceManager GraphicsDeviceManager { get; set; }

		public bool ShowChatMessages { get; set; }
		public bool ShowChatPrompt { get; set; }
		public bool GamePaused { get; set; }
		public SortedList<DateTime, Texture2D> CurrentChatMessages { get; set; }
		public KeyboardDispatcher KeyboardDispatcher { get; private set; }
		public InputHandler InputHandler { get; set; }
		public XNAMenu PreGameMenu { get; set; }
		
		SortedList<DateTime, ChatMessage> untexturedChatMessages;

		KeyboardState previousState;
		XNAComponentPanel loginPanel;
		XNAComponentPanel registerPanel;
		XNAComponentPanel inGamePanel;
		XNAComponentPanel preGameMenuPanel;
		XNAComponentPanel backgroundPanel;
		XNAComponentPanel howToPlayPanel;
		XNATextBox usernameTextbox;
		XNATextBox passwordTextbox;
		XNATextBox confirmPasswordTextbox;
		XNAHyperLink cancelHyperlink;
		ChatRenderer chatRenderer;
		OverlayRenderer overlayRenderer;
		Background background;
		
		TextBoxEvent loginTBEvent;
		TextBoxEvent registerTBEvent;
		TextBoxEvent subscribeToUsernameBox;
		TextBoxEvent subscribeToPasswordBox;
		TextBoxEvent subscribeToConfirmBox;

		// Miscellaneous
		AudioManager audioManager;
		const int maxChatRecords = 20;

		class DescendedDateComparer : IComparer<DateTime>
		{
			public int Compare(DateTime x, DateTime y)
			{
				// use the default comparer to do the original comparison for datetimes
				int ascendingResult = Comparer<DateTime>.Default.Compare(x, y);

				// turn the result around
				return 0 - ascendingResult;
			}
		}

		public AnglerGame()
		{
			World.GameState = GameState.Uninitialized;

			GraphicsDeviceManager = new GraphicsDeviceManager(this);
#if DEBUG
			// Maintain specific resolution for the login image
			GraphicsDeviceManager.PreferredBackBufferWidth = 889;
			GraphicsDeviceManager.PreferredBackBufferHeight = 500;
#else
			GraphicsDeviceManager.IsFullScreen = true;
			System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(this.Window.Handle);
			GraphicsDeviceManager.PreferredBackBufferWidth = screen.Bounds.Width;
			GraphicsDeviceManager.PreferredBackBufferHeight = screen.Bounds.Height;
#endif
			GraphicsDeviceManager.ApplyChanges();

			FXCollection.Content = Content;
			Content.RootDirectory = "Content";
		}

		protected override void OnExiting(object sender, EventArgs args)
		{
			if (World.MainPlayer != null)
			{
				if(World.MainPlayer.LoggedIn)
					World.MainPlayer.SendToServer(ServerAction.ClientLogout);
				World.MainPlayer.SendToServer(ServerAction.ClientDisconnect);
			}

			base.OnExiting(sender, args);
		}

		protected override void Initialize()
		{
			(System.Windows.Forms.Form.FromHandle(this.Window.Handle)).Visible = false;
			try
			{
				World.MainPlayer = new ConnectedPlayer("127.0.0.1", 8085 /*, true*/); //uncomment to try out dual-mode IPv6 sockets!
				World.MainPlayer.AddChatMessage = (message) =>
				{
					untexturedChatMessages.Add(DateTime.Now, message);

					if (untexturedChatMessages.Count + CurrentChatMessages.Count > maxChatRecords)
						CurrentChatMessages.RemoveAt(CurrentChatMessages.Count - 1);
				};

				World.MainPlayer.OnSocketError = (o, e) =>
					{
						World.GameState = GameState.PregameMenu;
						XNADialog dlg = new XNADialog(this, "You were unexpectedly disconnected from the game server! Restart the game.", "Server connection error!");
						dlg.DialogClosing += (sender, args) =>
							{
								//exit the game once the dialog is closed (custom close action)
								this.Dispose();
								this.Exit();
							};
					};
				
				(System.Windows.Forms.Form.FromHandle(this.Window.Handle)).Visible = true;
			}
			catch
			{
				// Let the client know that we couldn't connect to the server
				System.Windows.Forms.MessageBox.Show("The client was unable to contact the server. Make sure the server is started.", "Error connecting!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				Environment.Exit(0);
				Dispose();
				return;
			}

			ShowChatMessages = false;
			ShowChatPrompt = false;
			GamePaused = false;
			CurrentChatMessages = new SortedList<DateTime, Texture2D>(new DescendedDateComparer());
			untexturedChatMessages = new SortedList<DateTime, ChatMessage>();

			// Load map
			try
			{
				World.CurrentMap = new Map(typeof(AnglerGame).Assembly.GetManifestResourceStream("GameClient.Maps.game.bmap"));

				// Make sure all items are initialized
				for (int i = 0; i < World.CurrentMap.Width; i++)
				{
					for (int j = 0; j < World.CurrentMap.Height; j++)
						World.CurrentMap.AddTile(new Vector2(i, j), LAYERS.Item, new ItemTile(ItemTileSpec.NONE, i, j));
				}
			}
			catch
			{
				throw new Exception("There was a problem loading the map!");
			}

			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Load textures, map and sounds
			Tileset ts = new Tileset();
			try
			{
				ts.LoadFromStream(typeof(AnglerGame).Assembly.GetManifestResourceStream("GameClient.Maps.game.tileset"));

				FXCollection.Textures = new List<Texture2D>();
				for (int i = 0; i < ts.Images.Images.Count; i++)
				{
					using (MemoryStream ms = new MemoryStream())
					{
						ts.Images.Images[i].Save(ms, System.Drawing.Imaging.ImageFormat.Png);
						FXCollection.Textures.Add(Texture2D.FromStream(GraphicsDevice, ms));
					}
				}

				// Load sounds
				FXCollection.Songs = new List<LoopedSoundPlayer>();
				FXCollection.SoundEffects = new List<SoundEffectInstance>();
				FXCollection.Songs[Songs.PreGame = FXCollection.Load<SoundEffect>("Audio\\Music\\titleSong")].Overlap = new TimeSpan(0, 0, 0, 0, 1500);
				FXCollection.Songs[Songs.InGame = FXCollection.Load<SoundEffect>("Audio\\Music\\inGameBackground")].Overlap = new TimeSpan(0, 0, 0, 0, 2500);
				SoundEffects.Select = FXCollection.Load<SoundEffectInstance>("Audio\\SoundEffects\\pop");
				SoundEffects.Collect = FXCollection.Load<SoundEffectInstance>("Audio\\SoundEffects\\boop");
				SoundEffects.Blip = FXCollection.Load<SoundEffectInstance>("Audio\\SoundEffects\\blip");
			}
			catch
			{
				throw new Exception("There was a problem loading a resource!");
			}

			// Create audio zones for enemies
			for (int i = 1; i < World.Players.Count; i++)
			{
				Player e = World.Players[i];

				if (e is Enemy)
				{
					// Temporary - just use the color red for enemies, rather than an actual texture
					Texture2D texture = new Texture2D(GraphicsDevice, 1, 1);
					texture.SetData(new[] { Color.Red });
					FXCollection.Textures.Add(texture);

					World.Players[i].CurrentGraphicIndex = FXCollection.Textures.Count - 1;
					audioManager.AddZone(new EnemyAudioZone(e as Enemy, FXCollection.Load<SoundEffectInstance>("Audio\\Music\\tone"), 200));
				}
			}

			World.LightsFX = new LightsFX(
				Content.Load<Effect>("Effects\\resolveShadowsEffect"),
				Content.Load<Effect>("Effects\\reductionEffect"),
				Content.Load<Effect>("Effects\\2xMultiBlend"));

			// Eventually, the lower four lines graphics will be pulled from the tileset
			World.MainPlayer.BackGraphicIndex = FXCollection.Load<Texture2D>("back_sprite");
			World.MainPlayer.FrontGraphicIndex = FXCollection.Load<Texture2D>("front_sprite");
			World.MainPlayer.LeftGraphicIndex = FXCollection.Load<Texture2D>("left_sprite");
			World.MainPlayer.RightGraphicIndex = FXCollection.Load<Texture2D>("right_sprite");

			World.MainPlayer.CurrentGraphicIndex = World.MainPlayer.FrontGraphicIndex;

			KeyboardDispatcher = new KeyboardDispatcher(Window);

			inGamePanel = new XNAComponentPanel(this);
			loginPanel = new XNAComponentPanel(this);
			registerPanel = new XNAComponentPanel(this);
			preGameMenuPanel = new XNAComponentPanel(this);
			howToPlayPanel = new XNAComponentPanel(this);
			backgroundPanel = new XNAComponentPanel(this);

			int screenWidth = GraphicsDevice.Viewport.Width;
			int screenHeight = GraphicsDevice.Viewport.Height;

			Texture2D[] textboxTextures = new Texture2D[4]
				{
					Content.Load<Texture2D>("textboxBack"),
					Content.Load<Texture2D>("textboxLeft"),
					Content.Load<Texture2D>("textboxRight"),
					Content.Load<Texture2D>("textboxCaret")
				};

			// Background image with ripple effect
			background = new Background(this, Content.Load<Texture2D>("titleBackground"));
			RippleEffect rippleEffect = new RippleEffect(backgroundPanel);
			loginPanel.Components.Add(rippleEffect);
			registerPanel.Components.Add(rippleEffect);
			preGameMenuPanel.Components.Add(rippleEffect);
			howToPlayPanel.Components.Add(rippleEffect);

			PreGameMenu = new XNAMenu(this, new Rectangle(screenWidth / 2 - 150, screenHeight / 2 - 100, 300, 200))
			{
				ForeColor = SD.Color.FromArgb(25, 50, 150),
				HighlightColor = SD.Color.FromArgb(4, 4, 99),
				Font = new SD.Font("Calibri", 36),
				RenderingHint = SD.Text.TextRenderingHint.SingleBitPerPixelGridFit,
				ItemHeight = 60
			};
			PreGameMenu.SelectionChanged += (o, e) => { FXCollection.SoundEffects[SoundEffects.Blip].Play(); };
			PreGameMenu.AddMenuItem("Log In", (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();
				World.GameState = GameState.Login;
			});
			PreGameMenu.AddMenuItem("Create Account", (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();
				World.GameState = GameState.Register;
			});
			PreGameMenu.AddMenuItem("How to Play", (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();

				World.GameState = GameState.HowToPlay;
			});
			PreGameMenu.AddMenuItem("Quit Game", (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();
				System.Threading.Tasks.Task.Factory.StartNew(() =>
				{
					System.Threading.Thread.Sleep(300); // Make sure the select sound plays
					Exit();
				});
			});

			XNAPictureBox titlePicture = new XNAPictureBox(this, new Rectangle(100, screenHeight / 2 - 50, screenWidth - 200, 200))
			{
				StretchMode = StretchMode.CenterInFrame,
				Texture = Content.Load<Texture2D>("logo")
			};
			loginPanel.Components.Add(titlePicture);
			registerPanel.Components.Add(titlePicture);
			preGameMenuPanel.Components.Add(titlePicture);
			howToPlayPanel.Components.Add(titlePicture);

			//temporary action objects that are used to construct the member TextBoxEvent variables
			Action<object, EventArgs> loginEvent;
			Action<object, EventArgs> registerEvent;
			// Login event
			loginEvent = (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();

				World.GameState = GameState.LoggingIn;

				World.MainPlayer.SendToServer(ServerAction.ClientLogin,
					new LoginData(usernameTextbox.Text, passwordTextbox.Text));
			};

			loginTBEvent = new TextBoxEvent(loginEvent);

			// Register event
			registerEvent = (o, e) =>
			{
				if (passwordTextbox.Text == confirmPasswordTextbox.Text)
				{
					FXCollection.SoundEffects[SoundEffects.Select].Play();

					World.GameState = GameState.Registering;

					World.MainPlayer.SendToServer(ServerAction.ClientCreateAcc,
						new LoginData(usernameTextbox.Text, passwordTextbox.Text));
				}
				else
				{
					FXCollection.SoundEffects[SoundEffects.Select].Play();

					XNADialog errDlg = new XNADialog(this, "Passwords do not match.", "Error creating account!");
					errDlg.DialogClosing += (sender, args) => { FXCollection.SoundEffects[SoundEffects.Select].Play(); };
				}
			};

			XNALabel howToPlayText = new XNALabel(this, new Rectangle(screenWidth / 4, screenHeight / 2 - 80, screenWidth / 2, 300));
			howToPlayText.Text = Resources.HowToPlayText;
			howToPlayText.Font = new SD.Font("Calibri", 16);
			howToPlayPanel.Components.Add(howToPlayText);

			XNAHyperLink backHyperlink = new XNAHyperLink(
				this,
				new Rectangle(3 * screenWidth / 4 - 50, screenHeight / 2 + 75, 50, 30),
				"Arial", 12.0f,
				SD.FontStyle.Bold,
				SD.Text.TextRenderingHint.SingleBitPerPixelGridFit);
			backHyperlink.DrawOrder = (int)RenderOrder.UILayer;
			backHyperlink.ForeColor = SD.Color.FromArgb(139, 95, 71);
			backHyperlink.HighlightColor = SD.Color.FromArgb(150, 139, 95, 71);
			backHyperlink.Text = "Back";
			backHyperlink.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			backHyperlink.OnClick += (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();
				World.GameState = GameState.PregameMenu;
			};
			howToPlayPanel.Components.Add(backHyperlink);

			registerTBEvent = new TextBoxEvent(registerEvent);

			subscribeToUsernameBox = (object sender, EventArgs e) => { usernameTextbox.Selected = true; KeyboardDispatcher.Subscriber = usernameTextbox; };
			subscribeToPasswordBox = (object sender, EventArgs e) => { passwordTextbox.Selected = true; KeyboardDispatcher.Subscriber = passwordTextbox; };
			subscribeToConfirmBox = (object sender, EventArgs e) => { confirmPasswordTextbox.Selected = true; KeyboardDispatcher.Subscriber = confirmPasswordTextbox; };

			// Update error message event
			World.MainPlayer.UpdateErrorMessage = (sender, args) =>
			{
				//close any open dialog
				XNADialog errMsg = new XNADialog(this, args.Message, args.Caption);
				if (!(sender as ConnectedPlayer).Connected)
					errMsg.DialogClosing += (s, a) =>
					{
						FXCollection.SoundEffects[SoundEffects.Select].Play();
						this.Exit();
					};
			};

			World.MainPlayer.BacteriaCollect = (sender, args) =>
			{
				FXCollection.SoundEffects[SoundEffects.Collect].Play();

				overlayRenderer.RenderBacteriaCount();
			};

			// Username field
			usernameTextbox = new XNATextBox(this, new Rectangle(screenWidth / 2 - 100, screenHeight / 2 - 70, 200, 30), textboxTextures, "Arial", 12.0f);
			usernameTextbox.DrawOrder = (int)RenderOrder.UILayer;
			usernameTextbox.MaxChars = 30;
			usernameTextbox.DefaultText = "Username";
			usernameTextbox.Clicked += subscribeToUsernameBox;
			subscribeToUsernameBox(usernameTextbox);
			usernameTextbox.OnTabPressed += subscribeToPasswordBox;
			loginPanel.Components.Add(usernameTextbox);
			registerPanel.Components.Add(usernameTextbox);

			// Password field
			passwordTextbox = new XNATextBox(this, new Rectangle(screenWidth / 2 - 100, screenHeight / 2 - 20, 200, 30), textboxTextures, "Arial", 12.0f);
			passwordTextbox.DrawOrder = (int)RenderOrder.UILayer;
			passwordTextbox.MaxChars = 30;
			passwordTextbox.DefaultText = "Password";
			passwordTextbox.PasswordBox = true;
			passwordTextbox.Clicked += subscribeToPasswordBox;
			loginPanel.Components.Add(passwordTextbox);
			registerPanel.Components.Add(passwordTextbox);

			// Confirm password field
			confirmPasswordTextbox = new XNATextBox(this, new Rectangle(screenWidth / 2 - 100, screenHeight / 2 + 30, 200, 30), textboxTextures, "Arial", 12.0f);
			confirmPasswordTextbox.DrawOrder = (int)RenderOrder.UILayer;
			confirmPasswordTextbox.MaxChars = 30;
			confirmPasswordTextbox.DefaultText = "Confirm Password";
			confirmPasswordTextbox.PasswordBox = true;
			confirmPasswordTextbox.Clicked += subscribeToConfirmBox;
			confirmPasswordTextbox.OnTabPressed += subscribeToUsernameBox;
			confirmPasswordTextbox.OnEnterPressed += new TextBoxEvent(registerEvent);
			registerPanel.Components.Add(confirmPasswordTextbox);

			Texture2D[] loginButtonTextures = new Texture2D[2]
				{
					Content.Load<Texture2D>("button"),
					Content.Load<Texture2D>("buttonHover")
				};

			XNAButton loginButton = new XNAButton(this, new Vector2(screenWidth / 2 + 20, screenHeight / 2 + 30), "Login");
			loginButton.DrawOrder = (int)RenderOrder.UILayer;
			loginButton.OnClick += new XNAButton.ButtonClickEvent(loginEvent);
			loginPanel.Components.Add(loginButton);

			XNAButton registerButton = new XNAButton(this, new Vector2(screenWidth / 2 + 20, screenHeight / 2 + 80), "Register");
			registerButton.DrawOrder = (int)RenderOrder.UILayer;
			registerButton.OnClick += new XNAButton.ButtonClickEvent(registerEvent);
			registerPanel.Components.Add(registerButton);

			cancelHyperlink = new XNAHyperLink(
				this,
				new Rectangle(screenWidth / 2 - 95, screenHeight / 2 + 80, 100, 30),
				"Arial", 12.0f,
				SD.FontStyle.Bold,
				SD.Text.TextRenderingHint.SingleBitPerPixelGridFit);
			cancelHyperlink.DrawOrder = (int)RenderOrder.UILayer;
			cancelHyperlink.ForeColor = SD.Color.FromArgb(139, 95, 71);
			cancelHyperlink.HighlightColor = SD.Color.FromArgb(150, 139, 95, 71);
			cancelHyperlink.Text = "Cancel";
			cancelHyperlink.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			cancelHyperlink.OnClick += (o, e) =>
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();
				World.GameState = GameState.PregameMenu;
			};
			registerPanel.Components.Add(cancelHyperlink);
			loginPanel.Components.Add(cancelHyperlink);

			// Map renderer
			MapRenderer mapRenderer = new MapRenderer(this, World.CurrentMap);
			inGamePanel.Components.Add(mapRenderer);

			// NPC rendering engine
			GraphicEngine npcEngine = new GraphicEngine(this, RenderOrder.NPCLayer);
			GraphicsEngineComponent.CreateAndAdd<MinorPlayerRenderer>(npcEngine);
			inGamePanel.Components.Add(npcEngine);

			// Overlay rendering engine
			GraphicEngine overlayEngine = new GraphicEngine(this, RenderOrder.OverlayLayer);
			overlayRenderer = GraphicsEngineComponent.CreateAndAdd<OverlayRenderer>(overlayEngine);
			inGamePanel.Components.Add(overlayEngine);

			// Audio manager
			inGamePanel.Components.Add(audioManager = new AudioManager(this));

			// Input handler
			InputHandler = new InputHandler(this);
			inGamePanel.Components.Add(InputHandler);
			InputHandler.ChatBoxClosing += (o, e) =>
			{
				if (ShowChatPrompt && chatRenderer.MessageBox.Text != "")
				{
					World.MainPlayer.SendToServer(ServerAction.ClientSay, new TalkData(chatRenderer.MessageBox.Text));
					chatRenderer.MessageBox.Text = "";
				}
			};
			InputHandler.RemoveTilde += () =>
				{
					chatRenderer.MessageBox.Text = chatRenderer.MessageBox.Text.TrimEnd('`');
				};

			// Main Player
			inGamePanel.Components.Add(MainPlayer = new PlayerComponent(this, World.MainPlayer));

			// Pause menu
			GraphicEngine pauseMenuEngine = new GraphicEngine(this, RenderOrder.PauseMenuLayer);
			GraphicsEngineComponent.CreateAndAdd<PauseMenuRenderer>(pauseMenuEngine);
			inGamePanel.Components.Add(pauseMenuEngine);

			// Chat
			GraphicEngine chatEngine = new GraphicEngine(this, RenderOrder.ChatLayer);
			chatRenderer = GraphicsEngineComponent.CreateAndAdd<ChatRenderer>(chatEngine);
			inGamePanel.Components.Add(chatEngine);

			World.GameState = GameState.PregameMenu;

		}

		protected override void UnloadContent()
		{
			base.UnloadContent();

			if (World.GameState != GameState.Uninitialized)
			{
				foreach (Texture2D texture in FXCollection.Textures)
					texture.Dispose();
			}
		}

		protected override void Update(GameTime gameTime)
		{
			KeyboardState state = Keyboard.GetState();

			if (World.GameState == GameState.Uninitialized)
				return;
			else if (World.GameState == GameState.PregameMenu && !Components.Contains(preGameMenuPanel))
			{
				Components.Clear();
				Components.Add(preGameMenuPanel);
				
				KeyboardDispatcher.Subscriber = null;

				usernameTextbox.OnEnterPressed -= loginTBEvent;
				passwordTextbox.OnEnterPressed -= loginTBEvent;
				confirmPasswordTextbox.OnEnterPressed -= registerTBEvent;

				if (!FXCollection.Songs[Songs.PreGame].Playing)
					FXCollection.Songs[Songs.PreGame].Play();
				if (FXCollection.Songs[Songs.InGame].Playing)
					FXCollection.Songs[Songs.InGame].Stop();

				if (!backgroundPanel.Components.Contains(background))
					backgroundPanel.Components.Add(background);

				if (!backgroundPanel.Components.Contains(PreGameMenu))
					backgroundPanel.Components.Add(PreGameMenu);
			}
			else if (World.GameState == GameState.Login && !Components.Contains(loginPanel))
			{
				loginPanel.ClearTextBoxes(); //if going back to the login screen clear the previously typed data out of the text boxes
				Components.Clear();
				Components.Add(loginPanel);

				KeyboardDispatcher.Subscriber = usernameTextbox;

				cancelHyperlink.DrawLocation = new Vector2(cancelHyperlink.DrawLocation.X, GraphicsDevice.Viewport.Height / 2 + 30);

				usernameTextbox.OnEnterPressed -= registerTBEvent;
				passwordTextbox.OnEnterPressed -= registerTBEvent;
				passwordTextbox.OnTabPressed -= subscribeToConfirmBox;
				confirmPasswordTextbox.OnEnterPressed -= registerTBEvent;

				usernameTextbox.OnEnterPressed += loginTBEvent;
				passwordTextbox.OnEnterPressed += loginTBEvent;
				passwordTextbox.OnTabPressed += subscribeToUsernameBox;

				if (!FXCollection.Songs[Songs.PreGame].Playing)
					FXCollection.Songs[Songs.PreGame].Play();
				if (FXCollection.Songs[Songs.InGame].Playing)
					FXCollection.Songs[Songs.InGame].Stop();

				if (backgroundPanel.Components.Contains(PreGameMenu))
					backgroundPanel.Components.Remove(PreGameMenu);
			}
			else if (World.GameState == GameState.InGame && !Components.Contains(inGamePanel))
			{
				InputHandler.PreviousState = state;
				Components.Clear();
				Components.Add(inGamePanel);
				
				//the text boxes are ALWAYS listening (because the subscriber listening for an enter keypress works outside of the game component model)
				usernameTextbox.OnEnterPressed -= loginTBEvent;
				passwordTextbox.OnEnterPressed -= loginTBEvent;
				confirmPasswordTextbox.OnEnterPressed -= registerTBEvent;

				if (FXCollection.Songs[Songs.PreGame].Playing)
					FXCollection.Songs[Songs.PreGame].Stop();
				if (!FXCollection.Songs[Songs.InGame].Playing)
					FXCollection.Songs[Songs.InGame].Play();
			}
			else if (World.GameState == GameState.Register && !Components.Contains(registerPanel))
			{
				Components.Clear();
				Components.Add(registerPanel);

				KeyboardDispatcher.Subscriber = usernameTextbox;

				cancelHyperlink.DrawLocation = new Vector2(cancelHyperlink.DrawLocation.X, GraphicsDevice.Viewport.Height / 2 + 80);

				usernameTextbox.OnEnterPressed -= loginTBEvent;
				passwordTextbox.OnEnterPressed -= loginTBEvent;
				passwordTextbox.OnTabPressed -= subscribeToUsernameBox;
				confirmPasswordTextbox.OnEnterPressed += registerTBEvent;

				usernameTextbox.OnEnterPressed += registerTBEvent;
				passwordTextbox.OnEnterPressed += registerTBEvent;
				passwordTextbox.OnTabPressed += subscribeToConfirmBox;

				if (!FXCollection.Songs[Songs.PreGame].Playing)
					FXCollection.Songs[Songs.PreGame].Play();
				if (FXCollection.Songs[Songs.InGame].Playing)
					FXCollection.Songs[Songs.InGame].Stop();

				if (backgroundPanel.Components.Contains(PreGameMenu))
					backgroundPanel.Components.Remove(PreGameMenu);
			}
			else if (World.GameState == GameState.HowToPlay && !Components.Contains(howToPlayPanel))
			{
				Components.Clear();
				Components.Add(howToPlayPanel);
				
				if (backgroundPanel.Components.Contains(PreGameMenu))
					backgroundPanel.Components.Remove(PreGameMenu);
			}

			if (state.IsKeyDown(Keys.Escape) && !previousState.IsKeyDown(Keys.Escape))
			{
				if (World.GameState == GameState.Login || World.GameState == GameState.Register || World.GameState == GameState.HowToPlay)
				{
					FXCollection.SoundEffects[SoundEffects.Select].Play();
					usernameTextbox.Text = "";
					passwordTextbox.Text = "";
					confirmPasswordTextbox.Text = "";
					World.GameState = GameState.PregameMenu;
				}
			}

			// Render new text messages
			if (World.GameState == GameState.InGame)
			{
				if (untexturedChatMessages.Count > 0)
				{
					foreach (var pair in untexturedChatMessages)
						CurrentChatMessages.Add(pair.Key, chatRenderer.GenerateTexture(pair.Value));
					untexturedChatMessages.Clear();
				}
			}

			// We have an in-game dialog (for teh winz) change the mouse visibility. If a dialog is open, DON'T CHANGE IT BACK!
			if (XNAControl.Dialogs != null && XNAControl.Dialogs.Count == 0)
			{
				IsMouseVisible = World.GameState == GameState.Login
					|| World.GameState == GameState.Register
					|| World.GameState == GameState.LoggingIn
					|| World.GameState == GameState.Registering
					|| World.GameState == GameState.PregameMenu
					|| World.GameState == GameState.HowToPlay;
			}

			previousState = state;

			foreach (LoopedSoundPlayer player in FXCollection.Songs)
				player.Update();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDeviceManager.GraphicsDevice.Clear(Color.Black);

			if (World.GameState == GameState.PregameMenu)
				PreGameMenu.Draw(gameTime);

			int screenWidth = GraphicsDevice.Viewport.Width;

			base.Draw(gameTime);
		}
	}
}
