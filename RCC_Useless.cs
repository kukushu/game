using UnityEngine;
using UnityEngine.UI;

public class RCC_Useless : MonoBehaviour
{
	public enum Useless
	{
		MainController,
		MobileControllers,
		Behavior,
		Graphics
	}

	public Useless useless;

	private void Awake()
	{
		int value = 0;
		if (useless == Useless.Behavior)
		{
			value = RCC_Settings.Instance.behaviorSelectedIndex;
		}
		if (useless == Useless.MainController)
		{
			value = RCC_Settings.Instance.controllerSelectedIndex;
		}
		if (useless == Useless.MobileControllers)
		{
			switch (RCC_Settings.Instance.mobileController)
			{
			case RCC_Settings.MobileController.TouchScreen:
				value = 0;
				break;
			case RCC_Settings.MobileController.Gyro:
				value = 1;
				break;
			case RCC_Settings.MobileController.SteeringWheel:
				value = 2;
				break;
			case RCC_Settings.MobileController.Joystick:
				value = 3;
				break;
			}
		}
		if (useless == Useless.Graphics)
		{
			value = QualitySettings.GetQualityLevel();
		}
		GetComponent<Dropdown>().value = value;
		GetComponent<Dropdown>().RefreshShownValue();
	}
}
