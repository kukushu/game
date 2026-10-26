using System;
using System.Collections;
using UnityEngine;

public class HelicopterController : MonoBehaviour
{
	public World w;

	public AudioSource HelicopterSound;

	public ControlPanel ControlPanel;

	public Rigidbody HelicopterModel;

	public HeliRotorController MainRotorController;

	public HeliRotorController SubRotorController;

	public float TurnForce = 3f;

	public float ForwardForce = 10f;

	public float ForwardTiltForce = 20f;

	public float TurnTiltForce = 30f;

	public float BaseHeight = 100f;

	public float EffectiveHeight = 100f;

	public float turnTiltForcePercent = 1.5f;

	public float turnForcePercent = 1.3f;

	public float _engineForce;

	public float maxEngineForce = 70f;

	private Vector2 hMove = Vector2.zero;

	private Vector2 hTilt = Vector2.zero;

	private float hTurn;

	public bool IsOnGround = true;

	public float EngineForce
	{
		get
		{
			return _engineForce;
		}
		set
		{
			MainRotorController.RotarSpeed = value * 80f;
			SubRotorController.RotarSpeed = value * 40f;
			HelicopterSound.pitch = Mathf.Clamp(value / 50f, 0f, 1.4f);
			_engineForce = value;
		}
	}

	private void Start()
	{
		ControlPanel controlPanel = ControlPanel;
		controlPanel.KeyPressed = (Action<PressedKeyCode[]>)Delegate.Combine(controlPanel.KeyPressed, new Action<PressedKeyCode[]>(OnKeyPressed));
	}

	private void FixedUpdate()
	{
		LiftProcess();
		MoveProcess();
		TiltProcess();
		EffectiveHeight = BaseHeight + 0.5f * HelicopterModel.transform.position.y;
	}

	private void MoveProcess()
	{
		float b = TurnForce * Mathf.Lerp(hMove.x, hMove.x * (turnTiltForcePercent - Mathf.Abs(hMove.y)), Mathf.Max(0f, hMove.y));
		hTurn = Mathf.Lerp(hTurn, b, 0.03f * TurnForce);
		HelicopterModel.AddRelativeTorque(0f, hTurn * HelicopterModel.mass, 0f);
		HelicopterModel.AddRelativeForce(Vector3.forward * Mathf.Max(0f, hMove.y * ForwardForce * HelicopterModel.mass));
	}

	private void LiftProcess()
	{
		float t = 1f - Mathf.Clamp(HelicopterModel.transform.position.y / EffectiveHeight, 0f, 1f);
		t = Mathf.Lerp(0f, EngineForce, t) * HelicopterModel.mass;
		HelicopterModel.AddRelativeForce(Vector3.up * t);
	}

	private void TiltProcess()
	{
		hTilt.x = Mathf.Lerp(hTilt.x, hMove.x * TurnTiltForce, Time.deltaTime);
		hTilt.y = Mathf.Lerp(hTilt.y, hMove.y * ForwardTiltForce, Time.deltaTime);
		HelicopterModel.transform.localRotation = Quaternion.Euler(hTilt.y, HelicopterModel.transform.localEulerAngles.y, 0f - hTilt.x);
	}

	private void OnKeyPressed(PressedKeyCode[] obj)
	{
		float num = 0f;
		float num2 = 0f;
		if (hMove.y > 0f)
		{
			num = -0.03f;
		}
		else if (hMove.y < 0f)
		{
			num = 0.03f;
		}
		if (hMove.x > 0f)
		{
			num2 = -0.03f;
		}
		else if (hMove.x < 0f)
		{
			num2 = 0.03f;
		}
		for (int i = 0; i < obj.Length; i++)
		{
			switch (obj[i])
			{
			case PressedKeyCode.SpeedUpPressed:
				EngineForce += 0.15f;
				if (EngineForce > maxEngineForce)
				{
					EngineForce = maxEngineForce;
				}
				break;
			case PressedKeyCode.SpeedDownPressed:
				EngineForce -= 0.3f;
				if (EngineForce < 0f)
				{
					EngineForce = 0f;
				}
				break;
			case PressedKeyCode.ForwardPressed:
				if (!IsOnGround)
				{
					num = 0.03f;
				}
				break;
			case PressedKeyCode.BackPressed:
				if (!IsOnGround)
				{
					num = -0.03f;
				}
				break;
			case PressedKeyCode.LeftPressed:
				if (!IsOnGround)
				{
					num2 = -0.03f;
				}
				break;
			case PressedKeyCode.RightPressed:
				if (!IsOnGround)
				{
					num2 = 0.03f;
				}
				break;
			case PressedKeyCode.TurnRightPressed:
				if (!IsOnGround)
				{
					float y2 = (turnForcePercent - Mathf.Abs(hMove.y)) * HelicopterModel.mass;
					HelicopterModel.AddRelativeTorque(0f, y2, 0f);
				}
				break;
			case PressedKeyCode.TurnLeftPressed:
				if (!IsOnGround)
				{
					float y = (0f - (turnForcePercent - Mathf.Abs(hMove.y))) * HelicopterModel.mass;
					HelicopterModel.AddRelativeTorque(0f, y, 0f);
				}
				break;
			}
		}
		hMove.x += num2;
		hMove.x = Mathf.Clamp(hMove.x, -1f, 1f);
		hMove.y += num;
		hMove.y = Mathf.Clamp(hMove.y, -1f, 1f);
	}

	private void OnCollisionEnter(Collision collision)
	{
		float magnitude = collision.relativeVelocity.magnitude;
		if (magnitude > 1f)
		{
			w.buldoze.sm.Play("Impact");
			if (!(magnitude > 2f))
			{
				return;
			}
			GameObject gameObject = collision.collider.gameObject;
			if (gameObject != null)
			{
				BlockData ifNotNull = w.GetIfNotNull((int)gameObject.transform.position.x, (int)gameObject.transform.position.z);
				if (ifNotNull != null && ifNotNull.lO != null && !ifNotNull.lO.isRubles && ifNotNull.lO.Stage > 0)
				{
					ifNotNull.lO.negatives = 10f;
					GameObject gameObject2 = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Construction/Fire"));
					gameObject2.transform.position = new Vector3(ifNotNull.lO.Anchor.x, ifNotNull.lO.wP.y + 0.504f, ifNotNull.lO.Anchor.z);
					FireCoroutine component = gameObject2.GetComponent<FireCoroutine>();
					component.lotOnFire = ifNotNull.lO;
					foreach (ServiceCreator service in w.Services)
					{
						if (service.type == ServiceCreator.Type.Fire && service.carsOut < service.Stage)
						{
							w.indexer.zoning.lm.traffic.SendSpecialVehicleOnPath(service, ifNotNull.lO, "Vehicles/FireEngine", component);
							return;
						}
					}
				}
			}
			((GameObject)UnityEngine.Object.Instantiate(Resources.Load("Construction/Explosion"))).transform.position = base.gameObject.transform.position;
			StartCoroutine(ExplodeAndQuit());
		}
		else
		{
			IsOnGround = true;
		}
	}

	private void OnCollisionExit()
	{
		IsOnGround = false;
	}

	private IEnumerator ExplodeAndQuit()
	{
		yield return new WaitForSeconds(2f);
		w.indexer.ExitCopterMode();
	}
}
