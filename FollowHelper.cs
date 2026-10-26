using UnityEngine;

public class FollowHelper : MonoBehaviour
{
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f));
	}
}
