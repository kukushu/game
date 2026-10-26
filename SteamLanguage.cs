using Steamworks;
using UnityEngine;

public class SteamLanguage : MonoBehaviour
{
	public static SteamLanguage script;

	private bool unlockTest;

	private void Awake()
	{
		script = this;
		if (!SteamManager.Initialized)
		{
			gameObject.SetActive(value: false);
		}
	}

	public void GetSteamLanguageCode()
	{
		string currentGameLanguage = SteamApps.GetCurrentGameLanguage();
		Debug.Log(currentGameLanguage);
		switch (currentGameLanguage)
		{
		case "english":
			LOCSYS.language = LOCSYS.Language.English;
			break;
		case "french":
			LOCSYS.language = LOCSYS.Language.French;
			break;
		case "japanese":
			LOCSYS.language = LOCSYS.Language.Japanese;
			break;
		case "brazilian":
		case "portuguese":
			LOCSYS.language = LOCSYS.Language.Brazilian;
			break;
		case "spanish":
		case "latam":
			LOCSYS.language = LOCSYS.Language.Spanish;
			break;
		case "thai":
			LOCSYS.language = LOCSYS.Language.Thai;
			break;
		case "schinese":
		case "tchinese":
			LOCSYS.language = LOCSYS.Language.Chinese;
			break;
		default:
			LOCSYS.language = LOCSYS.Language.English;
			break;
		}
	}

	public void UnlockAchievement(string ID)
	{
		CheckLock(ID);
		if (!unlockTest)
		{
			SteamUserStats.SetAchievement(ID);
			SteamUserStats.StoreStats();
		}
	}

	private void CheckLock(string ID)
	{
		SteamUserStats.GetAchievement(ID, out unlockTest);
	}
}
