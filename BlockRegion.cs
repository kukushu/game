using System;
using UnityEngine;

public class BlockRegion
{
	private enum Cubeside
	{
		BOTTOM,
		TOP,
		LEFT,
		RIGHT,
		FRONT,
		BACK
	}

	public enum BlockType
	{
		GRASS,
		STONE,
		DARKSTONE,
		SAND,
		WATER,
		GRASSTREE,
		ROAD,
		RESIDENTIAL,
		AIR
	}

	public BlockType bType;

	public bool isSolid;

	public ChunkRegion owner;

	public GameObject parent;

	public Vector3 arrayPosition;

	public Vector3 worldPosition;

	public float heightPos;

	public float unrounded;

	public Color32 c32;

	public BlockRegion(BlockType b, Vector3 aPos, float rPos, GameObject p, ChunkRegion o, Vector3Int wPos, float unr)
	{
		bType = b;
		owner = o;
		parent = p;
		arrayPosition = aPos;
		heightPos = rPos;
		worldPosition = wPos;
		unrounded = unr;
		SetType(bType);
	}

	private void CreateQuad(Cubeside side, bool _isCliff)
	{
		Mesh mesh = new Mesh();
		Vector3[] vertices = new Vector3[4];
		Vector3[] normals = new Vector3[4];
		Vector2[] uv = new Vector2[4];
		int[] triangles = new int[6];
		Vector2 vector;
		Vector2 vector2;
		Vector2 vector3;
		Vector2 vector4;
		if ((bType == BlockType.GRASS && side == Cubeside.TOP) || (bType == BlockType.GRASSTREE && side == Cubeside.TOP))
		{
			int[] array = new int[5] { 9, 10, 11, 12, 13 };
			int num = array[UnityEngine.Random.Range(0, array.Length)];
			vector = UVs.blockUVs[num, 0];
			vector2 = UVs.blockUVs[num, 1];
			vector3 = UVs.blockUVs[num, 2];
			vector4 = UVs.blockUVs[num, 3];
		}
		else if ((bType == BlockType.GRASS && !_isCliff) || (bType == BlockType.GRASSTREE && !_isCliff))
		{
			vector = UVs.blockUVs[7, 0];
			vector2 = UVs.blockUVs[7, 1];
			vector3 = UVs.blockUVs[7, 2];
			vector4 = UVs.blockUVs[7, 3];
		}
		else if (((bType == BlockType.GRASS) & _isCliff) || ((bType == BlockType.GRASSTREE) & _isCliff))
		{
			vector = UVs.blockUVs[1, 0];
			vector2 = UVs.blockUVs[1, 1];
			vector3 = UVs.blockUVs[1, 2];
			vector4 = UVs.blockUVs[1, 3];
		}
		else if (bType == BlockType.SAND && side != Cubeside.TOP)
		{
			vector = UVs.blockUVs[8, 0];
			vector2 = UVs.blockUVs[8, 1];
			vector3 = UVs.blockUVs[8, 2];
			vector4 = UVs.blockUVs[8, 3];
		}
		else if (bType == BlockType.DARKSTONE && side != Cubeside.TOP)
		{
			vector = UVs.blockUVs[1, 0];
			vector2 = UVs.blockUVs[1, 1];
			vector3 = UVs.blockUVs[1, 2];
			vector4 = UVs.blockUVs[1, 3];
		}
		else
		{
			vector = UVs.blockUVs[(int)bType, 0];
			vector2 = UVs.blockUVs[(int)bType, 1];
			vector3 = UVs.blockUVs[(int)bType, 2];
			vector4 = UVs.blockUVs[(int)bType, 3];
		}
		Vector3 vector5 = new Vector3(-0.5f, -10.5f, 0.5f);
		Vector3 vector6 = new Vector3(0.5f, -10.5f, 0.5f);
		Vector3 vector7 = new Vector3(0.5f, -10.5f, -0.5f);
		Vector3 vector8 = new Vector3(-0.5f, -10.5f, -0.5f);
		Vector3 vector9 = new Vector3(-0.5f, 0.5f, 0.5f);
		Vector3 vector10 = new Vector3(0.5f, 0.5f, 0.5f);
		Vector3 vector11 = new Vector3(0.5f, 0.5f, -0.5f);
		Vector3 vector12 = new Vector3(-0.5f, 0.5f, -0.5f);
		switch (side)
		{
		case Cubeside.BOTTOM:
			vertices = new Vector3[4] { vector5, vector6, vector7, vector8 };
			normals = new Vector3[4]
			{
				Vector3.down,
				Vector3.down,
				Vector3.down,
				Vector3.down
			};
			uv = new Vector2[4] { vector4, vector3, vector, vector2 };
			triangles = new int[6] { 3, 1, 0, 3, 2, 1 };
			break;
		case Cubeside.TOP:
			vertices = new Vector3[4] { vector11, vector10, vector9, vector12 };
			normals = new Vector3[4]
			{
				Vector3.up,
				Vector3.up,
				Vector3.up,
				Vector3.up
			};
			uv = new Vector2[4] { vector4, vector3, vector2, vector };
			triangles = new int[6] { 3, 1, 0, 3, 2, 1 };
			break;
		case Cubeside.LEFT:
			vertices = new Vector3[4] { vector12, vector9, vector5, vector8 };
			normals = new Vector3[4]
			{
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left
			};
			uv = new Vector2[4] { vector4, vector3, vector, vector2 };
			triangles = new int[6] { 3, 1, 0, 3, 2, 1 };
			break;
		case Cubeside.RIGHT:
			vertices = new Vector3[4] { vector10, vector11, vector7, vector6 };
			normals = new Vector3[4]
			{
				Vector3.right,
				Vector3.right,
				Vector3.right,
				Vector3.right
			};
			uv = new Vector2[4] { vector4, vector3, vector, vector2 };
			triangles = new int[6] { 3, 1, 0, 3, 2, 1 };
			break;
		case Cubeside.FRONT:
			vertices = new Vector3[4] { vector9, vector10, vector6, vector5 };
			normals = new Vector3[4]
			{
				Vector3.forward,
				Vector3.forward,
				Vector3.forward,
				Vector3.forward
			};
			uv = new Vector2[4] { vector4, vector3, vector, vector2 };
			triangles = new int[6] { 3, 1, 0, 3, 2, 1 };
			break;
		case Cubeside.BACK:
			vertices = new Vector3[4] { vector11, vector12, vector8, vector7 };
			normals = new Vector3[4]
			{
				Vector3.back,
				Vector3.back,
				Vector3.back,
				Vector3.back
			};
			uv = new Vector2[4] { vector4, vector3, vector, vector2 };
			triangles = new int[6] { 3, 1, 0, 3, 2, 1 };
			break;
		}
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uv;
		mesh.triangles = triangles;
		if (bType != BlockType.SAND && !_isCliff && bType != BlockType.DARKSTONE)
		{
			TerrainColor.ColorWizardry(owner.cZ * 8 + (int)worldPosition.z * 8);
			mesh.colors32 = TerrainColor.GrassColor(unrounded * 8f - 210f, mesh, 0, 0.5f);
		}
		if (bType == BlockType.SAND)
		{
			mesh.colors32 = TerrainColor.SandColor(27.5f, mesh);
		}
		GameObject gameObject = new GameObject("Quad");
		gameObject.transform.position = new Vector3(arrayPosition.x, heightPos, arrayPosition.z);
		gameObject.transform.parent = parent.transform;
		((MeshFilter)gameObject.AddComponent(typeof(MeshFilter))).mesh = mesh;
	}

	public BlockRegion GetBlock(int x, int y, int z)
	{
		BlockRegion[,,] chunkData;
		if (x < 0 || x >= WorldRegion.chunkSize || z < 0 || z >= WorldRegion.chunkSize)
		{
			int num = x + (int)parent.transform.position.x;
			int num2 = z + (int)parent.transform.position.z;
			int num3 = (int)parent.transform.position.x;
			int num4 = (int)parent.transform.position.z;
			if ((float)num < worldPosition.x)
			{
				num3 -= WorldRegion.chunkSize;
			}
			if ((float)num > worldPosition.x)
			{
				num3 += WorldRegion.chunkSize;
			}
			if ((float)num2 < worldPosition.z)
			{
				num4 -= WorldRegion.chunkSize;
			}
			if ((float)num2 > worldPosition.z)
			{
				num4 += WorldRegion.chunkSize;
			}
			x = num - num3;
			z = num2 - num4;
			string key = WorldRegion.BuildChunkName(new Vector3(num3, 0f, num4));
			if (!WorldRegion.coroutineChunks.TryGetValue(key, out var value))
			{
				return null;
			}
			chunkData = value.chunkData;
		}
		else
		{
			chunkData = owner.chunkData;
		}
		return chunkData[x, y, z];
	}

	public bool HasSolidNeighbour(int x, int y, int z, float rPos)
	{
		try
		{
			new Vector3(x, y, z);
			BlockRegion block = GetBlock(x, y, z);
			if (block != null)
			{
				if (block.heightPos >= rPos)
				{
					return true;
				}
				return false;
			}
			return false;
		}
		catch (IndexOutOfRangeException)
		{
		}
		return false;
	}

	public bool NeighbourIsTooHigh(int x, int y, int z, float rPos)
	{
		try
		{
			BlockRegion block = GetBlock(x, y, z);
			if (block != null)
			{
				if (block.heightPos >= rPos + 0.75f || block.heightPos <= rPos - 0.75f)
				{
					return true;
				}
				return false;
			}
			return true;
		}
		catch (IndexOutOfRangeException)
		{
		}
		return false;
	}

	public void Draw()
	{
		if (bType == BlockType.AIR)
		{
			return;
		}
		if (bType == BlockType.WATER)
		{
			CreateQuad(Cubeside.TOP, _isCliff: false);
			return;
		}
		if (!HasSolidNeighbour((int)arrayPosition.x, (int)arrayPosition.y, (int)arrayPosition.z + 1, heightPos))
		{
			if (NeighbourIsTooHigh((int)arrayPosition.x, (int)arrayPosition.y, (int)arrayPosition.z + 1, heightPos))
			{
				CreateQuad(Cubeside.FRONT, _isCliff: true);
			}
			else
			{
				CreateQuad(Cubeside.FRONT, _isCliff: false);
			}
		}
		if (!HasSolidNeighbour((int)arrayPosition.x, (int)arrayPosition.y, (int)arrayPosition.z - 1, heightPos))
		{
			if (NeighbourIsTooHigh((int)arrayPosition.x, (int)arrayPosition.y, (int)arrayPosition.z - 1, heightPos))
			{
				CreateQuad(Cubeside.BACK, _isCliff: true);
			}
			else
			{
				CreateQuad(Cubeside.BACK, _isCliff: false);
			}
		}
		CreateQuad(Cubeside.TOP, _isCliff: false);
		if (!HasSolidNeighbour((int)arrayPosition.x - 1, (int)arrayPosition.y, (int)arrayPosition.z, heightPos))
		{
			if (NeighbourIsTooHigh((int)arrayPosition.x - 1, (int)arrayPosition.y, (int)arrayPosition.z, heightPos))
			{
				CreateQuad(Cubeside.LEFT, _isCliff: true);
			}
			else
			{
				CreateQuad(Cubeside.LEFT, _isCliff: false);
			}
		}
		if (!HasSolidNeighbour((int)arrayPosition.x + 1, (int)arrayPosition.y, (int)arrayPosition.z, heightPos))
		{
			if (NeighbourIsTooHigh((int)arrayPosition.x + 1, (int)arrayPosition.y, (int)arrayPosition.z, heightPos))
			{
				CreateQuad(Cubeside.RIGHT, _isCliff: true);
			}
			else
			{
				CreateQuad(Cubeside.RIGHT, _isCliff: false);
			}
		}
	}

	public void SetType(BlockType b)
	{
		bType = b;
		if (bType == BlockType.AIR || bType == BlockType.WATER)
		{
			isSolid = false;
		}
		else
		{
			isSolid = true;
		}
		if (bType == BlockType.WATER)
		{
			parent = owner.water.gameObject;
		}
		else
		{
			parent = owner.chunk.gameObject;
		}
	}
}
