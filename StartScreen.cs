using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class StartScreen : MonoBehaviour
{
	public VideoPlayer video;

	public GameObject crane;

	public Image image;

	public Slider slider;

	private AsyncOperation asyncOperation;

	private void Start()
	{
		StartCoroutine(DoStuff());
	}

	private IEnumerator DoStuff()
	{
		asyncOperation = SceneManager.LoadSceneAsync("RegionView");
		asyncOperation.allowSceneActivation = false;
		yield return new WaitForSeconds(1f);
		video.Play();
		yield return new WaitForSeconds(4.75f);
		crane.SetActive(value: true);
		yield return new WaitForSecondsRealtime(0.5f);
		slider.gameObject.SetActive(value: true);
		slider.value = 10f;
		yield return new WaitForSecondsRealtime(1f);
		slider.value = 20f;
		yield return new WaitForSecondsRealtime(1f);
		slider.value = 40f;
		yield return new WaitForSecondsRealtime(1f);
		slider.value = 60f;
		yield return new WaitForSecondsRealtime(1f);
		slider.value = 80f;
		yield return new WaitForSecondsRealtime(1f);
		slider.value = 100f;
		yield return new WaitForSecondsRealtime(0.1f);
		asyncOperation.allowSceneActivation = true;
	}
}
