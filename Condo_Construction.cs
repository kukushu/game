using UnityEngine;

public class Condo_Construction : MonoBehaviour
{
	public GameObject Core;

	public GameObject Walls;

	public GameObject Windows;

	private Vector3 plus = new Vector3(0f, 0.375f, 0f);

	public void CoreUp()
	{
		Core.transform.position += plus;
	}

	public void WallsUp()
	{
		Walls.transform.position += plus;
	}

	public void WindowsUp()
	{
		Windows.transform.position += plus;
	}
}
