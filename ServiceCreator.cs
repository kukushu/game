using System.Collections.Generic;
using UnityEngine;

public class ServiceCreator : MonoBehaviour
{
	public enum Type
	{
		School,
		Hospital,
		Government,
		Fire,
		Police,
		Power,
		Airport,
		Park,
		Water,
		Religion,
		TrainStation,
		Arcology,
		Prison,
		Drone,
		Port
	}

	public int Grade = 50;

	public float polGrade;

	public float capGrade;

	public float employGrade;

	public int Capacity = 500;

	public int Use;

	public int Orientation;

	public Vector3 worldPos = Vector3.zero;

	public Vector3 Anchor = Vector3.zero;

	public string typeName = "";

	public Vector2Int Entry;

	public List<Vector2Int> blockPos = new List<Vector2Int>();

	public float Maintenance;

	public int Cost;

	public bool open = true;

	public GameObject TreesParent;

	private List<GameObject> treesGOs = new List<GameObject>();

	public List<Vector3> treesPos = new List<Vector3>();

	private List<GameObject> vehiclesGOs = new List<GameObject>();

	public List<Vector3> vehiclesPos = new List<Vector3>();

	public Type type;

	public int Stage = 1;

	public int MaxStage = 3;

	public int HiredEmployees;

	public int MinEmployees;

	public int MaxEmployees;

	public int FixedEmployees;

	public bool isDecayed;

	private MeshRenderer houseRenderer;

	private List<Material> decayMat = new List<Material>();

	public short carsOut;

	private Roading r;

	public bool reload;

	private Economy eco;

	private World w;

	public AudioSource audioSource;

	private void Start()
	{
		w = World.staticWorld;
		eco = w.eco;
		r = eco.r;
		if (reload)
		{
			Reload();
		}
		else
		{
			SetAudioClip();
		}
		decayMat.Clear();
		decayMat.Add(Zoning.dConcrete);
		decayMat.Add(Zoning.dBricks);
		decayMat.Add(Zoning.dWood);
		decayMat.Add(Zoning.dMetal);
		decayMat.Add(Zoning.dWindow);
		houseRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
	}

	public void CustomSpawnCar(Vector2 pos, int rotation)
	{
		GameObject gameObject = new GameObject("car");
		gameObject.transform.position = new Vector3(Anchor.x + pos.x, rotation, Anchor.z + pos.y);
		gameObject.transform.parent = TreesParent.transform;
		vehiclesGOs.Add(gameObject);
	}

	public void CustomSpawnSpecialVehicle(Vector2 carPos, int rotation, string[] vehiclesList)
	{
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(vehiclesList[Random.Range(0, vehiclesList.Length)]));
		gameObject.name = worldPos.x + "_car_" + worldPos.z;
		gameObject.transform.Rotate(0f, rotation, 0f, Space.Self);
		gameObject.transform.position = new Vector3(Anchor.x + carPos.x, 30.02f, Anchor.z + carPos.y);
		gameObject.isStatic = true;
		gameObject.transform.parent = transform;
	}

	public void SpawnTree(Vector3 treePos)
	{
		GameObject gameObject = new GameObject("tree");
		gameObject.transform.position = treePos;
		gameObject.transform.parent = TreesParent.transform;
		treesGOs.Add(gameObject);
	}

	public void CreateTrees(World w)
	{
		for (int i = 0; i < treesGOs.Count; i++)
		{
			treesPos.Add(treesGOs[i].transform.position);
		}
		w.blockData[Entry.x, Entry.y].o.updateFlora = true;
		Object.Destroy(TreesParent);
	}

	public void CreateVehicles(World w)
	{
		for (int i = 0; i < vehiclesGOs.Count; i++)
		{
			vehiclesPos.Add(vehiclesGOs[i].transform.position);
		}
		w.blockData[Entry.x, Entry.y].o.updateProps = true;
	}

	public void Rating()
	{
		float num = 1f;
		float num2 = 200 - Use * 110 / Capacity;
		if (num2 > 100f)
		{
			num2 = 100f;
		}
		else if (num2 < -100f)
		{
			num2 = -100f;
		}
		float num3 = (HiredEmployees - MinEmployees) * 100 / (MaxEmployees - MinEmployees);
		if (num3 > 100f)
		{
			num3 = 100f;
		}
		else if (num3 < 0f)
		{
			num3 = 0f;
		}
		if (type == Type.School)
		{
			num = eco.SchoolEfficiency * 25f + 50f;
		}
		else if (type == Type.Hospital)
		{
			num = eco.HospitalEfficiency * 25f + 50f;
		}
		else if (type == Type.Fire || type == Type.Police)
		{
			num = eco.Security * 25f + 50f;
		}
		if (num > 200f)
		{
			num = 200f;
		}
		Grade = Mathf.RoundToInt((num + num2 + num3) / 3f);
		if (Grade < 0)
		{
			Grade = 0;
		}
		else if (Grade > 140)
		{
			Grade = 140;
		}
		if (typeName == "U")
		{
			Grade = 100;
		}
		polGrade = num;
		employGrade = num3;
		capGrade = num2;
	}

	private void SetAudioClip()
	{
		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
		}
		audioSource.clip = r.la.SetServiceClip(this);
		audioSource.volume = SettingsStatic.CityVolume;
		audioSource.maxDistance = 58f;
		audioSource.spatialBlend = 1f;
		audioSource.rolloffMode = AudioRolloffMode.Custom;
		audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, w.distanceCurve);
		audioSource.dopplerLevel = 0f;
	}

	public void Reload()
	{
		if (type == Type.Arcology)
		{
			eco.w.Arcologies.Add(this);
		}
		else
		{
			w.Services.Add(this);
		}
		BlockData ifNotNull = w.GetIfNotNull(Entry.x, Entry.y);
		if (type == Type.Park && Maintenance < 40f)
		{
			ifNotNull = w.GetIfNotNull((int)worldPos.x, (int)worldPos.z);
		}
		if (ifNotNull == null)
		{
			return;
		}
		ifNotNull.o.services.Add(this);
		if (typeName == "HQsc")
		{
			eco.HQbuilding = this;
		}
		foreach (Vector2Int blockPo in blockPos)
		{
			BlockData blockData = r.w.blockData[blockPo.x, blockPo.y];
			blockData.st = (sbyte)Stage;
			blockData.t = BlockData.Type.BUILDING;
			blockData.sO = this;
		}
		carsOut = 0;
		if (TryGetComponent<AudioSource>(out var component))
		{
			audioSource = component;
			SetAudioClip();
		}
	}

	private void PopulateGrass()
	{
		foreach (Vector2Int blockPo in blockPos)
		{
			BlockData blockData = r.w.blockData[blockPo.x, blockPo.y];
			if (Random.value < 0.3f)
			{
				blockData.g = true;
			}
		}
		r.w.blockData[Entry.x, Entry.y].o.updateFlora = true;
	}

	public void DecayService()
	{
		if (isDecayed || type == Type.Government || type == Type.Religion || type == Type.TrainStation || type == Type.Park || type == Type.Arcology)
		{
			return;
		}
		eco.budget.Balance += (float)(MinEmployees * 40) + Maintenance * 20f;
		isDecayed = true;
		if (Capacity > 1)
		{
			Capacity = Mathf.RoundToInt((float)Capacity * 0.5f);
		}
		Maintenance *= 0.2f;
		if (FixedEmployees > 1)
		{
			FixedEmployees = Mathf.RoundToInt((float)FixedEmployees * 0.5f);
		}
		if (HiredEmployees > 1)
		{
			HiredEmployees = Mathf.RoundToInt((float)HiredEmployees * 0.5f);
		}
		if (houseRenderer == null)
		{
			Debug.Log("Render is null");
			return;
		}
		Material[] sharedMaterials = houseRenderer.sharedMaterials;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			if (i < decayMat.Count)
			{
				if (!sharedMaterials[i].name.StartsWith("sk_wi") && !sharedMaterials[i].name.StartsWith("sk_at"))
				{
					sharedMaterials[i] = decayMat[i];
				}
			}
			else if (!sharedMaterials[i].name.StartsWith("sk_at"))
			{
				sharedMaterials[i] = decayMat[Random.Range(0, decayMat.Count - 1)];
			}
		}
		houseRenderer.sharedMaterials = sharedMaterials;
		PopulateGrass();
	}
}
