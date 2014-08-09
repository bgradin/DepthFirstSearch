using CommonCode.GameLogic;
using CommonCode.Networking;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GameClient
{
	public sealed class AudioManager : AnglerGameDrawableComponent
	{
		public AudioZone DefaultZone { get; set; }
		public AudioZone CurrentZone { get; private set; }

		public AudioManager(AnglerGame game)
			: base(game)
		{
		}

		public void AddZone(AudioZone zone)
		{
			m_zones.Add(zone);
		}

		public override void Initialize()
		{
			m_zones = new List<AudioZone>();
		}

		public override void Update(GameTime gameTime)
		{
			if (m_zones != null)
			{
				AudioZone sourceZone = new AudioZone(-1);
				float volume = 0;

				// Find out if we need to transition to a different audio zone
				foreach (AudioZone zone in m_zones)
				{
					if (zone.ContainsPlayer(World.MainPlayer))
					{
						if (zone is EnemyAudioZone)
						{
							EnemyAudioZone enemyZone = zone as EnemyAudioZone;

							if (enemyZone != null)
							{
								float distance = enemyZone.DistanceFromPlayer(World.MainPlayer);
								float currentVolume = (float)(enemyZone.Radius - distance) / (float)enemyZone.Radius;

								if (currentVolume > volume)
								{
									volume = currentVolume;
									sourceZone = zone;
								}
							}
						}
						else if (!(sourceZone is EnemyAudioZone))
							sourceZone = zone;
						else
							sourceZone = DefaultZone;
					}
				}

				// If we do, transition to the next audio zone
				if (sourceZone.SongIndex != -1)
				{
					//if (sourceZone != CurrentZone)
					//	FXCollection.Songs[sourceZone.SongIndex].Play();
				}
				//else if (CurrentZone != null && CurrentZone.SongIndex != -1)
				//	FXCollection.Songs[CurrentZone.SongIndex].Stop();

				if (CurrentZone != sourceZone)
					CurrentZone = sourceZone;

				//if (CurrentZone != null && CurrentZone.SongIndex != -1 && FXCollection.Songs[CurrentZone.SongIndex].Volume != volume)
				//	FXCollection.Songs[CurrentZone.SongIndex].Volume = volume;
			}

			base.Update(gameTime);
		}

		List<AudioZone> m_zones;
	}
}
