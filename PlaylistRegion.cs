using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaylistRegion : MonoBehaviour
{
	public List<AudioClip> tracks;

	public int rTrack;

	public bool isRegion;

	public AudioSource source;

	public AudioSource ambient;

	private int delay;

	public Economy eco;

	private void Start()
	{
		rTrack = Random.Range(0, tracks.Count - 1);
		source.volume = SettingsStatic.MusicVolume;
		ambient.volume = SettingsStatic.CityVolume;
	}

	private void Update()
	{
		delay++;
		if (delay > 600)
		{
			delay = 0;
			if (!source.isPlaying)
			{
				PlayNext();
			}
		}
	}

	private void PlayNext()
	{
		rTrack++;
		if (rTrack >= tracks.Count)
		{
			rTrack = 0;
		}
		if (!isRegion)
		{
			AdaptivePlaylist();
		}
		source.clip = tracks[rTrack];
		source.Play();
	}

	public IEnumerator FadeOutVol()
	{
		while (source.volume > 0.01f)
		{
			yield return new WaitForEndOfFrame();
			source.volume -= 0.05f;
			ambient.volume -= 0.05f;
		}
		yield return new WaitForFixedUpdate();
	}

	private void AdaptivePlaylist()
	{
		if (eco.Population < 1000f)
		{
			if (rTrack < 2)
			{
				rTrack = 2;
			}
			else if (rTrack > 3 && rTrack < 6)
			{
				rTrack = 6;
			}
			else if (rTrack > 6 && rTrack < 9)
			{
				rTrack = 9;
			}
			else if (rTrack == 10)
			{
				rTrack = 11;
			}
			else if (rTrack == 12)
			{
				rTrack = 13;
			}
			else if (rTrack > 15 && rTrack < 19)
			{
				rTrack = 19;
			}
			else if (rTrack == 22 || rTrack == 23)
			{
				rTrack = 24;
			}
			else if (rTrack == 26)
			{
				rTrack = 0;
			}
		}
		else if (eco.Population < 30000f)
		{
			if (rTrack == 4)
			{
				rTrack = 5;
			}
			else if (rTrack == 16 || rTrack == 17)
			{
				rTrack = 18;
			}
			else if (rTrack == 22)
			{
				rTrack = 23;
			}
			else if (rTrack == 26)
			{
				rTrack = 0;
			}
		}
	}
}
