using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOver : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	public TextMeshProUGUI line1;

	public TextMeshProUGUI line2;

	public bool isUsage;

	public bool isInteger;

	public float data;

	public float fadeTime = 2f;

	public Color32 positiveColor;

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (isInteger)
		{
			line1.text = data.ToString("N0") + "%";
		}
		else
		{
			line1.text = data.ToString("+#;-#;+0");
		}
		if (isUsage)
		{
			if (data < 98f)
			{
				line1.color = positiveColor;
			}
			else
			{
				line1.color = new Color32(181, 118, 118, byte.MaxValue);
			}
		}
		else if (data < 0f)
		{
			line1.color = new Color32(181, 118, 118, byte.MaxValue);
		}
		else
		{
			line1.color = positiveColor;
		}
		StartCoroutine(FadeOut());
		if (line2 != null)
		{
			line2.color = new Color(line2.color.r, line2.color.g, line2.color.b, 1f);
			StartCoroutine(FadeOutSecondary());
		}
	}

	private IEnumerator FadeOut()
	{
		yield return new WaitForSecondsRealtime(fadeTime / 2f);
		while (line1.color.a > 0f)
		{
			float a = line1.color.a;
			line1.color = new Color(line1.color.r, line1.color.g, line1.color.b, a - Time.fixedDeltaTime / fadeTime);
			yield return null;
		}
	}

	private IEnumerator FadeOutSecondary()
	{
		yield return new WaitForSecondsRealtime(fadeTime / 2f);
		while (line2.color.a > 0f)
		{
			float a = line2.color.a;
			line2.color = new Color(line2.color.r, line2.color.g, line2.color.b, a - Time.fixedDeltaTime / fadeTime);
			yield return null;
		}
	}
}
