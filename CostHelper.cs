using TMPro;
using UnityEngine;

public class CostHelper : MonoBehaviour
{
	public GameObject go;

	public TextMeshProUGUI CostText;

	public Budget budget;

	public SoundManager sm;

	private int Cost;

	public bool CanAfford;

	public Color white;

	public Color red;

	public void SetCost(int c)
	{
		gameObject.SetActive(value: true);
		transform.position = Input.mousePosition + new Vector3(250f, 0f, 250f);
		CostText.text = "$" + c.ToString("N0");
		if ((float)c > budget.Balance)
		{
			CostText.color = red;
			CanAfford = false;
		}
		else
		{
			CostText.color = white;
			CanAfford = true;
		}
		Cost = c;
	}

	public void PayForIt()
	{
		budget.Balance -= Cost;
		Cost = 0;
		budget.UI_Balance.text = "$" + budget.Balance.ToString("N0");
		if (budget.Balance < 1f)
		{
			budget.UI_Balance.color = red;
			sm.Play("NoMoney");
		}
		else
		{
			budget.UI_Balance.color = white;
		}
	}

	public void PayAmount(int amount)
	{
		budget.Balance -= amount;
		budget.UI_Balance.text = "$" + budget.Balance.ToString("N0");
		if (budget.Balance < 1f)
		{
			budget.UI_Balance.color = red;
			sm.Play("NoMoney");
		}
		else
		{
			budget.UI_Balance.color = white;
		}
	}
}
