using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.IO;
using System.Threading;

namespace GameClient
{
	class LoopedSoundPlayer
	{
		public TimeSpan Overlap { get; set; }
		public TimeSpan Length { get; private set; }

		public float Volume
		{
			get
			{
				return m_primarySound.Volume;
			}
			set
			{
				m_primarySound.Volume = value;
				m_secondarySound.Volume = value;
			}
		}

		public bool Playing
		{
			get { return m_primarySound.State == SoundState.Playing || m_secondarySound.State == SoundState.Playing; }
		}

		SoundEffectInstance m_primarySound;
		SoundEffectInstance m_secondarySound;
		DateTime m_startTime;

		public LoopedSoundPlayer(SoundEffect effect)
		{
			Length = effect.Duration;
			m_primarySound = effect.CreateInstance();
			m_secondarySound = effect.CreateInstance();
		}

		public void Play()
		{
			m_primarySound.Stop();
			m_primarySound.Play();
			m_startTime = DateTime.Now;
		}

		public void Stop()
		{
			m_primarySound.Stop();
			m_secondarySound.Stop();
		}

		public void Update()
		{
			if (m_primarySound.State == SoundState.Playing && m_secondarySound.State != SoundState.Playing && DateTime.Now > (m_startTime + Length) - Overlap)
			{
				m_secondarySound.Stop();
				m_secondarySound.Play();
				m_startTime = DateTime.Now;
			}
			else if (m_secondarySound.State == SoundState.Playing && m_primarySound.State != SoundState.Playing && DateTime.Now > (m_startTime + Length) - Overlap)
			{
				m_primarySound.Stop();
				m_primarySound.Play();
				m_startTime = DateTime.Now;
			}
		}
	}
}
