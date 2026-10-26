using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/RCC UI Mobile Buttons")]
public class RCC_MobileButtons : RCC_Core
{
	private static RCC_MobileButtons instance;

	private RCC_Settings RCCSettingsInstance;

	public RCC_UIController gasButton;

	public RCC_UIController gradualGasButton;

	public RCC_UIController brakeButton;

	public RCC_UIController leftButton;

	public RCC_UIController rightButton;

	public RCC_UISteeringWheelController steeringWheel;

	public RCC_UIController handbrakeButton;

	public RCC_UIController NOSButton;

	public RCC_UIController NOSButtonSteeringWheel;

	public GameObject gearButton;

	public RCC_UIJoystick joystick;

	public RCC_Inputs inputs = new RCC_Inputs();

	private float throttleInput;

	private float brakeInput;

	private float leftInput;

	private float rightInput;

	private float steeringWheelInput;

	private float handbrakeInput;

	private float boostInput = 1f;

	private float gyroInput;

	private float joystickInput;

	private float horizontalInput;

	private float verticalInput;

	private bool canUseNos;

	private Vector3 orgBrakeButtonPos;

	public static RCC_MobileButtons Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Object.FindObjectOfType<RCC_MobileButtons>();
			}
			return instance;
		}
	}

	private RCC_Settings RCCSettings
	{
		get
		{
			if (RCCSettingsInstance == null)
			{
				RCCSettingsInstance = RCC_Settings.Instance;
				return RCCSettingsInstance;
			}
			return RCCSettingsInstance;
		}
	}

	private void Start()
	{
		if ((bool)brakeButton)
		{
			orgBrakeButtonPos = brakeButton.transform.position;
		}
		CheckController();
	}

	private void OnEnable()
	{
		RCC_SceneManager.OnControllerChanged += CheckController;
		RCC_SceneManager.OnVehicleChanged += CheckController;
	}

	private void CheckController()
	{
		if ((bool)RCC_SceneManager.Instance.activePlayerVehicle)
		{
			if (RCCSettings.selectedControllerType == RCC_Settings.ControllerType.Mobile)
			{
				EnableButtons();
			}
			else
			{
				DisableButtons();
			}
		}
	}

	private void DisableButtons()
	{
		if ((bool)gasButton)
		{
			gasButton.gameObject.SetActive(value: false);
		}
		if ((bool)gradualGasButton)
		{
			gradualGasButton.gameObject.SetActive(value: false);
		}
		if ((bool)leftButton)
		{
			leftButton.gameObject.SetActive(value: false);
		}
		if ((bool)rightButton)
		{
			rightButton.gameObject.SetActive(value: false);
		}
		if ((bool)brakeButton)
		{
			brakeButton.gameObject.SetActive(value: false);
		}
		if ((bool)steeringWheel)
		{
			steeringWheel.gameObject.SetActive(value: false);
		}
		if ((bool)handbrakeButton)
		{
			handbrakeButton.gameObject.SetActive(value: false);
		}
		if ((bool)NOSButton)
		{
			NOSButton.gameObject.SetActive(value: false);
		}
		if ((bool)NOSButtonSteeringWheel)
		{
			NOSButtonSteeringWheel.gameObject.SetActive(value: false);
		}
		if ((bool)gearButton)
		{
			gearButton.gameObject.SetActive(value: false);
		}
		if ((bool)joystick)
		{
			joystick.gameObject.SetActive(value: false);
		}
	}

	private void EnableButtons()
	{
		if ((bool)gasButton)
		{
			gasButton.gameObject.SetActive(value: true);
		}
		if ((bool)leftButton)
		{
			leftButton.gameObject.SetActive(value: true);
		}
		if ((bool)rightButton)
		{
			rightButton.gameObject.SetActive(value: true);
		}
		if ((bool)brakeButton)
		{
			brakeButton.gameObject.SetActive(value: true);
		}
		if ((bool)steeringWheel)
		{
			steeringWheel.gameObject.SetActive(value: true);
		}
		if ((bool)handbrakeButton)
		{
			handbrakeButton.gameObject.SetActive(value: true);
		}
		if (canUseNos)
		{
			if ((bool)NOSButton)
			{
				NOSButton.gameObject.SetActive(value: true);
			}
			if ((bool)NOSButtonSteeringWheel)
			{
				NOSButtonSteeringWheel.gameObject.SetActive(value: true);
			}
		}
		if ((bool)joystick)
		{
			joystick.gameObject.SetActive(value: true);
		}
	}

	private void Update()
	{
		if (RCCSettings.selectedControllerType != RCC_Settings.ControllerType.Mobile)
		{
			return;
		}
		switch (RCCSettings.mobileController)
		{
		case RCC_Settings.MobileController.TouchScreen:
			gyroInput = 0f;
			if ((bool)steeringWheel && steeringWheel.gameObject.activeInHierarchy)
			{
				steeringWheel.gameObject.SetActive(value: false);
			}
			if ((bool)NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
			{
				NOSButton.gameObject.SetActive(canUseNos);
			}
			if ((bool)joystick && joystick.gameObject.activeInHierarchy)
			{
				joystick.gameObject.SetActive(value: false);
			}
			if (!leftButton.gameObject.activeInHierarchy)
			{
				brakeButton.transform.position = orgBrakeButtonPos;
				leftButton.gameObject.SetActive(value: true);
			}
			if (!rightButton.gameObject.activeInHierarchy)
			{
				rightButton.gameObject.SetActive(value: true);
			}
			break;
		case RCC_Settings.MobileController.Gyro:
			gyroInput = Mathf.Lerp(gyroInput, Input.acceleration.x * RCCSettings.gyroSensitivity, Time.deltaTime * 5f);
			brakeButton.transform.position = leftButton.transform.position;
			if ((bool)steeringWheel && steeringWheel.gameObject.activeInHierarchy)
			{
				steeringWheel.gameObject.SetActive(value: false);
			}
			if ((bool)NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
			{
				NOSButton.gameObject.SetActive(canUseNos);
			}
			if ((bool)joystick && joystick.gameObject.activeInHierarchy)
			{
				joystick.gameObject.SetActive(value: false);
			}
			if (leftButton.gameObject.activeInHierarchy)
			{
				leftButton.gameObject.SetActive(value: false);
			}
			if (rightButton.gameObject.activeInHierarchy)
			{
				rightButton.gameObject.SetActive(value: false);
			}
			break;
		case RCC_Settings.MobileController.SteeringWheel:
			gyroInput = 0f;
			if ((bool)steeringWheel && !steeringWheel.gameObject.activeInHierarchy)
			{
				steeringWheel.gameObject.SetActive(value: true);
				brakeButton.transform.position = orgBrakeButtonPos;
			}
			if ((bool)NOSButton && NOSButton.gameObject.activeInHierarchy)
			{
				NOSButton.gameObject.SetActive(value: false);
			}
			if ((bool)NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeInHierarchy != canUseNos)
			{
				NOSButtonSteeringWheel.gameObject.SetActive(canUseNos);
			}
			if ((bool)joystick && joystick.gameObject.activeInHierarchy)
			{
				joystick.gameObject.SetActive(value: false);
			}
			if (leftButton.gameObject.activeInHierarchy)
			{
				leftButton.gameObject.SetActive(value: false);
			}
			if (rightButton.gameObject.activeInHierarchy)
			{
				rightButton.gameObject.SetActive(value: false);
			}
			break;
		case RCC_Settings.MobileController.Joystick:
			gyroInput = 0f;
			if ((bool)steeringWheel && steeringWheel.gameObject.activeInHierarchy)
			{
				steeringWheel.gameObject.SetActive(value: false);
			}
			if ((bool)NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
			{
				NOSButton.gameObject.SetActive(canUseNos);
			}
			if ((bool)joystick && !joystick.gameObject.activeInHierarchy)
			{
				joystick.gameObject.SetActive(value: true);
				brakeButton.transform.position = orgBrakeButtonPos;
			}
			if (leftButton.gameObject.activeInHierarchy)
			{
				leftButton.gameObject.SetActive(value: false);
			}
			if (rightButton.gameObject.activeInHierarchy)
			{
				rightButton.gameObject.SetActive(value: false);
			}
			break;
		}
		throttleInput = GetInput(gasButton) + GetInput(gradualGasButton);
		brakeInput = GetInput(brakeButton);
		leftInput = GetInput(leftButton);
		rightInput = GetInput(rightButton);
		handbrakeInput = GetInput(handbrakeButton);
		boostInput = Mathf.Clamp(GetInput(NOSButton) + GetInput(NOSButtonSteeringWheel), 0f, 1f);
		if ((bool)steeringWheel)
		{
			steeringWheelInput = steeringWheel.input;
		}
		if ((bool)joystick)
		{
			joystickInput = joystick.inputHorizontal;
		}
		FeedRCC();
	}

	private void FeedRCC()
	{
		if ((bool)RCC_SceneManager.Instance.activePlayerVehicle)
		{
			canUseNos = RCC_SceneManager.Instance.activePlayerVehicle.useNOS;
			inputs.throttleInput = throttleInput;
			inputs.brakeInput = brakeInput;
			inputs.steerInput = 0f - leftInput + rightInput + steeringWheelInput + gyroInput + joystickInput;
			inputs.handbrakeInput = handbrakeInput;
			inputs.boostInput = boostInput;
		}
	}

	private float GetInput(RCC_UIController button)
	{
		if (button == null)
		{
			return 0f;
		}
		return button.input;
	}

	private Vector2 GetInput(RCC_UIJoystick joystick)
	{
		if (joystick == null)
		{
			return Vector2.zero;
		}
		return joystick.inputVector;
	}

	private void OnDisable()
	{
		RCC_SceneManager.OnControllerChanged -= CheckController;
		RCC_SceneManager.OnVehicleChanged -= CheckController;
	}
}
