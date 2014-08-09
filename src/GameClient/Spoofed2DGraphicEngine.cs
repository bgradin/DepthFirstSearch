using GameClassLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace AnglerGameClient
{
	/// <summary>
	/// A class for rendering 2D objects in 3D space
	/// </summary>
	public sealed class Spoofed2DGraphicEngine : GraphicEngine
	{
		public Spoofed2DGraphicEngine(AnglerGame game, RenderOrder order = RenderOrder.EffectsLayer)
			: base(game, order)
		{
			StencilState = DepthStencilState.DepthRead;
		}

		public override void Initialize()
		{
			// Set up the effect used to create a 2D surface out of a 3D object
			Effect = new BasicEffect(AnglerGame.GraphicsDeviceManager.GraphicsDevice);
			Effect.VertexColorEnabled = true;
			Effect.Projection = Matrix.CreateOrthographicOffCenter(0,
				AnglerGame.GraphicsDeviceManager.GraphicsDevice.Viewport.Width,
				AnglerGame.GraphicsDeviceManager.GraphicsDevice.Viewport.Height,
				0, 0, 1);

			// Set up the rasterizer state
			RasterizerState = new RasterizerState();
			RasterizerState.ScissorTestEnable = true;
			RasterizerState.FillMode = FillMode.Solid;
			RasterizerState.CullMode = CullMode.None;
			AnglerGame.GraphicsDeviceManager.GraphicsDevice.RasterizerState = RasterizerState;

			base.Initialize();
		}
	}
}
