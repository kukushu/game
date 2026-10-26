using ES3Internal;
using UnityEngine.AzureSky;
using UnityEngine.Scripting;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[]
{
	"days", "Population", "otherCitiesPop", "InformalPop", "LCPartPercent", "LCnaturalGrowth", "LCPop", "MCPartPercent", "MCnaturalGrowth", "MCPop",
	"UCPartPercent", "UCnaturalGrowth", "UCPop", "PopGrowthNominal", "Liberals", "Conservatives", "ApprovalRate", "GlobalLeaning", "Slum_Demand", "LC_Demand",
	"MC_Demand", "UC_Demand", "M_Demand", "HT_Demand", "R_Demand", "O_Demand", "P_Demand", "Jobs", "ActivePop", "ActivePercent",
	"Unemployment", "GDPgrowth", "retailJobs", "retailPart", "officeJobs", "officePart", "manufactureJobs", "manufacturePart", "hightechJobs", "hightechPart",
	"serviceJobs", "servicePart", "calendar", "SocialLadderPolicy", "PolicyLeaning", "MCaccess", "UCaccess", "FreedomIndex", "CivilRights", "riotCivilRights",
	"BirthRatePolicy", "BirthRate", "Security", "riotSecurity", "EnviroPol", "Environment", "SchoolEfficiency", "HospitalEfficiency", "Corruption", "ApprovalRight",
	"ApprovalLeft", "Cost", "Religiousness", "ReligionScore", "Retail", "Office", "Manufacturing", "HighTech", "enactedPolicies", "data",
	"SchoolCapacity", "HighSchoolCapacity", "TotalPupils", "TotalStudents", "HospitalCapacity", "HospitalUse", "FireCapacity", "FireUse", "PoliceCapacity", "PoliceUse",
	"Schools", "HighSchools", "Hospitals", "PoliceStations", "FireStations", "BuildingsCount", "PowerCapacity", "PowerUse", "WaterCapacity", "WaterUse",
	"EducationQuality", "EducationLevel", "HealthScore", "SecurityScore", "EnviroScore", "ReligiousLevel", "QualityOfLife", "Inequality", "PollutionRate", "GDP",
	"AverageValue", "ongoingDecay", "isBankrupted", "Undocumented", "Immigrants", "Locals", "polBudgetDivider", "HQbuilding", "timeController"
})]
public class ES3UserType_Economy : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3UserType_Economy()
		: base(typeof(Economy))
	{
		Instance = this;
		priority = 1;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		Economy economy = (Economy)obj;
		writer.WriteProperty("Population", economy.Population, ES3Type_float.Instance);
		writer.WriteProperty("otherCitiesPop", economy.otherCitiesPop, ES3Type_float.Instance);
		writer.WriteProperty("InformalPop", economy.InformalPop, ES3Type_float.Instance);
		writer.WriteProperty("LCPartPercent", economy.LCPartPercent, ES3Type_float.Instance);
		writer.WriteProperty("LCnaturalGrowth", economy.LCnaturalGrowth, ES3Type_float.Instance);
		writer.WriteProperty("LCPop", economy.LCPop, ES3Type_float.Instance);
		writer.WriteProperty("MCPartPercent", economy.MCPartPercent, ES3Type_float.Instance);
		writer.WriteProperty("MCnaturalGrowth", economy.MCnaturalGrowth, ES3Type_float.Instance);
		writer.WriteProperty("MCPop", economy.MCPop, ES3Type_float.Instance);
		writer.WriteProperty("UCPartPercent", economy.UCPartPercent, ES3Type_float.Instance);
		writer.WriteProperty("UCnaturalGrowth", economy.UCnaturalGrowth, ES3Type_float.Instance);
		writer.WriteProperty("UCPop", economy.UCPop, ES3Type_float.Instance);
		writer.WriteProperty("PopGrowthNominal", economy.PopGrowthNominal, ES3Type_float.Instance);
		writer.WriteProperty("Liberals", economy.Liberals, ES3Type_float.Instance);
		writer.WriteProperty("Conservatives", economy.Conservatives, ES3Type_float.Instance);
		writer.WriteProperty("ApprovalRate", economy.ApprovalRate, ES3Type_int.Instance);
		writer.WriteProperty("GlobalLeaning", economy.GlobalLeaning, ES3Type_float.Instance);
		writer.WriteProperty("Slum_Demand", economy.Slum_Demand, ES3Type_float.Instance);
		writer.WriteProperty("LC_Demand", economy.LC_Demand, ES3Type_float.Instance);
		writer.WriteProperty("MC_Demand", economy.MC_Demand, ES3Type_float.Instance);
		writer.WriteProperty("UC_Demand", economy.UC_Demand, ES3Type_float.Instance);
		writer.WriteProperty("M_Demand", economy.M_Demand, ES3Type_float.Instance);
		writer.WriteProperty("HT_Demand", economy.HT_Demand, ES3Type_float.Instance);
		writer.WriteProperty("R_Demand", economy.R_Demand, ES3Type_float.Instance);
		writer.WriteProperty("O_Demand", economy.O_Demand, ES3Type_float.Instance);
		writer.WriteProperty("P_Demand", economy.P_Demand, ES3Type_float.Instance);
		writer.WriteProperty("Jobs", economy.Jobs, ES3Type_float.Instance);
		writer.WriteProperty("ActivePop", economy.ActivePop, ES3Type_float.Instance);
		writer.WriteProperty("ActivePercent", economy.ActivePercent, ES3Type_float.Instance);
		writer.WriteProperty("Unemployment", economy.Unemployment, ES3Type_float.Instance);
		writer.WriteProperty("GDPgrowth", economy.GDPgrowth, ES3Type_float.Instance);
		writer.WriteProperty("retailJobs", economy.retailJobs, ES3Type_float.Instance);
		writer.WriteProperty("retailPart", economy.retailPart, ES3Type_float.Instance);
		writer.WriteProperty("officeJobs", economy.officeJobs, ES3Type_float.Instance);
		writer.WriteProperty("officePart", economy.officePart, ES3Type_float.Instance);
		writer.WriteProperty("manufactureJobs", economy.manufactureJobs, ES3Type_float.Instance);
		writer.WriteProperty("manufacturePart", economy.manufacturePart, ES3Type_float.Instance);
		writer.WriteProperty("hightechJobs", economy.hightechJobs, ES3Type_float.Instance);
		writer.WriteProperty("hightechPart", economy.hightechPart, ES3Type_float.Instance);
		writer.WriteProperty("serviceJobs", economy.serviceJobs, ES3Type_float.Instance);
		writer.WriteProperty("servicePart", economy.servicePart, ES3Type_float.Instance);
		writer.WritePropertyByRef("calendar", economy.calendar);
		writer.WriteProperty("SocialLadderPolicy", economy.SocialLadderPolicy, ES3Type_float.Instance);
		writer.WriteProperty("PolicyLeaning", economy.PolicyLeaning, ES3Type_float.Instance);
		writer.WriteProperty("MCaccess", economy.MCaccess, ES3Type_float.Instance);
		writer.WriteProperty("UCaccess", economy.UCaccess, ES3Type_float.Instance);
		writer.WriteProperty("FreedomIndex", economy.FreedomIndex, ES3Type_float.Instance);
		writer.WriteProperty("CivilRights", economy.CivilRights, ES3Type_float.Instance);
		writer.WriteProperty("riotCivilRights", economy.riotCivilRights, ES3Type_float.Instance);
		writer.WriteProperty("BirthRatePolicy", economy.BirthRatePolicy, ES3Type_float.Instance);
		writer.WriteProperty("BirthRate", economy.BirthRate, ES3Type_float.Instance);
		writer.WriteProperty("Security", economy.Security, ES3Type_float.Instance);
		writer.WriteProperty("riotSecurity", economy.riotSecurity, ES3Type_float.Instance);
		writer.WriteProperty("EnviroPol", economy.EnviroPol, ES3Type_float.Instance);
		writer.WriteProperty("Environment", economy.Environment, ES3Type_float.Instance);
		writer.WriteProperty("SchoolEfficiency", economy.SchoolEfficiency, ES3Type_float.Instance);
		writer.WriteProperty("HospitalEfficiency", economy.HospitalEfficiency, ES3Type_float.Instance);
		writer.WriteProperty("Corruption", economy.Corruption, ES3Type_float.Instance);
		writer.WriteProperty("ApprovalRight", economy.ApprovalRight, ES3Type_float.Instance);
		writer.WriteProperty("ApprovalLeft", economy.ApprovalLeft, ES3Type_float.Instance);
		writer.WriteProperty("Cost", economy.Cost, ES3Type_float.Instance);
		writer.WriteProperty("Religiousness", economy.Religiousness, ES3Type_float.Instance);
		writer.WriteProperty("ReligionScore", economy.ReligionScore, ES3Type_float.Instance);
		writer.WriteProperty("Retail", economy.Retail, ES3Type_float.Instance);
		writer.WriteProperty("Office", economy.Office, ES3Type_float.Instance);
		writer.WriteProperty("Manufacturing", economy.Manufacturing, ES3Type_float.Instance);
		writer.WriteProperty("HighTech", economy.HighTech, ES3Type_float.Instance);
		writer.WriteProperty("enactedPolicies", economy.enactedPolicies, ES3Type_int.Instance);
		writer.WriteProperty("data", economy.data, ES3TypeMgr.GetES3Type(typeof(int[,,])));
		writer.WriteProperty("SchoolCapacity", economy.SchoolCapacity, ES3Type_int.Instance);
		writer.WriteProperty("HighSchoolCapacity", economy.HighSchoolCapacity, ES3Type_int.Instance);
		writer.WriteProperty("TotalPupils", economy.TotalPupils, ES3Type_int.Instance);
		writer.WriteProperty("TotalStudents", economy.TotalStudents, ES3Type_int.Instance);
		writer.WriteProperty("HospitalCapacity", economy.HospitalCapacity, ES3Type_int.Instance);
		writer.WriteProperty("HospitalUse", economy.HospitalUse, ES3Type_int.Instance);
		writer.WriteProperty("FireCapacity", economy.FireCapacity, ES3Type_int.Instance);
		writer.WriteProperty("FireUse", economy.FireUse, ES3Type_int.Instance);
		writer.WriteProperty("PoliceCapacity", economy.PoliceCapacity, ES3Type_int.Instance);
		writer.WriteProperty("PoliceUse", economy.PoliceUse, ES3Type_int.Instance);
		writer.WriteProperty("Schools", economy.Schools, ES3Type_int.Instance);
		writer.WriteProperty("HighSchools", economy.HighSchools, ES3Type_int.Instance);
		writer.WriteProperty("Hospitals", economy.Hospitals, ES3Type_int.Instance);
		writer.WriteProperty("PoliceStations", economy.PoliceStations, ES3Type_int.Instance);
		writer.WriteProperty("FireStations", economy.FireStations, ES3Type_int.Instance);
		writer.WriteProperty("BuildingsCount", economy.BuildingsCount, ES3Type_int.Instance);
		writer.WriteProperty("WaterCapacity", economy.WaterCapacity, ES3Type_int.Instance);
		writer.WriteProperty("WaterUse", economy.WaterUse, ES3Type_int.Instance);
		writer.WriteProperty("EducationQuality", economy.EducationQuality, ES3Type_float.Instance);
		writer.WriteProperty("EducationLevel", economy.EducationLevel, ES3Type_float.Instance);
		writer.WriteProperty("HealthScore", economy.HealthScore, ES3Type_int.Instance);
		writer.WriteProperty("SecurityScore", economy.SecurityScore, ES3Type_int.Instance);
		writer.WriteProperty("QualityOfLife", economy.QualityOfLife, ES3Type_int.Instance);
		writer.WriteProperty("Inequality", economy.Inequality, ES3Type_int.Instance);
		writer.WriteProperty("PollutionRate", economy.PollutionRate, ES3Type_float.Instance);
		writer.WriteProperty("GDP", economy.GDP, ES3Type_long.Instance);
		writer.WriteProperty("AverageValue", economy.AverageValue, ES3Type_int.Instance);
		writer.WriteProperty("ongoingDecay", economy.ongoingDecay, ES3Type_bool.Instance);
		writer.WriteProperty("isBankrupted", economy.isBankrupted, ES3Type_bool.Instance);
		writer.WriteProperty("Undocumented", economy.Undocumented, ES3Type_float.Instance);
		writer.WriteProperty("Immigrants", economy.Immigrants, ES3Type_float.Instance);
		writer.WriteProperty("Locals", economy.Locals, ES3Type_float.Instance);
		writer.WritePropertyByRef("HQbuilding", economy.HQbuilding);
		writer.WritePropertyByRef("timeController", economy.timeController);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		Economy economy = (Economy)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "Population":
				economy.Population = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "otherCitiesPop":
				economy.otherCitiesPop = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "InformalPop":
				economy.InformalPop = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "LCPartPercent":
				economy.LCPartPercent = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "LCnaturalGrowth":
				economy.LCnaturalGrowth = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "LCPop":
				economy.LCPop = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "MCPartPercent":
				economy.MCPartPercent = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "MCnaturalGrowth":
				economy.MCnaturalGrowth = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "MCPop":
				economy.MCPop = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "UCPartPercent":
				economy.UCPartPercent = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "UCnaturalGrowth":
				economy.UCnaturalGrowth = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "UCPop":
				economy.UCPop = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "PopGrowthNominal":
				economy.PopGrowthNominal = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Liberals":
				economy.Liberals = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Conservatives":
				economy.Conservatives = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ApprovalRate":
				economy.ApprovalRate = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "GlobalLeaning":
				economy.GlobalLeaning = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Slum_Demand":
				economy.Slum_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "LC_Demand":
				economy.LC_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "MC_Demand":
				economy.MC_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "UC_Demand":
				economy.UC_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "M_Demand":
				economy.M_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "HT_Demand":
				economy.HT_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "R_Demand":
				economy.R_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "O_Demand":
				economy.O_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "P_Demand":
				economy.P_Demand = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Jobs":
				economy.Jobs = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ActivePop":
				economy.ActivePop = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ActivePercent":
				economy.ActivePercent = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Unemployment":
				economy.Unemployment = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "GDPgrowth":
				economy.GDPgrowth = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "retailJobs":
				economy.retailJobs = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "retailPart":
				economy.retailPart = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "officeJobs":
				economy.officeJobs = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "officePart":
				economy.officePart = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "manufactureJobs":
				economy.manufactureJobs = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "manufacturePart":
				economy.manufacturePart = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "hightechJobs":
				economy.hightechJobs = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "hightechPart":
				economy.hightechPart = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "serviceJobs":
				economy.serviceJobs = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "servicePart":
				economy.servicePart = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "calendar":
				economy.calendar = reader.Read<Calendar>(ES3UserType_Calendar.Instance);
				break;
			case "SocialLadderPolicy":
				economy.SocialLadderPolicy = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "PolicyLeaning":
				economy.PolicyLeaning = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "MCaccess":
				economy.MCaccess = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "UCaccess":
				economy.UCaccess = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "FreedomIndex":
				economy.FreedomIndex = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "CivilRights":
				economy.CivilRights = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "riotCivilRights":
				economy.riotCivilRights = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "BirthRatePolicy":
				economy.BirthRatePolicy = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "BirthRate":
				economy.BirthRate = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Security":
				economy.Security = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "riotSecurity":
				economy.riotSecurity = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "EnviroPol":
				economy.EnviroPol = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Environment":
				economy.Environment = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "SchoolEfficiency":
				economy.SchoolEfficiency = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "HospitalEfficiency":
				economy.HospitalEfficiency = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Corruption":
				economy.Corruption = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ApprovalRight":
				economy.ApprovalRight = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ApprovalLeft":
				economy.ApprovalLeft = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Cost":
				economy.Cost = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Religiousness":
				economy.Religiousness = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "ReligionScore":
				economy.ReligionScore = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Retail":
				economy.Retail = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Office":
				economy.Office = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Manufacturing":
				economy.Manufacturing = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "HighTech":
				economy.HighTech = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "enactedPolicies":
				economy.enactedPolicies = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "data":
				economy.data = reader.Read<int[,,]>();
				break;
			case "SchoolCapacity":
				economy.SchoolCapacity = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "HighSchoolCapacity":
				economy.HighSchoolCapacity = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "TotalPupils":
				economy.TotalPupils = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "TotalStudents":
				economy.TotalStudents = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "HospitalCapacity":
				economy.HospitalCapacity = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "HospitalUse":
				economy.HospitalUse = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "FireCapacity":
				economy.FireCapacity = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "FireUse":
				economy.FireUse = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "PoliceCapacity":
				economy.PoliceCapacity = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "PoliceUse":
				economy.PoliceUse = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "Schools":
				economy.Schools = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "HighSchools":
				economy.HighSchools = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "Hospitals":
				economy.Hospitals = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "PoliceStations":
				economy.PoliceStations = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "FireStations":
				economy.FireStations = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "BuildingsCount":
				economy.BuildingsCount = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "WaterCapacity":
				economy.WaterCapacity = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "WaterUse":
				economy.WaterUse = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "EducationQuality":
				economy.EducationQuality = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "EducationLevel":
				economy.EducationLevel = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "HealthScore":
				economy.HealthScore = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "SecurityScore":
				economy.SecurityScore = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "QualityOfLife":
				economy.QualityOfLife = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "Inequality":
				economy.Inequality = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "PollutionRate":
				economy.PollutionRate = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "GDP":
				economy.GDP = reader.Read<long>(ES3Type_long.Instance);
				break;
			case "AverageValue":
				economy.AverageValue = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "ongoingDecay":
				economy.ongoingDecay = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "isBankrupted":
				economy.isBankrupted = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "Undocumented":
				economy.Undocumented = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Immigrants":
				economy.Immigrants = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "Locals":
				economy.Locals = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "HQbuilding":
				economy.HQbuilding = reader.Read<ServiceCreator>();
				break;
			case "timeController":
				economy.timeController = reader.Read<AzureTimeController>();
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
