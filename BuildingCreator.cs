using UnityEngine;

public class BuildingCreator : MonoBehaviour
{
	public static void CreateBuilding(Vector3 pos, GameObject parent, int stage)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(stage switch
		{
			1 => "Residential/RS1", 
			2 => "Residential/RS2", 
			3 => "Residential/RS3", 
			4 => "Residential/RS4", 
			5 => "Residential/RS5", 
			6 => "Residential/RS6", 
			7 => "Residential/RS7", 
			11 => "Residential/RS1F", 
			12 => "Residential/RS2F", 
			13 => "Residential/RS3F", 
			14 => "Residential/RS4F", 
			15 => "Residential/RS5F", 
			16 => "Residential/RS6F", 
			17 => "Residential/RS7F", 
			_ => "Residential/RS2", 
		}));
		gameObject.transform.position = new Vector3(pos.x, pos.y + 0.49f, pos.z);
		gameObject.transform.SetParent(parent.transform);
	}

	public static void CreateTree(Vector3 pos, GameObject parent, int cZ, float unrounded)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(GetRegionClimate(pos, cZ, unrounded) switch
		{
			0 => "Trees/RegionTrop", 
			1 => "Trees/RegionTemp", 
			_ => "Trees/RegionArid", 
		}));
		int num = Random.Range(0, 360);
		Vector2 vector = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
		gameObject.transform.rotation = Quaternion.Euler(0f, num, 0f);
		gameObject.transform.position = new Vector3(pos.x + vector.x, pos.y + 0.49f, pos.z + vector.y);
		gameObject.transform.SetParent(parent.transform);
	}

	private static int GetRegionClimate(Vector3 pos, float cZ, float unrounded)
	{
		float num = Mathf.Clamp((unrounded * 8f - 240f) * 3.33f, 0f, 100f);
		float num2 = Mathf.Clamp((cZ * 40f + pos.z) * 8f / 25.6f, 0f, 100f);
		if (num + num2 * 1.25f > 100f)
		{
			return 1;
		}
		if (num + 20f > num2)
		{
			return 2;
		}
		return 0;
	}
}
