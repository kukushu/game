using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public Sound[] sounds;

	private SoundManager instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				sound.source = gameObject.AddComponent<AudioSource>();
				sound.source.clip = sound.clip;
				sound.source.volume = sound.volume;
				sound.source.pitch = sound.pitch;
				sound.source.loop = sound.loop;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	public void Play(string name)
	{
		Sound sound = Array.Find(sounds, (Sound sound2) => sound2.name == name);
		if (sound == null)
		{
			Debug.Log("Typo in sound names");
			return;
		}
		sound.source.priority = 1;
		sound.source.volume = SettingsStatic.UIVolume;
		sound.source.Play();
	}

	public void StopPlay(string name)
	{
		Sound sound = Array.Find(sounds, (Sound sound2) => sound2.name == name);
		if (sound == null)
		{
			Debug.Log("Typo in sound names");
		}
		else
		{
			sound.source.Stop();
		}
	}
}
