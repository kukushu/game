using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class CSVLoader
{
	private TextAsset csvFile;

	private char lineSeparator = '\n';

	private char surround = '"';

	private string[] fieldSeperator = new string[1] { ";" };

	public void LoadCSV(string filepath)
	{
		csvFile = Resources.Load<TextAsset>(filepath);
	}

	public Dictionary<string, string> GetDictionaryValues(string attributeId)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string[] array = csvFile.text.Split(lineSeparator);
		int num = -1;
		string[] array2 = array[0].Split(fieldSeperator, StringSplitOptions.None);
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].Contains(attributeId))
			{
				num = i;
				break;
			}
		}
		Regex regex = new Regex(";(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
		for (int j = 1; j < array.Length; j++)
		{
			string input = array[j];
			string[] array3 = regex.Split(input);
			for (int k = 0; k < array3.Length; k++)
			{
				array3[k] = array3[k].TrimStart(' ', surround);
				array3[k] = array3[k].Replace("\"", "");
			}
			if (array3.Length > num)
			{
				string key = array3[0];
				if (!dictionary.ContainsKey(key))
				{
					string value = array3[num];
					dictionary.Add(key, value);
				}
			}
		}
		return dictionary;
	}
}
