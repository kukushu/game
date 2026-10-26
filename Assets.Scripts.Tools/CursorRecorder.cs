using UnityEngine;

namespace Assets.Scripts.Tools;

public class CursorRecorder : MonoBehaviour
{
	private void FixedUpdate()
	{
		gameObject.transform.position = Input.mousePosition;
	}
}
