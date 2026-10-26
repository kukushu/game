using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public string content;

	public string header;

	public int capacity;

	public int cost;

	private Coroutine co;

	public void OnPointerEnter(PointerEventData eventData)
	{
		TooltipSystem.Show(content, header, capacity, cost);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		TooltipSystem.Hide();
	}

	public void OnDisable()
	{
		TooltipSystem.Hide();
	}

	private IEnumerator OnEnter()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		TooltipSystem.Show(content, header, capacity, cost);
	}
}
