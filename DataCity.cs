using System.Collections.Generic;
using UnityEngine;

public class DataCity
{
	public string cityName;

	public string codeName;

	public bool built;

	public bool owned;

	public bool isCapital;

	public int population;

	public int aiportConnections;

	public int jobsAvailable;

	public int jobsShared;

	public int unemployment;

	public int balance;

	public int powerCapacity;

	public int powerUse;

	public int LCpart;

	public int MCpart;

	public int UCpart;

	public int GDPcap;

	public float LCbirth;

	public float MCbirth;

	public float UCbirth;

	public int UnivCapacity;

	public int TradeCapacity = 1;

	public int TradeUse;

	public int LC_City;

	public int MC_City;

	public int UC_City;

	public int O_City;

	public int R_City;

	public int M_City;

	public int HT_City;

	public int sharedFund;

	public Vector3 pos;

	public int rX;

	public int rZ;

	public List<Vector3Int> cons = new List<Vector3Int>();

	public List<Connection> connections = new List<Connection>();
}
