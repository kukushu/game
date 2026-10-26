using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServiceTool : MonoBehaviour
{
	public Color green;

	public Color yellow;

	public Color red;

	public TextMeshProUGUI BuildingType;

	public TextMeshProUGUI Stage;

	public TextMeshProUGUI UpgradeText;

	public TextMeshProUGUI Capacity;

	public TextMeshProUGUI Use;

	public TextMeshProUGUI Cost;

	public TooltipTrigger UseTT;

	public TextMeshProUGUI EmployeeType;

	public TextMeshProUGUI EmployeeNumber;

	public Slider slider;

	public Sprite schoolLogo;

	public Sprite highschoolLogo;

	public Sprite hospitalLogo;

	public Sprite policeLogo;

	public Sprite fireLogo;

	public Sprite airLogo;

	public Sprite powerLogo;

	public Sprite waterLogo;

	public Sprite govLogo;

	public Sprite droneLogo;

	public Sprite portLogo;

	public ServiceCreator selectedSC;

	public GameObject tService;

	public GameObject serviceEditorWindow;

	public GameObject upgradeButton;

	public Image UniversalLogo;

	public GameObject GradeStuff;

	public GameObject Aiguille;

	public TextMeshProUGUI A;

	public TextMeshProUGUI B;

	public TextMeshProUGUI C;

	public TextMeshProUGUI D;

	public TextMeshProUGUI E;

	public TextMeshProUGUI F;

	public Color white;

	public Color grey;

	public GameObject dropdownGO;

	public TextMeshProUGUI cddGrade;

	public TextMeshProUGUI cddEmployee;

	public TextMeshProUGUI cddPolicies;

	public TextMeshProUGUI cddCapacity;

	public TMP_Dropdown listServices;

	public GameObject OpenClose;

	public Button B_Open;

	public Button B_Close;

	public GameObject selected;

	public World world;

	public Indexer indexer;

	public SoundManager sm;

	public CostHelper costHelper;

	public bool isDisplayed;

	public GameObject serviceAnchor;

	private List<ServiceCreator> clickableServices = new List<ServiceCreator>();

	public void OpenServiceEditor(ServiceCreator sc)
	{
		serviceEditorWindow.SetActive(value: true);
		isDisplayed = true;
		Indexer.sliderOnScreen = true;
		transform.position = serviceAnchor.transform.position;
		selectedSC = sc;
		indexer.bi.serviceSelector.transform.position = sc.Anchor;
		StartCoroutine(UpdateTexts());
		cddCapacity.text = Mathf.RoundToInt(selectedSC.capGrade) + " / 100";
		cddEmployee.text = Mathf.RoundToInt(selectedSC.employGrade) + " / 100";
		cddPolicies.text = Mathf.RoundToInt(selectedSC.polGrade) + " / 100";
		cddGrade.text = selectedSC.Grade + " / 100";
		if (sc.type == ServiceCreator.Type.Fire || sc.type == ServiceCreator.Type.Police || sc.type == ServiceCreator.Type.Prison || sc.type == ServiceCreator.Type.School || sc.type == ServiceCreator.Type.Hospital || sc.type == ServiceCreator.Type.Power || sc.type == ServiceCreator.Type.Water)
		{
			listServices.gameObject.SetActive(value: true);
			UpdateList();
		}
		else
		{
			listServices.gameObject.SetActive(value: false);
		}
	}

	private void UpdateList()
	{
		clickableServices.Clear();
		listServices.ClearOptions();
		List<string> list = new List<string>();
		int num = -1;
		int value = 0;
		foreach (ServiceCreator service in world.Services)
		{
			if (service.type == selectedSC.type)
			{
				num++;
				string item = ((service.type == ServiceCreator.Type.Water || service.type == ServiceCreator.Type.Power) ? (GetBuildingName(service) + " #" + num + " (" + service.HiredEmployees + "/" + service.MaxEmployees + ")") : (GetBuildingName(service) + " #" + num + " (" + service.Stage + "/" + service.MaxStage + ")"));
				list.Add(item);
				clickableServices.Add(service);
				if (service == selectedSC)
				{
					value = num;
				}
			}
		}
		listServices.AddOptions(list);
		listServices.value = value;
		listServices.RefreshShownValue();
	}

	public void SwitchToService()
	{
		ServiceCreator serviceCreator = clickableServices[listServices.value];
		if (!(serviceCreator == null))
		{
			selectedSC = serviceCreator;
			SetTexts();
			world.RTScam.LookAt = serviceCreator.Anchor;
		}
	}

	private string GetBuildingName(ServiceCreator sc)
	{
		if (sc.type == ServiceCreator.Type.School)
		{
			if (sc.typeName == "S")
			{
				return LOCSYS.Translate("School");
			}
			if (sc.typeName == "U")
			{
				return LOCSYS.Translate("University");
			}
			if (sc.typeName == "B")
			{
				return LOCSYS.Translate("Library");
			}
			return LOCSYS.Translate("HighSchool");
		}
		if (sc.type == ServiceCreator.Type.Hospital)
		{
			return LOCSYS.Translate("Hospital");
		}
		if (sc.type == ServiceCreator.Type.Fire)
		{
			return LOCSYS.Translate("FireStation");
		}
		if (sc.type == ServiceCreator.Type.Police)
		{
			return LOCSYS.Translate("PoliceStation");
		}
		if (sc.type == ServiceCreator.Type.Prison)
		{
			return LOCSYS.Translate(sc.typeName);
		}
		if (sc.type == ServiceCreator.Type.Drone)
		{
			return LOCSYS.Translate(sc.typeName);
		}
		if (sc.type == ServiceCreator.Type.Airport)
		{
			return LOCSYS.Translate("Airport");
		}
		if (sc.type == ServiceCreator.Type.Port)
		{
			return LOCSYS.Translate("Port");
		}
		if (sc.type == ServiceCreator.Type.Power)
		{
			if (sc.typeName == "C")
			{
				return LOCSYS.Translate("Coal");
			}
			if (sc.typeName == "D")
			{
				return LOCSYS.Translate("Diesel");
			}
			if (sc.typeName == "N")
			{
				return LOCSYS.Translate("Nuclear");
			}
			if (sc.typeName == "F")
			{
				return LOCSYS.Translate("Hydrogen");
			}
			if (sc.typeName == "W")
			{
				return LOCSYS.Translate("Turbine");
			}
			return LOCSYS.Translate("Solar");
		}
		if (sc.type == ServiceCreator.Type.Water)
		{
			return LOCSYS.Translate("Pumping");
		}
		if (sc.type == ServiceCreator.Type.Government)
		{
			return LOCSYS.Translate(sc.typeName);
		}
		return "Unknow";
	}

	private void SetTexts()
	{
		slider.maxValue = 500000f;
		slider.minValue = 0f;
		ServiceCreator serviceCreator = selectedSC;
		if (serviceCreator == null)
		{
			return;
		}
		slider.value = serviceCreator.HiredEmployees;
		slider.maxValue = serviceCreator.MaxEmployees;
		slider.minValue = serviceCreator.MinEmployees;
		if (slider.maxValue == slider.minValue)
		{
			slider.gameObject.SetActive(value: false);
		}
		else
		{
			slider.gameObject.SetActive(value: true);
		}
		SetGradeAiguille(serviceCreator);
		if (serviceCreator.Stage >= serviceCreator.MaxStage)
		{
			upgradeButton.SetActive(value: false);
		}
		else
		{
			upgradeButton.SetActive(value: true);
			SetUpdradeCost(serviceCreator);
		}
		if (serviceCreator.type == ServiceCreator.Type.Government)
		{
			OpenClose.SetActive(value: false);
			upgradeButton.SetActive(value: false);
		}
		else
		{
			OpenClose.SetActive(value: true);
		}
		if (serviceCreator.open)
		{
			selected.transform.position = B_Open.gameObject.transform.position;
		}
		else
		{
			selected.transform.position = B_Close.gameObject.transform.position;
		}
		if (serviceCreator.type == ServiceCreator.Type.School)
		{
			EmployeeType.text = LOCSYS.Translate("Teachers");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity;
			Use.text = LOCSYS.Translate("Students") + serviceCreator.Use;
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "peopleService";
			Cost.text = "$" + serviceCreator.Cost;
			if (serviceCreator.typeName == "S")
			{
				BuildingType.text = LOCSYS.Translate("School");
			}
			else if (serviceCreator.typeName == "U")
			{
				BuildingType.text = LOCSYS.Translate("University");
				EmployeeType.text = LOCSYS.Translate("Fictitious");
				UseTT.content = "govService";
			}
			else if (serviceCreator.typeName == "B")
			{
				BuildingType.text = LOCSYS.Translate("Library");
				EmployeeType.text = LOCSYS.Translate("Books");
				Capacity.text = "";
				Use.text = "";
			}
			else
			{
				BuildingType.text = LOCSYS.Translate("HighSchool");
			}
			UniversalLogo.sprite = schoolLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Hospital)
		{
			BuildingType.text = LOCSYS.Translate("Hospital");
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity;
			Use.text = LOCSYS.Translate("Patients") + serviceCreator.Use;
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "peopleService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Physicians");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = hospitalLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Fire)
		{
			BuildingType.text = LOCSYS.Translate("FireStation");
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity + LOCSYS.Translate("Buildings");
			Use.text = LOCSYS.Translate("Assigned") + serviceCreator.Use + " " + LOCSYS.Translate("Buildings");
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "peopleService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Firemen");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = fireLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Police)
		{
			BuildingType.text = LOCSYS.Translate("PoliceStation");
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity + " " + LOCSYS.Translate("Buildings");
			Use.text = LOCSYS.Translate("Assigned") + serviceCreator.Use + " " + LOCSYS.Translate("Buildings");
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "peopleService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Policemen");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = policeLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Prison)
		{
			BuildingType.text = LOCSYS.Translate(serviceCreator.typeName);
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity;
			Use.text = LOCSYS.Translate("Inmates") + ": " + serviceCreator.Use + " " + LOCSYS.Translate("Buildings");
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "peopleService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Guards");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = policeLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Drone)
		{
			BuildingType.text = LOCSYS.Translate(serviceCreator.typeName);
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity;
			Use.text = LOCSYS.Translate("Passengers") + serviceCreator.Use;
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "peopleService";
			Cost.text = "$" + serviceCreator.Maintenance;
			EmployeeType.text = "";
			EmployeeNumber.text = "";
			UniversalLogo.sprite = droneLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Airport)
		{
			BuildingType.text = LOCSYS.Translate("Airport");
			Stage.text = LOCSYS.Translate("Stage ") + (serviceCreator.Stage + 1) + " / " + (serviceCreator.MaxStage + 1);
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity;
			Use.text = LOCSYS.Translate("Passengers") + serviceCreator.Use;
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "airService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Connections");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = airLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Port)
		{
			BuildingType.text = LOCSYS.Translate("Port");
			Stage.text = LOCSYS.Translate("Stage ") + "1 / 1";
			Capacity.text = LOCSYS.Translate("Capacity") + serviceCreator.Capacity;
			Use.text = LOCSYS.Translate("Traffic") + ": " + serviceCreator.Use;
			SetColor(serviceCreator.Use, Use, serviceCreator.Capacity);
			UseTT.content = "";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = "";
			EmployeeNumber.text = "";
			UniversalLogo.sprite = portLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Power)
		{
			if (serviceCreator.typeName == "C")
			{
				BuildingType.text = LOCSYS.Translate("Coal");
			}
			else if (serviceCreator.typeName == "D")
			{
				BuildingType.text = LOCSYS.Translate("Diesel");
			}
			else if (serviceCreator.typeName == "N")
			{
				BuildingType.text = LOCSYS.Translate("Nuclear");
			}
			else if (serviceCreator.typeName == "F")
			{
				BuildingType.text = LOCSYS.Translate("Hydrogen");
			}
			else if (serviceCreator.typeName == "W")
			{
				BuildingType.text = LOCSYS.Translate("Turbine");
			}
			else if (serviceCreator.typeName == "S")
			{
				BuildingType.text = LOCSYS.Translate("Solar");
			}
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Production") + serviceCreator.Capacity * serviceCreator.HiredEmployees;
			Use.text = LOCSYS.Translate("Power Used") + serviceCreator.Use + "%";
			SetColor(serviceCreator.Use, Use);
			UseTT.content = "prodService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Generators Used");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = powerLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Water)
		{
			BuildingType.text = LOCSYS.Translate("Pumping");
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = LOCSYS.Translate("Production") + serviceCreator.Capacity * serviceCreator.HiredEmployees;
			Use.text = LOCSYS.Translate("Water Used") + serviceCreator.Use + "%";
			SetColor(serviceCreator.Use, Use);
			UseTT.content = "prodService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Pumps Used");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = waterLogo;
		}
		else if (serviceCreator.type == ServiceCreator.Type.Government)
		{
			BuildingType.text = LOCSYS.Translate(serviceCreator.typeName);
			Stage.text = LOCSYS.Translate("Stage ") + serviceCreator.Stage + " / " + serviceCreator.MaxStage;
			Capacity.text = "";
			Use.text = "";
			UseTT.content = "govService";
			Cost.text = "$" + serviceCreator.Cost;
			EmployeeType.text = LOCSYS.Translate("Fictitious");
			EmployeeNumber.text = serviceCreator.HiredEmployees + " / " + serviceCreator.MaxEmployees;
			UniversalLogo.sprite = govLogo;
		}
		if (!serviceCreator.open)
		{
			Use.text = LOCSYS.Translate("closed");
			Capacity.text = "0";
			Use.color = red;
		}
	}

	public void SetEmployees()
	{
		selectedSC.HiredEmployees = Mathf.RoundToInt(slider.value);
		if (Messages.step == 16 && selectedSC.HiredEmployees == 2)
		{
			Messages.counter++;
		}
		SetTexts();
	}

	public void CloseServiceEditor()
	{
		serviceEditorWindow.SetActive(value: false);
		StopCoroutine(UpdateTexts());
		isDisplayed = false;
		Indexer.sliderOnScreen = false;
	}

	private void SetColor(int use, TextMeshProUGUI t, int cap = 100)
	{
		if (use > cap)
		{
			t.color = red;
		}
		else if ((float)use * 1.11f > (float)cap)
		{
			t.color = yellow;
		}
		else
		{
			t.color = green;
		}
	}

	public void UpgradeButton()
	{
		int cost = SetUpdradeCost(selectedSC);
		if (selectedSC.type == ServiceCreator.Type.School)
		{
			if (selectedSC.typeName == "S")
			{
				SchoolUpgrade(cost);
			}
			else if (selectedSC.typeName == "H")
			{
				HighSchoolUpgrade(cost);
			}
			if (Messages.step == 21)
			{
				Messages.counter++;
			}
		}
		else if (selectedSC.type == ServiceCreator.Type.Hospital)
		{
			HospitalUpgrade(cost);
		}
		else if (selectedSC.type == ServiceCreator.Type.Fire)
		{
			FireUpgrade(cost);
		}
		else if (selectedSC.type == ServiceCreator.Type.Police)
		{
			PoliceUpgrade(cost);
		}
		else if (selectedSC.type == ServiceCreator.Type.Prison)
		{
			PrisonUpgrade(cost);
		}
		else if (selectedSC.type == ServiceCreator.Type.Airport)
		{
			AirportUpgrade(cost);
		}
		else if (selectedSC.type == ServiceCreator.Type.Drone)
		{
			DroneUpgrade(cost);
		}
		sm.Play("PlopBuild");
	}

	public void OpenBuilding()
	{
		ServiceCreator serviceCreator = selectedSC;
		if (!(serviceCreator == null))
		{
			serviceCreator.open = true;
			selected.transform.position = B_Open.gameObject.transform.position;
		}
	}

	public void CloseBuilding()
	{
		ServiceCreator serviceCreator = selectedSC;
		if (!(serviceCreator == null))
		{
			serviceCreator.open = false;
			selected.transform.position = B_Close.gameObject.transform.position;
		}
	}

	public void HQUpdate(int newStage, ServiceCreator HQ)
	{
		switch (newStage)
		{
		case 2:
		{
			world.blockData[HQ.Entry.x, HQ.Entry.y].o.services.Remove(HQ);
			world.Services.Remove(HQ);
			GameObject gameObject4 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component4 = gameObject4.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Government/GovHQ_S2"))).transform.parent = gameObject4.transform;
			gameObject4.transform.position = HQ.transform.position;
			gameObject4.name = HQ.worldPos.x + "_HQ_" + HQ.worldPos.z;
			component4.Capacity = 1;
			component4.Maintenance = 150f;
			component4.type = ServiceCreator.Type.Government;
			component4.MinEmployees = 0;
			component4.MaxEmployees = 5000;
			component4.FixedEmployees = 0;
			component4.HiredEmployees = 0;
			component4.Stage = 2;
			component4.MaxStage = 5;
			component4.worldPos = HQ.worldPos;
			component4.Orientation = HQ.Orientation;
			component4.Anchor = HQ.Anchor;
			component4.Entry = HQ.Entry;
			component4.TreesParent = new GameObject("TreesParent");
			component4.TreesParent.transform.parent = component4.gameObject.transform;
			component4.typeName = "HQsc";
			component4.blockPos = HQ.blockPos;
			indexer.eco.HQbuilding = component4;
			world.blockData[component4.Entry.x, component4.Entry.y].o.services.Add(component4);
			world.Services.Add(component4);
			foreach (Vector2Int blockPo in HQ.blockPos)
			{
				BlockData blockData4 = world.blockData[blockPo.x, blockPo.y];
				blockData4.t = BlockData.Type.BUILDING;
				blockData4.sO = component4;
			}
			component4.SpawnTree(new Vector3(component4.Anchor.x + 3.6f, component4.worldPos.y, component4.Anchor.z + 3.6f));
			component4.SpawnTree(new Vector3(component4.Anchor.x + 3.6f, component4.worldPos.y, component4.Anchor.z + 2.4f));
			component4.SpawnTree(new Vector3(component4.Anchor.x - 3.6f, component4.worldPos.y, component4.Anchor.z + 2.4f));
			component4.SpawnTree(new Vector3(component4.Anchor.x - 3.6f, component4.worldPos.y, component4.Anchor.z + 3.6f));
			component4.SpawnTree(new Vector3(component4.Anchor.x - 1.3f, component4.worldPos.y, component4.Anchor.z - 3.45f));
			component4.SpawnTree(new Vector3(component4.Anchor.x + 1.3f, component4.worldPos.y, component4.Anchor.z - 3.45f));
			component4.CustomSpawnCar(new Vector2(1.15f, 1.9f), 270);
			component4.CustomSpawnCar(new Vector2(-1.05f, 1.9f), 270);
			gameObject4.transform.Rotate(0f, HQ.Orientation, 0f, Space.Self);
			gameObject4.transform.parent = tService.transform;
			component4.CreateVehicles(world);
			component4.CreateTrees(world);
			Object.Destroy(HQ.gameObject);
			break;
		}
		case 3:
		{
			world.blockData[HQ.Entry.x, HQ.Entry.y].o.services.Remove(HQ);
			world.Services.Remove(HQ);
			GameObject gameObject2 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component2 = gameObject2.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Government/GovHQ_S3"))).transform.parent = gameObject2.transform;
			gameObject2.transform.position = HQ.transform.position;
			gameObject2.name = HQ.worldPos.x + "_HQ_" + HQ.worldPos.z;
			component2.Capacity = 1;
			component2.Maintenance = 300f;
			component2.type = ServiceCreator.Type.Government;
			component2.MinEmployees = 0;
			component2.MaxEmployees = 5000;
			component2.FixedEmployees = 0;
			component2.HiredEmployees = 0;
			component2.Stage = 3;
			component2.MaxStage = 5;
			component2.worldPos = HQ.worldPos;
			component2.Orientation = HQ.Orientation;
			component2.Anchor = HQ.Anchor;
			component2.Entry = HQ.Entry;
			component2.TreesParent = new GameObject("TreesParent");
			component2.TreesParent.transform.parent = component2.gameObject.transform;
			component2.typeName = "HQsc";
			component2.blockPos = HQ.blockPos;
			indexer.eco.HQbuilding = component2;
			world.blockData[component2.Entry.x, component2.Entry.y].o.services.Add(component2);
			world.Services.Add(component2);
			foreach (Vector2Int blockPo2 in HQ.blockPos)
			{
				BlockData blockData2 = world.blockData[blockPo2.x, blockPo2.y];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component2;
			}
			component2.SpawnTree(new Vector3(component2.Anchor.x + 3.6f, component2.worldPos.y, component2.Anchor.z + 3.6f));
			component2.SpawnTree(new Vector3(component2.Anchor.x + 3.6f, component2.worldPos.y, component2.Anchor.z + 2f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 3.6f, component2.worldPos.y, component2.Anchor.z + 2f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 3.6f, component2.worldPos.y, component2.Anchor.z + 3.6f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 1.3f, component2.worldPos.y, component2.Anchor.z - 3.45f));
			component2.SpawnTree(new Vector3(component2.Anchor.x + 1.3f, component2.worldPos.y, component2.Anchor.z - 3.45f));
			component2.CustomSpawnCar(new Vector2(1.15f, 1.9f), 270);
			component2.CustomSpawnCar(new Vector2(-1.05f, 1.9f), 270);
			gameObject2.transform.Rotate(0f, HQ.Orientation, 0f, Space.Self);
			gameObject2.transform.parent = tService.transform;
			component2.CreateVehicles(world);
			component2.CreateTrees(world);
			Object.Destroy(HQ.gameObject);
			break;
		}
		case 4:
		{
			world.blockData[HQ.Entry.x, HQ.Entry.y].o.services.Remove(HQ);
			world.Services.Remove(HQ);
			GameObject gameObject3 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component3 = gameObject3.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Government/GovHQ_S4"))).transform.parent = gameObject3.transform;
			gameObject3.transform.position = HQ.transform.position;
			gameObject3.name = HQ.worldPos.x + "_HQ_" + HQ.worldPos.z;
			component3.Capacity = 1;
			component3.Maintenance = 300f;
			component3.type = ServiceCreator.Type.Government;
			component3.MinEmployees = 0;
			component3.MaxEmployees = 5000;
			component3.FixedEmployees = 0;
			component3.HiredEmployees = 0;
			component3.Stage = 4;
			component3.MaxStage = 5;
			component3.worldPos = HQ.worldPos;
			component3.Orientation = HQ.Orientation;
			component3.Anchor = HQ.Anchor;
			component3.Entry = HQ.Entry;
			component3.TreesParent = new GameObject("TreesParent");
			component3.TreesParent.transform.parent = component3.gameObject.transform;
			component3.typeName = "HQsc";
			component3.blockPos = HQ.blockPos;
			indexer.eco.HQbuilding = component3;
			world.blockData[component3.Entry.x, component3.Entry.y].o.services.Add(component3);
			world.Services.Add(component3);
			foreach (Vector2Int blockPo3 in HQ.blockPos)
			{
				BlockData blockData3 = world.blockData[blockPo3.x, blockPo3.y];
				blockData3.t = BlockData.Type.BUILDING;
				blockData3.sO = component3;
			}
			component3.SpawnTree(new Vector3(component3.Anchor.x + 3.6f, component3.worldPos.y, component3.Anchor.z + 3.6f));
			component3.SpawnTree(new Vector3(component3.Anchor.x + 3.6f, component3.worldPos.y, component3.Anchor.z + 2f));
			component3.SpawnTree(new Vector3(component3.Anchor.x - 3.6f, component3.worldPos.y, component3.Anchor.z + 2f));
			component3.SpawnTree(new Vector3(component3.Anchor.x - 3.6f, component3.worldPos.y, component3.Anchor.z + 3.6f));
			component3.SpawnTree(new Vector3(component3.Anchor.x - 1.3f, component3.worldPos.y, component3.Anchor.z - 3.45f));
			component3.SpawnTree(new Vector3(component3.Anchor.x + 1.3f, component3.worldPos.y, component3.Anchor.z - 3.45f));
			component3.CustomSpawnCar(new Vector2(1.15f, 1.9f), 270);
			component3.CustomSpawnCar(new Vector2(-1.05f, 1.9f), 270);
			gameObject3.transform.Rotate(0f, HQ.Orientation, 0f, Space.Self);
			gameObject3.transform.parent = tService.transform;
			component3.CreateVehicles(world);
			component3.CreateTrees(world);
			Object.Destroy(HQ.gameObject);
			break;
		}
		case 5:
		{
			world.blockData[HQ.Entry.x, HQ.Entry.y].o.services.Remove(HQ);
			world.Services.Remove(HQ);
			GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Government/GovHQ_S5"))).transform.parent = gameObject.transform;
			gameObject.transform.position = HQ.transform.position;
			gameObject.name = HQ.worldPos.x + "_HQ_" + HQ.worldPos.z;
			component.Capacity = 1;
			component.Maintenance = 300f;
			component.type = ServiceCreator.Type.Government;
			component.MinEmployees = 0;
			component.MaxEmployees = 5000;
			component.FixedEmployees = 0;
			component.HiredEmployees = 0;
			component.Stage = 5;
			component.MaxStage = 5;
			component.worldPos = HQ.worldPos;
			component.Orientation = HQ.Orientation;
			component.Anchor = HQ.Anchor;
			component.Entry = HQ.Entry;
			component.TreesParent = new GameObject("TreesParent");
			component.TreesParent.transform.parent = component.gameObject.transform;
			component.typeName = "HQsc";
			component.blockPos = HQ.blockPos;
			indexer.eco.HQbuilding = component;
			world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
			world.Services.Add(component);
			foreach (Vector2Int blockPo4 in HQ.blockPos)
			{
				BlockData blockData = world.blockData[blockPo4.x, blockPo4.y];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
			}
			component.SpawnTree(new Vector3(component.Anchor.x + 3.6f, component.worldPos.y, component.Anchor.z + 3.6f));
			component.SpawnTree(new Vector3(component.Anchor.x + 3.6f, component.worldPos.y, component.Anchor.z + 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.6f, component.worldPos.y, component.Anchor.z + 2f));
			component.SpawnTree(new Vector3(component.Anchor.x - 3.6f, component.worldPos.y, component.Anchor.z + 3.6f));
			component.CustomSpawnCar(new Vector2(1.15f, 1.9f), 270);
			component.CustomSpawnCar(new Vector2(-1.05f, 1.9f), 270);
			gameObject.transform.Rotate(0f, HQ.Orientation, 0f, Space.Self);
			gameObject.transform.parent = tService.transform;
			component.CreateVehicles(world);
			component.CreateTrees(world);
			Object.Destroy(HQ.gameObject);
			break;
		}
		}
	}

	private void SchoolUpgrade(int cost)
	{
		if (selectedSC.Stage == 1)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/School_Evo2"))).transform.parent = gameObject.transform;
			gameObject.transform.position = selectedSC.transform.position;
			gameObject.name = selectedSC.worldPos.x + "_school_" + selectedSC.worldPos.z;
			component.Capacity = 480;
			component.Maintenance = 75f;
			component.MinEmployees = 12;
			component.MaxEmployees = 24;
			component.FixedEmployees = 6;
			component.HiredEmployees = 18;
			component.Stage = 2;
			component.MaxStage = 4;
			component.Use = 0;
			component.type = ServiceCreator.Type.School;
			component.worldPos = selectedSC.worldPos;
			component.Orientation = selectedSC.Orientation;
			component.Anchor = selectedSC.Anchor;
			component.Entry = selectedSC.Entry;
			component.TreesParent = new GameObject("TreesParent");
			component.TreesParent.transform.parent = component.gameObject.transform;
			component.blockPos = selectedSC.blockPos;
			component.typeName = "S";
			float num = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component.HiredEmployees = Mathf.RoundToInt(num / 100f * (float)(component.MaxEmployees - component.MinEmployees) + (float)component.MinEmployees);
			world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
			world.Services.Add(component);
			foreach (Vector2Int blockPo in selectedSC.blockPos)
			{
				BlockData blockData = world.blockData[blockPo.x, blockPo.y];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
			}
			component.CustomSpawnCar(new Vector2(0.34f, 1.76f), 90);
			component.CustomSpawnCar(new Vector2(-0.53f, 1.76f), 90);
			component.SpawnTree(new Vector3(component.Anchor.x + 1.11f, component.worldPos.y, component.Anchor.z - 1.48f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.1f, component.worldPos.y, component.Anchor.z + 1.82f));
			gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject.transform.parent = tService.transform;
			component.CreateVehicles(world);
			component.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component);
		}
		else if (selectedSC.Stage == 2)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject2 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component2 = gameObject2.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/School_Evo3"))).transform.parent = gameObject2.transform;
			gameObject2.transform.position = selectedSC.transform.position;
			gameObject2.name = selectedSC.worldPos.x + "_school_" + selectedSC.worldPos.z;
			component2.Capacity = 720;
			component2.Maintenance = 100f;
			component2.MinEmployees = 18;
			component2.MaxEmployees = 36;
			component2.FixedEmployees = 10;
			component2.HiredEmployees = 27;
			component2.Stage = 3;
			component2.MaxStage = 4;
			component2.Use = 0;
			component2.type = ServiceCreator.Type.School;
			component2.worldPos = selectedSC.worldPos;
			component2.Orientation = selectedSC.Orientation;
			component2.Anchor = selectedSC.Anchor;
			component2.Entry = selectedSC.Entry;
			component2.TreesParent = new GameObject("TreesParent");
			component2.TreesParent.transform.parent = component2.gameObject.transform;
			component2.blockPos = selectedSC.blockPos;
			component2.typeName = "S";
			float num2 = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component2.HiredEmployees = Mathf.RoundToInt(num2 / 100f * (float)(component2.MaxEmployees - component2.MinEmployees) + (float)component2.MinEmployees);
			world.blockData[component2.Entry.x, component2.Entry.y].o.services.Add(component2);
			world.Services.Add(component2);
			foreach (Vector2Int blockPo2 in selectedSC.blockPos)
			{
				BlockData blockData2 = world.blockData[blockPo2.x, blockPo2.y];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component2;
			}
			component2.CustomSpawnCar(new Vector2(0.34f, 1.76f), 90);
			component2.CustomSpawnCar(new Vector2(-0.53f, 1.76f), 90);
			component2.CustomSpawnCar(new Vector2(1.2f, 1.76f), 90);
			component2.SpawnTree(new Vector3(component2.Anchor.x - 1.1f, component2.worldPos.y, component2.Anchor.z + 1.82f));
			gameObject2.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject2.transform.parent = tService.transform;
			component2.CreateVehicles(world);
			component2.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component2);
		}
		else
		{
			if (selectedSC.Stage != 3)
			{
				return;
			}
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject3 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component3 = gameObject3.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/School_Evo4"))).transform.parent = gameObject3.transform;
			gameObject3.transform.position = selectedSC.transform.position;
			gameObject3.name = selectedSC.worldPos.x + "_school_" + selectedSC.worldPos.z;
			component3.Capacity = 1800;
			component3.Maintenance = 250f;
			component3.MinEmployees = 45;
			component3.MaxEmployees = 90;
			component3.FixedEmployees = 25;
			component3.HiredEmployees = 68;
			component3.Stage = 4;
			component3.MaxStage = 4;
			component3.Use = 0;
			component3.type = ServiceCreator.Type.School;
			component3.worldPos = selectedSC.worldPos;
			component3.Orientation = selectedSC.Orientation;
			component3.Anchor = selectedSC.Anchor;
			component3.Entry = selectedSC.Entry;
			component3.TreesParent = new GameObject("TreesParent");
			component3.TreesParent.transform.parent = component3.gameObject.transform;
			component3.blockPos = selectedSC.blockPos;
			component3.typeName = "S";
			float num3 = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component3.HiredEmployees = Mathf.RoundToInt(num3 / 100f * (float)(component3.MaxEmployees - component3.MinEmployees) + (float)component3.MinEmployees);
			world.blockData[component3.Entry.x, component3.Entry.y].o.services.Add(component3);
			world.Services.Add(component3);
			foreach (Vector2Int blockPo3 in selectedSC.blockPos)
			{
				BlockData blockData3 = world.blockData[blockPo3.x, blockPo3.y];
				blockData3.t = BlockData.Type.BUILDING;
				blockData3.sO = component3;
			}
			component3.CustomSpawnCar(new Vector2(0.34f, 1.76f), 90);
			component3.CustomSpawnCar(new Vector2(-0.53f, 1.76f), 90);
			component3.CustomSpawnCar(new Vector2(1.2f, 1.76f), 90);
			component3.SpawnTree(new Vector3(component3.Anchor.x - 1.75f, component3.worldPos.y, component3.Anchor.z - 1.75f));
			gameObject3.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject3.transform.parent = tService.transform;
			component3.CreateVehicles(world);
			component3.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component3);
		}
	}

	private void DroneUpgrade(int cost)
	{
		if (selectedSC.Stage == 1)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/AirTaxi_Evo2"))).transform.parent = gameObject.transform;
			gameObject.transform.position = selectedSC.transform.position;
			gameObject.name = selectedSC.worldPos.x + "_drone_" + selectedSC.worldPos.z;
			component.Capacity = 60000;
			component.Maintenance = 1700f;
			component.MinEmployees = 1;
			component.MaxEmployees = 1;
			component.FixedEmployees = 1;
			component.HiredEmployees = 1;
			component.Stage = 2;
			component.MaxStage = 3;
			component.Use = 0;
			component.type = ServiceCreator.Type.Drone;
			component.worldPos = selectedSC.worldPos;
			component.Orientation = selectedSC.Orientation;
			component.Anchor = selectedSC.Anchor;
			component.Entry = selectedSC.Entry;
			component.TreesParent = new GameObject("TreesParent");
			component.TreesParent.transform.parent = component.gameObject.transform;
			component.blockPos = selectedSC.blockPos;
			component.typeName = "Drone";
			world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
			world.Services.Add(component);
			foreach (Vector2Int blockPo in selectedSC.blockPos)
			{
				BlockData blockData = world.blockData[blockPo.x, blockPo.y];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
			}
			component.SpawnTree(new Vector3(component.Anchor.x + 1.5f, component.worldPos.y, component.Anchor.z + 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x - 1.5f, component.worldPos.y, component.Anchor.z + 1.5f));
			component.SpawnTree(new Vector3(component.Anchor.x + 1.5f, component.worldPos.y, component.Anchor.z - 1.5f));
			gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject.transform.parent = tService.transform;
			component.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component);
		}
		else
		{
			if (selectedSC.Stage != 2)
			{
				return;
			}
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject2 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component2 = gameObject2.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/AirTaxi_Evo3"))).transform.parent = gameObject2.transform;
			gameObject2.transform.position = selectedSC.transform.position;
			gameObject2.name = selectedSC.worldPos.x + "_drone_" + selectedSC.worldPos.z;
			component2.Capacity = 90000;
			component2.Maintenance = 2400f;
			component2.MinEmployees = 1;
			component2.MaxEmployees = 1;
			component2.FixedEmployees = 1;
			component2.HiredEmployees = 1;
			component2.Stage = 3;
			component2.MaxStage = 3;
			component2.Use = 0;
			component2.type = ServiceCreator.Type.Drone;
			component2.worldPos = selectedSC.worldPos;
			component2.Orientation = selectedSC.Orientation;
			component2.Anchor = selectedSC.Anchor;
			component2.Entry = selectedSC.Entry;
			component2.TreesParent = new GameObject("TreesParent");
			component2.TreesParent.transform.parent = component2.gameObject.transform;
			component2.blockPos = selectedSC.blockPos;
			component2.typeName = "Drone";
			world.blockData[component2.Entry.x, component2.Entry.y].o.services.Add(component2);
			world.Services.Add(component2);
			foreach (Vector2Int blockPo2 in selectedSC.blockPos)
			{
				BlockData blockData2 = world.blockData[blockPo2.x, blockPo2.y];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component2;
			}
			component2.SpawnTree(new Vector3(component2.Anchor.x + 1.5f, component2.worldPos.y, component2.Anchor.z + 1.5f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 1.5f, component2.worldPos.y, component2.Anchor.z + 1.5f));
			component2.SpawnTree(new Vector3(component2.Anchor.x + 1.5f, component2.worldPos.y, component2.Anchor.z - 1.5f));
			gameObject2.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject2.transform.parent = tService.transform;
			component2.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component2);
		}
	}

	private void HighSchoolUpgrade(int cost)
	{
		if (selectedSC.Stage != 1)
		{
			return;
		}
		world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
		world.Services.Remove(selectedSC);
		GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load("Services/HighSchool_Evo2"))).transform.parent = gameObject.transform;
		gameObject.transform.position = selectedSC.transform.position;
		gameObject.name = selectedSC.worldPos.x + "_school_" + selectedSC.worldPos.z;
		component.Capacity = 2100;
		component.Maintenance = 60f;
		component.MinEmployees = 60;
		component.MaxEmployees = 80;
		component.FixedEmployees = 20;
		component.HiredEmployees = 70;
		component.Stage = 2;
		component.MaxStage = 2;
		component.Use = 0;
		component.type = ServiceCreator.Type.School;
		component.worldPos = selectedSC.worldPos;
		component.Orientation = selectedSC.Orientation;
		component.Anchor = selectedSC.Anchor;
		component.Entry = selectedSC.Entry;
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		component.blockPos = selectedSC.blockPos;
		component.typeName = "H";
		float num = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
		component.HiredEmployees = Mathf.RoundToInt(num / 100f * (float)(component.MaxEmployees - component.MinEmployees) + (float)component.MinEmployees);
		world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		world.Services.Add(component);
		foreach (Vector2Int blockPo in selectedSC.blockPos)
		{
			BlockData blockData = world.blockData[blockPo.x, blockPo.y];
			blockData.t = BlockData.Type.BUILDING;
			blockData.sO = component;
		}
		component.SpawnTree(new Vector3(component.Anchor.x - 1f, component.Anchor.y, component.Anchor.z - 0.5f));
		component.SpawnTree(new Vector3(component.Anchor.x - 3.55f, component.Anchor.y, component.Anchor.z + 0.05f));
		gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
		gameObject.transform.parent = tService.transform;
		component.CreateTrees(world);
		costHelper.PayAmount(cost);
		Object.Destroy(selectedSC.gameObject);
		OpenServiceEditor(component);
	}

	private void HospitalUpgrade(int cost)
	{
		if (selectedSC.Stage == 1)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/Hospital_Evo2"))).transform.parent = gameObject.transform;
			gameObject.transform.position = selectedSC.transform.position;
			gameObject.name = selectedSC.worldPos.x + "_hospital_" + selectedSC.worldPos.z;
			component.Capacity = 160;
			component.Maintenance = 100f;
			component.MinEmployees = 30;
			component.MaxEmployees = 60;
			component.FixedEmployees = 60;
			component.HiredEmployees = 45;
			component.Stage = 2;
			component.Use = 0;
			component.type = ServiceCreator.Type.Hospital;
			component.worldPos = selectedSC.worldPos;
			component.Orientation = selectedSC.Orientation;
			component.Anchor = selectedSC.Anchor;
			component.Entry = selectedSC.Entry;
			component.TreesParent = new GameObject("TreesParent");
			component.TreesParent.transform.parent = component.gameObject.transform;
			component.blockPos = selectedSC.blockPos;
			float num = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component.HiredEmployees = Mathf.RoundToInt(num / 100f * (float)(component.MaxEmployees - component.MinEmployees) + (float)component.MinEmployees);
			world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
			world.Services.Add(component);
			foreach (Vector2Int blockPo in selectedSC.blockPos)
			{
				BlockData blockData = world.blockData[blockPo.x, blockPo.y];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
			}
			component.SpawnTree(new Vector3(component.Anchor.x - 1.72f, component.worldPos.y, component.Anchor.z + 0.85f));
			component.CustomSpawnCar(new Vector2(-0.6f, 1.72f), 90);
			gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject.transform.parent = tService.transform;
			component.CreateVehicles(world);
			component.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component);
		}
		else
		{
			if (selectedSC.Stage != 2)
			{
				return;
			}
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject2 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component2 = gameObject2.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/Hospital_Evo3"))).transform.parent = gameObject2.transform;
			gameObject2.transform.position = selectedSC.transform.position;
			gameObject2.name = selectedSC.worldPos.x + "_hospital_" + selectedSC.worldPos.z;
			component2.Capacity = 320;
			component2.Maintenance = 120f;
			component2.MinEmployees = 60;
			component2.MaxEmployees = 120;
			component2.FixedEmployees = 120;
			component2.HiredEmployees = 90;
			component2.Stage = 3;
			component2.Use = 0;
			component2.type = ServiceCreator.Type.Hospital;
			component2.worldPos = selectedSC.worldPos;
			component2.Orientation = selectedSC.Orientation;
			component2.Anchor = selectedSC.Anchor;
			component2.Entry = selectedSC.Entry;
			component2.TreesParent = new GameObject("TreesParent");
			component2.TreesParent.transform.parent = component2.gameObject.transform;
			component2.blockPos = selectedSC.blockPos;
			float num2 = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component2.HiredEmployees = Mathf.RoundToInt(num2 / 100f * (float)(component2.MaxEmployees - component2.MinEmployees) + (float)component2.MinEmployees);
			world.blockData[component2.Entry.x, component2.Entry.y].o.services.Add(component2);
			world.Services.Add(component2);
			foreach (Vector2Int blockPo2 in selectedSC.blockPos)
			{
				BlockData blockData2 = world.blockData[blockPo2.x, blockPo2.y];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component2;
			}
			component2.CustomSpawnCar(new Vector2(-0.6f, 1.72f), 90);
			component2.CustomSpawnCar(new Vector2(-1.48f, 1.72f), 90);
			gameObject2.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject2.transform.parent = tService.transform;
			component2.CreateVehicles(world);
			component2.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component2);
		}
	}

	private void FireUpgrade(int cost)
	{
		if (selectedSC.Stage != 1)
		{
			return;
		}
		world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
		world.Services.Remove(selectedSC);
		GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load("Services/Firestation_Evo2"))).transform.parent = gameObject.transform;
		gameObject.transform.position = selectedSC.transform.position;
		gameObject.name = selectedSC.worldPos.x + "_Firestation_" + selectedSC.worldPos.z;
		component.Capacity = 500;
		component.Maintenance = 10f;
		component.MinEmployees = 20;
		component.MaxEmployees = 30;
		component.FixedEmployees = 4;
		component.HiredEmployees = 25;
		component.Stage = 2;
		component.MaxStage = 2;
		component.Use = 0;
		component.type = ServiceCreator.Type.Fire;
		component.worldPos = selectedSC.worldPos;
		component.Orientation = selectedSC.Orientation;
		component.Anchor = selectedSC.Anchor;
		component.Entry = selectedSC.Entry;
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		component.blockPos = selectedSC.blockPos;
		float num = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
		component.HiredEmployees = Mathf.RoundToInt(num / 100f * (float)(component.MaxEmployees - component.MinEmployees) + (float)component.MinEmployees);
		world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		world.Services.Add(component);
		foreach (Vector2Int blockPo in selectedSC.blockPos)
		{
			BlockData blockData = world.blockData[blockPo.x, blockPo.y];
			blockData.t = BlockData.Type.BUILDING;
			blockData.sO = component;
		}
		component.CustomSpawnCar(new Vector2(0.58f, 1.25f), 0);
		component.CustomSpawnCar(new Vector2(-1.28f, 1.25f), 0);
		gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
		gameObject.transform.parent = tService.transform;
		Object.Destroy(selectedSC.gameObject);
		component.CreateVehicles(world);
		component.CreateTrees(world);
		costHelper.PayAmount(cost);
		OpenServiceEditor(component);
	}

	private void PoliceUpgrade(int cost)
	{
		if (selectedSC.Stage != 1)
		{
			return;
		}
		world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
		world.Services.Remove(selectedSC);
		GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load("Services/Policestation_Evo2"))).transform.parent = gameObject.transform;
		gameObject.transform.position = selectedSC.transform.position;
		gameObject.name = selectedSC.worldPos.x + "_Policestation_" + selectedSC.worldPos.z;
		component.Capacity = 500;
		component.Maintenance = 10f;
		component.MinEmployees = 48;
		component.MaxEmployees = 72;
		component.FixedEmployees = 12;
		component.HiredEmployees = 60;
		component.Stage = 2;
		component.MaxStage = 2;
		component.Use = 0;
		component.type = ServiceCreator.Type.Police;
		component.worldPos = selectedSC.worldPos;
		component.Orientation = selectedSC.Orientation;
		component.Anchor = selectedSC.Anchor;
		component.Entry = selectedSC.Entry;
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		component.blockPos = selectedSC.blockPos;
		float num = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
		component.HiredEmployees = Mathf.RoundToInt(num / 100f * (float)(component.MaxEmployees - component.MinEmployees) + (float)component.MinEmployees);
		world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		world.Services.Add(component);
		foreach (Vector2Int blockPo in selectedSC.blockPos)
		{
			BlockData blockData = world.blockData[blockPo.x, blockPo.y];
			blockData.t = BlockData.Type.BUILDING;
			blockData.sO = component;
		}
		component.CustomSpawnCar(new Vector2(0.94f, 1.43f), 0);
		component.CustomSpawnCar(new Vector2(0f, 1.43f), 0);
		component.CustomSpawnCar(new Vector2(-0.45f, 1.43f), 180);
		component.CustomSpawnCar(new Vector2(-1.41f, 1.43f), 180);
		component.CustomSpawnCar(new Vector2(-0.93f, 1.43f), 0);
		gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
		gameObject.transform.parent = tService.transform;
		component.CreateVehicles(world);
		costHelper.PayAmount(cost);
		Object.Destroy(selectedSC.gameObject);
		OpenServiceEditor(component);
	}

	private void PrisonUpgrade(int cost)
	{
		if (selectedSC.Stage != 1)
		{
			return;
		}
		world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
		world.Services.Remove(selectedSC);
		GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
		ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
		((GameObject)Object.Instantiate(Resources.Load("Services/Prison_Evo2"))).transform.parent = gameObject.transform;
		gameObject.transform.position = selectedSC.transform.position;
		gameObject.name = selectedSC.worldPos.x + "_Prison_" + selectedSC.worldPos.z;
		component.Capacity = 625;
		component.Maintenance = 525f;
		component.MinEmployees = 20;
		component.MaxEmployees = 20;
		component.FixedEmployees = 20;
		component.HiredEmployees = 20;
		component.Stage = 2;
		component.MaxStage = 2;
		component.Use = 0;
		component.type = ServiceCreator.Type.Prison;
		component.worldPos = selectedSC.worldPos;
		component.Orientation = selectedSC.Orientation;
		component.Anchor = selectedSC.Anchor;
		component.Entry = selectedSC.Entry;
		component.typeName = "Prison";
		component.TreesParent = new GameObject("TreesParent");
		component.TreesParent.transform.parent = component.gameObject.transform;
		component.blockPos = selectedSC.blockPos;
		world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
		world.Services.Add(component);
		foreach (Vector2Int blockPo in selectedSC.blockPos)
		{
			BlockData blockData = world.blockData[blockPo.x, blockPo.y];
			blockData.t = BlockData.Type.BUILDING;
			blockData.sO = component;
		}
		gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
		gameObject.transform.parent = tService.transform;
		costHelper.PayAmount(cost);
		Object.Destroy(selectedSC.gameObject);
		OpenServiceEditor(component);
	}

	private void AirportUpgrade(int cost)
	{
		if (selectedSC.Stage == 0)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component = gameObject.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/Airport_Evo1"))).transform.parent = gameObject.transform;
			gameObject.transform.position = selectedSC.transform.position;
			gameObject.name = selectedSC.worldPos.x + "_airport_" + selectedSC.worldPos.z;
			component.Capacity = 1000;
			component.Maintenance = 320f;
			component.MinEmployees = 5;
			component.MaxEmployees = 7;
			component.FixedEmployees = 40;
			component.HiredEmployees = 5;
			component.Stage = 1;
			component.MaxStage = 4;
			component.Use = 0;
			component.type = ServiceCreator.Type.Airport;
			component.worldPos = selectedSC.worldPos;
			component.Orientation = selectedSC.Orientation;
			component.Anchor = selectedSC.Anchor;
			component.Entry = selectedSC.Entry;
			component.TreesParent = new GameObject("TreesParent");
			component.TreesParent.transform.parent = component.gameObject.transform;
			component.blockPos = selectedSC.blockPos;
			float num = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component.HiredEmployees = Mathf.RoundToInt(num / 100f * (float)(component.MaxEmployees - component.MinEmployees) + (float)component.MinEmployees);
			world.blockData[component.Entry.x, component.Entry.y].o.services.Add(component);
			world.Services.Add(component);
			foreach (Vector2Int blockPo in selectedSC.blockPos)
			{
				BlockData blockData = world.blockData[blockPo.x, blockPo.y];
				blockData.t = BlockData.Type.BUILDING;
				blockData.sO = component;
			}
			component.CustomSpawnCar(new Vector2(0.34f, 11.24f), 90);
			component.CustomSpawnCar(new Vector2(-1.62f, 11.24f), 270);
			component.CustomSpawnCar(new Vector2(1.65f, 11.73f), 270);
			component.CustomSpawnCar(new Vector2(-1.62f, 12.21f), 270);
			component.CustomSpawnCar(new Vector2(-1.62f, 12.73f), 270);
			component.SpawnTree(new Vector3(component.Anchor.x - 3.8f, component.worldPos.y, component.Anchor.z + 12.7f));
			component.SpawnTree(new Vector3(component.Anchor.x - 5.7f, component.worldPos.y, component.Anchor.z + 10.3f));
			component.SpawnTree(new Vector3(component.Anchor.x - 5.8f, component.worldPos.y, component.Anchor.z - 0.3f));
			component.SpawnTree(new Vector3(component.Anchor.x - 4.2f, component.worldPos.y, component.Anchor.z - 2.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7f, component.worldPos.y, component.Anchor.z + 0.4f));
			component.SpawnTree(new Vector3(component.Anchor.x - 7.2f, component.worldPos.y, component.Anchor.z - 5.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 4.1f, component.worldPos.y, component.Anchor.z - 12.6f));
			component.SpawnTree(new Vector3(component.Anchor.x - 6.1f, component.worldPos.y, component.Anchor.z - 11.4f));
			gameObject.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject.transform.parent = tService.transform;
			component.CreateVehicles(world);
			component.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component);
		}
		else if (selectedSC.Stage == 1)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject2 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component2 = gameObject2.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/Airport_Evo2"))).transform.parent = gameObject2.transform;
			gameObject2.transform.position = selectedSC.transform.position;
			gameObject2.name = selectedSC.worldPos.x + "_airport_" + selectedSC.worldPos.z;
			component2.Capacity = 3000;
			component2.Maintenance = 640f;
			component2.MinEmployees = 15;
			component2.MaxEmployees = 19;
			component2.FixedEmployees = 80;
			component2.HiredEmployees = 15;
			component2.Stage = 2;
			component2.MaxStage = 4;
			component2.Use = 0;
			component2.type = ServiceCreator.Type.Airport;
			component2.worldPos = selectedSC.worldPos;
			component2.Orientation = selectedSC.Orientation;
			component2.Anchor = selectedSC.Anchor;
			component2.Entry = selectedSC.Entry;
			component2.TreesParent = new GameObject("TreesParent");
			component2.TreesParent.transform.parent = component2.gameObject.transform;
			component2.blockPos = selectedSC.blockPos;
			float num2 = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component2.HiredEmployees = Mathf.RoundToInt(num2 / 100f * (float)(component2.MaxEmployees - component2.MinEmployees) + (float)component2.MinEmployees);
			world.blockData[component2.Entry.x, component2.Entry.y].o.services.Add(component2);
			world.Services.Add(component2);
			foreach (Vector2Int blockPo2 in selectedSC.blockPos)
			{
				BlockData blockData2 = world.blockData[blockPo2.x, blockPo2.y];
				blockData2.t = BlockData.Type.BUILDING;
				blockData2.sO = component2;
			}
			component2.CustomSpawnCar(new Vector2(0.34f, 11.24f), 90);
			component2.CustomSpawnCar(new Vector2(0.34f, 11.73f), 270);
			component2.CustomSpawnCar(new Vector2(0.34f, 12.73f), 90);
			component2.CustomSpawnCar(new Vector2(-0.34f, 12.73f), 270);
			component2.CustomSpawnCar(new Vector2(-0.34f, 11.24f), 90);
			component2.CustomSpawnCar(new Vector2(-1.62f, 11.24f), 270);
			component2.CustomSpawnCar(new Vector2(-1.62f, 11.73f), 90);
			component2.CustomSpawnCar(new Vector2(1.65f, 11.73f), 270);
			component2.CustomSpawnCar(new Vector2(-1.62f, 12.21f), 90);
			component2.CustomSpawnCar(new Vector2(-1.62f, 12.73f), 270);
			component2.CustomSpawnCar(new Vector2(1.65f, 13.23f), 90);
			component2.SpawnTree(new Vector3(component2.Anchor.x - 3.8f, component2.worldPos.y, component2.Anchor.z + 12.7f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 5.7f, component2.worldPos.y, component2.Anchor.z + 10.3f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 5.8f, component2.worldPos.y, component2.Anchor.z - 0.3f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 4.2f, component2.worldPos.y, component2.Anchor.z - 2.6f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 7f, component2.worldPos.y, component2.Anchor.z + 0.4f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 7.2f, component2.worldPos.y, component2.Anchor.z - 5.6f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 4.1f, component2.worldPos.y, component2.Anchor.z - 12.6f));
			component2.SpawnTree(new Vector3(component2.Anchor.x - 6.1f, component2.worldPos.y, component2.Anchor.z - 11.4f));
			gameObject2.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject2.transform.parent = tService.transform;
			component2.CreateVehicles(world);
			component2.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component2);
		}
		else if (selectedSC.Stage == 2)
		{
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject3 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component3 = gameObject3.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/Airport_Evo3"))).transform.parent = gameObject3.transform;
			gameObject3.transform.position = selectedSC.transform.position;
			gameObject3.name = selectedSC.worldPos.x + "_airport_" + selectedSC.worldPos.z;
			component3.Capacity = 7000;
			component3.Maintenance = 1280f;
			component3.MinEmployees = 35;
			component3.MaxEmployees = 41;
			component3.FixedEmployees = 160;
			component3.HiredEmployees = 35;
			component3.Stage = 3;
			component3.MaxStage = 4;
			component3.Use = 0;
			component3.type = ServiceCreator.Type.Airport;
			component3.worldPos = selectedSC.worldPos;
			component3.Orientation = selectedSC.Orientation;
			component3.Anchor = selectedSC.Anchor;
			component3.Entry = selectedSC.Entry;
			component3.TreesParent = new GameObject("TreesParent");
			component3.TreesParent.transform.parent = component3.gameObject.transform;
			component3.blockPos = selectedSC.blockPos;
			float num3 = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component3.HiredEmployees = Mathf.RoundToInt(num3 / 100f * (float)(component3.MaxEmployees - component3.MinEmployees) + (float)component3.MinEmployees);
			world.blockData[component3.Entry.x, component3.Entry.y].o.services.Add(component3);
			world.Services.Add(component3);
			foreach (Vector2Int blockPo3 in selectedSC.blockPos)
			{
				BlockData blockData3 = world.blockData[blockPo3.x, blockPo3.y];
				blockData3.t = BlockData.Type.BUILDING;
				blockData3.sO = component3;
			}
			component3.CustomSpawnCar(new Vector2(0.34f, 11.24f), 90);
			component3.CustomSpawnCar(new Vector2(0.34f, 11.73f), 270);
			component3.CustomSpawnCar(new Vector2(0.34f, 12.73f), 90);
			component3.CustomSpawnCar(new Vector2(-0.34f, 12.73f), 270);
			component3.CustomSpawnCar(new Vector2(-0.34f, 11.24f), 90);
			component3.CustomSpawnCar(new Vector2(-0.34f, 11.73f), 270);
			component3.CustomSpawnCar(new Vector2(-1.62f, 11.24f), 270);
			component3.CustomSpawnCar(new Vector2(-1.62f, 11.73f), 90);
			component3.CustomSpawnCar(new Vector2(1.65f, 11.73f), 270);
			component3.CustomSpawnCar(new Vector2(1.65f, 12.21f), 90);
			component3.CustomSpawnCar(new Vector2(0.34f, 12.21f), 270);
			component3.CustomSpawnCar(new Vector2(-0.34f, 13.74f), 90);
			component3.CustomSpawnCar(new Vector2(-1.62f, 13.74f), 270);
			component3.CustomSpawnCar(new Vector2(-1.62f, 12.21f), 90);
			component3.CustomSpawnCar(new Vector2(-1.62f, 12.73f), 270);
			component3.CustomSpawnCar(new Vector2(1.65f, 13.23f), 90);
			component3.SpawnTree(new Vector3(component3.Anchor.x - 0.1f, component3.worldPos.y, component3.Anchor.z - 4.3f));
			component3.SpawnTree(new Vector3(component3.Anchor.x - 1f, component3.worldPos.y, component3.Anchor.z - 12.7f));
			component3.SpawnTree(new Vector3(component3.Anchor.x + 0.2f, component3.worldPos.y, component3.Anchor.z + 0.75f));
			gameObject3.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject3.transform.parent = tService.transform;
			component3.CreateVehicles(world);
			component3.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component3);
		}
		else
		{
			if (selectedSC.Stage != 3)
			{
				return;
			}
			world.blockData[selectedSC.Entry.x, selectedSC.Entry.y].o.services.Remove(selectedSC);
			world.Services.Remove(selectedSC);
			GameObject gameObject4 = Object.Instantiate(indexer.plopping.serviceUnit);
			ServiceCreator component4 = gameObject4.GetComponent<ServiceCreator>();
			((GameObject)Object.Instantiate(Resources.Load("Services/Airport_Evo4"))).transform.parent = gameObject4.transform;
			gameObject4.transform.position = selectedSC.transform.position;
			gameObject4.name = selectedSC.worldPos.x + "_airport_" + selectedSC.worldPos.z;
			component4.Capacity = 16000;
			component4.Maintenance = 3000f;
			component4.MinEmployees = 80;
			component4.MaxEmployees = 90;
			component4.FixedEmployees = 320;
			component4.HiredEmployees = 80;
			component4.Stage = 4;
			component4.MaxStage = 4;
			component4.Use = 0;
			component4.type = ServiceCreator.Type.Airport;
			component4.worldPos = selectedSC.worldPos;
			component4.Orientation = selectedSC.Orientation;
			component4.Anchor = selectedSC.Anchor;
			component4.Entry = selectedSC.Entry;
			component4.TreesParent = new GameObject("TreesParent");
			component4.TreesParent.transform.parent = component4.gameObject.transform;
			component4.blockPos = selectedSC.blockPos;
			float num4 = (selectedSC.HiredEmployees - selectedSC.MinEmployees) * 100 / (selectedSC.MaxEmployees - selectedSC.MinEmployees);
			component4.HiredEmployees = Mathf.RoundToInt(num4 / 100f * (float)(component4.MaxEmployees - component4.MinEmployees) + (float)component4.MinEmployees);
			world.blockData[component4.Entry.x, component4.Entry.y].o.services.Add(component4);
			world.Services.Add(component4);
			foreach (Vector2Int blockPo4 in selectedSC.blockPos)
			{
				BlockData blockData4 = world.blockData[blockPo4.x, blockPo4.y];
				blockData4.t = BlockData.Type.BUILDING;
				blockData4.sO = component4;
			}
			component4.SpawnTree(new Vector3(component4.Anchor.x, component4.worldPos.y, component4.Anchor.z - 7f));
			component4.SpawnTree(new Vector3(component4.Anchor.x, component4.worldPos.y, component4.Anchor.z - 9f));
			component4.SpawnTree(new Vector3(component4.Anchor.x + 1.57f, component4.worldPos.y, component4.Anchor.z + 11.6f));
			component4.SpawnTree(new Vector3(component4.Anchor.x + 1.57f, component4.worldPos.y, component4.Anchor.z + 13.7f));
			component4.SpawnTree(new Vector3(component4.Anchor.x - 1.57f, component4.worldPos.y, component4.Anchor.z + 11.6f));
			component4.SpawnTree(new Vector3(component4.Anchor.x - 1.57f, component4.worldPos.y, component4.Anchor.z + 13.7f));
			gameObject4.transform.Rotate(0f, selectedSC.Orientation, 0f, Space.Self);
			gameObject4.transform.parent = tService.transform;
			component4.CreateTrees(world);
			costHelper.PayAmount(cost);
			Object.Destroy(selectedSC.gameObject);
			OpenServiceEditor(component4);
		}
	}

	public void MoreEmployees()
	{
		if (selectedSC.type == ServiceCreator.Type.School)
		{
			selectedSC.HiredEmployees += 2;
		}
		else if (selectedSC.type == ServiceCreator.Type.Hospital)
		{
			selectedSC.HiredEmployees += 4;
		}
		else if (selectedSC.type == ServiceCreator.Type.Police || selectedSC.type == ServiceCreator.Type.Prison)
		{
			selectedSC.HiredEmployees += 4;
		}
		else if (selectedSC.type == ServiceCreator.Type.Fire)
		{
			selectedSC.HiredEmployees += 2;
		}
		else if (selectedSC.type == ServiceCreator.Type.Airport)
		{
			if (selectedSC.Stage <= 2)
			{
				selectedSC.HiredEmployees++;
			}
			else
			{
				selectedSC.HiredEmployees += 2;
			}
		}
		else if (selectedSC.type == ServiceCreator.Type.Power || selectedSC.type == ServiceCreator.Type.Water)
		{
			selectedSC.HiredEmployees++;
			if (Messages.step == 16)
			{
				Messages.counter++;
			}
		}
		if (selectedSC.HiredEmployees > selectedSC.MaxEmployees)
		{
			selectedSC.HiredEmployees = selectedSC.MaxEmployees;
		}
		SetTexts();
	}

	public void LessEmployees()
	{
		if (selectedSC.type == ServiceCreator.Type.School)
		{
			selectedSC.HiredEmployees -= 2;
		}
		else if (selectedSC.type == ServiceCreator.Type.Hospital)
		{
			selectedSC.HiredEmployees -= 4;
		}
		else if (selectedSC.type == ServiceCreator.Type.Police || selectedSC.type == ServiceCreator.Type.Prison)
		{
			selectedSC.HiredEmployees -= 4;
		}
		else if (selectedSC.type == ServiceCreator.Type.Fire)
		{
			selectedSC.HiredEmployees -= 2;
		}
		else if (selectedSC.type == ServiceCreator.Type.Airport)
		{
			if (selectedSC.Stage <= 2)
			{
				selectedSC.HiredEmployees -= 2;
			}
			else
			{
				selectedSC.HiredEmployees -= 5;
			}
		}
		else if (selectedSC.type == ServiceCreator.Type.Power || selectedSC.type == ServiceCreator.Type.Water)
		{
			selectedSC.HiredEmployees--;
		}
		if (selectedSC.HiredEmployees < selectedSC.MinEmployees)
		{
			selectedSC.HiredEmployees = selectedSC.MinEmployees;
		}
		SetTexts();
	}

	private void SetGradeAiguille(ServiceCreator sc)
	{
		if (sc.type == ServiceCreator.Type.Government || sc.type == ServiceCreator.Type.Power || sc.type == ServiceCreator.Type.Drone || sc.type == ServiceCreator.Type.Water || sc.type == ServiceCreator.Type.Airport || sc.type == ServiceCreator.Type.Prison || sc.type == ServiceCreator.Type.Arcology || sc.type == ServiceCreator.Type.TrainStation || sc.type == ServiceCreator.Type.Port)
		{
			GradeStuff.SetActive(value: false);
			return;
		}
		int grade = selectedSC.Grade;
		GradeStuff.SetActive(value: true);
		Vector3 position = Aiguille.transform.position;
		if (grade > 90)
		{
			Aiguille.transform.position = new Vector3(A.transform.position.x, position.y, position.z);
			A.color = green;
			B.color = grey;
			C.color = grey;
			D.color = grey;
			E.color = grey;
			F.color = grey;
		}
		else if (grade > 80)
		{
			Aiguille.transform.position = new Vector3(B.transform.position.x, position.y, position.z);
			B.color = green;
			A.color = grey;
			C.color = grey;
			D.color = grey;
			E.color = grey;
			F.color = grey;
		}
		else if (grade > 60)
		{
			Aiguille.transform.position = new Vector3(C.transform.position.x, position.y, position.z);
			C.color = yellow;
			B.color = grey;
			A.color = grey;
			D.color = grey;
			E.color = grey;
			F.color = grey;
		}
		else if (grade > 40)
		{
			Aiguille.transform.position = new Vector3(D.transform.position.x, position.y, position.z);
			D.color = yellow;
			B.color = grey;
			C.color = grey;
			A.color = grey;
			E.color = grey;
			F.color = grey;
		}
		else if (grade > 20)
		{
			Aiguille.transform.position = new Vector3(E.transform.position.x, position.y, position.z);
			E.color = red;
			B.color = grey;
			C.color = grey;
			D.color = grey;
			A.color = grey;
			F.color = grey;
		}
		else
		{
			Aiguille.transform.position = new Vector3(F.transform.position.x, position.y, position.z);
			F.color = red;
			B.color = grey;
			C.color = grey;
			D.color = grey;
			E.color = grey;
			A.color = grey;
		}
	}

	private int SetUpdradeCost(ServiceCreator sc)
	{
		int result = 0;
		if (sc.type == ServiceCreator.Type.School)
		{
			if (sc.typeName == "S")
			{
				if (sc.Stage == 1)
				{
					result = 1250;
				}
				else if (sc.Stage == 2)
				{
					result = 1250;
				}
				else if (sc.Stage == 3)
				{
					result = 1750;
				}
			}
			else if (sc.Stage == 1)
			{
				result = 2250;
			}
		}
		else if (sc.type == ServiceCreator.Type.Hospital)
		{
			if (sc.Stage == 1)
			{
				result = 3500;
			}
			else if (sc.Stage == 2)
			{
				result = 3500;
			}
		}
		else if (sc.type == ServiceCreator.Type.Fire)
		{
			if (sc.Stage == 1)
			{
				result = 1750;
			}
		}
		else if (sc.type == ServiceCreator.Type.Police)
		{
			if (sc.Stage == 1)
			{
				result = 2250;
			}
		}
		else if (sc.type == ServiceCreator.Type.Prison)
		{
			if (sc.Stage == 1)
			{
				result = 12500;
			}
		}
		else if (sc.type == ServiceCreator.Type.Drone)
		{
			if (sc.Stage == 1)
			{
				result = 40000;
			}
			else if (sc.Stage == 2)
			{
				result = 60000;
			}
		}
		else if (sc.type == ServiceCreator.Type.Airport)
		{
			if (sc.Stage == 0)
			{
				result = 6500;
			}
			else if (sc.Stage == 1)
			{
				result = 18000;
			}
			else if (sc.Stage == 2)
			{
				result = 35000;
			}
			else if (sc.Stage == 3)
			{
				result = 72000;
			}
		}
		UpgradeText.text = LOCSYS.Translate("UPGRADE") + " ($" + result.ToString("N0") + ")";
		return result;
	}

	private IEnumerator UpdateTexts()
	{
		while (isDisplayed)
		{
			SetTexts();
			yield return new WaitForSecondsRealtime(0.5f);
		}
	}

	public void ShowDropdown()
	{
		dropdownGO.SetActive(value: true);
		cddCapacity.text = Mathf.RoundToInt(selectedSC.capGrade) + " / 100";
		cddEmployee.text = Mathf.RoundToInt(selectedSC.employGrade) + " / 100";
		cddPolicies.text = Mathf.RoundToInt(selectedSC.polGrade) + " / 100";
		cddGrade.text = selectedSC.Grade + " / 100";
	}

	public void HideDropdown()
	{
		dropdownGO.SetActive(value: false);
	}
}
