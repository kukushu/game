using UnityEngine;

public class RCC_DemoVehicles : ScriptableObject
{
	public RCC_CarControllerV3[] vehicles;

	private static RCC_DemoVehicles instance;

	public static RCC_DemoVehicles Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Resources.Load("RCC Assets/RCC_DemoVehicles") as RCC_DemoVehicles;
			}
			return instance;
		}
	}
}
