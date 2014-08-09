using CommonCode.GameLogic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using System.Linq;

namespace GameClient
{
	/// <summary>
	/// Main component type for GraphicsEngine
	/// </summary>
	public class GraphicsEngineComponent : DrawableGameComponent
	{
		protected GraphicsEngineComponent(GraphicEngine engine)
			: base(engine.Game)
		{
			Engine = engine;
		}

		public static T CreateAndAdd<T>(GraphicEngine engine) where T : GraphicsEngineComponent
		{
			T newComponent = (T)Activator.CreateInstance(typeof(T), new object[] { engine });
			engine.Components.Add(newComponent);
			return newComponent;
		}

		public GraphicEngine Engine { get; private set; }
	}

	/// <summary>
	/// A class for rendering objects
	/// </summary>
	public class GraphicEngine : AnglerGameDrawableComponent
	{
		public List<GraphicsEngineComponent> Components { get; set; }
		public SpriteBatch SpriteBatch { get; set; }
		public RasterizerState RasterizerState { get; set; }
		public BasicEffect Effect { get; set; }
		public BlendState BlendState { get; set; }
		public SamplerState SamplerState { get; set; }
		public DepthStencilState StencilState { get; set; }
		public SpriteSortMode SortMode { get; set; }

		public GraphicEngine(AnglerGame game, RenderOrder order = RenderOrder.Background)
			: base(game)
		{
			DrawOrder = (int)order;
			Components = new List<GraphicsEngineComponent>();

			SortMode = SpriteSortMode.Deferred;
			BlendState = BlendState.AlphaBlend;
			SamplerState = SamplerState.LinearClamp;
			StencilState = DepthStencilState.None;
			RasterizerState = new RasterizerState() { ScissorTestEnable = true };
		}

		public override void Initialize()
		{
			foreach (GraphicsEngineComponent component in Components)
				component.Initialize();

			base.Initialize();
		}

		protected override void LoadContent()
		{
			SpriteBatch = new SpriteBatch(GraphicsDevice);
			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			foreach (GraphicsEngineComponent component in Components)
				component.Update(gameTime);

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			if (Effect != null)
			{
				SpriteBatch.Begin(SortMode, BlendState, SamplerState, StencilState, RasterizerState, Effect);
				Effect.CurrentTechnique.Passes[0].Apply();
			}
			else
			{
				SpriteBatch.Begin(SortMode, BlendState, SamplerState, StencilState, RasterizerState);
			}

			List<GraphicsEngineComponent> sortedComponents = Components.OrderBy(i => i.DrawOrder).ToList();

			// Draw all child components
			for (int i = 0; i < sortedComponents.Count; i++)
				sortedComponents[i].Draw(gameTime);

			SpriteBatch.End();

			base.Draw(gameTime);
		}

		protected override void Dispose(bool disposing)
		{
			foreach (GraphicsEngineComponent component in Components)
				component.Dispose();

			if (SpriteBatch != null)
				SpriteBatch.Dispose();

			base.Dispose(disposing);
		}
	}
}
