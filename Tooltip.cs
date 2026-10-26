using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
	public TextMeshProUGUI headerField;

	public TextMeshProUGUI contentField;

	public TextMeshProUGUI detailsField;

	public LayoutElement layout;

	public int characterMax;

	public RectTransform rectTransform;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	public void SetText(string content, string header = "", string details = "")
	{
		if (string.IsNullOrEmpty(header))
		{
			headerField.gameObject.SetActive(value: false);
		}
		else
		{
			headerField.gameObject.SetActive(value: true);
			headerField.text = header;
		}
		if (string.IsNullOrEmpty(details))
		{
			detailsField.gameObject.SetActive(value: false);
		}
		else
		{
			detailsField.gameObject.SetActive(value: true);
			detailsField.text = details;
		}
		contentField.text = content;
		int length = headerField.text.Length;
		int length2 = contentField.text.Length;
		int length3 = detailsField.text.Length;
		layout.enabled = length > characterMax || length2 > characterMax || length3 > characterMax;
	}

	public void UpdatePos()
	{
		float num = Screen.currentResolution.height;
		Vector2 vector = Input.mousePosition;
		if (vector.y < (float)(Screen.currentResolution.height / 2))
		{
			vector.y += num / 250f + 80f;
		}
		else
		{
			vector.y -= num / 250f + 80f;
		}
		float x = vector.x / (float)Screen.width;
		float y = vector.y / (float)Screen.height;
		rectTransform.pivot = new Vector2(x, y);
		transform.position = vector;
	}
}
