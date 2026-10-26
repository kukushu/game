using System;
using System.Collections;
using UnityEngine;

public class RCC_DetachablePart : MonoBehaviour
{
	[Serializable]
	private class DetachableJoint
	{
		[HideInInspector]
		public ConfigurableJoint joint;

		internal ConfigurableJointMotion jointMotionAngularX;

		internal ConfigurableJointMotion jointMotionAngularY;

		internal ConfigurableJointMotion jointMotionAngularZ;

		internal ConfigurableJointMotion jointMotionX;

		internal ConfigurableJointMotion jointMotionY;

		internal ConfigurableJointMotion jointMotionZ;
	}

	public Transform COM;

	private Rigidbody rigid;

	public bool lockAtStart = true;

	public float strength = 100f;

	private bool broken;

	public bool isBreakable = true;

	public bool isDetachable = true;

	public int loosePoint = 35;

	public int detachPoint;

	public Vector3 addTorqueAfterLoose = Vector3.zero;

	private DetachableJoint detachableJoint = new DetachableJoint();

	private void Start()
	{
		rigid = GetComponent<Rigidbody>();
		ConfigurableJoint component = gameObject.GetComponent<ConfigurableJoint>();
		if ((bool)COM)
		{
			rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);
		}
		if ((bool)component)
		{
			detachableJoint.joint = component;
			if (lockAtStart)
			{
				StartCoroutine(LockParts());
			}
		}
		else
		{
			Debug.LogWarning("Configurable Joint not found for " + gameObject.name + "!");
			enabled = false;
		}
	}

	private IEnumerator LockParts()
	{
		yield return new WaitForFixedUpdate();
		detachableJoint.jointMotionAngularX = detachableJoint.joint.angularXMotion;
		detachableJoint.jointMotionAngularY = detachableJoint.joint.angularYMotion;
		detachableJoint.jointMotionAngularZ = detachableJoint.joint.angularZMotion;
		detachableJoint.jointMotionX = detachableJoint.joint.xMotion;
		detachableJoint.jointMotionY = detachableJoint.joint.yMotion;
		detachableJoint.jointMotionZ = detachableJoint.joint.zMotion;
		detachableJoint.joint.angularXMotion = ConfigurableJointMotion.Locked;
		detachableJoint.joint.angularYMotion = ConfigurableJointMotion.Locked;
		detachableJoint.joint.angularZMotion = ConfigurableJointMotion.Locked;
		detachableJoint.joint.xMotion = ConfigurableJointMotion.Locked;
		detachableJoint.joint.yMotion = ConfigurableJointMotion.Locked;
		detachableJoint.joint.zMotion = ConfigurableJointMotion.Locked;
	}

	private void Update()
	{
		if (!broken && addTorqueAfterLoose != Vector3.zero && strength <= (float)loosePoint)
		{
			float z = transform.InverseTransformDirection(rigid.velocity).z;
			rigid.AddRelativeTorque(new Vector3(addTorqueAfterLoose.x * z, addTorqueAfterLoose.y * z, addTorqueAfterLoose.z * z));
		}
	}

	public void OnCollision(Collision collision)
	{
		if (broken)
		{
			return;
		}
		Vector3 relativeVelocity = collision.relativeVelocity;
		relativeVelocity *= 1f - Mathf.Abs(Vector3.Dot(transform.up, collision.contacts[0].normal));
		float num = Mathf.Abs(Vector3.Dot(collision.contacts[0].normal, relativeVelocity.normalized));
		ContactPoint[] contacts = collision.contacts;
		foreach (ContactPoint contactPoint in contacts)
		{
			Vector3 vector = transform.InverseTransformPoint(contactPoint.point);
			if (vector.magnitude < 2f)
			{
				strength -= (2f - vector.magnitude) * num * 10f;
				strength = Mathf.Clamp(strength, 0f, float.PositiveInfinity);
			}
		}
		DamageParts();
	}

	private void DamageParts()
	{
		if (broken)
		{
			return;
		}
		if (strength <= (float)detachPoint)
		{
			if ((bool)detachableJoint.joint)
			{
				broken = true;
				UnityEngine.Object.Destroy(detachableJoint.joint);
				transform.SetParent(null);
				UnityEngine.Object.Destroy(gameObject, 3f);
			}
		}
		else if (strength <= (float)loosePoint && (bool)detachableJoint.joint)
		{
			detachableJoint.joint.angularXMotion = detachableJoint.jointMotionAngularX;
			detachableJoint.joint.angularYMotion = detachableJoint.jointMotionAngularY;
			detachableJoint.joint.angularZMotion = detachableJoint.jointMotionAngularZ;
			detachableJoint.joint.xMotion = detachableJoint.jointMotionX;
			detachableJoint.joint.yMotion = detachableJoint.jointMotionY;
			detachableJoint.joint.zMotion = detachableJoint.jointMotionZ;
		}
	}
}
