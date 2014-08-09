using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GameClient
{
	static class FXCollection
	{
		public static ContentManager Content { get; set; }

		public static List<Texture2D> Textures { get; set; }

		public static List<LoopedSoundPlayer> Songs { get; set; }

		public static List<SoundEffectInstance> SoundEffects { get; set; }

		public static int Load<T>(string assetName)
		{
			if (typeof(T) == typeof(Texture2D))
			{
				Textures.Add(Content.Load<Texture2D>(assetName));
				return Textures.Count - 1;
			}
			else if (typeof(T) == typeof(SoundEffectInstance))
			{
				SoundEffectInstance sfxInstance = Content.Load<SoundEffect>(assetName).CreateInstance();

				SoundEffects.Add(sfxInstance);
				return SoundEffects.Count - 1;
			}
			else if (typeof(T) == typeof(SoundEffect))
			{
				SoundEffectInstance sfxInstance = Content.Load<SoundEffect>(assetName).CreateInstance();

				Songs.Add(new LoopedSoundPlayer(Content.Load<SoundEffect>(assetName)));
				return Songs.Count - 1;
			}

			return -1;
		}
	}
}
