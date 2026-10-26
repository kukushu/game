using UnityEngine;
using UnityEngine.UI;

public class Gamespeed : MonoBehaviour
{
	public Button speed_0;

	public Button speed_1;

	public Button speed_2;

	public Button speed_3;

	public GameObject indicator;

	public GameObject pauseBar;

	public int previousSpeed = 1;

	public void Speed_0()
	{
		if (!Indexer.copterMode)
		{
			Time.timeScale = 0f;
			pauseBar.SetActive(value: true);
			indicator.transform.position = speed_0.transform.position;
		}
	}

	public void Speed_1()
	{
		Time.timeScale = 1f;
		pauseBar.SetActive(value: false);
		Time.fixedDeltaTime = 0.03f;
		indicator.transform.position = speed_1.transform.position;
		previousSpeed = 1;
	}

	public void Speed_2()
	{
		if (!Indexer.copterMode)
		{
			Time.timeScale = 3f;
			pauseBar.SetActive(value: false);
			Time.fixedDeltaTime = 0.03f;
			indicator.transform.position = speed_2.transform.position;
			previousSpeed = 2;
		}
	}

	public void Speed_3()
	{
		if (!Indexer.copterMode)
		{
			Time.timeScale = 6f;
			pauseBar.SetActive(value: false);
			Time.fixedDeltaTime = 0.04f;
			indicator.transform.position = speed_3.transform.position;
			previousSpeed = 3;
		}
	}
}
