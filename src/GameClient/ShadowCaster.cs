using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CommonCode.Networking;
using System;

namespace GameClient
{
	public class ShadowCaster : GraphicsEngineComponent
	{
		LightSource _lightSource;

		ShadowMapResolver shadowMapResolver;
		ShadowCasterMap shadowMap;

		public ShadowCaster(GraphicEngine engine)
			: base(engine)
		{
		}

		protected override void LoadContent()
		{
			if (Engine.SpriteBatch == null)
				Engine.SpriteBatch = new SpriteBatch(GraphicsDevice);

			cloner = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
			shadowMapResolver = new ShadowMapResolver(GraphicsDevice, World.LightsFX, GraphicsDevice.Viewport.Width);
			_lightSource = new LightSource(Engine.AnglerGame.GraphicsDeviceManager, GraphicsDevice.Viewport.Width, LightAreaQuality.VeryHigh, Color.White);
			shadowMap = new ShadowCasterMap(PrecisionSettings.VeryHigh, Engine.AnglerGame.GraphicsDeviceManager, Engine.SpriteBatch);

			base.LoadContent();
		}

		public void DrawShadows(Texture2D[] textures, Point[] locations, RenderTarget2D screenGround, RenderTarget2D screenLights, float density)
		{
			if (_lightSource == null)
				return;

			if (textures == null || density > 1 || density < 0 || textures.Length != locations.Length)
				throw new Exception("Invalid parameter");

			// This is called every frame because of the cat animation.
			// If your shadow casters map is static, you can generate it once.
			shadowMap.StartGeneratingShadowCasteMap(false);
			{
				for (int i = 0; i < textures.Length; i++)
				{
					var location = World.CurrentMap.PositionOnVisibleMap(locations[i].X, locations[i].Y);

					shadowMap.AddShadowCaster(textures[i], new Vector2(location.X + 4, location.Y + 4), textures[i].Width, textures[i].Height);
				}
			}
			shadowMap.EndGeneratingShadowCasterMap();

			Vector2 lightPosition = new Vector2(Engine.AnglerGame.MainPlayer.ScreenX + Const.TILE_SIZE / 2,
				Engine.AnglerGame.MainPlayer.ScreenY + Const.TILE_SIZE / 2);

			shadowMapResolver.ResolveShadows(this.shadowMap, _lightSource, PostEffect.None, lightPosition, World.MainPlayer.Radius + 5);

			// We print the lights in an image
			GraphicsDevice.SetRenderTarget(screenLights);
			{
				GraphicsDevice.Clear(Color.Black);
				Engine.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
				_lightSource.Draw(Engine.SpriteBatch);
				Engine.SpriteBatch.End();
			}

			// Clone the ground texture
			GraphicsDevice.SetRenderTarget(cloner);
			GraphicsDevice.Clear(Color.White);
			Engine.SpriteBatch.Begin();
			Engine.SpriteBatch.Draw(screenGround, Vector2.Zero, Color.White);
			Engine.SpriteBatch.End();

			// This command impress a texture on another using 2xMultiplicative blend, which is perfect to paste our lights on the underlying image
			World.LightsFX.PrintLightsOverTexture(screenGround, Engine.SpriteBatch, Engine.AnglerGame.GraphicsDeviceManager, screenLights, cloner, density);
		}

		RenderTarget2D cloner;
	}
}
