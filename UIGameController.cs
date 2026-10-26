using UnityEngine;
using UnityEngine.UI;

public class UIGameController : MonoBehaviour
{
	public Text EngineForceView;

	public GameObject RestartButton;

	public GameObject InfoButton;

	public GameObject InfoPanel;

	public static UIGameController runtime;

	private void Awake()
	{
		runtime = this;
	}

	private void Start()
	{
		ShowInfo();
	}

	private void Update()
	{
	}

	private void ShowInfoPanel(bool isShow)
	{
		EngineForceView.gameObject.SetActive(!isShow);
		RestartButton.SetActive(!isShow);
		InfoButton.SetActive(!isShow);
		InfoPanel.SetActive(isShow);
	}

	public void ShowInfo()
	{
		ShowInfoPanel(isShow: true);
	}

	public void HideInfo()
	{
		ShowInfoPanel(isShow: false);
	}

	public void RestartGame()
	{
	}
}
