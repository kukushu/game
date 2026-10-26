using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextLocaliser : MonoBehaviour
{
	private TextMeshProUGUI textField;

	public string key;

	private void Start()
	{
		textField = GetComponent<TextMeshProUGUI>();
		string text = LOCSYS.Translate(key);
		textField.text = text;
	}
}
