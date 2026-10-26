using System.Collections;
using UnityEngine;

public class PortCrane : MonoBehaviour
{
	public float speed = 0.01f;

	private void Start()
	{
		StartCoroutine(Movement());
	}

	private IEnumerator Movement()
	{
		while (true)
		{
			for (float i = -4f; i < 4f; i += speed)
			{
				transform.localPosition = new Vector3(i, 0.5f, -2.87f);
				yield return new WaitForEndOfFrame();
			}
			yield return new WaitForSeconds(6f);
			for (float i = 4f; i > -4f; i -= speed)
			{
				transform.localPosition = new Vector3(i, 0.5f, -2.87f);
				yield return new WaitForEndOfFrame();
			}
			yield return new WaitForSeconds(6f);
		}
	}
}
