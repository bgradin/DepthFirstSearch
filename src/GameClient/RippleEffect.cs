using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace GameClient
{
	// Based off of Habib's lake water shader
	// http://habibs.wordpress.com/lake/
	class RippleEffect : DrawableGameComponent
	{
		GraphicsDevice m_device;
		ContentManager m_contentManager;

		Matrix m_viewMatrix, m_projectionMatrix;

		Effect m_effect;

		RenderTarget2D m_refractionRenderTarg;
		Texture2D m_refractionMap;

		Texture2D m_waterBumpMap;

		Vector3 m_cameraPosition = new Vector3(10, 10, 1);
		Vector3 m_cameraAngles = new Vector3(-MathHelper.Pi / 2, 0, MathHelper.PiOver2);

		VertexPositionTexture[] m_waterVertices;

		float m_elapsedTime = 0.0f;

		float m_waterLevel = 1f;
		public float WaterLevel
		{
			get { return m_waterLevel; }
			set { m_waterLevel = value; }
		}

		DrawableGameComponent m_component;

		float m_waveHeight = 0.07f;
		public float WaveHeight
		{
			get { return m_waveHeight; }
			set { m_waveHeight = value; }
		}

		float m_waterSpeed = 6.5f;

		int m_specularLightPowerValue = 364; // exponent
		int m_specularLigthPerturbationValue = 4; // displacement power

		//Fresnel calculation settings
		int m_fresnelMode = 0; /// current Fresnel mode
		int m_fresnelCount = 1; /// counter of possible Fresnel modes

		/// <summary>
		/// Changes Fresnel calculation mode state 
		/// </summary>
		public void UpdateFresnelMode()
		{
			// increase mode variable - and calculate int part to limit max to m_iFresnelCount
			m_fresnelMode++;
			m_fresnelMode = m_fresnelMode % m_fresnelCount;
		}

		public int GetFresnelMode()
		{
			return m_fresnelMode;
		}

		float m_reflectionRefractionRatio = 1;
		public float FresnelValue
		{
			get { return m_reflectionRefractionRatio; }
		}

		void UpdateFresnelValue(float updateWith)
		{
			if (updateWith < 0)
			{
				m_reflectionRefractionRatio /= updateWith;
			}
			else
			{
				m_reflectionRefractionRatio *= updateWith;
			}
		}

		/// <summary>
		/// A dull color is added to the final water color - this value influences the dulling factor.
		/// </summary>
		float m_dullBlendFactor = 0.45f;
		public float DullBlendFactor
		{
			get { return m_dullBlendFactor; }
			set { m_dullBlendFactor = value; }
		}

		public RippleEffect(DrawableGameComponent component)
			: base(component.Game)
		{
			m_component = component;
			m_device = m_component.Game.GraphicsDevice;
			m_contentManager = m_component.Game.Content;
		}

		public override void Initialize()
		{
			// create render  targets for intermediate states
			m_refractionRenderTarg = new RenderTarget2D(m_device, m_device.Viewport.Width, m_device.Viewport.Height, true, SurfaceFormat.Color, DepthFormat.Depth16);

			// Loading the bump map
			m_waterBumpMap = m_contentManager.Load<Texture2D>("Effects\\waterbump");

			// load the effect file
			m_effect = m_contentManager.Load<Effect>("Effects\\WaterEffects");

			// init the vertex buffer
			SetUpVertices();

			// Calculate rotation matrix using rotation along X and Z axes 
			Matrix cameraRotation = Matrix.CreateRotationX(m_cameraAngles.X) * Matrix.CreateRotationZ(m_cameraAngles.Z);

			m_cameraPosition = new Vector3(m_device.Viewport.Width / 2, m_device.Viewport.Height / 2, 10);

			// Calculate view vector: pos + (0,1,0) vector transformed into the direction
			// of the current view
			Vector3 targetPos = m_cameraPosition + Vector3.Transform(new Vector3(0, 1, 0), cameraRotation);

			m_viewMatrix = Matrix.CreateLookAt(m_cameraPosition, targetPos, Vector3.Up);
			m_projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, (float)m_device.Viewport.Width / (float)m_device.Viewport.Height, 1.0f, 2000.0f);

			base.Initialize();
		}

		public void UpdateRefractionMap(GameTime gameTime)
		{
			m_device.SetRenderTarget(m_refractionRenderTarg);
			m_device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkGreen, 1.0f, 0);

			m_component.Draw(gameTime);

			// Set the device render target back to the back buffer.
			m_device.SetRenderTarget(null);

			// save refraction map for later use
			m_refractionMap = (Texture2D)m_refractionRenderTarg;
		}

		public override void Update(GameTime gameTime)
		{
			if (m_component != null)
				m_component.Update(gameTime);

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			UpdateRefractionMap(gameTime);

			m_elapsedTime += (float)gameTime.ElapsedGameTime.Milliseconds / 100000.0f;

			////Set effect to be used
			m_effect.CurrentTechnique = m_effect.Techniques["Water"];

			//Set up parameters for rendering:
			Matrix worldMatrix = Matrix.Identity;
			m_effect.Parameters["xWorld"].SetValue(worldMatrix);
			m_effect.Parameters["xView"].SetValue(m_viewMatrix);
			m_effect.Parameters["xReflectionView"].SetValue(m_projectionMatrix);
			m_effect.Parameters["xProjection"].SetValue(m_projectionMatrix);
			//effect.Parameters["xReflectionMap"].SetValue(reflectionMap);
			m_effect.Parameters["xRefractionMap"].SetValue(m_refractionMap);
			m_effect.Parameters["xDrawMode"].SetValue(m_reflectionRefractionRatio);
			m_effect.Parameters["fresnelMode"].SetValue(m_fresnelMode);
			m_effect.Parameters["xdullBlendFactor"].SetValue(m_dullBlendFactor);
			m_effect.Parameters["xEnableTextureBlending"].SetValue(true);
			m_effect.Parameters["xWaterBumpMap"].SetValue(m_waterBumpMap);

			// parameteres for the wave
			m_effect.Parameters["xWaveLength"].SetValue(0.01f);
			m_effect.Parameters["xWaveHeight"].SetValue(WaveHeight);
			m_effect.Parameters["xCamPos"].SetValue(m_cameraPosition);

			m_effect.Parameters["xTime"].SetValue(m_elapsedTime / m_waterSpeed);
			m_effect.Parameters["xWindForce"].SetValue(20.0f);
			Matrix windDirection = Matrix.CreateRotationZ(MathHelper.PiOver2);
			m_effect.Parameters["xWindDirection"].SetValue(windDirection);

			//specular reflection parameters
			m_effect.Parameters["specPower"].SetValue(m_specularLightPowerValue);
			m_effect.Parameters["specPerturb"].SetValue(m_specularLigthPerturbationValue);

			//draw water surface
			foreach (EffectPass pass in m_effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				//m_Device.VertexDeclaration = new VertexDeclaration(m_Device, VertexPositionTexture.VertexElements);
				m_device.DrawUserPrimitives(PrimitiveType.TriangleList, m_waterVertices, 0, 2);
			}

			base.Draw(gameTime);
		}

		void SetUpVertices()
		{
			// 2 triangles mean 6 vertex values
			m_waterVertices = new VertexPositionTexture[6];

			int width = m_device.Viewport.Width;
			int height = m_device.Viewport.Height;

			//first triangle
			m_waterVertices[0] = new VertexPositionTexture(new Vector3(0, 0, m_waterLevel), new Vector2(0, 1));
			m_waterVertices[2] = new VertexPositionTexture(new Vector3(width, height, m_waterLevel), new Vector2(1, 0));
			m_waterVertices[1] = new VertexPositionTexture(new Vector3(0, height, m_waterLevel), new Vector2(0, 0));

			//second triangle
			m_waterVertices[3] = new VertexPositionTexture(new Vector3(0, 0, m_waterLevel), new Vector2(0, 1));
			m_waterVertices[5] = new VertexPositionTexture(new Vector3(width, 0, m_waterLevel), new Vector2(1, 1));
			m_waterVertices[4] = new VertexPositionTexture(new Vector3(width, height, m_waterLevel), new Vector2(1, 0));
		}
	}
}
