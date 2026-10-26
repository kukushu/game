using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Tools;

public class Siren : MonoBehaviour
{
	private void Start()
	{
		StartCoroutine(TurnAround());
	}

	private IEnumerator TurnAround()
	{
		while (true)
		{
			yield return new WaitForSeconds(0.2f);
			gameObject.transform.Rotate(0f, 180f, 0f);
		}
	}
}
