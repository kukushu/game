using System.Collections.Generic;
using UnityEngine;

public class SlumCreator : MonoBehaviour
{
	public Vector3 wP = Vector3.zero;

	public List<Vector2Int> bdList = new List<Vector2Int>();

	private int[] rotation = new int[4] { 0, 90, 180, 270 };

	public int _r;

	public float landValue;

	public GameObject Building;

	public int Occupants;

	public int Stage = 1;

	public string reason = "";

	private string Type = "A";

	public World world;

	private HashSet<Chunk> updates = new HashSet<Chunk>();

	public bool reload;

	public AudioSource audioSource;

	private int clipPlayed;

	private void Start()
	{
		if (reload)
		{
			Reload();
		}
	}

	public void LevelTerrain()
	{
		foreach (Vector2Int bd in bdList)
		{
			BlockData ifNotNull = world.GetIfNotNull(bd.x, bd.y);
			if (ifNotNull == null)
			{
				return;
			}
			ifNotNull.t = BlockData.Type.BUILDING;
			ifNotNull.FlatHeight(wP.y);
			ifNotNull.b = true;
			ifNotNull.uO = this;
			CheckChunks(ifNotNull);
		}
		landValue = Mathf.RoundToInt(world.GetIfNotNull(bdList[0].x, bdList[0].y).v[0]);
		BuildSlum();
	}

	private void BuildSlum()
	{
		string path;
		if (Random.value < 0.5f)
		{
			path = Dico.Slum_A[0];
			Type = "A";
		}
		else
		{
			path = Dico.Slum_B[0];
			Type = "B";
		}
		_r = rotation[Random.Range(0, rotation.Length)];
		Building = (GameObject)Object.Instantiate(Resources.Load(path));
		Building.transform.position = new Vector3(wP.x, wP.y + 0.501f, wP.z);
		Building.transform.Rotate(0f, _r, 0f, Space.Self);
		Building.transform.parent = transform;
		UpdateChunks();
	}

	public void SlumStageUp()
	{
		if (Stage == 1)
		{
			Stage = 2;
			string path = ((!(Type == "A")) ? "Residential/SlumB_2" : "Residential/SlumA_2");
			Object.Destroy(Building);
			Building = (GameObject)Object.Instantiate(Resources.Load(path));
			Building.transform.position = new Vector3(wP.x, wP.y + 0.501f, wP.z);
			Building.transform.Rotate(0f, _r, 0f, Space.Self);
			Building.transform.parent = transform;
		}
		else if (Stage == 2)
		{
			Stage = 3;
			string path2 = ((!(Type == "A")) ? "Residential/SlumB_3" : "Residential/SlumA_3");
			Object.Destroy(Building);
			Building = (GameObject)Object.Instantiate(Resources.Load(path2));
			Building.transform.position = new Vector3(wP.x, wP.y + 0.501f, wP.z);
			Building.transform.Rotate(0f, _r, 0f, Space.Self);
			Building.transform.parent = transform;
		}
		else if (Stage == 3)
		{
			Stage = 4;
			string path3 = ((!(Type == "A")) ? "Residential/SlumB_4" : "Residential/SlumA_4");
			Object.Destroy(Building);
			Building = (GameObject)Object.Instantiate(Resources.Load(path3));
			Building.transform.position = new Vector3(wP.x, wP.y + 0.501f, wP.z);
			Building.transform.Rotate(0f, _r, 0f, Space.Self);
			Building.transform.parent = transform;
		}
		SetAudioClip();
	}

	public void PlayAudioClip()
	{
		clipPlayed++;
		if (clipPlayed > 5)
		{
			SetAudioClip();
			clipPlayed = 0;
		}
		else if (audioSource.enabled)
		{
			audioSource.Play();
		}
	}

	private void SetAudioClip()
	{
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		audioSource.clip = world.la.SetSlumClip(this);
		audioSource.volume = SettingsStatic.CityVolume;
		audioSource.maxDistance = 50f;
		audioSource.spatialBlend = 1f;
		audioSource.rolloffMode = AudioRolloffMode.Custom;
		audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, world.distanceCurve);
		audioSource.dopplerLevel = 0f;
		if (Random.value < 0.9f)
		{
			audioSource.enabled = false;
		}
		else
		{
			audioSource.enabled = true;
		}
	}

	private void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData bounded = world.GetBounded(b.wP.x - 1, b.wP.y);
			updates.Add(bounded.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData bounded2 = world.GetBounded(b.wP.x + 1, b.wP.y);
			updates.Add(bounded2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData bounded3 = world.GetBounded(b.wP.x, b.wP.y - 1);
			updates.Add(bounded3.o);
		}
		else if (b.pos[1] == 15)
		{
			BlockData bounded4 = world.GetBounded(b.wP.x, b.wP.y + 1);
			updates.Add(bounded4.o);
		}
	}

	private void UpdateChunks()
	{
		foreach (Chunk update in updates)
		{
			update.updateTerrain = true;
			update.updateFlora = true;
		}
		updates.Clear();
	}

	private void Reload()
	{
		world.SlumBuildings.Add(this);
		foreach (Vector2Int bd in bdList)
		{
			BlockData blockData = world.blockData[bd.x, bd.y];
			blockData.st = (sbyte)Stage;
			blockData.t = BlockData.Type.BUILDING;
			blockData.uO = this;
		}
		transform.position = wP;
		transform.Rotate(0f, _r, 0f);
	}
}
