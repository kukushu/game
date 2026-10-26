using System.Collections;
using UnityEngine;

public class Crane : MonoBehaviour
{
	public GameObject Arm;

	public GameObject Tower;

	private bool isTurning;

	private bool isRaising;

	private bool invertRot;

	private float rotation;

	private float height;

	private void Start()
	{
		StartCoroutine(WorkingCrane());
	}

	private void Update()
	{
		if (isTurning)
		{
			if (rotation > 90f)
			{
				invertRot = true;
			}
			else if (rotation < 0f)
			{
				invertRot = false;
			}
			if (!invertRot)
			{
				rotation += 0.06f;
				Arm.transform.Rotate(0f, 0.06f, 0f, Space.Self);
			}
			else
			{
				rotation -= 0.06f * Time.deltaTime;
				Arm.transform.Rotate(0f, -0.06f, 0f, Space.Self);
			}
		}
		if (isRaising)
		{
			height += 0.01f;
			Arm.transform.position += new Vector3(0f, 0.01f, 0f);
			Tower.transform.position += new Vector3(0f, 0.01f, 0f);
			if (height >= 0.375f)
			{
				isRaising = false;
				height = 0f;
			}
		}
	}

	public void SetInitialRotation(int R)
	{
		Arm.transform.Rotate(0f, R, 0f, Space.Self);
	}

	private IEnumerator WorkingCrane()
	{
		while (true)
		{
			isRaising = true;
			isTurning = true;
			yield return new WaitForSeconds(6f);
		}
	}
}
