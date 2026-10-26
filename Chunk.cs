using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chunk
{
	public Material terrainMaterial;

	public Material fluidMaterial;

	public Material leafMaterial;

	public Material trunkMaterial;

	public Material grassMaterial;

	public Material roadMaterial;

	public Material avenueMaterial;

	public Material propMaterial;

	public Material vehicleMaterial;

	public GameObject chunk;

	public GameObject Leaves;

	public GameObject Trunks;

	public GameObject closeLeaves;

	public GameObject farLeaves;

	public GameObject closeTrunk;

	public GameObject farTrunk;

	public GameObject grass;

	public GameObject props;

	public GameObject vehicles;

	public GameObject roads;

	public GameObject avenues;

	public LODGroup leavesLOD;

	public LODGroup trunkLOD;

	public World world;

	public List<LotCreator> lots = new List<LotCreator>();

	public List<ServiceCreator> services = new List<ServiceCreator>();

	public List<RoadCreator> lroads = new List<RoadCreator>();

	public List<RoadCreator> lavenues = new List<RoadCreator>();

	public ChunkMB mb;

	public Vector2Int wPos;

	public bool updateTerrain;

	public bool updatingTerrain;

	public bool updateProps;

	public bool updatingProps;

	public bool updateFlora;

	public bool updatingFlora;

	public int expiredRoads;

	public bool updatingRoads;

	public List<Mesh> meshTerrain;

	public List<Mesh> meshProps;

	public List<Mesh> meshRoads;

	public List<Mesh> meshTrees;

	public List<Mesh> meshGrass;

	public Chunk(Vector3 position, World w)
	{
		world = w;
		wPos = new Vector2Int((int)position.x, (int)position.z);
		chunk = new GameObject(World.BuildChunkName(position));
		chunk.transform.position = position;
		meshTerrain = new List<Mesh>();
		meshProps = new List<Mesh>();
		meshRoads = new List<Mesh>();
		meshGrass = new List<Mesh>();
		meshTrees = new List<Mesh>();
		Leaves = new GameObject(World.BuildChunkName(position) + "_Leaves");
		Leaves.transform.position = position;
		closeLeaves = new GameObject(World.BuildChunkName(position) + "_CloseLeaves");
		closeLeaves.transform.position = position;
		farLeaves = new GameObject(World.BuildChunkName(position) + "_FarLeaves");
		farLeaves.transform.position = position;
		Trunks = new GameObject(World.BuildChunkName(position) + "_Trunks");
		Trunks.transform.position = position;
		closeTrunk = new GameObject(World.BuildChunkName(position) + "_CloseTrunk");
		closeTrunk.transform.position = position;
		farTrunk = new GameObject(World.BuildChunkName(position) + "_FarTrunk");
		farTrunk.transform.position = position;
		grass = new GameObject(World.BuildChunkName(position) + "_G");
		grass.transform.position = position;
		roads = new GameObject(World.BuildChunkName(position) + "_Roads")
		{
			layer = 9
		};
		avenues = new GameObject(World.BuildChunkName(position) + "_Ave")
		{
			layer = 9
		};
		props = new GameObject(World.BuildChunkName(position) + "_Props")
		{
			layer = 11
		};
		vehicles = new GameObject(World.BuildChunkName(position) + "_Cars")
		{
			layer = 11
		};
		mb = chunk.AddComponent<ChunkMB>();
		mb.SetOwner(this);
		terrainMaterial = w.terrainT;
		fluidMaterial = w.fluidTexture;
		leafMaterial = w.leafTexture;
		trunkMaterial = w.trunkTexture;
		grassMaterial = w.grassTexture;
		roadMaterial = w.roadTexture;
		avenueMaterial = w.avenueTexture;
		propMaterial = w.propTexture;
		vehicleMaterial = w.vehicleTexture;
		if (w.loaded)
		{
			LoadChunk();
		}
		else
		{
			BuildChunk();
		}
	}

	private void BuildChunk()
	{
		int num = world.rX * (World.worldSize * World.chunkSize);
		int num2 = world.rZ * (World.worldSize * World.chunkSize);
		for (byte b = 0; b < World.chunkSize; b++)
		{
			for (byte b2 = 0; b2 < World.chunkSize; b2++)
			{
				int num3 = (int)((float)(int)b2 + chunk.transform.position.x);
				int num4 = (int)((float)(int)b + chunk.transform.position.z);
				byte[] cPos = new byte[2] { b2, b };
				Vector2Int vector2Int = new Vector2Int(num3, num4);
				float num5 = ((!world.loaded) ? Heightmapper.GetCityHeight(num3 + num, num4 + num2) : world.blockData[num3, num4].h);
				num5 *= 4f;
				num5 = Mathf.RoundToInt(num5);
				num5 /= 4f;
				byte b3 = (byte)((Utils.GenerateHeight(num3, num4) + num5) / 2f);
				float num6 = 0.15f + (52f - 0.15f * num5) / 100f + UnityEngine.Random.Range(-0.12f, 0f);
				float num7 = UnityEngine.Random.Range(0.35f, 1f);
				if (num5 > 30f)
				{
					if ((float)(int)b3 / 100f < num6)
					{
						Block.Create(BlockData.Type.GRASSTREE, cPos, num5, chunk, this, vector2Int, _isGrass: false, world, b3);
					}
					else
					{
						Block.Create(BlockData.Type.GRASS, cPos, num5, chunk, this, vector2Int, _isGrass: true, world, b3);
					}
				}
				else if (num5 == 30f && b3 > 57)
				{
					Block.Create(BlockData.Type.GRASSTREE, cPos, num5, chunk, this, vector2Int, _isGrass: true, world, b3);
				}
				else if (num5 == 29.75f && b3 > 67)
				{
					Block.Create(BlockData.Type.GRASSTREE, cPos, num5, chunk, this, vector2Int, _isGrass: true, world, b3);
				}
				else if (num5 == 30f && num7 > 0.8f)
				{
					Block.Create(BlockData.Type.SAND, cPos, num5, chunk, this, vector2Int, _isGrass: true, world, b3);
				}
				else if (num5 == 29.75f && num7 > 0.95f)
				{
					Block.Create(BlockData.Type.SAND, cPos, num5, chunk, this, vector2Int, _isGrass: true, world, b3);
				}
				else
				{
					Block.Create(BlockData.Type.SAND, cPos, num5, chunk, this, vector2Int, _isGrass: false, world, b3);
				}
			}
		}
	}

	private void LoadChunk()
	{
		for (int i = 0; i < World.chunkSize; i++)
		{
			for (int j = 0; j < World.chunkSize; j++)
			{
				int num = (int)((float)j + chunk.transform.position.x);
				int num2 = (int)((float)i + chunk.transform.position.z);
				world.blockData[num, num2].o = this;
			}
		}
	}

	public void Redraw()
	{
		if (!updatingTerrain && !updatingFlora && !updatingProps)
		{
			updatingTerrain = true;
			updatingFlora = true;
			updatingProps = true;
			MeshFilter component = chunk.GetComponent<MeshFilter>();
			meshTerrain.Add(component.sharedMesh);
			UnityEngine.Object.DestroyImmediate(component);
			UnityEngine.Object.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
			UnityEngine.Object.DestroyImmediate(chunk.GetComponent<MeshCollider>());
			DrawChunk();
		}
	}

	public IEnumerator TerrainRedraw()
	{
		int delay = 0;
		for (int z = 0; z < World.chunkSize; z++)
		{
			for (int x = 0; x < World.chunkSize; x++)
			{
				int num = (int)((float)x + chunk.transform.position.x);
				int num2 = (int)((float)z + chunk.transform.position.z);
				Block.Draw(world.blockData[num, num2], world);
				delay++;
				if (delay >= 16)
				{
					delay = 0;
					yield return null;
				}
			}
		}
		MeshFilter component = chunk.GetComponent<MeshFilter>();
		meshTerrain.Add(component.sharedMesh);
		UnityEngine.Object.DestroyImmediate(component);
		UnityEngine.Object.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
		UnityEngine.Object.DestroyImmediate(chunk.GetComponent<MeshCollider>());
		CombineTerrainQuads(terrainMaterial);
		MeshCollider obj = chunk.AddComponent(typeof(MeshCollider)) as MeshCollider;
		obj.sharedMesh = chunk.transform.GetComponent<MeshFilter>().sharedMesh;
		obj.sharedMaterial = world.dirt;
		ClearTerrainMesh();
		updatingTerrain = false;
		updateTerrain = false;
	}

	public void DrawChunk()
	{
		UpdateTrees();
		UpdateGrass();
		UpdateProps();
		for (int i = 0; i < World.chunkSize; i++)
		{
			for (int j = 0; j < World.chunkSize; j++)
			{
				int num = (int)((float)j + chunk.transform.position.x);
				int num2 = (int)((float)i + chunk.transform.position.z);
				Block.Draw(world.blockData[num, num2], world);
			}
		}
		try
		{
			CombineTerrainQuads(terrainMaterial);
			MeshCollider obj = chunk.AddComponent(typeof(MeshCollider)) as MeshCollider;
			obj.sharedMesh = chunk.transform.GetComponent<MeshFilter>().sharedMesh;
			obj.sharedMaterial = world.dirt;
		}
		catch (Exception ex)
		{
			Debug.Log("Chunk cannot be rendered " + ex);
		}
		ClearTerrainMesh();
		updateTerrain = false;
		updatingTerrain = false;
		updateFlora = false;
		updatingFlora = false;
	}

	public void UpdateGrass()
	{
		MeshFilter component = grass.GetComponent<MeshFilter>();
		if (component != null)
		{
			meshGrass.Add(component.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component);
		UnityEngine.Object.DestroyImmediate(grass.GetComponent<MeshRenderer>());
		foreach (LotCreator lot in lots)
		{
			for (int i = 0; i < lot.grassPos.Count; i++)
			{
				BlockData ifNotNull = world.GetIfNotNull((int)lot.grassPos[i].x, (int)lot.grassPos[i].z);
				if (ifNotNull != null)
				{
					VegetationCreator.CreateGrassProp(lot.grassPos[i], grass, lot.lClass, ifNotNull, mb);
				}
			}
		}
		for (int j = 0; j < World.chunkSize; j++)
		{
			for (int k = 0; k < World.chunkSize; k++)
			{
				BuildGrass(k, j);
			}
		}
		CombineQuads(grass, grassMaterial);
		grass.transform.parent = world.Biosphere.transform;
		grass.layer = 11;
		ClearGrassMesh();
	}

	private GameObject makeCloseLeaves()
	{
		for (int i = 0; i < World.chunkSize; i++)
		{
			for (int j = 0; j < World.chunkSize; j++)
			{
				BuildCloseTrees(j, i, closeLeaves, isTrunk: false);
			}
		}
		foreach (LotCreator lot in lots)
		{
			for (int k = 0; k < lot.treesPos.Count; k++)
			{
				VegetationCreator.CreateClosePropTree(lot.treesPos[k], closeLeaves, isTrunk: false, mb, (int)lot.lClass, world.rZ);
			}
		}
		foreach (ServiceCreator service in services)
		{
			for (int l = 0; l < service.treesPos.Count; l++)
			{
				VegetationCreator.CreateClosePropTree(service.treesPos[l], closeLeaves, isTrunk: false, mb, 1, world.rZ);
			}
		}
		foreach (RoadCreator item in lavenues.Concat(lroads))
		{
			if (!(item.treePos == Vector3.zero))
			{
				VegetationCreator.CreateClosePropTree(item.treePos, closeLeaves, isTrunk: false, mb, 10, world.rZ);
			}
		}
		CombineQuads(closeLeaves, leafMaterial);
		closeLeaves.transform.parent = Leaves.transform;
		return closeLeaves;
	}

	private GameObject makeCloseTrunks()
	{
		for (int i = 0; i < World.chunkSize; i++)
		{
			for (int j = 0; j < World.chunkSize; j++)
			{
				BuildCloseTrees(j, i, closeTrunk, isTrunk: true);
			}
		}
		foreach (LotCreator lot in lots)
		{
			for (int k = 0; k < lot.treesPos.Count; k++)
			{
				VegetationCreator.CreateClosePropTree(lot.treesPos[k], closeTrunk, isTrunk: true, mb, (int)lot.lClass, world.rZ);
			}
		}
		foreach (ServiceCreator service in services)
		{
			for (int l = 0; l < service.treesPos.Count; l++)
			{
				VegetationCreator.CreateClosePropTree(service.treesPos[l], closeTrunk, isTrunk: true, mb, 1, world.rZ);
			}
		}
		foreach (RoadCreator item in lavenues.Concat(lroads))
		{
			if (!(item.treePos == Vector3.zero))
			{
				VegetationCreator.CreateClosePropTree(item.treePos, closeTrunk, isTrunk: true, mb, 10, world.rZ);
			}
		}
		CombineQuads(closeTrunk, trunkMaterial);
		closeTrunk.transform.parent = Trunks.transform;
		return closeTrunk;
	}

	private GameObject makeFarLeaves()
	{
		for (int i = 0; i < World.chunkSize; i++)
		{
			for (int j = 0; j < World.chunkSize; j++)
			{
				BuildFarTrees(j, i, farLeaves, isTrunk: false);
			}
		}
		foreach (LotCreator lot in lots)
		{
			for (int k = 0; k < lot.treesPos.Count; k++)
			{
				VegetationCreator.CreateFarPropTree(lot.treesPos[k], farLeaves, isTrunk: false, mb);
			}
		}
		foreach (ServiceCreator service in services)
		{
			for (int l = 0; l < service.treesPos.Count; l++)
			{
				VegetationCreator.CreateFarPropTree(service.treesPos[l], farLeaves, isTrunk: false, mb);
			}
		}
		foreach (RoadCreator item in lavenues.Concat(lroads))
		{
			if (!(item.treePos == Vector3.zero))
			{
				VegetationCreator.CreateFarPropTree(item.treePos, farLeaves, isTrunk: false, mb);
			}
		}
		CombineQuads(farLeaves, leafMaterial);
		farLeaves.transform.parent = Leaves.transform;
		return farLeaves;
	}

	private GameObject makeFarTrunks()
	{
		for (int i = 0; i < World.chunkSize; i++)
		{
			for (int j = 0; j < World.chunkSize; j++)
			{
				BuildFarTrees(j, i, farTrunk, isTrunk: true);
			}
		}
		foreach (LotCreator lot in lots)
		{
			for (int k = 0; k < lot.treesPos.Count; k++)
			{
				VegetationCreator.CreateFarPropTree(lot.treesPos[k], farTrunk, isTrunk: true, mb);
			}
		}
		foreach (ServiceCreator service in services)
		{
			for (int l = 0; l < service.treesPos.Count; l++)
			{
				VegetationCreator.CreateFarPropTree(service.treesPos[l], farTrunk, isTrunk: true, mb);
			}
		}
		foreach (RoadCreator item in lavenues.Concat(lroads))
		{
			if (!(item.treePos == Vector3.zero))
			{
				VegetationCreator.CreateFarPropTree(item.treePos, farTrunk, isTrunk: true, mb);
			}
		}
		CombineQuads(farTrunk, trunkMaterial);
		farTrunk.transform.parent = Trunks.transform;
		return farTrunk;
	}

	private void BuildCloseTrees(int x, int z, GameObject parent, bool isTrunk)
	{
		BlockData blockData = world.blockData[x + wPos.x, z + wPos.y];
		if (blockData != null && blockData.t == BlockData.Type.GRASSTREE && blockData != null)
		{
			VegetationCreator.CreateTree(new Vector3(x, blockData.h, z), parent, blockData, isTrunk, world.rZ);
		}
	}

	private void BuildFarTrees(int x, int z, GameObject parent, bool isTrunk)
	{
		BlockData blockData = world.blockData[x + wPos.x, z + wPos.y];
		if (blockData.t == BlockData.Type.GRASSTREE && blockData != null)
		{
			VegetationCreator.CreateFarTree(new Vector3(x, blockData.h, z), parent, blockData, isTrunk);
		}
	}

	private void BuildGrass(int x, int z)
	{
		BlockData blockData = world.blockData[x + wPos.x, z + wPos.y];
		if (blockData.g)
		{
			VegetationCreator.CreateGrass(new Vector3(x, blockData.h, z), grass, blockData);
		}
	}

	public void UpdateTrees()
	{
		MeshFilter component = Leaves.GetComponent<MeshFilter>();
		if (component != null)
		{
			meshTrees.Add(component.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component);
		UnityEngine.Object.DestroyImmediate(Leaves.GetComponent<MeshRenderer>());
		UnityEngine.Object.DestroyImmediate(Leaves.GetComponent<LODGroup>());
		MeshFilter component2 = Trunks.GetComponent<MeshFilter>();
		if (component2 != null)
		{
			meshTrees.Add(component2.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component2);
		UnityEngine.Object.DestroyImmediate(Trunks.GetComponent<MeshRenderer>());
		UnityEngine.Object.DestroyImmediate(Trunks.GetComponent<LODGroup>());
		MeshFilter component3 = closeLeaves.GetComponent<MeshFilter>();
		if (component3 != null)
		{
			meshTrees.Add(component3.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component3);
		UnityEngine.Object.DestroyImmediate(closeLeaves.GetComponent<MeshRenderer>());
		MeshFilter component4 = farLeaves.GetComponent<MeshFilter>();
		if (component4 != null)
		{
			meshTrees.Add(component4.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component4);
		UnityEngine.Object.DestroyImmediate(farLeaves.GetComponent<MeshRenderer>());
		MeshFilter component5 = closeTrunk.GetComponent<MeshFilter>();
		if (component5 != null)
		{
			meshTrees.Add(component5.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component5);
		UnityEngine.Object.DestroyImmediate(closeTrunk.GetComponent<MeshRenderer>());
		MeshFilter component6 = farTrunk.GetComponent<MeshFilter>();
		if (component6 != null)
		{
			meshTrees.Add(component6.sharedMesh);
		}
		UnityEngine.Object.DestroyImmediate(component6);
		UnityEngine.Object.DestroyImmediate(farTrunk.GetComponent<MeshRenderer>());
		leavesLOD = Leaves.AddComponent<LODGroup>();
		LOD[] array = new LOD[2];
		for (int i = 0; i < 2; i++)
		{
			GameObject gameObject = ((i != 0) ? makeFarLeaves() : makeCloseLeaves());
			gameObject.transform.parent = Leaves.transform;
			Renderer[] renderers = new Renderer[1] { gameObject.GetComponent<Renderer>() };
			if (i == 0)
			{
				array[0] = new LOD(0.35f / (float)(i + 1), renderers);
			}
			else
			{
				array[i] = new LOD(0.2f / (float)(i + 1), renderers);
			}
		}
		leavesLOD.SetLODs(array);
		leavesLOD.RecalculateBounds();
		Leaves.transform.parent = world.Biosphere.transform;
		trunkLOD = Trunks.AddComponent<LODGroup>();
		LOD[] array2 = new LOD[2];
		for (int j = 0; j < 2; j++)
		{
			GameObject gameObject2 = ((j != 0) ? makeFarTrunks() : makeCloseTrunks());
			gameObject2.transform.parent = Trunks.transform;
			Renderer[] renderers2 = new Renderer[1] { gameObject2.GetComponent<Renderer>() };
			if (j == 0)
			{
				array2[0] = new LOD(0.35f / (float)(j + 1), renderers2);
			}
			else
			{
				array2[j] = new LOD(0.2f / (float)(j + 1), renderers2);
			}
		}
		trunkLOD.SetLODs(array2);
		Trunks.transform.parent = world.Biosphere.transform;
		ClearTreeMesh();
	}

	public void UpdateProps()
	{
		MeshFilter component = props.GetComponent<MeshFilter>();
		if (component != null)
		{
			meshProps.Add(component.sharedMesh);
		}
		MeshRenderer component2 = props.GetComponent<MeshRenderer>();
		MeshFilter component3 = vehicles.GetComponent<MeshFilter>();
		if (component3 != null)
		{
			meshProps.Add(component3.sharedMesh);
		}
		MeshRenderer component4 = vehicles.GetComponent<MeshRenderer>();
		if (component != null)
		{
			UnityEngine.Object.DestroyImmediate(component);
			UnityEngine.Object.DestroyImmediate(component2);
		}
		if (component3 != null)
		{
			UnityEngine.Object.DestroyImmediate(component3);
			UnityEngine.Object.DestroyImmediate(component4);
		}
		foreach (LotCreator item in lots.ToList())
		{
			if (item == null)
			{
				lots.Remove(item);
			}
			foreach (Vector3 propsPo in item.propsPos)
			{
				BlockData blockData = world.blockData[(int)propsPo.x, (int)propsPo.z];
				if (!(blockData.pT == ""))
				{
					VegetationCreator.CreateProp(propsPo, props, blockData);
				}
			}
			foreach (Vector3 streetpropsPo in item.streetpropsPos)
			{
				BlockData blockData2 = world.blockData[(int)streetpropsPo.x, (int)streetpropsPo.z];
				if (!(blockData2.pT == ""))
				{
					VegetationCreator.CreateProp(streetpropsPo, props, blockData2);
				}
			}
			foreach (Vector3 vehiclesPo in item.vehiclesPos)
			{
				BlockData ifNotNull = world.GetIfNotNull((int)vehiclesPo.x, (int)vehiclesPo.z);
				if (ifNotNull != null)
				{
					VegetationCreator.CreateVehicleProp(vehiclesPo, vehicles, item, ifNotNull);
				}
			}
		}
		foreach (ServiceCreator service in services)
		{
			foreach (Vector3 vehiclesPo2 in service.vehiclesPos)
			{
				BlockData ifNotNull2 = world.GetIfNotNull((int)vehiclesPo2.x, (int)vehiclesPo2.z);
				if (ifNotNull2 != null)
				{
					VegetationCreator.CreateVehicleService(vehiclesPo2, vehicles, service, ifNotNull2);
				}
			}
			if (service.type == ServiceCreator.Type.Park)
			{
				BlockData blockData3 = world.blockData[(int)service.worldPos.x, (int)service.worldPos.z];
				if (!(blockData3.pT == ""))
				{
					VegetationCreator.CreatePark(new Vector3(blockData3.wP.x, blockData3.h + 0.502f, blockData3.wP.y), props, blockData3);
				}
			}
		}
		foreach (RoadCreator lroad in lroads)
		{
			if (lroad.rType == RoadCreator.RoadType.PATH)
			{
				if (lroad == null)
				{
					Debug.Log("Trying to rebuild inexistant road");
					continue;
				}
				GameObject gameObject = lroad.RebuildRoad();
				gameObject.transform.position = lroad.transform.position;
				gameObject.transform.parent = props.transform;
			}
		}
		CombineQuads(props, propMaterial);
		CombineQuads(vehicles, vehicleMaterial);
		ClearPropsMesh();
		updatingProps = false;
		updateProps = false;
	}

	public void UpdateRoads()
	{
		MeshFilter component = roads.GetComponent<MeshFilter>();
		MeshFilter component2 = avenues.GetComponent<MeshFilter>();
		MeshRenderer component3 = roads.GetComponent<MeshRenderer>();
		MeshRenderer component4 = avenues.GetComponent<MeshRenderer>();
		UnityEngine.Object.DestroyImmediate(roads.GetComponent<MeshCollider>());
		UnityEngine.Object.DestroyImmediate(avenues.GetComponent<MeshCollider>());
		if (component != null)
		{
			meshRoads.Add(component.sharedMesh);
			UnityEngine.Object.DestroyImmediate(component);
			UnityEngine.Object.DestroyImmediate(component3);
		}
		if (component2 != null)
		{
			meshRoads.Add(component2.sharedMesh);
			UnityEngine.Object.DestroyImmediate(component2);
			UnityEngine.Object.DestroyImmediate(component4);
		}
		foreach (RoadCreator lroad in lroads)
		{
			if (lroad == null)
			{
				Debug.Log("Trying to rebuild inexistant road");
			}
			else if (lroad.rType != RoadCreator.RoadType.PATH)
			{
				GameObject gameObject = lroad.RebuildRoad();
				if (gameObject == null)
				{
					Debug.Log("r is null");
					continue;
				}
				gameObject.transform.position = lroad.transform.position;
				gameObject.transform.parent = roads.transform;
			}
		}
		foreach (RoadCreator lavenue in lavenues)
		{
			if (lavenue == null)
			{
				Debug.Log("Trying to rebuild inexistant road");
				continue;
			}
			GameObject gameObject2 = lavenue.RebuildRoad();
			if (!(gameObject2 == null))
			{
				gameObject2.transform.position = lavenue.transform.position;
				gameObject2.transform.parent = avenues.transform;
			}
		}
		CombineQuads(roads, roadMaterial);
		MeshCollider obj = roads.AddComponent(typeof(MeshCollider)) as MeshCollider;
		obj.sharedMesh = roads.transform.GetComponent<MeshFilter>().sharedMesh;
		obj.sharedMaterial = world.asphalt;
		CombineQuads(avenues, avenueMaterial);
		MeshCollider obj2 = avenues.AddComponent(typeof(MeshCollider)) as MeshCollider;
		obj2.sharedMesh = avenues.transform.GetComponent<MeshFilter>().sharedMesh;
		obj2.sharedMaterial = world.asphalt;
		ClearRoadMesh();
		updatingRoads = false;
		expiredRoads = 0;
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
		meshFilter.mesh.CombineMeshes(array, mergeSubMeshes: true);
		(o.AddComponent(typeof(MeshRenderer)) as MeshRenderer).material = m;
		foreach (Transform item in o.transform)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private void CombineTerrainQuads(Material m)
	{
		MeshFilter[] componentsInChildren = chunk.GetComponentsInChildren<MeshFilter>();
		CombineInstance[] array = new CombineInstance[componentsInChildren.Length];
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].sharedMesh != null)
			{
				array[i].mesh = componentsInChildren[i].sharedMesh;
				array[i].transform = componentsInChildren[i].transform.localToWorldMatrix;
			}
		}
		MeshFilter meshFilter = (MeshFilter)chunk.AddComponent(typeof(MeshFilter));
		meshFilter.mesh = new Mesh();
		meshFilter.mesh.CombineMeshes(array, mergeSubMeshes: true);
		(chunk.AddComponent(typeof(MeshRenderer)) as MeshRenderer).material = m;
		foreach (Transform item in chunk.transform)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private void ClearTerrainMesh()
	{
		foreach (Mesh item in meshTerrain)
		{
			UnityEngine.Object.Destroy(item);
		}
		meshTerrain.Clear();
	}

	private void ClearPropsMesh()
	{
		foreach (Mesh meshProp in meshProps)
		{
			UnityEngine.Object.Destroy(meshProp);
		}
		meshProps.Clear();
	}

	private void ClearRoadMesh()
	{
		foreach (Mesh meshRoad in meshRoads)
		{
			UnityEngine.Object.Destroy(meshRoad);
		}
		meshRoads.Clear();
	}

	private void ClearTreeMesh()
	{
		foreach (Mesh meshTree in meshTrees)
		{
			UnityEngine.Object.Destroy(meshTree);
		}
		meshTrees.Clear();
	}

	private void ClearGrassMesh()
	{
		foreach (Mesh item in meshGrass)
		{
			UnityEngine.Object.Destroy(item);
		}
		meshGrass.Clear();
	}
}
