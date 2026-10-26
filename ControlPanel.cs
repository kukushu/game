using System;
using System.Collections.Generic;
using UnityEngine;

public class ControlPanel : MonoBehaviour
{
	[SerializeField]
	private KeyCode SpeedUp = KeyCode.Space;

	[SerializeField]
	private KeyCode SpeedDown = KeyCode.C;

	[SerializeField]
	private KeyCode Forward = KeyCode.W;

	[SerializeField]
	private KeyCode Back = KeyCode.S;

	[SerializeField]
	private KeyCode Left = KeyCode.A;

	[SerializeField]
	private KeyCode Right = KeyCode.D;

	[SerializeField]
	private KeyCode TurnLeft = KeyCode.Q;

	[SerializeField]
	private KeyCode TurnRight = KeyCode.E;

	private KeyCode[] keyCodes;

	public Action<PressedKeyCode[]> KeyPressed;

	private void Awake()
	{
		keyCodes = new KeyCode[8] { SpeedUp, SpeedDown, Forward, Back, Left, Right, TurnLeft, TurnRight };
	}

	private void Start()
	{
		if (SettingsStatic.azerty)
		{
			Forward = KeyCode.Z;
			TurnLeft = KeyCode.A;
			Left = KeyCode.Q;
		}
		else
		{
			Forward = KeyCode.W;
			TurnLeft = KeyCode.Q;
			Left = KeyCode.A;
		}
	}

	private void FixedUpdate()
	{
		List<PressedKeyCode> list = new List<PressedKeyCode>();
		for (int i = 0; i < keyCodes.Length; i++)
		{
			if (Input.GetKey(keyCodes[i]))
			{
				list.Add((PressedKeyCode)i);
			}
		}
		if (KeyPressed != null)
		{
			KeyPressed(list.ToArray());
		}
	}
}
