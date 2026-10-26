using UnityEngine;

public class HeliRotorController : MonoBehaviour
{
	public enum Axis
	{
		X,
		Y,
		Z
	}

	public Axis RotateAxis;

	private float _rotarSpeed;

	private float rotateDegree;

	private Vector3 OriginalRotate;

	public float RotarSpeed
	{
		get
		{
			return _rotarSpeed;
		}
		set
		{
			_rotarSpeed = Mathf.Clamp(value, 0f, 3000f);
		}
	}

	private void Start()
	{
		OriginalRotate = transform.localEulerAngles;
	}

	private void Update()
	{
		rotateDegree += RotarSpeed * Time.deltaTime;
		rotateDegree %= 360f;
		switch (RotateAxis)
		{
		case Axis.Y:
			transform.localRotation = Quaternion.Euler(OriginalRotate.x, rotateDegree, OriginalRotate.z);
			break;
		case Axis.Z:
			transform.localRotation = Quaternion.Euler(OriginalRotate.x, OriginalRotate.y, rotateDegree);
			break;
		default:
			transform.localRotation = Quaternion.Euler(rotateDegree, OriginalRotate.y, OriginalRotate.z);
			break;
		}
	}
}
