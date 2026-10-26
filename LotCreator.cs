using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LotCreator : MonoBehaviour
{
	public enum LotType
	{
		RESIDENTIAL,
		COMMERCIAL,
		INDUSTRIAL,
		MIXED
	}

	public enum LotClass
	{
		LOW,
		MIDDLE,
		UPPER,
		RETAIL,
		OFFICE,
		MANUFACTURE,
		HIGHTECH,
		PRIMARY,
		MIXRETAIL,
		MIXOFFICE
	}

	public enum LotSize
	{
		_4x4,
		_3x4,
		_2x4,
		_2x5,
		_2x6,
		_3x5,
		_3x6,
		_4x5,
		_4x6,
		_6x4,
		_6x5,
		_6x6
	}

	public LotType lType;

	public LotSize lSize;

	public LotClass lClass;

	public int lWidth = 4;

	public Vector2Int Entry;

	public RoadCreator RoadEntry;

	public LotCreator commuteLot;

	public List<RoadCreator> roadPath;

	public Roading r;

	public World w;

	public short MaxStage = 2;

	public short LandValue;

	public short desiredValue;

	public short nextValue;

	public short nextDemand;

	public short Orientation;

	public short Stage;

	public int Occupants;

	public short Visitors;

	public float TaxIncome;

	public float Trade;

	public bool powered = true;

	public bool watered = true;

	public bool underConstruction;

	public bool roadNetwork;

	public bool validCommute;

	public short pathTried;

	public string commuteState = "";

	public short carsOut;

	public short pedsOut;

	public bool hasChanged;

	public short lastChange;

	public float negatives;

	public bool isDecayed;

	public bool isRubles;

	public Vector3 Anchor = Vector3.zero;

	public Vector3 wP = Vector3.zero;

	public Vector2Int commuteAnchor;

	public bool painterHired;

	public float eduLevel;

	public float birthRate;

	public short secuLevel = 50;

	public short churchCount;

	public short reliLevel;

	public float poluLevel;

	public short healthLevel = 50;

	private List<GameObject> treesGOs = new List<GameObject>();

	public List<Vector3> treesPos = new List<Vector3>();

	private List<GameObject> grassGOs = new List<GameObject>();

	public List<Vector3> grassPos = new List<Vector3>();

	private List<GameObject> propsGOs = new List<GameObject>();

	public List<Vector3> propsPos = new List<Vector3>();

	private List<GameObject> streetpropsGOs = new List<GameObject>();

	public List<Vector3> streetpropsPos = new List<Vector3>();

	private List<GameObject> vehiclesGOs = new List<GameObject>();

	public List<Vector3> vehiclesPos = new List<Vector3>();

	public GameObject streetLight;

	private Vector3 streetLightPos;

	public bool lightOn;

	private MeshRenderer houseRenderer;

	public List<Vector2Int> blockPos = new List<Vector2Int>();

	private List<GameObject> carsInLot = new List<GameObject>();

	public string decayReason = "";

	private GameObject Props;

	private GameObject Ground;

	private GameObject TreesParent;

	private PropCreator propCreator;

	public AudioSource audioSource;

	public bool reload;

	public GameObject tempVacant;

	public GameObject dummy;

	private int clipPlayed;

	private void Start()
	{
		if (r == null)
		{
			r = GameObject.Find("World").GetComponent<Roading>();
			Debug.Log("Roading in LC was null. Finding it...");
		}
		if (w == null)
		{
			w = GameObject.Find("World").GetComponent<World>();
			Debug.Log("World in LC was null. Finding it...");
		}
		if (reload)
		{
			Reload();
			SetAudioClip();
		}
		RoadCreator ifNotNull = r.GetIfNotNull(Entry.x, 1, Entry.y);
		if (ifNotNull != null)
		{
			roadNetwork = true;
			RoadEntry = ifNotNull;
		}
	}

	public void Aiguilleur(int height)
	{
		if (isDecayed)
		{
			carsInLot.Clear();
			foreach (Transform item in transform)
			{
				Object.Destroy(item.gameObject);
			}
			isDecayed = false;
			isRubles = false;
			foreach (Vector2Int blockPo in blockPos)
			{
				BlockData blockData = r.w.blockData[blockPo.x, blockPo.y];
				if (Random.value < 0.3f)
				{
					blockData.g = false;
				}
			}
		}
		if (height >= 5)
		{
			StartCoroutine(MediumCondoConstruction(height));
		}
		else if (height >= 2)
		{
			StartCoroutine(SmallCondoConstruction(height - 1));
		}
		else
		{
			StartCoroutine(HouseConstruction());
		}
		hasChanged = true;
	}

	public void GroundSelector()
	{
		TreesParent = new GameObject("TreesParent");
		Props = new GameObject("Props");
		string text = "4x4";
		string text2 = "LC/LC1";
		if (lSize == LotSize._3x4)
		{
			text = "3x4";
		}
		else if (lSize == LotSize._4x4)
		{
			text = "4x4";
		}
		else if (lSize == LotSize._2x4)
		{
			text = "2x4";
		}
		else if (lSize == LotSize._2x5)
		{
			text = "2x5";
		}
		else if (lSize == LotSize._2x6)
		{
			text = "2x6";
		}
		else if (lSize == LotSize._3x5)
		{
			text = "3x5";
		}
		else if (lSize == LotSize._3x6)
		{
			text = "3x6";
		}
		else if (lSize == LotSize._4x5)
		{
			text = "4x5";
		}
		else if (lSize == LotSize._4x6)
		{
			text = "4x6";
		}
		else if (lSize == LotSize._6x4)
		{
			text = "6x4";
		}
		else if (lSize == LotSize._6x5)
		{
			text = "6x5";
		}
		else if (lSize == LotSize._6x6)
		{
			text = "6x6";
		}
		if (lClass == LotClass.LOW)
		{
			if (Stage == 1)
			{
				eduLevel = Mathf.Clamp(w.eco.EducationLevel / 5f, 0f, 100f);
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "LC/LC1";
			}
			else if (Stage == 2 || Stage == 3 || Stage == 4)
			{
				text2 = "LC/LC2";
			}
			else if (Stage >= 5)
			{
				text2 = "LC/LC3";
			}
			else if (Stage == 0)
			{
				if (Occupants > 300)
				{
					text2 = "MC/MC3";
				}
				else
				{
					text2 = ((Occupants <= 100) ? "MC/MC2" : "MC/MC2");
				}
			}
		}
		else if (lClass == LotClass.MIDDLE)
		{
			if (Stage != 1)
			{
				text2 = ((Stage >= 5) ? "MC/MC3" : "MC/MC2");
			}
			else
			{
				eduLevel = Mathf.Clamp(w.eco.EducationLevel / 3f, 0f, 100f);
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "MC/MC1";
			}
		}
		else if (lClass == LotClass.MIXRETAIL)
		{
			if (Stage < 3)
			{
				eduLevel = Mathf.Clamp(w.eco.EducationLevel / 3f, 0f, 100f);
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "M/M";
			}
			else
			{
				text2 = "M/M";
			}
		}
		else if (lClass == LotClass.UPPER)
		{
			if (Stage == 1)
			{
				eduLevel = Mathf.Clamp(w.eco.EducationLevel / 2f, 0f, 100f);
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "UC/UC1";
			}
			else if (Stage == 2 || Stage == 3)
			{
				text2 = "UC/UC2";
			}
			else if (Stage >= 4)
			{
				text2 = "UC/UC3";
			}
		}
		else if (lClass == LotClass.MIXOFFICE)
		{
			if (Stage < 3)
			{
				eduLevel = Mathf.Clamp(w.eco.EducationLevel / 2f, 0f, 100f);
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "M/M";
			}
			else
			{
				text2 = "M/M";
			}
		}
		else if (lClass == LotClass.MANUFACTURE || lClass == LotClass.PRIMARY)
		{
			if (Stage == 1 && lClass != LotClass.PRIMARY)
			{
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "BM/BM1";
			}
			else
			{
				if (Stage == 1 && tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
				text2 = "BM/BM2";
			}
		}
		else if (lClass == LotClass.HIGHTECH)
		{
			if (Stage == 1)
			{
				text2 = "R/R1";
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
			}
			else if (Stage == 2 || Stage == 3)
			{
				text2 = "O/O";
			}
			else if (Stage > 3)
			{
				text2 = "R/R3";
			}
		}
		else if (lClass == LotClass.RETAIL)
		{
			if (Stage != 1)
			{
				text2 = ((Stage != 2) ? "R/R3" : "R/R2");
			}
			else
			{
				text2 = "R/R1";
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
			}
		}
		else if (lClass == LotClass.OFFICE)
		{
			if (Stage == 1)
			{
				text2 = "R/R2";
				if (tempVacant != null)
				{
					Object.Destroy(tempVacant);
				}
			}
			else if (Stage == 2)
			{
				text2 = "O/O";
			}
			else if (Stage >= 3)
			{
				text2 = "O/O2";
			}
		}
		string path = "Lots/" + text2 + "_" + text;
		Ground = (GameObject)Object.Instantiate(Resources.Load(path));
		Ground.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		Ground.name = "ground";
		Ground.layer = 16;
		propCreator = Ground.GetComponentInChildren<PropCreator>();
		if (propCreator == null)
		{
			Debug.Log("Prop list null!");
		}
		streetpropsGOs.Clear();
		streetpropsPos.Clear();
		propsGOs.Clear();
		propsPos.Clear();
		grassGOs.Clear();
		vehiclesGOs.Clear();
		grassPos.Clear();
		treesGOs.Clear();
		treesPos.Clear();
		vehiclesPos.Clear();
		streetLight = null;
		streetLightPos = Vector3.zero;
		if (lType == LotType.COMMERCIAL && Stage >= 1 && Stage <= 3 && lWidth >= 3)
		{
			float x;
			if (lSize != LotSize._3x5 && lSize != LotSize._4x5 && lSize != LotSize._6x5)
			{
				x = ((lSize != LotSize._3x6 && lSize != LotSize._4x6 && lSize != LotSize._6x6) ? (-1.8f) : (-2.8f));
			}
			else
			{
				x = -2.3f;
			}
			float y = ((!(Random.value > 0.5f)) ? 0.75f : (-0.75f));
			SpawnSign(new Vector2(x, y));
		}
		BuildStreetProps();
		GetAndBuildProps(propCreator);
		UpdateBlockData(props: true, flora: true, terrain: true);
		Props.layer = 11;
		Props.transform.parent = Ground.transform;
		TreesParent.transform.parent = Ground.transform;
		HouseSelector(Ground);
	}

	private void SpawnCar(Vector2 pos)
	{
		GameObject gameObject = new GameObject("car");
		gameObject.transform.position = new Vector3(Anchor.x + pos.x, wP.y + 0.504f, Anchor.z + pos.y);
		gameObject.transform.parent = TreesParent.transform;
		vehiclesGOs.Add(gameObject);
	}

	private void SpawnPropOrTree(Vector2 pos, bool isFront)
	{
		float value = Random.value;
		if (value < 0.4f)
		{
			string[] array;
			if (isFront)
			{
				if (lClass != LotClass.LOW && lClass != LotClass.MANUFACTURE && lClass != LotClass.PRIMARY)
				{
					if (lClass == LotClass.MIDDLE || lClass == LotClass.RETAIL || lClass == LotClass.MIXRETAIL)
					{
						array = ((Stage != 1) ? Dico.RR_Dense : Dico.RR_Front);
					}
					else
					{
						array = ((Stage != 1) ? Dico.RRR_Dense : Dico.RRR_Front);
					}
				}
				else
				{
					array = ((Stage != 1) ? Dico.R_Dense : Dico.R_Front);
				}
			}
			else if (lClass != LotClass.LOW && lClass != LotClass.MANUFACTURE && lClass != LotClass.PRIMARY)
			{
				array = ((lClass != LotClass.MIDDLE && lClass != LotClass.RETAIL && lClass != LotClass.MIXRETAIL) ? Dico.RRR_Props : Dico.RR_Props);
			}
			else
			{
				array = Dico.R_Props;
			}
			GameObject gameObject = new GameObject(array[Random.Range(0, array.Length)]);
			gameObject.transform.position = new Vector3(Anchor.x + pos.x, wP.y + 0.504f, Anchor.z + pos.y);
			gameObject.transform.parent = Props.transform;
			propsGOs.Add(gameObject);
		}
		else if (value < 0.7f)
		{
			SpawnGrass(new Vector3(Anchor.x + pos.x, wP.y, Anchor.z + pos.y));
		}
		else
		{
			SpawnTree(new Vector3(Anchor.x + pos.x, wP.y + 0.504f, Anchor.z + pos.y));
		}
	}

	private void SpawnStreetProp(float Xoffset, float Yoffset, bool isLight)
	{
		string[] array;
		float num;
		if (lClass == LotClass.LOW || lClass == LotClass.MANUFACTURE)
		{
			array = Dico.R_Street;
			num = 1.4f;
		}
		else if (lClass == LotClass.MIDDLE || lClass == LotClass.RETAIL || lClass == LotClass.MIXRETAIL)
		{
			array = Dico.RR_Street;
			num = 1.2f;
		}
		else
		{
			array = Dico.RRR_Street;
			num = 1.6f;
		}
		if (Stage == 1)
		{
			num -= 0.4f;
		}
		else if (Stage == 2 || Stage == 3)
		{
			num -= 0.2f;
		}
		if (isLight)
		{
			GameObject gameObject = new GameObject("Lots/SL");
			gameObject.transform.position = new Vector3(Anchor.x - Yoffset, 0f, Anchor.z + Xoffset);
			gameObject.transform.parent = Props.transform;
			streetpropsGOs.Add(gameObject);
		}
		else if (Random.value < num)
		{
			GameObject gameObject2 = new GameObject(array[Random.Range(0, array.Length)]);
			gameObject2.transform.position = new Vector3(Anchor.x - Yoffset, 0f, Anchor.z + Xoffset);
			gameObject2.transform.parent = Props.transform;
			streetpropsGOs.Add(gameObject2);
		}
	}

	private void SpawnTree(Vector3 treePos)
	{
		GameObject gameObject = new GameObject("tree");
		gameObject.transform.position = treePos;
		gameObject.transform.parent = TreesParent.transform;
		treesGOs.Add(gameObject);
	}

	private void SpawnGrass(Vector3 grassPos)
	{
		GameObject gameObject = new GameObject("tree");
		gameObject.transform.position = grassPos;
		gameObject.transform.parent = TreesParent.transform;
		grassGOs.Add(gameObject);
	}

	private void SpawnSign(Vector2 signPos)
	{
		int num = 90;
		if (Random.value >= 0.5f)
		{
			num = 270;
		}
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(Dico.C_Sign[Random.Range(0, Dico.C_Sign.Length)]));
		gameObject.transform.Rotate(0f, num, 0f, Space.Self);
		gameObject.transform.position = new Vector3(Anchor.x + signPos.x, wP.y + 0.495f, Anchor.z + signPos.y);
		gameObject.transform.parent = Ground.transform;
		gameObject.layer = 13;
	}

	private void GetAndBuildProps(PropCreator propList)
	{
		if (propList == null)
		{
			Debug.Log("NO PROP TYPE!");
			return;
		}
		foreach (PropType prop in propList.Props)
		{
			if (!(prop == null))
			{
				Vector2 pos = new Vector2(prop.transform.localPosition.x, prop.transform.localPosition.z);
				if (prop.type == PropType.Type.Car)
				{
					SpawnCar(pos);
				}
				else if (prop.type == PropType.Type.Tree)
				{
					SpawnTree(new Vector3(Anchor.x + pos.x, wP.y + 0.504f, Anchor.z + pos.y));
				}
				else if (prop.type == PropType.Type.TreeOrProp || prop.type == PropType.Type.Prop)
				{
					SpawnPropOrTree(pos, isFront: false);
				}
				else if (prop.type == PropType.Type.Grass)
				{
					SpawnGrass(new Vector3(Anchor.x + pos.x, wP.y, Anchor.z + pos.y));
				}
				else if (prop.type == PropType.Type.Front)
				{
					SpawnPropOrTree(pos, isFront: true);
				}
			}
		}
	}

	private void HouseSelector(GameObject _ground)
	{
		string path = "none";
		SetGroundColor();
		bool flag = false;
		if (lClass == LotClass.LOW)
		{
			if (Stage == 1)
			{
				path = Dico.R1_house[Random.Range(0, Dico.R1_house.Length)];
			}
			else if (Stage == 2)
			{
				if (lWidth < 4)
				{
					flag = true;
				}
				path = Dico.R2_house[Random.Range(0, Dico.R2_house.Length)];
			}
			else if (Stage == 3)
			{
				path = ((lWidth >= 4) ? Dico.R3_house[Random.Range(0, Dico.R3_house.Length)] : Dico.R3S_house[Random.Range(0, Dico.R3S_house.Length)]);
			}
			else if (Stage == 4)
			{
				path = ((lWidth >= 4) ? Dico.R4_house[Random.Range(0, Dico.R4_house.Length)] : Dico.R4S_house[Random.Range(0, Dico.R4S_house.Length)]);
			}
			else if (Stage == 5)
			{
				path = Dico.R5_house[Random.Range(0, Dico.R5_house.Length)];
			}
			else if (Stage == 6)
			{
				path = Dico.R6_house[Random.Range(0, Dico.R6_house.Length)];
			}
			else if (Stage == 7)
			{
				path = Dico.R7_house[Random.Range(0, Dico.R7_house.Length)];
			}
			else if (Stage == 0)
			{
				if (Occupants > 300)
				{
					path = "Residential/HLM_C";
				}
				else
				{
					path = ((Occupants <= 100) ? "Residential/HLM_A" : "Residential/HLM_B");
				}
			}
		}
		else if (lClass == LotClass.MIDDLE)
		{
			if (Stage == 1)
			{
				if (lWidth < 4)
				{
					path = Dico.RR1S_house[Random.Range(0, Dico.RR1S_house.Length)];
					flag = true;
				}
				else
				{
					path = Dico.RR1_house[Random.Range(0, Dico.RR1_house.Length)];
				}
			}
			else if (Stage == 2)
			{
				if (lWidth < 4)
				{
					flag = true;
				}
				path = Dico.RR2_house[Random.Range(0, Dico.RR2_house.Length)];
			}
			else if (Stage == 3)
			{
				path = ((lWidth >= 4) ? Dico.RR3_house[Random.Range(0, Dico.RR3_house.Length)] : Dico.RR3S_house[Random.Range(0, Dico.RR3S_house.Length)]);
			}
			else if (Stage == 4)
			{
				path = ((lWidth >= 4) ? Dico.RR4_house[Random.Range(0, Dico.RR4_house.Length)] : Dico.RR4S_house[Random.Range(0, Dico.RR4S_house.Length)]);
			}
			else if (Stage == 5)
			{
				path = Dico.RR5_house[Random.Range(0, Dico.RR5_house.Length)];
			}
			else if (Stage == 6)
			{
				path = Dico.RR6_house[Random.Range(0, Dico.RR6_house.Length)];
			}
			else if (Stage == 7)
			{
				path = Dico.RR7_house[Random.Range(0, Dico.RR7_house.Length)];
			}
		}
		else if (lClass == LotClass.UPPER)
		{
			if (Stage == 1)
			{
				path = ((lWidth >= 4) ? Dico.RRR1_house[Random.Range(0, Dico.RRR1_house.Length)] : Dico.RRR1S_house[Random.Range(0, Dico.RRR1S_house.Length)]);
			}
			else if (Stage == 2)
			{
				path = ((lWidth >= 4) ? Dico.RRR2_house[Random.Range(0, Dico.RRR2_house.Length)] : Dico.RRR2S_house[Random.Range(0, Dico.RRR2S_house.Length)]);
			}
			else if (Stage == 3)
			{
				path = ((lWidth >= 4) ? Dico.RRR3_house[Random.Range(0, Dico.RRR3_house.Length)] : Dico.RRR3S_house[Random.Range(0, Dico.RRR3S_house.Length)]);
			}
			else if (Stage == 4)
			{
				path = ((lWidth >= 4) ? Dico.RRR4_house[Random.Range(0, Dico.RRR4_house.Length)] : Dico.RRR4S_house[Random.Range(0, Dico.RRR4S_house.Length)]);
			}
			else if (Stage == 5)
			{
				path = Dico.RRR5_house[Random.Range(0, Dico.RRR5_house.Length)];
			}
			else if (Stage == 6)
			{
				path = Dico.RRR6_house[Random.Range(0, Dico.RRR6_house.Length)];
			}
			else if (Stage == 7)
			{
				path = Dico.RRR7_house[Random.Range(0, Dico.RRR7_house.Length)];
			}
		}
		else if (lClass == LotClass.MIXRETAIL)
		{
			if (Stage == 1)
			{
				path = ((lWidth >= 4) ? Dico.RMC1_house[Random.Range(0, Dico.RMC1_house.Length)] : Dico.RMC1S_house[Random.Range(0, Dico.RMC1S_house.Length)]);
			}
			else if (Stage == 2)
			{
				path = ((lWidth >= 4) ? Dico.RMC2_house[Random.Range(0, Dico.RMC2_house.Length)] : Dico.RMC2S_house[Random.Range(0, Dico.RMC2S_house.Length)]);
			}
			else if (Stage == 3)
			{
				path = Dico.RMC3_house[Random.Range(0, Dico.RMC3_house.Length)];
			}
			else if (Stage == 4)
			{
				path = Dico.RMC4_house[Random.Range(0, Dico.RMC4_house.Length)];
			}
			flag = true;
		}
		else if (lClass == LotClass.MIXOFFICE)
		{
			if (Stage == 1)
			{
				path = ((lWidth >= 4) ? Dico.OUC1_house[Random.Range(0, Dico.OUC1_house.Length)] : Dico.OUC1S_house[Random.Range(0, Dico.OUC1S_house.Length)]);
			}
			else if (Stage == 2)
			{
				path = ((lWidth >= 4) ? Dico.OUC2_house[Random.Range(0, Dico.OUC2_house.Length)] : Dico.OUC2S_house[Random.Range(0, Dico.OUC2S_house.Length)]);
			}
			else if (Stage == 3)
			{
				path = Dico.OUC3_house[Random.Range(0, Dico.OUC3_house.Length)];
			}
			else if (Stage == 4)
			{
				path = Dico.OUC4_house[Random.Range(0, Dico.OUC4_house.Length)];
			}
			flag = true;
		}
		else if (lClass == LotClass.MANUFACTURE)
		{
			if (Stage == 1)
			{
				path = Dico.I1_house[Random.Range(0, Dico.I1_house.Length)];
			}
			else if (Stage == 2)
			{
				path = Dico.I2_house[Random.Range(0, Dico.I2_house.Length)];
			}
			else if (Stage == 3)
			{
				path = Dico.I3_house[Random.Range(0, Dico.I3_house.Length)];
			}
			else if (Stage == 4)
			{
				path = Dico.I4_house[Random.Range(0, Dico.I4_house.Length)];
			}
			else if (Stage == 0)
			{
				path = ((Occupants >= 300) ? "Industrial/Factory_B" : "Industrial/Factory_A");
			}
		}
		else if (lClass == LotClass.HIGHTECH)
		{
			if (Stage == 1)
			{
				if (lWidth < 4)
				{
					path = Dico.HT1S_house[Random.Range(0, Dico.HT1S_house.Length)];
					flag = true;
				}
				else
				{
					path = Dico.HT1_house[Random.Range(0, Dico.HT1_house.Length)];
				}
			}
			else if (Stage == 2)
			{
				path = Dico.HT2_house[Random.Range(0, Dico.HT2_house.Length)];
			}
			else if (Stage == 3)
			{
				path = Dico.HT3_house[Random.Range(0, Dico.HT3_house.Length)];
			}
			else if (Stage == 4)
			{
				path = Dico.HT4_house[Random.Range(0, Dico.HT4_house.Length)];
			}
		}
		else if (lClass == LotClass.RETAIL)
		{
			if (Stage == 1)
			{
				path = ((lWidth >= 4) ? Dico.C1_house[Random.Range(0, Dico.C1_house.Length)] : Dico.C1S_house[Random.Range(0, Dico.C1S_house.Length)]);
			}
			else if (Stage == 2)
			{
				path = ((lWidth >= 4) ? Dico.C2_house[Random.Range(0, Dico.C2_house.Length)] : Dico.C2S_house[Random.Range(0, Dico.C2S_house.Length)]);
			}
			else if (Stage == 3)
			{
				path = Dico.C3_house[Random.Range(0, Dico.C3_house.Length)];
			}
			else if (Stage == 4)
			{
				path = Dico.C4_house[Random.Range(0, Dico.C4_house.Length)];
			}
			else if (Stage == 5)
			{
				path = Dico.C5_house[Random.Range(0, Dico.C5_house.Length)];
			}
			else if (Stage == 0)
			{
				path = ((Occupants <= 500) ? "Commercial/PublicMarket" : "Commercial/RedDistrict");
			}
		}
		else if (lClass == LotClass.OFFICE)
		{
			if (Stage == 1)
			{
				if (lWidth < 4)
				{
					flag = true;
				}
				path = Dico.O1_house[Random.Range(0, Dico.O1_house.Length)];
			}
			else if (Stage == 2)
			{
				path = Dico.O2_house[Random.Range(0, Dico.O2_house.Length)];
			}
			else if (Stage == 3)
			{
				path = Dico.O3_house[Random.Range(0, Dico.O3_house.Length)];
			}
			else if (Stage == 4)
			{
				path = Dico.O4_house[Random.Range(0, Dico.O4_house.Length)];
			}
			else if (Stage == 5)
			{
				path = Dico.O5_house[Random.Range(0, Dico.O5_house.Length)];
			}
			else if (Stage == 6)
			{
				path = Dico.O6_house[Random.Range(0, Dico.O6_house.Length)];
			}
		}
		else if (lClass == LotClass.PRIMARY && Stage == 1)
		{
			path = Dico.Farm_house[Random.Range(0, Dico.Farm_house.Length)];
		}
		SetMaxStage();
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(path));
		gameObject.name = wP.x + "_house_" + wP.z;
		gameObject.layer = 15;
		float value = Random.value;
		int num;
		if (flag)
		{
			num = ((!(value <= 0.5f)) ? 270 : 90);
		}
		else
		{
			num = ((value <= 0.5f) ? 180 : 0);
		}
		gameObject.transform.Rotate(0f, num, 0f, Space.Self);
		gameObject.transform.position = propCreator.buildingTransform.position;
		houseRenderer = gameObject.GetComponent<MeshRenderer>();
		gameObject.transform.parent = _ground.transform;
		_ground.transform.Rotate(0f, Orientation, 0f, Space.Self);
		_ground.transform.parent = transform;
		underConstruction = false;
		for (int i = 0; i < treesGOs.Count; i++)
		{
			treesPos.Add(treesGOs[i].transform.position);
		}
		for (int j = 0; j < grassGOs.Count; j++)
		{
			grassPos.Add(grassGOs[j].transform.position);
		}
		for (int k = 0; k < vehiclesGOs.Count; k++)
		{
			vehiclesPos.Add(vehiclesGOs[k].transform.position);
		}
		for (int l = 0; l < streetpropsGOs.Count; l++)
		{
			Vector3 position = streetpropsGOs[l].transform.position;
			RoadCreator ifNotNull = r.GetIfNotNull((int)position.x, 1, (int)position.z);
			if (!(ifNotNull == null) && (ifNotNull.tile == "straight" || ifNotNull.tile == "side"))
			{
				position.y = ifNotNull.wPos.y;
				int num2 = (int)streetpropsGOs[l].transform.rotation.eulerAngles.y;
				BlockData blockData = r.w.blockData[(int)position.x, (int)position.z];
				blockData.pT = streetpropsGOs[l].name;
				blockData.pR = (short)num2;
				blockData.o.updateProps = true;
				if (streetpropsGOs[l].name == "Lots/SL")
				{
					streetLightPos = new Vector3(position.x, ifNotNull.wPos.y + 0.03f, position.z);
					position.y = ifNotNull.wPos.y + 0.1f;
				}
				streetpropsPos.Add(position);
			}
		}
		for (int m = 0; m < propsGOs.Count; m++)
		{
			Vector3 position2 = propsGOs[m].transform.position;
			propsPos.Add(propsGOs[m].transform.position);
			int num3 = (int)propsGOs[m].transform.rotation.eulerAngles.y;
			BlockData blockData2 = r.w.blockData[(int)position2.x, (int)position2.z];
			blockData2.pT = propsGOs[m].name;
			blockData2.pR = (short)num3;
			blockData2.o.updateProps = true;
		}
		Object.Destroy(TreesParent);
		Object.Destroy(propCreator.gameObject);
		SetAudioClip();
		if (streetLightPos != Vector3.zero)
		{
			GameObject gameObject2 = (GameObject)Object.Instantiate(Resources.Load("Lots/ZLamp"));
			gameObject2.transform.position = new Vector3(streetLightPos.x, streetLightPos.y, streetLightPos.z);
			gameObject2.isStatic = true;
			gameObject2.transform.parent = Ground.transform;
			streetLight = gameObject2;
		}
		if (lType == LotType.MIXED)
		{
			gameObject.AddComponent<BoxCollider>();
		}
		HousePainter component = gameObject.GetComponent<HousePainter>();
		if ((component != null) & !painterHired)
		{
			component.ChangePaint(houseRenderer);
			painterHired = true;
		}
	}

	private IEnumerator HouseConstruction()
	{
		int invert = 0;
		if (lWidth < 4)
		{
			invert = 90;
		}
		string path;
		string Construction2;
		string Construction3;
		if (lType == LotType.RESIDENTIAL)
		{
			path = "Construction/C_House_Stage1";
			Construction2 = "Construction/C_House_Stage2";
			Construction3 = "Construction/C_House_Stage3";
		}
		else
		{
			path = "Construction/C_Indus_Stage1";
			Construction2 = "Construction/C_Indus_Stage2";
			Construction3 = "Construction/C_Indus_Stage3";
		}
		GameObject stage1 = (GameObject)Object.Instantiate(Resources.Load(path));
		stage1.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		stage1.name = wP.x + "_construction_" + wP.z;
		stage1.transform.Rotate(0f, Orientation + invert, 0f, Space.Self);
		stage1.transform.parent = transform;
		yield return new WaitForSeconds(2f);
		Object.Destroy(stage1);
		GameObject stage2 = (GameObject)Object.Instantiate(Resources.Load(Construction2));
		stage2.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		stage2.name = wP.x + "_construction_" + wP.z;
		stage2.transform.Rotate(0f, Orientation + invert, 0f, Space.Self);
		stage2.transform.parent = transform;
		yield return new WaitForSeconds(2f);
		Object.Destroy(stage2);
		GameObject stage3 = (GameObject)Object.Instantiate(Resources.Load(Construction3));
		stage3.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		stage3.name = wP.x + "_construction_" + wP.z;
		stage3.transform.Rotate(0f, Orientation + invert, 0f, Space.Self);
		stage3.transform.parent = transform;
		yield return new WaitForSeconds(2f);
		Object.Destroy(stage3);
		Object.Destroy(tempVacant);
		GroundSelector();
	}

	private IEnumerator SmallCondoConstruction(int height)
	{
		carsInLot.Clear();
		int invert = 0;
		if (lWidth < 4)
		{
			invert = 90;
		}
		foreach (Transform item in transform)
		{
			Object.Destroy(item.gameObject);
		}
		string Construction1;
		string Construction2;
		string Construction3;
		if (lType == LotType.RESIDENTIAL || lType == LotType.MIXED)
		{
			Construction1 = "Construction/C_Condo_Stage1";
			Construction2 = "Construction/C_Condo_Stage2";
			Construction3 = "Construction/C_Condo_Stage3";
		}
		else
		{
			Construction1 = "Construction/C_Indus2_Stage1";
			Construction2 = "Construction/C_Indus2_Stage2";
			Construction3 = "Construction/C_Indus2_Stage3";
		}
		tempVacant = (GameObject)Object.Instantiate(Resources.Load(GetVacantPath()));
		tempVacant.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		tempVacant.name = wP.x + "_vacant_" + wP.z;
		tempVacant.transform.Rotate(0f, Orientation, 0f, Space.Self);
		tempVacant.transform.parent = transform;
		GameObject _crane = new GameObject();
		if (height >= 2)
		{
			Vector2 vector;
			if (Orientation != 0)
			{
				if (Orientation == 90)
				{
					vector = new Vector2(1f, 1.25f);
				}
				else
				{
					vector = ((Orientation != 180) ? new Vector2(-1f, -1.25f) : new Vector2(1.25f, -1f));
				}
			}
			else
			{
				vector = new Vector2(-1.25f, 1f);
			}
			_crane = (GameObject)Object.Instantiate(Resources.Load("Construction/CraneMedium"));
			_crane.transform.position = new Vector3(Anchor.x + vector.x, wP.y + 0.5f, Anchor.z + vector.y);
			_crane.name = wP.x + "_crane_" + wP.z;
			_crane.transform.Rotate(0f, Orientation, 0f, Space.Self);
			_crane.transform.parent = transform;
		}
		for (int i = 0; i <= height; i++)
		{
			GameObject stage1 = (GameObject)Object.Instantiate(Resources.Load(Construction1));
			stage1.transform.position = new Vector3(Anchor.x, wP.y + 0.504f + (float)i * 0.3875f, Anchor.z);
			stage1.name = wP.x + "_construction" + i + "_" + wP.z;
			stage1.transform.Rotate(0f, Orientation + invert, 0f, Space.Self);
			stage1.transform.parent = transform;
			yield return new WaitForSeconds(2f);
			Object.Destroy(stage1);
			GameObject stage2 = (GameObject)Object.Instantiate(Resources.Load(Construction2));
			stage2.transform.position = new Vector3(Anchor.x, wP.y + 0.504f + (float)i * 0.3875f, Anchor.z);
			stage2.name = wP.x + "_construction" + i + "_" + wP.z;
			stage2.transform.Rotate(0f, Orientation + invert, 0f, Space.Self);
			stage2.transform.parent = transform;
			yield return new WaitForSeconds(2f);
			Object.Destroy(stage2);
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(Construction3));
			gameObject.transform.position = new Vector3(Anchor.x, wP.y + 0.504f + (float)i * 0.3875f, Anchor.z);
			gameObject.name = wP.x + "_construction" + i + "_" + wP.z;
			gameObject.transform.Rotate(0f, Orientation + invert, 0f, Space.Self);
			gameObject.transform.parent = transform;
			yield return new WaitForSeconds(2f);
		}
		for (int j = 0; j <= height; j++)
		{
			Object.Destroy(transform.Find(wP.x + "_construction" + j + "_" + wP.z).gameObject);
		}
		Object.Destroy(tempVacant);
		Object.Destroy(_crane);
		GroundSelector();
	}

	private IEnumerator MediumCondoConstruction(int height)
	{
		carsInLot.Clear();
		int num = 0;
		if (lWidth < 4)
		{
			num = 90;
		}
		foreach (Transform item in transform)
		{
			Object.Destroy(item.gameObject);
		}
		string path = "Construction/C_Condo";
		if (height > 11)
		{
			path = "Construction/C_Sky";
		}
		tempVacant = (GameObject)Object.Instantiate(Resources.Load(GetVacantPath()));
		tempVacant.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		tempVacant.name = wP.x + "_vacant_" + wP.z;
		tempVacant.transform.Rotate(0f, Orientation, 0f, Space.Self);
		tempVacant.transform.parent = transform;
		string path2 = "Construction/CraneMedium";
		if (height > 11)
		{
			path2 = "Construction/CraneHigh";
		}
		Vector2 vector;
		if (Orientation != 0)
		{
			if (Orientation == 90)
			{
				vector = new Vector2(1f, 1.25f);
			}
			else
			{
				vector = ((Orientation != 180) ? new Vector2(-1f, -1.25f) : new Vector2(1.25f, -1f));
			}
		}
		else
		{
			vector = new Vector2(-1.25f, 1f);
		}
		GameObject _crane = (GameObject)Object.Instantiate(Resources.Load(path2));
		_crane.transform.position = new Vector3(Anchor.x + vector.x, wP.y + 0.5f, Anchor.z + vector.y);
		_crane.name = wP.x + "_crane_" + wP.z;
		_crane.transform.Rotate(0f, Orientation, 0f, Space.Self);
		_crane.transform.parent = transform;
		GameObject stage1 = (GameObject)Object.Instantiate(Resources.Load(path));
		stage1.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		stage1.name = wP.x + "_construction_" + wP.z;
		stage1.transform.Rotate(0f, Orientation + num, 0f, Space.Self);
		stage1.transform.parent = transform;
		Condo_Construction cc = stage1.GetComponent<Condo_Construction>();
		for (int i = 0; i <= height; i++)
		{
			yield return new WaitForSeconds(2f);
			cc.CoreUp();
			yield return new WaitForSeconds(2f);
			cc.WallsUp();
			yield return new WaitForSeconds(2f);
			cc.WindowsUp();
		}
		yield return new WaitForSeconds(2f);
		if (height > 11)
		{
			for (int i = 0; i >= 3; i++)
			{
				cc.WallsUp();
				yield return new WaitForSeconds(1f);
				cc.WindowsUp();
				yield return new WaitForSeconds(1f);
			}
		}
		else
		{
			cc.WallsUp();
			yield return new WaitForSeconds(2f);
			cc.WindowsUp();
			yield return new WaitForSeconds(2f);
			cc.WindowsUp();
			yield return new WaitForSeconds(1f);
		}
		Object.Destroy(stage1);
		Object.Destroy(tempVacant);
		Object.Destroy(_crane);
		GroundSelector();
	}

	private void BuildStreetProps()
	{
		float yoffset = 2.5f;
		if (lSize == LotSize._2x5 || lSize == LotSize._3x5 || lSize == LotSize._4x5 || lSize == LotSize._6x5)
		{
			yoffset = 3f;
		}
		else if (lSize == LotSize._2x6 || lSize == LotSize._3x6 || lSize == LotSize._4x6 || lSize == LotSize._6x6)
		{
			yoffset = 3.5f;
		}
		if (lSize == LotSize._2x4 || lSize == LotSize._2x5 || lSize == LotSize._2x6)
		{
			lWidth = 2;
			SpawnStreetProp(-0.5f, yoffset, isLight: false);
			SpawnStreetProp(0.5f, yoffset, isLight: true);
		}
		else if (lSize == LotSize._3x4 || lSize == LotSize._3x5 || lSize == LotSize._3x6)
		{
			lWidth = 3;
			SpawnStreetProp(-1f, yoffset, isLight: false);
			SpawnStreetProp(0f, yoffset, isLight: true);
			SpawnStreetProp(1f, yoffset, isLight: false);
		}
		else if (lSize == LotSize._4x4 || lSize == LotSize._4x5 || lSize == LotSize._4x6)
		{
			lWidth = 4;
			SpawnStreetProp(-1.5f, yoffset, isLight: false);
			SpawnStreetProp(-0.5f, yoffset, isLight: true);
			SpawnStreetProp(0.5f, yoffset, isLight: false);
			SpawnStreetProp(1.5f, yoffset, isLight: false);
		}
		else
		{
			lWidth = 6;
			SpawnStreetProp(-2.5f, yoffset, isLight: false);
			SpawnStreetProp(-1.5f, yoffset, isLight: false);
			SpawnStreetProp(-0.5f, yoffset, isLight: false);
			SpawnStreetProp(0.5f, yoffset, isLight: true);
			SpawnStreetProp(1.5f, yoffset, isLight: false);
			SpawnStreetProp(2.5f, yoffset, isLight: false);
		}
	}

	public void DecayLot(bool toParking)
	{
		if (houseRenderer == null || isDecayed || Stage == 0)
		{
			return;
		}
		float occupants = Occupants;
		nextDemand = 5;
		nextValue = 5;
		DisplayDummy();
		if (((Occupants > 3) & toParking) && lWidth > 2)
		{
			if (lClass == LotClass.OFFICE || (lClass == LotClass.RETAIL && Stage > 2) || lType == LotType.MIXED)
			{
				DecayToParkingLot(occupants);
				return;
			}
			if (lType == LotType.RESIDENTIAL && Stage > 4)
			{
				DecayToParkingLot(occupants);
				return;
			}
		}
		desiredValue = 1;
		w.eco.Stability -= 1f + (float)Stage / 20f;
		isDecayed = true;
		Occupants = 0;
		eduLevel /= 2f;
		Visitors = 0;
		float num;
		if (w.eco.Population < 50000f)
		{
			num = 10f;
		}
		else
		{
			num = ((!(w.eco.Population < 200000f)) ? 2f : 5f);
		}
		negatives += num;
		Material[] sharedMaterials = houseRenderer.sharedMaterials;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			if (i < w.decayMat.Count)
			{
				if (sharedMaterials[i].name.StartsWith("sk_wi"))
				{
					sharedMaterials[i] = w.decayMat[4];
				}
				else
				{
					sharedMaterials[i] = w.decayMat[i];
				}
			}
			else
			{
				sharedMaterials[i] = w.decayMat[4];
			}
		}
		foreach (GameObject item in carsInLot)
		{
			Object.Destroy(item);
		}
		houseRenderer.sharedMaterials = sharedMaterials;
		propsPos.Clear();
		vehiclesPos.Clear();
		Object.Destroy(Props);
		PopulateGrass();
		UpdateBlockData(props: true, flora: true, terrain: false);
		w.buldoze.GiveBackDemand(this, occupants);
		Visitors = 0;
		if (lType == LotType.RESIDENTIAL)
		{
			w.ResidentialBuildings.Remove(this);
			w.VacantResLots.Add(this);
			return;
		}
		if (lType == LotType.COMMERCIAL)
		{
			w.CommercialBuildings.Remove(this);
			w.VacantCommLots.Add(this);
			return;
		}
		if (lType == LotType.MIXED)
		{
			w.MixedBuildings.Remove(this);
			w.VacantMixedLots.Add(this);
			return;
		}
		w.IndustrialBuildings.Remove(this);
		w.VacantIndusLots.Add(this);
		ParticleSystem componentInChildren = houseRenderer.gameObject.GetComponentInChildren<ParticleSystem>();
		if (componentInChildren != null)
		{
			Object.Destroy(componentInChildren);
		}
	}

	private void DecayToParkingLot(float _occupants)
	{
		if (!(Ground == null))
		{
			propsPos.Clear();
			vehiclesPos.Clear();
			treesPos.Clear();
			grassPos.Clear();
			vehiclesGOs.Clear();
			Object.Destroy(Props);
			Object.Destroy(Ground);
			float num;
			if (w.eco.Population < 50000f)
			{
				num = 10f;
			}
			else
			{
				num = ((!(w.eco.Population < 200000f)) ? 2f : 5f);
			}
			Occupants = 2;
			Visitors = 0;
			Stage = 1;
			w.eco.Stability -= 1f + (float)Stage / 40f;
			eduLevel /= 2f;
			negatives += num;
			w.buldoze.GiveBackDemand(this, _occupants);
			string text;
			if (lSize == LotSize._3x4)
			{
				text = "3x4";
			}
			else if (lSize == LotSize._3x5)
			{
				text = "3x5";
			}
			else if (lSize == LotSize._3x6)
			{
				text = "3x6";
			}
			else if (lSize != LotSize._4x4)
			{
				if (lSize == LotSize._4x5)
				{
					text = "4x5";
				}
				else if (lSize == LotSize._4x6)
				{
					text = "4x6";
				}
				else if (lSize == LotSize._6x4)
				{
					text = "6x4";
				}
				else
				{
					text = ((lSize != LotSize._6x5) ? "6x6" : "6x5");
				}
			}
			else
			{
				text = "4x4";
			}
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Construction/P_" + text));
			gameObject.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
			gameObject.name = "ground";
			TreesParent = new GameObject("TreesParent");
			propCreator = gameObject.GetComponentInChildren<PropCreator>();
			GetAndBuildProps(propCreator);
			TreesParent.transform.parent = gameObject.transform;
			gameObject.transform.Rotate(0f, Orientation, 0f);
			gameObject.transform.parent = transform;
			for (int i = 0; i < vehiclesGOs.Count; i++)
			{
				vehiclesPos.Add(vehiclesGOs[i].transform.position);
			}
			vehiclesGOs.Clear();
			Ground = gameObject;
			Object.Destroy(TreesParent);
			UpdateBlockData(props: true, flora: true, terrain: false);
		}
	}

	public IEnumerator RublesTransform()
	{
		if (houseRenderer == null)
		{
			yield break;
		}
		if (!isDecayed)
		{
			DecayLot(toParking: false);
		}
		yield return new WaitForEndOfFrame();
		if (houseRenderer == null)
		{
			yield break;
		}
		Vector3 down = new Vector3(0f, 0.02f, 0f);
		GameObject house = houseRenderer.transform.gameObject;
		string text = ((Stage >= 3) ? "RubbleM" : "RubbleS");
		int num = ((Random.value <= 0.5f) ? 180 : 0);
		GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load("Construction/" + text));
		gameObject.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		gameObject.transform.Rotate(0f, Orientation + num, 0f, Space.Self);
		gameObject.transform.parent = transform;
		for (int i = 0; i < (10 + Stage * Stage) * 30; i++)
		{
			yield return new WaitForEndOfFrame();
			if (house == null)
			{
				break;
			}
			Vector3 position = house.transform.position;
			house.transform.position = position - down;
			down.y += 0.002f;
		}
		if (!(houseRenderer == null) && !(houseRenderer.gameObject == null))
		{
			Object.Destroy(houseRenderer.gameObject);
		}
	}

	private void UpdateBlockData(bool props, bool flora, bool terrain)
	{
		foreach (Vector2Int blockPo in blockPos)
		{
			BlockData blockData = r.w.blockData[blockPo.x, blockPo.y];
			int num = 0;
			if (lClass == LotClass.OFFICE || lClass == LotClass.UPPER || lClass == LotClass.RETAIL || lType == LotType.MIXED)
			{
				num = 10;
			}
			blockData.st = (sbyte)(Stage + num);
			blockData.t = BlockData.Type.BUILDING;
			blockData.lO = this;
			Chunk o = blockData.o;
			if (props)
			{
				o.updateProps = true;
			}
			if (flora)
			{
				o.updateFlora = true;
			}
			if (terrain)
			{
				o.updateTerrain = true;
			}
		}
	}

	private void PopulateGrass()
	{
		foreach (Vector2Int blockPo in blockPos)
		{
			BlockData blockData = r.w.blockData[blockPo.x, blockPo.y];
			if (Random.value < 0.8f)
			{
				blockData.g = true;
			}
		}
	}

	private void SetGroundColor()
	{
		foreach (Vector2Int blockPo in blockPos)
		{
			r.w.blockData[blockPo.x, blockPo.y].o.updateTerrain = true;
		}
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
		if (!(Ground == null))
		{
			if (audioSource == null)
			{
				audioSource = Ground.AddComponent<AudioSource>();
			}
			audioSource.clip = r.la.SetAudioClip(this);
			audioSource.volume = SettingsStatic.CityVolume;
			audioSource.maxDistance = 60f;
			audioSource.spatialBlend = 1f;
			audioSource.rolloffMode = AudioRolloffMode.Custom;
			audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, w.distanceCurve);
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
	}

	public IEnumerator ShitsGoingDown()
	{
		if (audioSource != null)
		{
			audioSource.enabled = true;
			float value = Random.value;
			if (Stage > 5)
			{
				audioSource.clip = r.la.buldozeLongA;
			}
			else if (Stage > 2)
			{
				if (value < 0.5f)
				{
					audioSource.clip = r.la.buldozeMediumA;
				}
				else
				{
					audioSource.clip = r.la.buldozeMediumB;
				}
			}
			else if (value < 0.5f)
			{
				audioSource.clip = r.la.buldozeShortA;
			}
			else
			{
				audioSource.clip = r.la.buldozeShortB;
			}
			audioSource.Play();
		}
		Vector3 down = new Vector3(0f, 0.02f, 0f);
		foreach (Vector2Int blockPo in blockPos)
		{
			w.GetBounded(blockPo.x, blockPo.y).lO = null;
		}
		for (int i = 0; i < (10 + Stage * Stage) * 20; i++)
		{
			yield return null;
			if (gameObject == null)
			{
				yield break;
			}
			Vector3 position = gameObject.transform.position;
			gameObject.transform.position = position - down;
			down.y += 0.002f;
		}
		yield return new WaitForSecondsRealtime(1.5f);
		if (!(gameObject == null))
		{
			Object.Destroy(gameObject);
		}
	}

	private string GetVacantPath()
	{
		string text;
		if (lSize == LotSize._2x4)
		{
			text = "2x4";
		}
		else if (lSize == LotSize._2x5)
		{
			text = "2x5";
		}
		else if (lSize == LotSize._2x6)
		{
			text = "2x6";
		}
		else if (lSize == LotSize._3x4)
		{
			text = "3x4";
		}
		else if (lSize == LotSize._3x5)
		{
			text = "3x5";
		}
		else if (lSize == LotSize._3x6)
		{
			text = "3x6";
		}
		else if (lSize != LotSize._4x4)
		{
			if (lSize == LotSize._4x5)
			{
				text = "4x5";
			}
			else if (lSize == LotSize._4x6)
			{
				text = "4x6";
			}
			else if (lSize == LotSize._6x4)
			{
				text = "6x4";
			}
			else
			{
				text = ((lSize != LotSize._6x5) ? "6x6" : "6x5");
			}
		}
		else
		{
			text = "4x4";
		}
		string text2;
		if (lType != LotType.RESIDENTIAL)
		{
			if (lType == LotType.COMMERCIAL)
			{
				text2 = "C";
			}
			else
			{
				text2 = ((lType != LotType.MIXED) ? "I" : "M");
			}
		}
		else
		{
			text2 = "R";
		}
		return "Lots/V/V_" + text2 + "_" + text;
	}

	private string GetGroundPath()
	{
		string text = "4x4";
		string text2 = "LC/LC1";
		if (lSize == LotSize._3x4)
		{
			text = "3x4";
		}
		else if (lSize == LotSize._4x4)
		{
			text = "4x4";
		}
		else if (lSize == LotSize._2x4)
		{
			text = "2x4";
		}
		else if (lSize == LotSize._2x5)
		{
			text = "2x5";
		}
		else if (lSize == LotSize._2x6)
		{
			text = "2x6";
		}
		else if (lSize == LotSize._3x5)
		{
			text = "3x5";
		}
		else if (lSize == LotSize._3x6)
		{
			text = "3x6";
		}
		else if (lSize == LotSize._4x5)
		{
			text = "4x5";
		}
		else if (lSize == LotSize._4x6)
		{
			text = "4x6";
		}
		else if (lSize == LotSize._6x4)
		{
			text = "6x4";
		}
		else if (lSize == LotSize._6x5)
		{
			text = "6x5";
		}
		else if (lSize == LotSize._6x6)
		{
			text = "6x6";
		}
		if (lClass == LotClass.LOW)
		{
			if (Stage == 1)
			{
				text2 = "LC/LC1";
			}
			else if (Stage == 2 || Stage == 3)
			{
				text2 = "LC/LC2";
			}
			else if (Stage >= 4)
			{
				text2 = "LC/LC3";
			}
			else if (Stage == 0)
			{
				if (Occupants > 300)
				{
					text2 = "MC/MC3";
				}
				else
				{
					text2 = ((Occupants <= 100) ? "MC/MC2" : "MC/MC2");
				}
			}
		}
		else if (lClass == LotClass.MIDDLE)
		{
			if (Stage == 1)
			{
				text2 = "MC/MC1";
			}
			else if (Stage == 2 || Stage == 3)
			{
				text2 = "MC/MC2";
			}
			else if (Stage >= 4)
			{
				text2 = "MC/MC3";
			}
		}
		else if (lClass == LotClass.UPPER)
		{
			if (Stage == 1)
			{
				text2 = "UC/UC1";
			}
			else if (Stage == 2 || Stage == 3)
			{
				text2 = "UC/UC2";
			}
			else if (Stage >= 4)
			{
				text2 = "UC/UC3";
			}
		}
		else if (lClass == LotClass.MIXRETAIL)
		{
			text2 = ((Stage >= 3) ? "M/M" : "M/M");
		}
		else if (lClass == LotClass.MIXOFFICE)
		{
			text2 = ((Stage >= 3) ? "M/M" : "M/M");
		}
		else if (lClass == LotClass.MANUFACTURE || lClass == LotClass.PRIMARY)
		{
			text2 = ((Stage != 1 || lClass == LotClass.PRIMARY) ? "BM/BM2" : "BM/BM1");
		}
		else if (lClass == LotClass.HIGHTECH)
		{
			if (Stage == 1)
			{
				text2 = "R/R1";
			}
			else if (Stage == 2 || Stage == 3)
			{
				text2 = "O/O";
			}
			else if (Stage > 3)
			{
				text2 = "R/R3";
			}
		}
		else if (lClass == LotClass.RETAIL)
		{
			if (Stage == 1)
			{
				text2 = "R/R1";
			}
			else
			{
				text2 = ((Stage != 2) ? "R/R3" : "R/R2");
			}
		}
		else if (lClass == LotClass.OFFICE)
		{
			if (Stage == 1)
			{
				text2 = "R/R2";
			}
			else if (Stage == 2)
			{
				text2 = "O/O";
			}
			else if (Stage >= 3)
			{
				text2 = "O/O2";
			}
		}
		return "Lots/" + text2 + "_" + text;
	}

	public void ChangeVacantLot()
	{
		StopAllCoroutines();
		foreach (Transform item in transform)
		{
			Object.Destroy(item.gameObject);
		}
		tempVacant = (GameObject)Object.Instantiate(Resources.Load(GetVacantPath()));
		tempVacant.transform.position = new Vector3(Anchor.x, wP.y + 0.504f, Anchor.z);
		tempVacant.name = wP.x + "_vacant_" + wP.z;
		tempVacant.transform.Rotate(0f, Orientation, 0f, Space.Self);
		tempVacant.transform.parent = transform;
	}

	public void DisplayDummy()
	{
		if (dummy != null || houseRenderer == null || lClass == LotClass.PRIMARY)
		{
			return;
		}
		string text;
		if (decayReason.StartsWith("Not"))
		{
			text = "DemD";
		}
		else if (decayReason.StartsWith("La"))
		{
			text = "PolluD";
		}
		else if (decayReason.StartsWith("NoW"))
		{
			text = "WorkD";
		}
		else if (decayReason.StartsWith("U"))
		{
			text = "UnempD";
		}
		else if (!watered)
		{
			text = "WatD";
		}
		else if (!powered)
		{
			text = "PowD";
		}
		else if (decayReason.StartsWith("Lo"))
		{
			text = "CrimD";
		}
		else if (decayReason.StartsWith("Vi"))
		{
			text = "CrimD";
		}
		else if (decayReason.StartsWith("No R"))
		{
			text = "CommuD";
		}
		else
		{
			if (!decayReason.StartsWith("Co"))
			{
				return;
			}
			text = "CommuD";
		}
		BoxCollider collider = GetCollider();
		if (collider == null)
		{
			Debug.Log("Collider null!");
			return;
		}
		dummy = (GameObject)Object.Instantiate(Resources.Load("Icons/" + text));
		Vector3 position = new Vector3(Anchor.x, collider.center.y * 2f + wP.y + 1f, Anchor.z);
		dummy.transform.position = position;
		dummy.transform.parent = gameObject.transform;
	}

	private void Reload()
	{
		if (reload)
		{
			negatives = 0f;
			if (lType == LotType.RESIDENTIAL)
			{
				if (Occupants > 0)
				{
					w.ResidentialBuildings.Add(this);
				}
				else
				{
					w.VacantResLots.Add(this);
				}
			}
			else if (lType == LotType.COMMERCIAL)
			{
				if (Occupants > 0)
				{
					w.CommercialBuildings.Add(this);
				}
				else
				{
					w.VacantCommLots.Add(this);
				}
			}
			else if (lType == LotType.INDUSTRIAL)
			{
				if (Occupants > 0)
				{
					w.IndustrialBuildings.Add(this);
				}
				else
				{
					w.VacantIndusLots.Add(this);
				}
			}
			else if (lType == LotType.MIXED)
			{
				if (Occupants > 0)
				{
					w.MixedBuildings.Add(this);
				}
				else
				{
					w.VacantMixedLots.Add(this);
				}
			}
			if (Occupants > 0 && !underConstruction)
			{
				Transform transform = base.transform.Find("ground");
				if (transform != null)
				{
					Ground = transform.gameObject;
				}
				else if ((lClass != LotClass.RETAIL || Occupants <= 500) && (lClass != LotClass.PRIMARY || Stage != 0 || Occupants <= 1))
				{
					Debug.Log("Can't find ground. type=" + lClass.ToString() + " Stage= " + Stage);
					foreach (Transform item in base.transform)
					{
						Object.Destroy(item.gameObject);
					}
					StartCoroutine(HouseConstruction());
					return;
				}
				if (Ground != null)
				{
					Transform transform2 = Ground.transform.Find(wP.x + "_house_" + wP.z);
					if (transform2 != null && transform2.gameObject != null)
					{
						houseRenderer = transform2.GetComponent<MeshRenderer>();
					}
					LoadGroundMesh();
				}
			}
			else if (isDecayed)
			{
				Transform transform3 = base.transform.Find("ground");
				if (transform3 != null)
				{
					Ground = transform3.gameObject;
					LoadGroundMesh();
				}
			}
			if (Occupants > 0 && Stage == 0 && (lClass != LotClass.RETAIL || Occupants <= 500) && lClass != LotClass.PRIMARY)
			{
				base.transform.position = new Vector3(Anchor.x, wP.y, Anchor.z);
				base.transform.Rotate(0f, Orientation, 0f);
				if (Ground != null)
				{
					Ground.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
					Ground.transform.localPosition = new Vector3(0f, 0.5f, 0f);
				}
				else
				{
					Debug.Log("Ground is null");
				}
			}
			r.w.blockData[Entry.x, Entry.y].o.lots.Add(this);
		}
		if (underConstruction)
		{
			foreach (Transform item2 in base.transform)
			{
				Object.Destroy(item2.gameObject);
			}
			if (Stage == 1)
			{
				StartCoroutine(HouseConstruction());
			}
			else if (Stage == 2)
			{
				StartCoroutine(SmallCondoConstruction(1));
			}
			else if (Stage == 3)
			{
				StartCoroutine(SmallCondoConstruction(2));
			}
			else if (Stage == 4)
			{
				StartCoroutine(MediumCondoConstruction(5));
			}
			else if (Stage == 5)
			{
				StartCoroutine(MediumCondoConstruction(10));
			}
			else if (Stage == 6)
			{
				StartCoroutine(MediumCondoConstruction(15));
			}
			else if (Stage == 7)
			{
				StartCoroutine(MediumCondoConstruction(20));
			}
			else
			{
				Debug.Log("Could not resume construction. Stage not added.");
			}
		}
		carsOut = 0;
		pedsOut = 0;
		UpdateBlockData(props: true, flora: true, terrain: true);
	}

	private void LoadGroundMesh()
	{
		MeshFilter component = Ground.GetComponent<MeshFilter>();
		if (!(component.sharedMesh != null))
		{
			GameObject gameObject = (GameObject)Object.Instantiate(Resources.Load(GetGroundPath()));
			MeshFilter component2 = gameObject.GetComponent<MeshFilter>();
			component.sharedMesh = component2.sharedMesh;
			Object.Destroy(gameObject);
		}
	}

	public BoxCollider GetCollider()
	{
		if (houseRenderer == null)
		{
			return null;
		}
		BoxCollider component = houseRenderer.gameObject.GetComponent<BoxCollider>();
		if (component == null)
		{
			return null;
		}
		return component;
	}

	public void SetMaxStage()
	{
		if (lSize == LotSize._2x4 || lSize == LotSize._2x5 || lSize == LotSize._2x6)
		{
			lWidth = 2;
		}
		else if (lSize == LotSize._3x4 || lSize == LotSize._3x5 || lSize == LotSize._3x6)
		{
			lWidth = 3;
		}
		else if (lSize == LotSize._4x4 || lSize == LotSize._4x5 || lSize == LotSize._4x6)
		{
			lWidth = 4;
		}
		else
		{
			lWidth = 6;
		}
		short num;
		if (lType != LotType.RESIDENTIAL)
		{
			if (lType == LotType.MIXED)
			{
				if (lWidth < 3)
				{
					num = 2;
				}
				else
				{
					num = (short)((lWidth >= 4) ? 4 : 2);
				}
			}
			else
			{
				num = (short)((lClass == LotClass.HIGHTECH || lClass == LotClass.MANUFACTURE) ? ((lWidth < 3) ? 1 : ((lWidth >= 4) ? 4 : 3)) : ((lClass == LotClass.PRIMARY) ? 1 : ((lClass == LotClass.RETAIL) ? ((lWidth < 3) ? 2 : ((lWidth >= 4) ? 5 : 3)) : ((lClass == LotClass.OFFICE) ? ((lWidth < 3) ? 1 : ((lWidth >= 4) ? 6 : 4)) : ((lType == LotType.COMMERCIAL) ? ((lWidth < 3) ? 2 : ((lWidth >= 4) ? 6 : 4)) : ((lWidth < 3) ? 1 : ((lWidth >= 4) ? 4 : 3)))))));
			}
		}
		else if (lWidth < 3)
		{
			num = 4;
		}
		else
		{
			num = (short)((lWidth >= 4) ? 7 : 6);
		}
		if (num < MaxStage)
		{
			MaxStage = num;
		}
	}

	public void FixMissingMaterials()
	{
		if (houseRenderer == null)
		{
			return;
		}
		Material[] sharedMaterials = houseRenderer.sharedMaterials;
		bool flag = false;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			if (sharedMaterials[i] == null)
			{
				sharedMaterials[i] = w.decayMat[1];
				flag = true;
			}
		}
		if (flag)
		{
			houseRenderer.sharedMaterials = sharedMaterials;
			Debug.Log("Fixed a missing material");
		}
	}
}
