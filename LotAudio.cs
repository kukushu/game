using UnityEngine;

public class LotAudio : MonoBehaviour
{
	public AudioClip ConstructionSoft;

	public AudioClip ConstructionBip;

	public AudioClip ConstructionGeneral;

	public AudioClip ManuA;

	public AudioClip ManuB;

	public AudioClip HTA;

	public AudioClip HTB;

	public AudioClip FactoryAlarm;

	public AudioClip SoftMachine;

	public AudioClip HeavyMachineC;

	public AudioClip HummingWarehouse;

	public AudioClip FutureFactoryA;

	public AudioClip FutureFactoryB;

	public AudioClip HeavyFan;

	public AudioClip WetMarket;

	public AudioClip WetMarketB;

	public AudioClip RoosterA;

	public AudioClip RoosterB;

	public AudioClip TVnoiseA;

	public AudioClip DishesA;

	public AudioClip DishesB;

	public AudioClip DishesC;

	public AudioClip KidsB;

	public AudioClip SmallCrowdA;

	public AudioClip DogBarksA;

	public AudioClip PopMusic;

	public AudioClip KidsA;

	public AudioClip YardA;

	public AudioClip DogBarksB;

	public AudioClip TondeuseA;

	public AudioClip DoorOpen;

	public AudioClip YardC;

	public AudioClip KidsC;

	public AudioClip YardB;

	public AudioClip TondeuseB;

	public AudioClip SoftPiano;

	public AudioClip SoftPianoB;

	public AudioClip SilentCrowd;

	public AudioClip CarUnlock;

	public AudioClip DogBarkC;

	public AudioClip Violin;

	public AudioClip YardD;

	public AudioClip StoreAmbientA;

	public AudioClip StoreAmbientB;

	public AudioClip RetailDoorA;

	public AudioClip StoreAmbientC;

	public AudioClip SmallCrowdB;

	public AudioClip SmallCrowdC;

	public AudioClip Elevator;

	public AudioClip AlarmOffice;

	public AudioClip CorporateSoundscape;

	public AudioClip BigCrowd;

	public AudioClip CityNoiseA;

	public AudioClip CityNoiseB;

	public AudioClip CityNoiseC;

	public AudioClip CityNoiseD;

	public AudioClip CityNoiseE;

	public AudioClip CityNoiseF;

	public AudioClip CityNoiseG;

	public AudioClip HonkA;

	public AudioClip HonkB;

	public AudioClip HonkC;

	public AudioClip HonkD;

	public AudioClip HonkE;

	public AudioClip HonkF;

	public AudioClip HonkG;

	public AudioClip HonkH;

	public AudioClip AccelerationA;

	public AudioClip AccelerationB;

	public AudioClip AccelerationC;

	public AudioClip AccelerationD;

	public AudioClip PedsA;

	public AudioClip PedsB;

	public AudioClip BrownNoise;

	public AudioClip BrownNoise2;

	public AudioClip BrownNoise3;

	public AudioClip CarAlarm;

	public AudioClip Brakes;

	public AudioClip Airport;

	public AudioClip Port;

	public AudioClip ChurchBells;

	public AudioClip GovPhone;

	public AudioClip MosqueCall;

	public AudioClip ParkA;

	public AudioClip ParkB;

	public AudioClip ParkC;

	public AudioClip ParkD;

	public AudioClip ParkE;

	public AudioClip PowerPlant;

	public AudioClip PumpStations;

	public AudioClip SchoolBell;

	public AudioClip HighSchoolAmbient;

	public AudioClip Prison;

	public AudioClip Future;

	public AudioClip buldozeShortA;

	public AudioClip buldozeShortB;

	public AudioClip buldozeMediumA;

	public AudioClip buldozeMediumB;

	public AudioClip buldozeLongA;

	public AudioClip SetAudioClip(LotCreator lc)
	{
		AudioClip result = HonkA;
		float value = Random.value;
		if (lc.lClass == LotCreator.LotClass.MANUFACTURE)
		{
			if (lc.Stage < 2)
			{
				return null;
			}
			if (value < 0.15f)
			{
				result = ManuA;
			}
			else if (value < 0.3f)
			{
				result = ManuB;
			}
			else if ((double)value < 0.45)
			{
				result = FactoryAlarm;
			}
			else if ((double)value < 0.6)
			{
				result = HeavyMachineC;
			}
			else if ((double)value < 0.75)
			{
				result = HummingWarehouse;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? HeavyFan : ConstructionBip);
			}
		}
		else if (lc.lClass == LotCreator.LotClass.HIGHTECH)
		{
			if (lc.Stage < 3)
			{
				return null;
			}
			if (value < 0.15f)
			{
				result = HTA;
			}
			else if (value < 0.3f)
			{
				result = HTB;
			}
			else if ((double)value < 0.45)
			{
				result = SoftMachine;
			}
			else if ((double)value < 0.6)
			{
				result = FutureFactoryA;
			}
			else if ((double)value < 0.75)
			{
				result = FutureFactoryB;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? HummingWarehouse : HeavyFan);
			}
		}
		else if (lc.lClass == LotCreator.LotClass.LOW)
		{
			if (lc.Stage < 5)
			{
				if (value < 0.15f)
				{
					result = TVnoiseA;
				}
				else if (value < 0.3f)
				{
					result = DishesA;
				}
				else if ((double)value < 0.45)
				{
					result = DishesB;
				}
				else if ((double)value < 0.6)
				{
					result = DishesC;
				}
				else if ((double)value < 0.75)
				{
					result = KidsB;
				}
				else
				{
					result = ((!((double)value < 0.9)) ? PopMusic : DogBarkC);
				}
			}
			else if (value < 0.15f)
			{
				result = TVnoiseA;
			}
			else if (value < 0.3f)
			{
				result = DishesA;
			}
			else if ((double)value < 0.45)
			{
				result = DishesB;
			}
			else if ((double)value < 0.6)
			{
				result = DishesC;
			}
			else if ((double)value < 0.75)
			{
				result = SmallCrowdA;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? CityNoiseB : CityNoiseA);
			}
		}
		else if (lc.lClass == LotCreator.LotClass.MIDDLE || lc.lClass == LotCreator.LotClass.MIXRETAIL)
		{
			if (lc.Stage < 5 && lc.lClass != LotCreator.LotClass.MIXRETAIL)
			{
				if (value < 0.15f)
				{
					result = DogBarksA;
				}
				else if (value < 0.3f)
				{
					result = PopMusic;
				}
				else if ((double)value < 0.45)
				{
					result = KidsA;
				}
				else if ((double)value < 0.6)
				{
					result = YardA;
				}
				else if ((double)value < 0.75)
				{
					result = DogBarksB;
				}
				else
				{
					result = ((!((double)value < 0.9)) ? DoorOpen : TondeuseA);
				}
			}
			else if (value < 0.15f)
			{
				result = KidsC;
			}
			else if (value < 0.3f)
			{
				result = DishesA;
			}
			else if ((double)value < 0.45)
			{
				result = KidsB;
			}
			else if ((double)value < 0.6)
			{
				result = YardC;
			}
			else if ((double)value < 0.75)
			{
				result = DogBarkC;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? CityNoiseB : CityNoiseC);
			}
		}
		else if (lc.lClass == LotCreator.LotClass.UPPER || lc.lClass == LotCreator.LotClass.MIXOFFICE)
		{
			if (lc.Stage < 5 && lc.lClass != LotCreator.LotClass.MIXOFFICE)
			{
				if (value < 0.15f)
				{
					result = TondeuseB;
				}
				else if (value < 0.3f)
				{
					result = YardB;
				}
				else if ((double)value < 0.45)
				{
					result = SoftPianoB;
				}
				else if ((double)value < 0.6)
				{
					result = SoftPiano;
				}
				else if ((double)value < 0.75)
				{
					result = YardD;
				}
				else
				{
					result = ((!((double)value < 0.9)) ? Violin : FutureFactoryB);
				}
			}
			else if (value < 0.15f)
			{
				result = SilentCrowd;
			}
			else if (value < 0.3f)
			{
				result = CityNoiseD;
			}
			else if ((double)value < 0.45)
			{
				result = SmallCrowdC;
			}
			else if ((double)value < 0.6)
			{
				result = YardC;
			}
			else if ((double)value < 0.75)
			{
				result = FutureFactoryA;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? SoftPianoB : Elevator);
			}
		}
		else if (lc.lClass == LotCreator.LotClass.RETAIL)
		{
			if (lc.Stage < 3)
			{
				return null;
			}
			if (value < 0.15f)
			{
				result = RetailDoorA;
			}
			else if (value < 0.3f)
			{
				result = StoreAmbientA;
			}
			else if ((double)value < 0.45)
			{
				result = StoreAmbientB;
			}
			else if ((double)value < 0.6)
			{
				result = StoreAmbientC;
			}
			else if ((double)value < 0.75)
			{
				result = SmallCrowdB;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? CityNoiseB : SmallCrowdC);
			}
		}
		else if (lc.lClass == LotCreator.LotClass.OFFICE)
		{
			if (lc.Stage < 3)
			{
				return null;
			}
			if (value < 0.15f)
			{
				result = CityNoiseG;
			}
			else if (value < 0.3f)
			{
				result = CityNoiseF;
			}
			else if ((double)value < 0.45)
			{
				result = AlarmOffice;
			}
			else if ((double)value < 0.6)
			{
				result = Elevator;
			}
			else if ((double)value < 0.75)
			{
				result = CorporateSoundscape;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? CityNoiseC : BigCrowd);
			}
		}
		return result;
	}

	public AudioClip SetSlumClip(SlumCreator sc)
	{
		float value = Random.value;
		if (sc.Stage < 2)
		{
			return null;
		}
		if (value < 0.15f)
		{
			return RoosterA;
		}
		if (value < 0.3f)
		{
			return RoosterB;
		}
		if ((double)value < 0.45)
		{
			return WetMarket;
		}
		if ((double)value < 0.6)
		{
			return WetMarketB;
		}
		if ((double)value < 0.75)
		{
			return KidsA;
		}
		if ((double)value < 0.9)
		{
			return KidsB;
		}
		return ConstructionSoft;
	}

	public AudioClip SetServiceClip(ServiceCreator sc)
	{
		AudioClip result = null;
		float value = Random.value;
		if (sc.type == ServiceCreator.Type.Airport)
		{
			result = Airport;
		}
		else if (sc.type == ServiceCreator.Type.Port)
		{
			result = Port;
		}
		else if (sc.type == ServiceCreator.Type.Government)
		{
			result = GovPhone;
		}
		else if (sc.type == ServiceCreator.Type.Park)
		{
			if (value < 0.2f)
			{
				result = ParkA;
			}
			else if (value < 0.4f)
			{
				result = ParkB;
			}
			else if ((double)value < 0.6)
			{
				result = ParkC;
			}
			else
			{
				result = ((!((double)value < 0.8)) ? ParkE : ParkD);
			}
		}
		else if (sc.type == ServiceCreator.Type.Power)
		{
			result = PowerPlant;
		}
		else if (sc.type == ServiceCreator.Type.Religion)
		{
			result = ((!(sc.typeName == "Muslim")) ? ChurchBells : MosqueCall);
		}
		else if (sc.type == ServiceCreator.Type.School)
		{
			result = ((!(sc.typeName == "S")) ? HighSchoolAmbient : SchoolBell);
		}
		else if (sc.type == ServiceCreator.Type.Water)
		{
			result = PumpStations;
		}
		else if (sc.type == ServiceCreator.Type.Prison)
		{
			result = Prison;
		}
		else if (sc.type == ServiceCreator.Type.Arcology)
		{
			if (value < 0.15f)
			{
				result = DogBarksA;
			}
			else if (value < 0.3f)
			{
				result = DishesA;
			}
			else if ((double)value < 0.45)
			{
				result = KidsB;
			}
			else if ((double)value < 0.6)
			{
				result = YardD;
			}
			else if ((double)value < 0.75)
			{
				result = YardA;
			}
			else
			{
				result = ((!((double)value < 0.9)) ? CityNoiseB : CityNoiseC);
			}
		}
		else if (sc.type == ServiceCreator.Type.Drone)
		{
			result = Future;
		}
		return result;
	}
}
