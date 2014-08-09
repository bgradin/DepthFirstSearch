using CommonCode.GameLogic;
using CommonCode.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameClient
{
	class MapRenderer : GraphicEngine
	{
		public MapRenderer(AnglerGame game, Map map)
			: base(game, RenderOrder.GraphicLayer)
		{
			m_map = map;
			shadowCaster = new ShadowCaster(this);

			Components.Add(shadowCaster);
		}

		protected override void LoadContent()
		{
			screenLights = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
			screenGround = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
			solids = new SortedList<float, List<locatedTexture>>();

			base.LoadContent();
		}

		public override void Draw(GameTime gameTime)
		{
			if (m_map != null)
			{
				int screenWidth = GraphicsDevice.Viewport.Width;
				int screenHeight = GraphicsDevice.Viewport.Height;

				GraphicsDevice.SetRenderTarget(screenGround);
				GraphicsDevice.Clear(Color.Black);
				Rectangle bounds = AnglerGame.MainPlayer.Bounds;
				int x = bounds.X < 0 ? 0 : bounds.X;
				int y = bounds.Y < 0 ? 0 : bounds.Y;
				Rectangle clipRectangle = new Rectangle(
					x,
					y,
					bounds.X + bounds.Width > screenWidth ? screenWidth - x : (bounds.X < 0 ? bounds.Width + bounds.X : bounds.Width),
					bounds.Y + bounds.Height > screenHeight ? screenHeight - y : (bounds.Y < 0 ? bounds.Height + bounds.Y : bounds.Height));

				//draw the tile texture tiles across the screen
				GraphicsDevice.ScissorRectangle = clipRectangle;
				SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState, StencilState, RasterizerState);

				// Draw graphics and special tiles
				solids.Clear();
				for (int i = m_map.VisibleBounds.Left; i < m_map.VisibleBounds.Right; i++)
				{
					for (int j = m_map.VisibleBounds.Top; j < m_map.VisibleBounds.Bottom; j++)
					{
						Rectangle rect = m_map.PositionOnVisibleMap(i, j);

						Tile tile = World.CurrentMap.GetTile(i, j, LAYERS.Graphic);

						if (tile is GraphicTile)
						{
							GraphicTile gt = tile as GraphicTile;

							SpriteBatch.Draw(FXCollection.Textures[gt.Graphic],
								new Vector2(rect.X, rect.Y),
								Color.White);
						}

						tile = World.CurrentMap.GetTile(i, j, LAYERS.Special);
						if (tile != null && (tile as SpecialTile).Type == SpecialTileSpec.NONE)
							continue;

						if (tile is SpecialTile)
						{
							SpecialTile st = tile as SpecialTile;
							if (st.Type == SpecialTileSpec.WALL)
							{
								locatedTexture locText = new locatedTexture(new Point(i, j), FXCollection.Textures[st.Graphic]);

								if (!solids.ContainsKey(st.Density))
									solids.Add(st.Density, new List<locatedTexture>());

								solids[st.Density].Add(locText);

								SpriteBatch.Draw(FXCollection.Textures[st.Graphic],
									new Vector2(rect.X, rect.Y),
									Color.White);
							}
						}
					}
				}
				SpriteBatch.End();

				// Draw shadows to the ground render target
				foreach (var pair in solids)
				{
					Texture2D[] textures = pair.Value.Select(i => i.texture).ToArray();
					Point[] locations = pair.Value.Select(i => i.location).ToArray();

					shadowCaster.DrawShadows(textures, locations, screenGround, screenLights, pair.Key);
				}

				// Draw the resulting graphic
				GraphicsDevice.SetRenderTarget(null);
				GraphicsDevice.ScissorRectangle = clipRectangle;
				GraphicsDevice.Clear(Color.Black);
				SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState, StencilState, RasterizerState);
				SpriteBatch.Draw(screenGround, Vector2.Zero, Color.White);

				for (int i = m_map.VisibleBounds.Left; i < m_map.VisibleBounds.Right; i++)
				{
					for (int j = m_map.VisibleBounds.Top; j < m_map.VisibleBounds.Bottom; j++)
					{
						Rectangle rect = m_map.PositionOnVisibleMap(i, j);

						Tile tile = World.CurrentMap.GetTile(i, j, LAYERS.Special);
						if (tile == null)
							continue;

						if (tile is SpecialTile)
						{
							SpecialTile st = tile as SpecialTile;
							if (st.Type == SpecialTileSpec.NONE || st.Density <= 0.25)
								continue;

							SpriteBatch.Draw(FXCollection.Textures[st.Graphic],
								new Vector2(rect.X, rect.Y),
								Color.White);
						}
					}
				}

				SpriteBatch.End();
				GraphicsDevice.ScissorRectangle = GraphicsDevice.Viewport.Bounds;
			}

			base.Draw(gameTime);
		}

		struct locatedTexture
		{
			public locatedTexture(Point point, Texture2D text)
			{
				location = point;
				texture = text;
			}

			public Point location;
			public Texture2D texture;
		}

		SortedList<float, List<locatedTexture>> solids;
		Map m_map;
		RenderTarget2D screenLights;
		RenderTarget2D screenGround;
		ShadowCaster shadowCaster;
	}
}
