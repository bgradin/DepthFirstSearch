using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CommonCode.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameClient
{
	public sealed class InputHandler : AnglerGameDrawableComponent
	{
		private bool allowMove = true;

		public KeyboardState PreviousState { get; set; }

		public EventHandler ChatBoxClosing { get; set; }
		public Action RemoveTilde { get; set; } // Remove the tilde from the object pointed to by the KeyHandler

		public InputHandler(AnglerGame game)
			: base(game)
		{
		}

		public override void Update(GameTime gameTime)
		{
			KeyboardState state = Keyboard.GetState();

			if (state.IsKeyDown(Keys.Escape) && !PreviousState.IsKeyDown(Keys.Escape))
			{
				FXCollection.SoundEffects[SoundEffects.Select].Play();

				AnglerGame.GamePaused = !AnglerGame.GamePaused;
			}

			if (!AnglerGame.GamePaused && state.IsKeyDown(Keys.Enter) && !PreviousState.IsKeyDown(Keys.Enter))
			{
				if (AnglerGame.ShowChatPrompt)
					ChatBoxClosing(null, null);

				AnglerGame.ShowChatPrompt = !AnglerGame.ShowChatPrompt;
			}

			if (state.IsKeyDown(Keys.OemTilde) && !PreviousState.IsKeyDown(Keys.OemTilde))
			{
				AnglerGame.ShowChatMessages = !AnglerGame.ShowChatMessages;

				if (RemoveTilde != null)
					RemoveTilde();
			}

			Keys directionKey = Keys.E; // Start with some random key (this will change)
			if (World.MainPlayer.CurrentGraphicIndex == World.MainPlayer.BackGraphicIndex)
				directionKey = Keys.Up;
			if (World.MainPlayer.CurrentGraphicIndex == World.MainPlayer.FrontGraphicIndex)
				directionKey = Keys.Down;
			if (World.MainPlayer.CurrentGraphicIndex == World.MainPlayer.RightGraphicIndex)
				directionKey = Keys.Right;
			if (World.MainPlayer.CurrentGraphicIndex == World.MainPlayer.LeftGraphicIndex)
				directionKey = Keys.Left;

			if (allowMove)
			{
				// Enqueue a key in the recent key presses if it's been pressed
				foreach (Keys key in arrowKeys)
				{
					Direction d = (Direction)Enum.Parse(typeof(Direction), key.ToString());

					int pos = 0;
					switch (d)
					{
						case Direction.Up:
							pos = World.MainPlayer.Y - 1;
							break;
						case Direction.Down:
							pos = World.MainPlayer.Y + 1;
							break;
						case Direction.Left:
							pos = World.MainPlayer.X - 1;
							break;
						case Direction.Right:
							pos = World.MainPlayer.X + 1;
							break;
					}

					if (state.IsKeyDown(key)
						&& !AnglerGame.GamePaused
						&& World.MainPlayer.NextMoves.Count < 2
						&& !World.MainPlayer.NextMoves.Contains(key)
						&& !World.MainPlayer.PendingResponse
						&& (key != directionKey || !World.MainPlayer.MovementBlocked)
						&& World.MainPlayer.CheckPosition(d, pos))
					{
						World.MainPlayer.NextMoves.Enqueue(key);
						break;
					}
				}
			}

			// Handle recently pressed keys
			// We can't send a response if we're already waiting for one or if the game is paused
			if (!World.MainPlayer.PendingResponse &&
				// We can move if we're holding the same direction we're already going and our movement isn't blocked
				((!World.MainPlayer.MovementBlocked && World.MainPlayer.NextMoves.Count > 0 && World.MainPlayer.NextMoves.Peek() == directionKey)
				// We can move if we're not already moving
					|| (World.MainPlayer.OffsetX == 0 && World.MainPlayer.OffsetY == 0)))
			{
				Keys? key;
				int dist = World.MainPlayer.GetMove(World.CurrentMap, out key);

				bool move = true;
				if (key == Keys.Up && (World.CurrentMap.GetTile(World.MainPlayer.X, World.MainPlayer.Y - 1, LAYERS.Special) as SpecialTile).Type == SpecialTileSpec.WALL)
				{
					move = false;
					World.MainPlayer.CurrentGraphicIndex = World.MainPlayer.BackGraphicIndex;
				}
				else if (key == Keys.Down && (World.CurrentMap.GetTile(World.MainPlayer.X, World.MainPlayer.Y + 1, LAYERS.Special) as SpecialTile).Type == SpecialTileSpec.WALL)
				{
					move = false;
					World.MainPlayer.CurrentGraphicIndex = World.MainPlayer.FrontGraphicIndex;
				}
				else if (key == Keys.Right && (World.CurrentMap.GetTile(World.MainPlayer.X + 1, World.MainPlayer.Y, LAYERS.Special) as SpecialTile).Type == SpecialTileSpec.WALL)
				{
					move = false;
					World.MainPlayer.CurrentGraphicIndex = World.MainPlayer.RightGraphicIndex;
				}
				else if (key == Keys.Left && (World.CurrentMap.GetTile(World.MainPlayer.X - 1, World.MainPlayer.Y, LAYERS.Special) as SpecialTile).Type == SpecialTileSpec.WALL)
				{
					move = false;
					World.MainPlayer.CurrentGraphicIndex = World.MainPlayer.LeftGraphicIndex;
				}

				// Send walk command to server
				if (dist != 0 && key != null && move)
					World.MainPlayer.SendToServer(ServerAction.Walk, new WalkData((Direction)Enum.Parse(typeof(Direction), key.ToString()), dist));

				
				if (World.MainPlayer.X == 30 && World.MainPlayer.Y == 0 && firstDialog == true)
				{
					XNAControls.XNADialog YouWinDialog = new XNAControls.XNADialog(Game, "You've successfully escaped the treacherous labyrinth of deep sea trenches!", "You win!");
					firstDialog = false;
					Game.IsMouseVisible = true;
					allowMove = false;
					YouWinDialog.DialogClosing += (sender, args) =>
						{
							firstDialog = true; 
							Game.IsMouseVisible = false;

							Packet pack = new Packet(ServerAction.Warp);
							pack.AddData<byte>((byte)ServerAction.Warp);
							World.MainPlayer.SendData(pack);

							allowMove = true;
						};
				}
			}

			PreviousState = state;

			base.Update(gameTime);
		}

		readonly Keys[] arrowKeys = { Keys.Up, Keys.Down, Keys.Left, Keys.Right };

		private bool firstDialog = true; 
	}
}
