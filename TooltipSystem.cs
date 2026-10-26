using UnityEngine;

public class TooltipSystem : MonoBehaviour
{
	private static TooltipSystem current;

	public Tooltip tooltip;

	public void Awake()
	{
		current = this;
	}

	public static void Show(string content, string header = "", int Capacity = 0, int Cost = 0)
	{
		content = LOCSYS.Translate(content);
		header = LOCSYS.Translate(header);
		string details = ((Capacity == 0) ? "" : (LOCSYS.Translate("Capacity") + " " + Capacity.ToString("N0") + "  -  " + LOCSYS.Translate("mcost") + " $" + Cost.ToString("N0")));
		current.tooltip.UpdatePos();
		current.tooltip.SetText(content, header, details);
		current.tooltip.gameObject.SetActive(value: true);
	}

	public static void Hide()
	{
		if (!(current == null))
		{
			current.tooltip.gameObject.SetActive(value: false);
		}
	}
}
