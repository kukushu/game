using System;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Truck Trailer")]
[RequireComponent(typeof(Rigidbody))]
public class RCC_TruckTrailer : MonoBehaviour
{
	[Serializable]
	public class TrailerWheel
	{
		public WheelCollider wheelCollider;

		public Transform wheelModel;

		public void Torque(float torque)
		{
			wheelCollider.motorTorque = torque;
		}

		public void Brake(float torque)
		{
			wheelCollider.brakeTorque = torque;
		}
	}

	public class JointRestrictions
	{
		public ConfigurableJointMotion motionX;

		public ConfigurableJointMotion motionY;

		public ConfigurableJointMotion motionZ;

		public ConfigurableJointMotion angularMotionX;

		public ConfigurableJointMotion angularMotionY;

		public ConfigurableJointMotion angularMotionZ;

		public void Get(ConfigurableJoint configurableJoint)
		{
			motionX = configurableJoint.xMotion;
			motionY = configurableJoint.yMotion;
			motionZ = configurableJoint.zMotion;
			angularMotionX = configurableJoint.angularXMotion;
			angularMotionY = configurableJoint.angularYMotion;
			angularMotionZ = configurableJoint.angularZMotion;
		}

		public void Set(ConfigurableJoint configurableJoint)
		{
			configurableJoint.xMotion = motionX;
			configurableJoint.yMotion = motionY;
			configurableJoint.zMotion = motionZ;
			configurableJoint.angularXMotion = angularMotionX;
			configurableJoint.angularYMotion = angularMotionY;
			configurableJoint.angularZMotion = angularMotionZ;
		}

		public void Reset(ConfigurableJoint configurableJoint)
		{
			configurableJoint.xMotion = ConfigurableJointMotion.Free;
			configurableJoint.yMotion = ConfigurableJointMotion.Free;
			configurableJoint.zMotion = ConfigurableJointMotion.Free;
			configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
			configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
			configurableJoint.angularZMotion = ConfigurableJointMotion.Free;
		}
	}

	private RCC_CarControllerV3 carController;

	private Rigidbody rigid;

	private ConfigurableJoint joint;

	public Transform COM;

	private bool isSleeping;

	public TrailerWheel[] trailerWheels;

	public bool attached;

	public JointRestrictions jointRestrictions = new JointRestrictions();

	private void Start()
	{
		rigid = GetComponent<Rigidbody>();
		joint = GetComponentInParent<ConfigurableJoint>();
		jointRestrictions.Get(joint);
		rigid.interpolation = RigidbodyInterpolation.None;
		rigid.interpolation = RigidbodyInterpolation.Interpolate;
		joint.configuredInWorldSpace = true;
		if ((bool)joint.connectedBody)
		{
			AttachTrailer(joint.connectedBody.gameObject.GetComponent<RCC_CarControllerV3>());
			return;
		}
		carController = null;
		joint.connectedBody = null;
		jointRestrictions.Reset(joint);
	}

	private void FixedUpdate()
	{
		attached = joint.connectedBody;
		rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);
		if ((bool)carController)
		{
			for (int i = 0; i < trailerWheels.Length; i++)
			{
				trailerWheels[i].Torque(carController.throttleInput * (attached ? 1f : 0f));
				trailerWheels[i].Brake(attached ? 0f : 5000f);
			}
		}
	}

	private void Update()
	{
		if (rigid.velocity.magnitude < 0.01f && Mathf.Abs(rigid.angularVelocity.magnitude) < 0.01f)
		{
			isSleeping = true;
		}
		else
		{
			isSleeping = false;
		}
		for (int i = 0; i < trailerWheels.Length; i++)
		{
			trailerWheels[i].Torque(carController.throttleInput * (attached ? 1f : 0f));
			trailerWheels[i].Brake(attached ? 0f : 5000f);
		}
		WheelAlign();
	}

	public void WheelAlign()
	{
		if (isSleeping)
		{
			return;
		}
		for (int i = 0; i < trailerWheels.Length; i++)
		{
			if (!trailerWheels[i].wheelModel)
			{
				Debug.LogError(transform.name + " wheel of the " + transform.name + " is missing wheel model. This wheel is disabled");
				enabled = false;
				break;
			}
			Vector3 pos = Vector3.zero;
			Quaternion quat = Quaternion.identity;
			trailerWheels[i].wheelCollider.GetWorldPose(out pos, out quat);
			trailerWheels[i].wheelModel.transform.position = pos;
			trailerWheels[i].wheelModel.transform.rotation = quat;
		}
	}

	public void DetachTrailer()
	{
		carController = null;
		joint.connectedBody = null;
		jointRestrictions.Reset(joint);
		if ((bool)RCC_SceneManager.Instance.activePlayerCamera)
		{
			StartCoroutine(RCC_SceneManager.Instance.activePlayerCamera.AutoFocus());
		}
	}

	public void AttachTrailer(RCC_CarControllerV3 vehicle)
	{
		carController = vehicle;
		joint.connectedBody = vehicle.rigid;
		jointRestrictions.Set(joint);
		vehicle.attachedTrailer = this;
		if ((bool)RCC_SceneManager.Instance.activePlayerCamera)
		{
			StartCoroutine(RCC_SceneManager.Instance.activePlayerCamera.AutoFocus(transform, carController.transform));
		}
	}

	private void OnTriggerEnter(Collider col)
	{
	}
}
