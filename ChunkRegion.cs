using System.Collections.Generic;
using UnityEngine;

public class ChunkRegion
{
	public enum ChunkStatus
	{
		DRAW,
		DONE,
		KEEP
	}

	public Material terrainMaterial;

	public Material treeMaterial;

	public Material fluidMaterial;

	public Material buildingMaterial;

	public BlockRegion[,,] chunkData;

	public GameObject chunk;

	public GameObject tree;

	public GameObject water;

	public Heightmapper heightmapper;

	public int cX;

	public int cZ;

	public WorldRegion region;

	public DataCity city;

	public List<LotCreator> lots = new List<LotCreator>();

	public ChunkStatus status;

	public ChunkMB mb;

	public bool toUpdate;

	public bool isUpdating;

	private void BuildChunk()
	{
		WorldRegion.chunkList.Add(this);
		chunkData = new BlockRegion[WorldRegion.chunkSize, WorldRegion.worldHeight, WorldRegion.chunkSize];
		for (int i = 0; i < WorldRegion.chunkSize; i++)
		{
			for (int j = 16; j <= 30; j++)
			{
				for (int k = 0; k < WorldRegion.chunkSize; k++)
				{
					int num = (int)((float)k + chunk.transform.position.x);
					int num2 = (int)((float)j + chunk.transform.position.y);
					int num3 = (int)((float)i + chunk.transform.position.z);
					Vector3Int vector3Int = new Vector3Int(k, j, i);
					Vector3Int wPos = new Vector3Int((int)chunk.transform.position.x + k, (int)chunk.transform.position.y + j, (int)chunk.transform.position.z + i);
					bool flag = false;
					BlockLight blockLight = WorldRegion.blockData[num * 8, num3 * 8];
					float num4;
					if (city != null && city.built && blockLight != null)
					{
						num4 = (float)blockLight.h / 100f;
						num4 = ((!(num4 < 30f)) ? (25f + num4 / 6f) : (15f + num4 / 2f));
						flag = blockLight.t[1];
					}
					else
					{
						num4 = Heightmapper.GetRegionHeight(num * 8, num3 * 8) + 2f;
					}
					float num5 = num4;
					num4 *= 4f;
					num4 = Mathf.RoundToInt(num4);
					num4 /= 4f;
					float num6 = (30f - num5) / 100f;
					float num7 = Random.Range(0f, 0.03f);
					switch (num2)
					{
					case 16:
						if (num5 >= 30f)
						{
							if (city != null && city.built && blockLight != null)
							{
								if (blockLight.s > 1)
								{
									chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.DARKSTONE, vector3Int, num4, chunk.gameObject, this, wPos, num5);
								}
								else if (flag)
								{
									chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.GRASSTREE, vector3Int, num4, chunk.gameObject, this, wPos, num5);
								}
								else
								{
									chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.GRASS, vector3Int, num4, chunk.gameObject, this, wPos, num5);
								}
							}
							else if (num6 + num7 > 0f)
							{
								chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.GRASSTREE, vector3Int, num4, chunk.gameObject, this, wPos, num5);
							}
							else
							{
								chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.GRASS, vector3Int, num4, chunk.gameObject, this, wPos, num5);
							}
						}
						else
						{
							chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.SAND, vector3Int, num4, chunk.gameObject, this, wPos, num5);
						}
						break;
					case 30:
						if (num4 < 30f)
						{
							chunkData[k, j, i] = new BlockRegion(BlockRegion.BlockType.WATER, vector3Int, 30f, chunk.gameObject, this, wPos, num5);
						}
						break;
					}
					status = ChunkStatus.DRAW;
				}
			}
		}
	}

	public void Redraw()
	{
		if (!isUpdating)
		{
			isUpdating = true;
			Object.DestroyImmediate(chunk.GetComponent<MeshFilter>());
			Object.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
			Object.DestroyImmediate(chunk.GetComponent<Collider>());
			Object.DestroyImmediate(water.GetComponent<MeshFilter>());
			Object.DestroyImmediate(water.GetComponent<MeshRenderer>());
			DrawChunk();
		}
	}

	public void DrawChunk()
	{
		if (!city.owned)
		{
			int index = Random.Range(0, Dico.cityName.Count);
			city.cityName = Dico.cityName[index];
			city.pos = chunk.transform.position;
		}
		UpdateTree();
		for (int i = 0; i < WorldRegion.chunkSize; i++)
		{
			for (int j = 0; j < WorldRegion.chunkSize; j++)
			{
				chunkData[j, 16, i].Draw();
				if (chunkData[j, 30, i] != null)
				{
					chunkData[j, 30, i].Draw();
				}
			}
		}
		DrawBuildings();
		CombineQuads(chunk.gameObject, terrainMaterial);
		(chunk.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider).sharedMesh = chunk.transform.GetComponent<MeshFilter>().mesh;
		CombineQuads(water.gameObject, fluidMaterial);
		CombineQuads(tree.gameObject, treeMaterial);
		status = ChunkStatus.DONE;
		isUpdating = false;
		toUpdate = false;
	}

	public void UpdateTree()
	{
		for (int i = 0; i < WorldRegion.chunkSize; i++)
		{
			for (int j = 0; j < WorldRegion.chunkSize; j++)
			{
				BlockRegion blockRegion = chunkData[j, 16, i];
				if (blockRegion == null || blockRegion.bType != BlockRegion.BlockType.GRASSTREE)
				{
					continue;
				}
				if (WorldRegion.chunkSize == 64)
				{
					if (Random.value > 0.5f)
					{
						BuildingCreator.CreateTree(new Vector3(j, blockRegion.heightPos, i), tree, cZ, blockRegion.unrounded);
					}
				}
				else
				{
					BuildingCreator.CreateTree(new Vector3(j, blockRegion.heightPos, i), tree, cZ, blockRegion.unrounded);
				}
			}
		}
	}

	private void DrawBuildings()
	{
		if (!city.owned)
		{
			return;
		}
		Vector3 pos = new Vector3(chunk.transform.position.x + (float)(WorldRegion.chunkSize / 2), 50f, chunk.transform.position.z + (float)(WorldRegion.chunkSize / 2));
		region.regionUI.CreateText(city.population, city.cityName, pos, city.isCapital);
		int num = 20;
		if (WorldRegion.chunkSize == 64)
		{
			num = 32;
		}
		int num2 = cX * (16 * num);
		int num3 = cZ * (16 * num);
		for (int i = 0; i < WorldRegion.chunkSize; i++)
		{
			for (int j = 0; j < WorldRegion.chunkSize; j++)
			{
				bool flag = false;
				BlockRegion blockRegion = chunkData[i, 16, j];
				GameObject gameObject = new GameObject(i + "_b_" + j);
				gameObject.transform.position = new Vector3(chunk.transform.position.x + (float)i, blockRegion.heightPos, (int)chunk.transform.position.z + j);
				BlockLight blockLight = null;
				BlockLight blockLight2 = null;
				BlockLight blockLight3 = null;
				BlockLight blockLight4 = null;
				int num4 = num2 + i * 8;
				int num5 = num3 + j * 8;
				if (i - 2 > 0 && j + 3 < WorldRegion.chunkSize)
				{
					blockLight = WorldRegion.blockData[num4 - 2, num5 + 3];
				}
				if (i + 3 < WorldRegion.chunkSize && j + 3 < WorldRegion.chunkSize)
				{
					blockLight2 = WorldRegion.blockData[num4 + 3, num5 + 3];
				}
				if (i - 2 > 0 && j - 1 > 0)
				{
					blockLight3 = WorldRegion.blockData[num4 - 2, num5 - 1];
				}
				if (i + 2 < WorldRegion.chunkSize && j - 1 > 0)
				{
					blockLight4 = WorldRegion.blockData[num4 + 2, num5 - 1];
				}
				if (blockLight != null && blockLight.s > 0)
				{
					BuildingCreator.CreateBuilding(new Vector3(-0.375f, 0f, 0.125f), gameObject, blockLight.s);
					flag = true;
				}
				if (blockLight2 != null && blockLight2.s > 0)
				{
					BuildingCreator.CreateBuilding(new Vector3(0.125f, 0f, 0.125f), gameObject, blockLight2.s);
					flag = true;
				}
				if (blockLight3 != null && blockLight3.s > 0)
				{
					BuildingCreator.CreateBuilding(new Vector3(-0.375f, 0f, -0.375f), gameObject, blockLight3.s);
					flag = true;
				}
				if (blockLight4 != null && blockLight4.s > 0)
				{
					BuildingCreator.CreateBuilding(new Vector3(0.125f, 0f, -0.375f), gameObject, blockLight4.s);
					flag = true;
				}
				if (!flag)
				{
					Object.Destroy(gameObject);
					continue;
				}
				CombineQuads(gameObject, buildingMaterial);
				gameObject.layer = 15;
			}
		}
	}

	public ChunkRegion(Vector3 position, WorldRegion w, int _x, int _z, DataCity _city)
	{
		Vector3 vector = new Vector3(0f, -0.12f, 0f);
		cX = _x;
		cZ = _z;
		chunk = new GameObject(WorldRegion.BuildChunkName(position));
		chunk.transform.position = position;
		chunk.isStatic = false;
		tree = new GameObject(WorldRegion.BuildChunkName(position) + "_T");
		tree.transform.position = position;
		tree.isStatic = true;
		water = new GameObject(WorldRegion.BuildChunkName(position) + "_W");
		water.transform.position = position + vector;
		water.isStatic = true;
		mb = chunk.AddComponent<ChunkMB>();
		mb.SetRegionOwner(this);
		terrainMaterial = w.textureAtlas;
		fluidMaterial = w.fluidTexture;
		buildingMaterial = w.buildingTexture;
		treeMaterial = w.treeTexture;
		heightmapper = w.heightmapper;
		region = w;
		city = _city;
		BuildChunk();
	}

	public void DestroyChunk()
	{
		Object.DestroyImmediate(chunk.GetComponent<MeshFilter>());
		Object.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
		Object.DestroyImmediate(chunk.GetComponent<Collider>());
		Object.DestroyImmediate(water.GetComponent<MeshFilter>());
		Object.DestroyImmediate(water.GetComponent<MeshRenderer>());
	}

	private void CombineQuads(GameObject o, Material m)
	{
		MeshFilter[] componentsInChildren = o.GetComponentsInChildren<MeshFilter>();
		CombineInstance[] array = new CombineInstance[componentsInChildren.Length];
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			array[i].mesh = componentsInChildren[i].sharedMesh;
			array[i].transform = componentsInChildren[i].transform.localToWorldMatrix;
		}
		MeshFilter meshFilter = (MeshFilter)o.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = new Mesh();
		meshFilter.mesh.CombineMeshes(array);
		(o.AddComponent(typeof(MeshRenderer)) as MeshRenderer).material = m;
		foreach (Transform item in o.transform)
		{
			Object.Destroy(item.gameObject);
		}
	}
}
