using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Tools.Helico;

public class HelicoAI : MonoBehaviour
{
	public AudioSource audioSource;

	public HeliRotorController MainRotorController;

	public HeliRotorController SubRotorController;

	private float speed;

	private void Start()
	{
		MainRotorController.RotarSpeed = 400f;
		SubRotorController.RotarSpeed = 200f;
		StartCoroutine(GoUp());
	}

	private IEnumerator GoUp()
	{
		yield return new WaitForSeconds(5f);
		for (int i = 0; i < 1000; i++)
		{
			transform.position += new Vector3(0f, speed, 0f);
			speed += 3E-05f;
			if (audioSource.volume < SettingsStatic.CityVolume)
			{
				audioSource.volume += 0.01f;
			}
			if (i == 800)
			{
				StartCoroutine(GoForward());
			}
			yield return null;
		}
	}

	private IEnumerator GoForward()
	{
		for (int i = 0; i < 2000; i++)
		{
			transform.position += new Vector3(0f, 0f, speed);
			if (i == 1900)
			{
				StartCoroutine(DestroySound());
			}
			yield return null;
		}
	}

	private IEnumerator DestroySound()
	{
		for (int i = 0; i < 100; i++)
		{
			audioSource.volume -= 0.01f;
			yield return null;
		}
		Object.Destroy(transform.gameObject);
	}
}
