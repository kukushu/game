using UnityEngine;

public class BlockData
{
	public enum Type
	{
		GRASS,
		SAND,
		GRASSTREE,
		ROAD,
		BUILDING
	}

	public Type t;

	public bool g;

	public bool r;

	public bool p;

	public bool b;

	public bool a;

	public bool u;

	public Chunk o;

	public byte[] pos = new byte[2];

	public Vector2Int wP;

	public float h;

	public byte bio;

	public byte e;

	public short[] v = new short[2];

	public ServiceCreator sO;

	public LotCreator lO;

	public SlumCreator uO;

	public sbyte st;

	public byte s1;

	public byte s2;

	public sbyte gT;

	public string pT = "";

	public short pR;

	public void FlatHeight(float newHeight)
	{
		h = newHeight;
		ClearTerrain();
	}

	public void ClearTerrain()
	{
		if (t == Type.GRASSTREE)
		{
			t = Type.GRASS;
		}
		g = false;
	}
}
