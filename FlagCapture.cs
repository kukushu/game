using System;
using System.Collections;
using UnityEngine;

public class FlagCapture : MonoBehaviour
{
	public RectTransform rectT;

	public GameObject FlagCreatorUI;

	public RegionUI regionUI;

	public Texture2D defaultFlag;

	public IEnumerator CaptureNow()
	{
		yield return new WaitForEndOfFrame();
		Vector3[] array = new Vector3[4];
		rectT.GetWorldCorners(array);
		float x = array[0].x;
		float y = array[0].y;
		int num = (int)array[3].x - (int)array[0].x;
		int num2 = (int)array[1].y - (int)array[0].y;
		Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGB24, mipChain: false);
		texture2D.ReadPixels(new Rect(x, y, num, num2), 0, 0);
		texture2D.Apply();
		byte[] inArray = texture2D.EncodeToPNG();
		UnityEngine.Object.Destroy(texture2D);
		string s = Convert.ToBase64String(inArray);
		Texture2D texture2D2 = new Texture2D(1, 1, TextureFormat.ARGB32, mipChain: false);
		texture2D2.LoadImage(Convert.FromBase64String(s));
		DataRegion.FlagTexture = texture2D2;
		ES3.SaveImage(texture2D2, DataRegion.nationName + "Flag.png");
		FlagCreatorUI.SetActive(value: false);
		yield return new WaitForEndOfFrame();
		regionUI.SetFlags();
	}

	public void CreateDefaultFlag()
	{
		DataRegion.FlagTexture = defaultFlag;
		ES3.SaveImage(defaultFlag, DataRegion.nationName + "Flag.png");
		FlagCreatorUI.SetActive(value: false);
		regionUI.SetFlags();
	}

	public void Capture()
	{
		StartCoroutine(CaptureNow());
	}
}
