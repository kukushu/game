using UnityEngine;
using UnityEngine.EventSystems;

public class RegionInteraction : MonoBehaviour
{
	public Vector3 pos;

	public int x1;

	public int z1;

	public int x2;

	public int z2;

	public int oldx1;

	public int oldz1;

	public int oldx2;

	public int oldz2;

	public WorldRegion region;

	public GameObject citySelector;

	public CityTool cityTool;

	public SoundManager sm;

	public RTS_Camera cam;

	public bool censusOpen;

	private void Update()
	{
		if (EventSystem.current.IsPointerOverGameObject() || !Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hitInfo, 500f) || !WorldRegion.coroutineChunks.TryGetValue(hitInfo.collider.gameObject.name, out var value))
		{
			return;
		}
		Vector3 vector = ((!Input.GetMouseButtonDown(0)) ? (hitInfo.point + hitInfo.normal / 2f) : (hitInfo.point - hitInfo.normal / 2f));
		vector.y = 16f;
		BlockRegion worldBlock = WorldRegion.GetWorldBlock(vector);
		if (worldBlock == null)
		{
			return;
		}
		pos = new Vector3(worldBlock.worldPosition.x, worldBlock.heightPos, worldBlock.worldPosition.z);
		if (!censusOpen)
		{
			value = worldBlock.owner;
			if (citySelector == null)
			{
				return;
			}
			citySelector.transform.position = new Vector3(value.city.pos.x + (float)(WorldRegion.chunkSize / 2) - 0.5f, 30f, value.city.pos.z + (float)(WorldRegion.chunkSize / 2) - 0.5f);
			if (Input.GetMouseButtonDown(0))
			{
				censusOpen = true;
				DataRegion.selectedCity = value.city;
				cityTool.gameObject.SetActive(value: true);
				cityTool.UpdateTexts();
				sm.Play("LightClick");
				x1 = (int)pos.x;
				z1 = (int)pos.z;
			}
		}
		x2 = (int)pos.x;
		z2 = (int)pos.z;
	}

	public void ExitToDesktop()
	{
		Application.Quit();
	}
}
