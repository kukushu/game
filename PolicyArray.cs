using System.Globalization;
using UnityEngine;

public class PolicyArray : MonoBehaviour
{
	public TextAsset csvFile;

	public string[,] grid;

	public int totalLines;

	private void Start()
	{
		if (grid == null)
		{
			grid = getCSVGrid(csvFile.text);
		}
	}

	public string[,] getCSVGrid(string csvText)
	{
		string[] array = csvText.Split("\n"[0]);
		totalLines = array.Length;
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(';');
			num = Mathf.Max(num, array2.Length);
		}
		string[,] array3 = new string[num + 1, array.Length + 1];
		for (int j = 0; j < array.Length; j++)
		{
			string[] array4 = array[j].Split(';');
			for (int k = 0; k < array4.Length; k++)
			{
				array3[k + 1, j + 1] = array4[k];
			}
		}
		return array3;
	}

	public string getValueAtIndex(int x, int y)
	{
		return grid[x, y];
	}

	public void setValueAtIndex(int x, int y, int value)
	{
		grid[x, y] = value.ToString();
	}

	public void recordValue(int x, int y, float value)
	{
		float num = Mathf.Round(value * 100f) / 100f;
		grid[x, y] = num.ToString(CultureInfo.InvariantCulture);
	}
}
