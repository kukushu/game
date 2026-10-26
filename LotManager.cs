using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AzureSky;
using UnityEngine.Rendering.PostProcessing;

public class LotManager : MonoBehaviour
{
	public World w;

	public Commute c;

	public Roading r;

	public Economy eco;

	public TrafficManager traffic;

	private float totalEduPoints;

	private float tempTotalEduPoints;

	private float totalReliPoints;

	private float totalBirthPoints;

	private float totalSecuPoints;

	private float tempSecuPoints;

	public AzureTimeController timeController;

	public Light Sun;

	private bool lightsOff = true;

	private bool lightsOn;

	public PostProcessVolume ppv;

	private Bloom bloom;

	private AutoExposure autoExposure;

	private float decayRedu = 0.5f;

	private bool t_validCommute;

	public Material windowsResidential;

	public Material windowOld;

	public Material windowNeutral;

	public Material windowRaw;

	public Material windowCommercial;

	public Material windowHorizon;

	public Material windowHito;

	public Material windowOxygen;

	public Material windowStab;

	public Material windowSteel;

	public Material windowOffice;

	public Material windowRicardo;

	public Material windowExchange;

	public Material vehicleMat;

	public Material logoMat;

	public Material propsMat;

	private void Start()
	{
		ppv.profile.TryGetSettings<Bloom>(out bloom);
		ppv.profile.TryGetSettings<AutoExposure>(out autoExposure);
		if (autoExposure != null && bloom != null)
		{
			DefaultLights();
		}
		else
		{
			Debug.Log("FUCKED");
		}
	}

	public void LaunchLotUpdates()
	{
		StartCoroutine(UpdateLotsShort());
		StartCoroutine(UpdateLotsLong());
	}

	private IEnumerator UpdateLotsShort()
	{
		while (true)
		{
			int totalValue = 0;
			int droneUse = 0;
			int counter = 0;
			decayRedu = 0.5f;
			if (eco.Population > 200000f)
			{
				decayRedu = 3f;
			}
			else if (eco.Population > 100000f)
			{
				decayRedu = 2f;
			}
			else if (eco.Population > 50000f)
			{
				decayRedu = 1f;
			}
			int maxCount = 8 + (int)decayRedu * 2;
			foreach (LotCreator lc in w.ResidentialBuildings.Concat(w.CommercialBuildings).Concat(w.IndustrialBuildings).Concat(w.MixedBuildings)
				.ToList())
			{
				if (lc == null)
				{
					continue;
				}
				totalValue += ValueUpdate(lc);
				CommuteUpdate(lc);
				if (lc.commuteState == "at")
				{
					droneUse += lc.Occupants;
				}
				counter++;
				if (counter >= maxCount)
				{
					if (timeController.m_timeline > 3f)
					{
						SpawnTraffic(lc);
					}
					counter = 0;
					yield return new WaitForEndOfFrame();
				}
				else
				{
					yield return new WaitForEndOfFrame();
				}
				if (timeController.m_timeline > 22.25f || timeController.m_timeline < 1f)
				{
					if (lc.streetLight != null && !lc.streetLight.activeSelf)
					{
						lc.streetLight.SetActive(value: true);
					}
				}
				else if (timeController.m_timeline > 2f && lc.streetLight != null && lc.streetLight.activeSelf)
				{
					lc.streetLight.SetActive(value: false);
				}
			}
			foreach (LotCreator item in w.VacantCommLots.Concat(w.VacantResLots).Concat(w.VacantIndusLots).Concat(w.VacantMixedLots)
				.ToList())
			{
				counter++;
				if (item == null)
				{
					continue;
				}
				item.healthLevel = (short)eco.HealthScore;
				item.secuLevel = (short)Mathf.Clamp(eco.SecurityLevel, 0f, 100f);
				ValueUpdate(item);
				if (!eco.growthStopped)
				{
					item.negatives -= 2f * decayRedu;
					if (item.negatives < 0f)
					{
						item.negatives = 0f;
					}
				}
				if (item.dummy != null && item.negatives < 2f)
				{
					Object.Destroy(item.dummy.gameObject);
				}
				if (counter >= 32)
				{
					counter = 0;
					yield return new WaitForEndOfFrame();
				}
			}
			eco.AverageValue = totalValue / (1 + w.ResidentialBuildings.Count + w.CommercialBuildings.Count + w.IndustrialBuildings.Count + w.MixedBuildings.Count);
			eco.DroneUse = droneUse;
			yield return new WaitForSeconds(0.5f);
		}
	}

	private IEnumerator UpdateLotsLong()
	{
		while (true)
		{
			int counter = 0;
			bool fxPlayed = false;
			tempTotalEduPoints = 0f;
			totalReliPoints = 0f;
			totalBirthPoints = 0f;
			tempSecuPoints = 0f;
			foreach (LotCreator item in w.ResidentialBuildings.Concat(w.MixedBuildings).ToList())
			{
				counter++;
				if (item == null)
				{
					continue;
				}
				ServicingUpdate(item);
				if (counter < 2)
				{
					continue;
				}
				if (!fxPlayed && Random.value < 0.05f)
				{
					if (eco.PowerCapacity > 1 && eco.PowerUse * 100 / (eco.PowerCapacity + 1) > 110)
					{
						SpawnFX(0, item);
						fxPlayed = true;
					}
					else if (eco.WaterCapacity > 1 && eco.WaterUse * 100 / (eco.WaterCapacity + 1) > 108)
					{
						SpawnFX(1, item);
						fxPlayed = true;
					}
				}
				counter = 0;
				yield return new WaitForEndOfFrame();
			}
			foreach (LotCreator item2 in w.IndustrialBuildings.Concat(w.CommercialBuildings).ToList())
			{
				counter++;
				BusinessLotsUpdate(item2);
				if (counter >= 60)
				{
					counter = 0;
					yield return new WaitForEndOfFrame();
				}
			}
			foreach (SlumCreator item3 in w.SlumBuildings.ToList())
			{
				counter++;
				if (!(item3 == null))
				{
					SlumUpdate(item3);
					totalBirthPoints += (float)item3.Occupants * (1f + eco.BirthRatePolicy);
					if (counter >= 20)
					{
						counter = 0;
						yield return new WaitForEndOfFrame();
					}
				}
			}
			foreach (ServiceCreator item4 in w.Arcologies.ToList())
			{
				item4.Grade = Mathf.RoundToInt((eco.BirthRatePolicy + 2f) * 100f);
				tempTotalEduPoints += (float)item4.MinEmployees * eco.EducationQuality;
				totalBirthPoints += (float)item4.MinEmployees * eco.BirthRatePolicy;
			}
			totalEduPoints = tempTotalEduPoints;
			eco.EducationLevel = totalEduPoints / (eco.Population + 1f);
			eco.ReligionScore = totalReliPoints / (eco.Population + 1f);
			eco.BirthRate = totalBirthPoints / (eco.Population + 1f);
			totalSecuPoints = tempSecuPoints;
			eco.SecurityLevel = totalSecuPoints / (eco.Population + 1f);
			if (timeController.m_timeline > 22f)
			{
				if (!lightsOn)
				{
					lightsOn = true;
					lightsOff = false;
					StartCoroutine(EmissiveON());
				}
			}
			else if (timeController.m_timeline > 1.5f && !lightsOff)
			{
				lightsOff = true;
				lightsOn = false;
				StartCoroutine(EmissiveOff());
			}
			if (World.dataReady)
			{
				yield return new WaitForSeconds(3f);
			}
			else
			{
				yield return new WaitForSecondsRealtime(1f);
			}
		}
	}

	public int ValueUpdate(LotCreator lc)
	{
		float num = 0f;
		float num2 = 0f;
		if ((lc.lType == LotCreator.LotType.COMMERCIAL && lc.RoadEntry != null) || (lc.lType == LotCreator.LotType.MIXED && lc.RoadEntry != null))
		{
			num2 = lc.RoadEntry.TrafficSlipsal / 50;
		}
		foreach (Vector2Int blockPo in lc.blockPos)
		{
			BlockData bounded = r.w.GetBounded(blockPo.x, blockPo.y);
			num += (float)(bounded.v[0] * 4) + num2;
		}
		num = Mathf.Round(num / eco.budget.GetPropertySize(lc));
		lc.LandValue = (short)num;
		return Mathf.RoundToInt(num);
	}

	private void CommuteUpdate(LotCreator lc)
	{
		if (!World.commuteReady)
		{
			return;
		}
		float value = Random.value;
		string text = "invalidcommute";
		if (!lc.hasChanged)
		{
			lc.lastChange++;
		}
		if (lc.lastChange > 4 || !lc.validCommute)
		{
			lc.hasChanged = true;
			lc.lastChange = 0;
		}
		if (lc.lClass == LotCreator.LotClass.PRIMARY && lc.Anchor.y < 29.5f)
		{
			lc.validCommute = true;
			lc.roadNetwork = true;
			lc.commuteState = "";
			return;
		}
		if (lc.RoadEntry == null)
		{
			RoadCreator ifNotNull = r.GetIfNotNull(lc.Entry.x, 1, lc.Entry.y);
			if (ifNotNull != null)
			{
				lc.RoadEntry = ifNotNull;
				lc.hasChanged = true;
			}
			else if (!lc.isDecayed)
			{
				lc.negatives += 4f * w.decayMultiplier;
				lc.decayReason = "No Road Access";
			}
		}
		if (lc.lType != LotCreator.LotType.RESIDENTIAL && lc.lType != LotCreator.LotType.MIXED)
		{
			lc.secuLevel = (short)Mathf.Clamp(eco.SecurityScore, 0, 100);
		}
		if (!lc.hasChanged || lc.isDecayed || lc.Occupants <= 0 || w.IndustrialBuildings.Count + w.CommercialBuildings.Count <= 0)
		{
			return;
		}
		t_validCommute = lc.validCommute;
		lc.validCommute = false;
		if (lc.lType == LotCreator.LotType.RESIDENTIAL || lc.lType == LotCreator.LotType.MIXED)
		{
			if ((Random.value > 0.4f && w.IndustrialBuildings.Count > 0) || w.CommercialBuildings.Count == 0)
			{
				lc.commuteLot = PickWorkPlace(LotCreator.LotType.INDUSTRIAL, lc);
			}
			else if (w.CommercialBuildings.Count > 0)
			{
				lc.commuteLot = PickWorkPlace(LotCreator.LotType.COMMERCIAL, lc);
			}
		}
		else
		{
			if (w.ResidentialBuildings.Count <= 0)
			{
				lc.commuteState = "invalidcommute";
				return;
			}
			lc.commuteLot = PickWorkPlace(LotCreator.LotType.RESIDENTIAL, lc);
		}
		RoadCreator roadCreator = null;
		RoadCreator roadCreator2 = null;
		RoadCreator roadCreator3 = null;
		RoadCreator roadCreator4 = null;
		Vector3 wP = lc.commuteLot.wP;
		List<RoadCreator> list = new List<RoadCreator>();
		List<RoadCreator> list2 = new List<RoadCreator>();
		foreach (RoadCreator trainStop in r.trainStops)
		{
			if (Mathf.Abs(lc.wP.x - trainStop.wPos.x) < 24f && Mathf.Abs(lc.wP.z - trainStop.wPos.z) < 24f)
			{
				list.Add(trainStop);
				roadCreator = trainStop;
			}
			if (Mathf.Abs(lc.commuteLot.wP.x - trainStop.wPos.x) < 24f && Mathf.Abs(lc.commuteLot.wP.z - trainStop.wPos.z) < 24f)
			{
				list2.Add(trainStop);
				roadCreator2 = trainStop;
			}
		}
		if (list.Count > 1)
		{
			roadCreator = list[0];
			foreach (RoadCreator item in list)
			{
				if (Mathf.Abs(item.wPos.x - lc.wP.x) + Mathf.Abs(item.wPos.z - lc.wP.z) < Mathf.Abs(roadCreator.wPos.x - lc.wP.x) + Mathf.Abs(roadCreator.wPos.z - lc.wP.z))
				{
					roadCreator = item;
				}
			}
		}
		if (list2.Count > 1)
		{
			roadCreator2 = list2[0];
			foreach (RoadCreator item2 in list2)
			{
				if (Mathf.Abs(item2.wPos.x - lc.wP.x) + Mathf.Abs(item2.wPos.z - lc.wP.z) < Mathf.Abs(roadCreator2.wPos.x - lc.wP.x) + Mathf.Abs(roadCreator2.wPos.z - lc.wP.z))
				{
					roadCreator2 = item2;
				}
			}
		}
		if (roadCreator != null && roadCreator2 != null && roadCreator != roadCreator2 && value > 0.3f)
		{
			c.FindPath(roadCreator, roadCreator2, lc, byCar: false);
			text = ((!lc.validCommute) ? "irregularrailnetwork" : "tc");
		}
		else
		{
			List<RoadCreator> list3 = new List<RoadCreator>();
			List<RoadCreator> list4 = new List<RoadCreator>();
			foreach (RoadCreator busStop in r.busStops)
			{
				if (Mathf.Abs(lc.wP.x - busStop.wPos.x) < 18f && Mathf.Abs(lc.wP.z - busStop.wPos.z) < 18f)
				{
					list3.Add(busStop);
					roadCreator3 = busStop;
				}
				if (Mathf.Abs(lc.commuteLot.wP.x - busStop.wPos.x) < 18f && Mathf.Abs(lc.commuteLot.wP.z - busStop.wPos.z) < 18f)
				{
					list4.Add(busStop);
					roadCreator4 = busStop;
				}
			}
			if (list3.Count > 1)
			{
				roadCreator3 = list3[0];
				foreach (RoadCreator item3 in list3)
				{
					if (Mathf.Abs(item3.wPos.x - lc.wP.x) + Mathf.Abs(item3.wPos.z - lc.wP.z) < Mathf.Abs(roadCreator3.wPos.x - lc.wP.x) + Mathf.Abs(roadCreator3.wPos.z - lc.wP.z))
					{
						roadCreator3 = item3;
					}
				}
			}
			if (list4.Count > 1)
			{
				roadCreator4 = list4[0];
				foreach (RoadCreator item4 in list4)
				{
					if (Mathf.Abs(item4.wPos.x - lc.wP.x) + Mathf.Abs(item4.wPos.z - lc.wP.z) < Mathf.Abs(roadCreator4.wPos.x - lc.wP.x) + Mathf.Abs(roadCreator4.wPos.z - lc.wP.z))
					{
						roadCreator4 = item4;
					}
				}
			}
			bool flag = false;
			if (value > 0.5f || value < 0.15f)
			{
				flag = true;
			}
			if ((roadCreator3 != null && roadCreator4 != null && roadCreator3 != roadCreator4) & flag)
			{
				c.FindPath(roadCreator3, roadCreator4, lc, byCar: true);
				text = ((!lc.validCommute) ? "irregularnetwork" : "bc");
			}
			else
			{
				float num = value * 100f;
				float num2 = eco.neighborsJobs * 100f / eco.Jobs;
				if (num < num2 && World.exitRoads.Count > 0)
				{
					RoadCreator roadCreator5 = World.exitRoads[Random.Range(0, World.exitRoads.Count)];
					c.FindPath(lc.RoadEntry, roadCreator5, lc, byCar: true);
					if (roadCreator5 == null)
					{
						World.exitRoads.Remove(roadCreator5);
						Debug.Log("Exit is null!");
						lc.commuteState = "irregularnetwork";
					}
					if (lc.validCommute)
					{
						text = "nc";
					}
					else
					{
						c.FindPath(lc.RoadEntry, lc.commuteLot.RoadEntry, lc, byCar: true);
						text = ((!lc.validCommute) ? "irregularnetwork" : "cc");
					}
				}
				else if ((lc.lClass == LotCreator.LotClass.UPPER && eco.DroneCapacity > eco.DroneUse + 1) || (lc.lClass == LotCreator.LotClass.MIXOFFICE && eco.DroneCapacity > eco.DroneUse + 1))
				{
					c.FindPath(lc.RoadEntry, lc.commuteLot.RoadEntry, lc, byCar: true);
					if (lc.validCommute)
					{
						text = "at";
					}
				}
				else
				{
					c.FindPath(lc.RoadEntry, lc.commuteLot.RoadEntry, lc, byCar: true);
					if (lc.validCommute)
					{
						text = ((lc.roadPath.Count >= 32) ? "cc" : "wc");
					}
				}
			}
		}
		if (text == "irregularnetwork" || text == "irregularrailnetwork")
		{
			c.FindPath(lc.RoadEntry, lc.commuteLot.RoadEntry, lc, byCar: true);
			if (lc.validCommute)
			{
				text = "cc";
			}
		}
		if (text == "tc")
		{
			roadCreator.commuteUsers.Add(lc);
			roadCreator2.commuteUsers.Add(lc);
		}
		else if (text == "bc")
		{
			roadCreator3.commuteUsers.Add(lc);
			roadCreator4.commuteUsers.Add(lc);
		}
		if (lc.validCommute)
		{
			lc.commuteAnchor = new Vector2Int((int)wP.x, (int)wP.z);
		}
		else if (text != "irregularrailnetwork")
		{
			text = "invalidcommute";
		}
		lc.commuteState = text;
		lc.hasChanged = false;
		lc.lastChange = 0;
	}

	private LotCreator PickWorkPlace(LotCreator.LotType type, LotCreator lc)
	{
		LotCreator commuteLot = lc.commuteLot;
		if (commuteLot != null && t_validCommute)
		{
			return commuteLot;
		}
		List<LotCreator> list = new List<LotCreator>();
		int num;
		if (eco.Population < 10000f)
		{
			num = 2;
		}
		else if (eco.Population < 50000f)
		{
			num = 6;
		}
		else
		{
			num = ((!(eco.Population < 150000f)) ? 24 : 12);
		}
		for (int i = 0; i < num; i++)
		{
			switch (type)
			{
			case LotCreator.LotType.RESIDENTIAL:
				list.Add(w.ResidentialBuildings[Random.Range(0, w.ResidentialBuildings.Count)]);
				break;
			case LotCreator.LotType.INDUSTRIAL:
				list.Add(w.IndustrialBuildings[Random.Range(0, w.IndustrialBuildings.Count)]);
				break;
			default:
				list.Add(w.CommercialBuildings[Random.Range(0, w.CommercialBuildings.Count)]);
				break;
			}
		}
		LotCreator lotCreator = list[0];
		float num2 = Mathf.Abs(lotCreator.wP.x - lc.wP.x) + Mathf.Abs(lotCreator.wP.z - lc.wP.z);
		foreach (LotCreator item in list)
		{
			float num3 = Mathf.Abs(item.wP.x - lc.wP.x) + Mathf.Abs(item.wP.z - lc.wP.z);
			if (num3 < num2)
			{
				lotCreator = item;
				num2 = num3;
			}
		}
		return lotCreator;
	}

	private void ServicingUpdate(LotCreator lc)
	{
		if (lc.lType == LotCreator.LotType.RESIDENTIAL || lc.lType == LotCreator.LotType.MIXED)
		{
			float num = (eco.EducationQuality - lc.eduLevel) / 80f;
			lc.eduLevel += num;
			if (lc.eduLevel > 120f)
			{
				lc.eduLevel = 120f;
			}
			else if (lc.eduLevel < 1f)
			{
				lc.eduLevel = 1f;
			}
			tempTotalEduPoints += (float)lc.Occupants * lc.eduLevel;
			lc.churchCount = 0;
			float num2 = (lc.LandValue - eco.AverageValue) / 25;
			float num3 = (float)eco.SecurityScore + num2;
			if (lc.lClass == LotCreator.LotClass.RETAIL && lc.Stage == 0 && lc.Occupants > 500)
			{
				num3 = 0f;
			}
			if (num3 < 0f)
			{
				num3 = 0f;
			}
			else if (num3 > 100f)
			{
				num3 = 100f;
			}
			lc.secuLevel = (short)num3;
			tempSecuPoints += num3;
			foreach (ServiceCreator service in w.Services)
			{
				if (service.type == ServiceCreator.Type.Hospital && service.carsOut < 1 && service.open)
				{
					traffic.ServiceSpawnSpecialVehicle(service, "Vehicles/Ambulance");
				}
				if ((service.type == ServiceCreator.Type.Religion || service.typeName == "B") && !(lc.RoadEntry == null) && service.open && (service.MinEmployees <= 1 || service.MinEmployees >= 1000) && service.worldPos.x > lc.RoadEntry.wPos.x - 32f && service.worldPos.x < lc.RoadEntry.wPos.x + 32f && service.worldPos.z > lc.RoadEntry.wPos.z - 32f && service.worldPos.z < lc.RoadEntry.wPos.z + 32f)
				{
					if (service.type == ServiceCreator.Type.Religion)
					{
						lc.churchCount++;
					}
					else if (num > 0f)
					{
						lc.eduLevel += num * (float)(service.Grade / 80);
					}
				}
			}
			if (lc.churchCount > 3)
			{
				lc.churchCount = 3;
			}
			int num4 = Mathf.RoundToInt((1f - (float)lc.Stage / 10f) * (float)lc.churchCount);
			lc.reliLevel = (short)(((float)num4 + eco.Religiousness) * 80f);
			if (lc.reliLevel < 0)
			{
				lc.reliLevel = 0;
			}
			else if (lc.reliLevel > 100)
			{
				lc.reliLevel = 100;
			}
			totalReliPoints += lc.reliLevel * lc.Occupants;
			lc.healthLevel = (short)((float)eco.HealthScore - lc.poluLevel / 4f - (float)((100 - lc.secuLevel) / 10));
			if (lc.lClass == LotCreator.LotClass.LOW)
			{
				lc.birthRate = 0.3f + eco.BirthRatePolicy * 1.2f + (float)lc.reliLevel / 55f - lc.eduLevel / 70f + (float)lc.healthLevel / 60f - (float)lc.Stage / 20f;
			}
			else if (lc.lClass == LotCreator.LotClass.MIDDLE || lc.lClass == LotCreator.LotClass.MIXRETAIL)
			{
				lc.birthRate = 0.1f + eco.BirthRatePolicy + (float)lc.reliLevel / 50f - lc.eduLevel / 70f + (float)lc.healthLevel / 65f - (float)lc.Stage / 20f;
			}
			else
			{
				lc.birthRate = eco.BirthRatePolicy * 0.8f + (float)lc.reliLevel / 40f - lc.eduLevel / 70f + (float)lc.healthLevel / 80f - (float)lc.Stage / 20f;
			}
			if (lc.birthRate < 0.5f)
			{
				lc.birthRate = 0.5f;
			}
			totalBirthPoints += lc.birthRate * (float)lc.Occupants;
			if (!lc.isDecayed && World.dataReady)
			{
				if (lc.LandValue < lc.desiredValue / 2)
				{
					lc.negatives += decayRedu * w.decayMultiplier;
					lc.decayReason = "Land Value Too Low";
				}
				else if (!lc.watered)
				{
					lc.negatives += decayRedu * w.decayMultiplier;
					lc.decayReason = "Water Shortage";
				}
				else if (!lc.powered)
				{
					lc.negatives += decayRedu * w.decayMultiplier;
					lc.decayReason = "Power Outage";
				}
				else if (!lc.validCommute && lc.pathTried > 5)
				{
					lc.negatives += decayRedu * w.decayMultiplier / 2f;
					lc.decayReason = "Could Not Reach Job";
				}
				else
				{
					lc.negatives -= 2f * decayRedu;
					if (lc.negatives < 0f)
					{
						lc.negatives = 0f;
						lc.decayReason = "";
					}
				}
			}
			else if (lc.watered && lc.powered)
			{
				lc.negatives -= decayRedu;
			}
			if (lc.dummy == null)
			{
				if (lc.negatives > 2f)
				{
					lc.DisplayDummy();
				}
			}
			else if (lc.decayReason.StartsWith("W") && lc.watered)
			{
				Object.Destroy(lc.dummy.gameObject);
			}
			else if (lc.decayReason.StartsWith("P") && lc.powered)
			{
				Object.Destroy(lc.dummy.gameObject);
			}
			else if (lc.decayReason.StartsWith("C") && lc.validCommute)
			{
				Object.Destroy(lc.dummy.gameObject);
			}
			else if (lc.dummy != null && lc.negatives < 2f)
			{
				Object.Destroy(lc.dummy.gameObject);
			}
		}
		if (lc.audioSource != null && Random.value < 0.1f && timeController.m_timeline > 2f && timeController.m_timeline < 22f)
		{
			lc.PlayAudioClip();
		}
	}

	private void BusinessLotsUpdate(LotCreator lc)
	{
		if (lc == null)
		{
			return;
		}
		if (!lc.isDecayed && World.dataReady)
		{
			if (!lc.validCommute && lc.pathTried > 5)
			{
				lc.negatives += decayRedu * w.decayMultiplier / 2f;
				lc.decayReason = "Could Not Reach Job";
			}
			else if (lc.LandValue < lc.desiredValue / 2)
			{
				lc.negatives += decayRedu * w.decayMultiplier;
				lc.decayReason = "Land Value Too Low";
			}
			else if (!lc.watered)
			{
				lc.negatives += decayRedu * w.decayMultiplier;
				lc.decayReason = "Water Shortage";
			}
			else if (!lc.powered)
			{
				lc.negatives += decayRedu * w.decayMultiplier;
				lc.decayReason = "Power Outage";
			}
			else
			{
				lc.negatives -= 2f * decayRedu;
				if (lc.negatives < 0f)
				{
					lc.negatives = 0f;
					lc.decayReason = "";
				}
			}
		}
		else if (lc.watered && lc.powered)
		{
			lc.negatives -= decayRedu * w.decayMultiplier;
		}
		if (lc.dummy == null)
		{
			if (lc.negatives > 2f)
			{
				lc.DisplayDummy();
			}
		}
		else if (lc.decayReason.StartsWith("W") && lc.watered)
		{
			Object.Destroy(lc.dummy.gameObject);
		}
		else if (lc.decayReason.StartsWith("P") && lc.powered)
		{
			Object.Destroy(lc.dummy.gameObject);
		}
		else if (lc.dummy != null && lc.negatives < 2f)
		{
			Object.Destroy(lc.dummy.gameObject);
		}
	}

	private void SlumUpdate(SlumCreator sc)
	{
		if (sc.audioSource != null && Random.value < 0.01f && timeController.m_timeline > 1f && timeController.m_timeline < 4f)
		{
			sc.PlayAudioClip();
		}
	}

	private void SpawnTraffic(LotCreator lc)
	{
		if (lc.commuteState == "cc" || lc.commuteState == "nc")
		{
			if (TrafficManager.cars < SettingsStatic.maxCars && lc.carsOut < Mathf.RoundToInt(Random.value + (float)(lc.Occupants / (12 + lc.Stage * SettingsStatic.carDensity))))
			{
				traffic.LotSpawnCar(lc);
			}
			if (TrafficManager.peds < SettingsStatic.maxPeds && lc.pedsOut < Mathf.RoundToInt(1f + (float)lc.Occupants / (10f + (float)lc.Stage * SettingsStatic.pedsDensity)))
			{
				traffic.SpawnPed(lc);
			}
		}
		else if (lc.commuteState == "bc")
		{
			if (TrafficManager.cars < SettingsStatic.maxCars && lc.carsOut < 1)
			{
				traffic.LotSpawnSpecialVehicle(lc, "Vehicles/C_Bus");
			}
			if (TrafficManager.peds < SettingsStatic.maxPeds && lc.pedsOut < Mathf.RoundToInt(1f + (float)lc.Occupants / (10f + (float)lc.Stage * SettingsStatic.pedsDensity)))
			{
				traffic.SpawnPed(lc);
			}
		}
		else if (lc.commuteState == "wc")
		{
			if (TrafficManager.peds < SettingsStatic.maxPeds && lc.pedsOut < Mathf.RoundToInt(1f + (float)lc.Occupants / (10f + (float)lc.Stage * SettingsStatic.pedsDensity)))
			{
				traffic.SpawnPed(lc);
			}
		}
		else if (lc.commuteState == "tc" && Random.value < 0.1f)
		{
			if (w.Rails.Count + w.Underground.Count > 0 && TrafficManager.nTrains <= r.trainStops.Count)
			{
				traffic.SpawnTrain(lc);
			}
		}
		else if (lc.commuteState == "at" && TrafficManager.cars < SettingsStatic.maxCars + 150 && lc.carsOut < Mathf.RoundToInt(Random.value + (float)(lc.Occupants / (10 + lc.Stage * SettingsStatic.carDensity))))
		{
			traffic.LotSpawnAirTaxi(lc);
		}
	}

	private void SpawnFX(int type, LotCreator lc)
	{
		switch (type)
		{
		case 0:
			if (lc.streetLight != null)
			{
				((GameObject)Object.Instantiate(Resources.Load("Government/PowerFX"))).transform.position = lc.streetLight.transform.position;
			}
			break;
		case 1:
			if (lc.RoadEntry != null)
			{
				((GameObject)Object.Instantiate(Resources.Load("Government/WaterFX"))).transform.position = lc.RoadEntry.gameObject.transform.position;
			}
			break;
		}
	}

	private IEnumerator EmissiveON()
	{
		yield return new WaitForEndOfFrame();
		vehicleMat.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowExchange.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowRicardo.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowsResidential.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowOld.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowNeutral.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowRaw.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowCommercial.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowHorizon.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowHito.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowOxygen.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowStab.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowSteel.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		windowOffice.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		logoMat.EnableKeyword("_EMISSION");
		yield return new WaitForSeconds(0.5f);
		propsMat.EnableKeyword("_EMISSION");
		for (int i = 0; i < 10; i++)
		{
			autoExposure.minLuminance.value -= 0.1f;
			bloom.intensity.value += 0.05f;
			yield return new WaitForSeconds(0.5f);
		}
		yield return new WaitForEndOfFrame();
	}

	private IEnumerator EmissiveOff()
	{
		yield return new WaitForEndOfFrame();
		bloom.intensity.value = 0.6f;
		yield return new WaitForEndOfFrame();
		windowExchange.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowRicardo.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowsResidential.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		autoExposure.minLuminance.value = -1.6f;
		windowOld.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowNeutral.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		autoExposure.minLuminance.value = -1.4f;
		windowRaw.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowCommercial.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		autoExposure.minLuminance.value = -1.2f;
		windowHorizon.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowHito.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowOxygen.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		autoExposure.minLuminance.value = -1f;
		windowStab.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		windowSteel.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		autoExposure.minLuminance.value = -0.8f;
		windowOffice.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		logoMat.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		autoExposure.minLuminance.value = -0.65f;
		vehicleMat.DisableKeyword("_EMISSION");
		yield return new WaitForSeconds(1f);
		propsMat.DisableKeyword("_EMISSION");
	}

	private void DefaultLights()
	{
		bloom.intensity.value = 0.6f;
		windowExchange.DisableKeyword("_EMISSION");
		windowRicardo.DisableKeyword("_EMISSION");
		windowsResidential.DisableKeyword("_EMISSION");
		windowOld.DisableKeyword("_EMISSION");
		windowNeutral.DisableKeyword("_EMISSION");
		windowRaw.DisableKeyword("_EMISSION");
		windowCommercial.DisableKeyword("_EMISSION");
		windowHorizon.DisableKeyword("_EMISSION");
		windowHito.DisableKeyword("_EMISSION");
		windowOxygen.DisableKeyword("_EMISSION");
		windowStab.DisableKeyword("_EMISSION");
		windowSteel.DisableKeyword("_EMISSION");
		windowOffice.DisableKeyword("_EMISSION");
		logoMat.DisableKeyword("_EMISSION");
		propsMat.DisableKeyword("_EMISSION");
		vehicleMat.DisableKeyword("_EMISSION");
		autoExposure.minLuminance.value = -0.65f;
	}
}
