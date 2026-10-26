using Steamworks;
using UnityEngine;

public class SteamAchievements : MonoBehaviour
{
	public static SteamAchievements script;

	private bool unlockTest;

	private void Awake()
	{
		script = this;
		if (!SteamManager.Initialized)
		{
			gameObject.SetActive(value: false);
			Debug.Log("Steam manager not initialized! Can't test achivements!");
		}
	}

	public void UnlockAchievement(string ID)
	{
		if (!SteamManager.Initialized)
		{
			gameObject.SetActive(value: false);
			return;
		}
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
