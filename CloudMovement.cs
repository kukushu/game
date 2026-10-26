using System.Collections;
using UnityEngine;

public class CloudMovement : MonoBehaviour
{
	public Vector3 Direction = new Vector3(1f, 0f, 0f);

	private Vector3 Virtual = Vector3.zero;

	private void Start()
	{
		StartCoroutine(MoveCloud());
	}

	private IEnumerator MoveCloud()
	{
		while (true)
		{
			Virtual += Direction;
			Vector3 vector = new Vector3(transform.position.x, transform.position.y, transform.position.z);
			ParticleSystem component = GetComponent<ParticleSystem>();
			if (component != null)
			{
				ParticleSystem.ShapeModule shape = component.shape;
				shape.position = vector + Virtual;
				if (shape.position.z > 100f)
				{
					WeatherController.CloudsAlive--;
					Object.Destroy(gameObject);
				}
			}
			else
			{
				Debug.Log("Cloud Is NULL");
			}
			yield return new WaitForSeconds(1f);
		}
	}
}
