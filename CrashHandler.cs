using System.Collections;
using UnityEngine;

public class CrashHandler : MonoBehaviour
{
	public WorldRegion w;

	public IEnumerator CrashCountdown()
	{
		for (int i = 0; i < 38; i++)
		{
			if (i == 26 && !w.allGood)
			{
				w.LoadingTask.text = "Loading unusually slow. Checking files integrity...";
			}
			if (i == 32 && !w.allGood)
			{
				w.LoadingTask.text = "Data corrupted! Please create a new Nation! Opening Nation view...";
			}
			yield return new WaitForSecondsRealtime(1f);
		}
		if (!w.allGood)
		{
			w.LoadingTask.text = "The Nation Could not be retrieved. Data Corrupted!";
			Debug.Log("Forcing Fade Out Screen!");
			StartCoroutine(w.FadeOutScreen());
		}
	}
}
