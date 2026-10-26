using UnityEngine;

public static class SettingsStatic
{
	public static bool firstTime = true;

	public static bool tutorial = true;

	public static bool redAlerts = true;

	public static bool blueInfo = true;

	public static bool edgeScroll = true;

	public static float mouseSense = 1.5f;

	public static float keyboardSense = 1f;

	public static bool azerty = false;

	public static int[] graphicsOption;

	public static int maxCars = 10000;

	public static int maxPeds = 10000;

	public static int carDensity = 4;

	public static float pedsDensity = 1f;

	public static int propsDistance = 200;

	public static int carsDistance = 250;

	public static int pedsDistance = 50;

	public static int resolutionWidth = 1920;

	public static int reolutionHeight = 1080;

	public static int smoothLevel = 30;

	public static float UIVolume = 0.5f;

	public static float MusicVolume = 0.2f;

	public static float CityVolume = 0.5f;

	public static float NatureVolume = 0.5f;

	public static float Carsvolume = 0.2f;

	public static void SaveSettings()
	{
		firstTime = false;
		ES3.Save("S0", firstTime, "settings.es3");
		ES3.Save("S1", tutorial, "settings.es3");
		ES3.Save("S2", redAlerts, "settings.es3");
		ES3.Save("S3", blueInfo, "settings.es3");
		ES3.Save("S4", graphicsOption, "settings.es3");
		ES3.Save("S5", maxCars, "settings.es3");
		ES3.Save("S6", maxPeds, "settings.es3");
		ES3.Save("S7", carDensity, "settings.es3");
		ES3.Save("S8", pedsDensity, "settings.es3");
		ES3.Save("S10", propsDistance, "settings.es3");
		ES3.Save("S11", carsDistance, "settings.es3");
		ES3.Save("S12", pedsDistance, "settings.es3");
		ES3.Save("S13", resolutionWidth, "settings.es3");
		ES3.Save("S14", reolutionHeight, "settings.es3");
		ES3.Save("S15", MusicVolume, "settings.es3");
		ES3.Save("S16", CityVolume, "settings.es3");
		ES3.Save("S17", Carsvolume, "settings.es3");
		ES3.Save("S18", NatureVolume, "settings.es3");
		ES3.Save("S19", keyboardSense, "settings.es3");
		ES3.Save("S20", mouseSense, "settings.es3");
		ES3.Save("S21", edgeScroll, "settings.es3");
		ES3.Save("S22", azerty, "settings.es3");
		ES3.Save("S23", smoothLevel, "settings.es3");
		ES3.Save("S24", UIVolume, "settings.es3");
	}

	public static void LoadSettings()
	{
		if (ES3.FileExists("settings.es3"))
		{
			firstTime = false;
			tutorial = ES3.Load("S1", "settings.es3", tutorial);
			redAlerts = ES3.Load("S2", "settings.es3", redAlerts);
			blueInfo = ES3.Load("S3", "settings.es3", blueInfo);
			graphicsOption = ES3.Load("S4", "settings.es3", graphicsOption);
			maxCars = ES3.Load("S5", "settings.es3", maxCars);
			maxPeds = ES3.Load("S6", "settings.es3", maxPeds);
			carDensity = ES3.Load("S7", "settings.es3", carDensity);
			pedsDensity = ES3.Load("S8", "settings.es3", pedsDensity);
			propsDistance = ES3.Load("S10", "settings.es3", propsDistance);
			carsDistance = ES3.Load("S11", "settings.es3", carsDistance);
			pedsDistance = ES3.Load("S12", "settings.es3", pedsDistance);
			resolutionWidth = ES3.Load("S13", "settings.es3", resolutionWidth);
			reolutionHeight = ES3.Load("S14", "settings.es3", reolutionHeight);
			MusicVolume = ES3.Load("S15", "settings.es3", MusicVolume);
			CityVolume = ES3.Load("S16", "settings.es3", CityVolume);
			Carsvolume = ES3.Load("S17", "settings.es3", Carsvolume);
			NatureVolume = ES3.Load("S18", "settings.es3", NatureVolume);
			keyboardSense = ES3.Load("S19", "settings.es3", keyboardSense);
			mouseSense = ES3.Load("S20", "settings.es3", mouseSense);
			edgeScroll = ES3.Load("S21", "settings.es3", edgeScroll);
			azerty = ES3.Load("S22", "settings.es3", azerty);
			smoothLevel = ES3.Load("S23", "settings.es3", smoothLevel);
			UIVolume = ES3.Load("S24", "settings.es3", UIVolume);
		}
		else
		{
			graphicsOption = new int[20];
			Debug.Log("First time. Applying Default Settings");
			firstTime = true;
		}
	}
}
