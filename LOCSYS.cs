using System.Collections.Generic;

public class LOCSYS
{
	public enum Language
	{
		English,
		French,
		Brazilian,
		Spanish,
		Japanese,
		Thai,
		Chinese
	}

	public static Language language;

	private static Dictionary<string, string> localisedEN;

	private static Dictionary<string, string> localisedFR;

	private static Dictionary<string, string> localisedPT;

	private static Dictionary<string, string> localisedES;

	private static Dictionary<string, string> localisedJA;

	private static Dictionary<string, string> localisedTH;

	private static Dictionary<string, string> localisedCN;

	public static bool isInit;

	public static void Init()
	{
		CSVLoader cSVLoader = new CSVLoader();
		cSVLoader.LoadCSV("localisation");
		localisedEN = cSVLoader.GetDictionaryValues("en");
		localisedFR = cSVLoader.GetDictionaryValues("fr");
		localisedPT = cSVLoader.GetDictionaryValues("pt");
		localisedES = cSVLoader.GetDictionaryValues("es");
		localisedJA = cSVLoader.GetDictionaryValues("ja");
		localisedTH = cSVLoader.GetDictionaryValues("th");
		localisedCN = cSVLoader.GetDictionaryValues("zh-CN");
		isInit = true;
	}

	public static string Translate(string key)
	{
		if (!isInit)
		{
			Init();
		}
		string value = key;
		switch (language)
		{
		case Language.English:
			localisedEN.TryGetValue(key, out value);
			break;
		case Language.French:
			localisedFR.TryGetValue(key, out value);
			break;
		case Language.Brazilian:
			localisedPT.TryGetValue(key, out value);
			break;
		case Language.Spanish:
			localisedES.TryGetValue(key, out value);
			break;
		case Language.Japanese:
			localisedJA.TryGetValue(key, out value);
			break;
		case Language.Thai:
			localisedTH.TryGetValue(key, out value);
			break;
		case Language.Chinese:
			localisedCN.TryGetValue(key, out value);
			break;
		}
		return value;
	}
}
