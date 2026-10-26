using System.Collections.Generic;
using UnityEngine;

public class Farming : MonoBehaviour
{
	public BlockInteraction bi;

	public CostHelper costHelper;

	public SoundManager sm;

	public World w;

	public Zoning z;

	private Renderer r;

	public GameObject whiteSquare;

	public HashSet<Chunk> updates = new HashSet<Chunk>();

	private bool suitable;

	private int minX = 10;

	private int maxX = 10;

	private int minZ = 10;

	private int maxZ = 10;

	private int[] rotations = new int[4] { 0, 90, 180, 270 };

	private void Start()
	{
		r = whiteSquare.GetComponent<Renderer>();
	}

	private void Update()
	{
		if (Input.GetMouseButton(0) && bi.tamere == 3 && Indexer.Index == 23)
		{
			FarmingUpdate(viewOnly: true);
		}
		if (Input.GetKeyDown(KeyCode.Escape) && Indexer.Index == 23 && bi.tamere == 3)
		{
			StopVisualize();
			bi.tamere = 0;
			sm.Play("Impractical");
		}
	}

	public void FarmingUpdate(bool viewOnly)
	{
		int x = bi.x1;
		int z = bi.z1;
		int x2 = bi.x2;
		int z2 = bi.z2;
		int oldx = bi.oldx2;
		int oldz = bi.oldz2;
		int num;
		int num2;
		if (x >= x2)
		{
			num = x2;
			num2 = x;
		}
		else
		{
			num = x;
			num2 = x2;
		}
		int num3;
		int num4;
		if (z >= z2)
		{
			num3 = z2;
			num4 = z;
		}
		else
		{
			num3 = z;
			num4 = z2;
		}
		int num5 = Mathf.Abs(x - x2) + 1;
		int num6 = Mathf.Abs(z - z2) + 1;
		int num7 = 0;
		int num8 = 0;
		if (num5 >= num6)
		{
			if (x2 < x)
			{
				num7 = 3;
			}
		}
		else if (z2 < z)
		{
			num8 = 3;
		}
		if (x2 != oldx || z2 != oldz)
		{
			StopVisualize();
			suitable = true;
			r.material.color = Color.white;
			TooltipSystem.Hide();
			BlockData blockData = bi.world.blockData[x - 1, z];
			BlockData blockData2 = bi.world.blockData[x + 1, z];
			BlockData blockData3 = bi.world.blockData[x, z - 1];
			BlockData blockData4 = bi.world.blockData[x, z + 1];
			this.z._xConnected = false;
			this.z.x_Connected = false;
			this.z._zConnected = false;
			this.z.z_Connected = false;
			int num9 = 0;
			if (blockData.r)
			{
				this.z._xConnected = true;
				num9++;
			}
			if (blockData2.r)
			{
				this.z.x_Connected = true;
				num9++;
			}
			if (blockData3.r)
			{
				this.z._zConnected = true;
				num9++;
			}
			if (blockData4.r)
			{
				this.z.z_Connected = true;
				num9++;
			}
			if (num9 == 1)
			{
				if (this.z._xConnected || this.z.x_Connected)
				{
					minX = 5;
					maxX = 9;
					minZ = 10;
					maxZ = 24;
				}
				else if (this.z._zConnected || this.z.z_Connected)
				{
					minX = 10;
					maxX = 24;
					minZ = 5;
					maxZ = 9;
				}
			}
			else
			{
				minX = 5;
				maxX = 24;
				minZ = 5;
				maxZ = 24;
				if (this.z._xConnected && this.z._zConnected)
				{
					this.z._zConnected = false;
				}
				else if (this.z._xConnected && this.z.z_Connected)
				{
					if (num5 >= num6)
					{
						this.z._xConnected = false;
					}
					else
					{
						this.z.z_Connected = false;
					}
				}
				else if (this.z.x_Connected && this.z.z_Connected)
				{
					if (num5 >= num6)
					{
						this.z.x_Connected = false;
					}
					else
					{
						this.z.z_Connected = false;
					}
				}
				else if (this.z.x_Connected && this.z._zConnected)
				{
					if (num5 >= num6)
					{
						this.z.x_Connected = false;
					}
					else
					{
						this.z._zConnected = false;
					}
				}
			}
			if (num5 < minX || num6 < minZ)
			{
				TooltipSystem.Show("", "fieldSmall");
				r.material.color = Color.red;
				suitable = false;
			}
			if (num5 > maxX || num6 > maxZ)
			{
				TooltipSystem.Show("", "fieldBig");
				r.material.color = Color.red;
				suitable = false;
			}
			costHelper.SetCost(num5 * 2 + num6 * 2);
			if (viewOnly)
			{
				for (int i = num; i <= num2; i++)
				{
					for (int j = num3; j <= num4; j++)
					{
						Visualize(new Vector2Int(i, j));
					}
				}
			}
			else
			{
				r.material.color = Color.white;
				int num10 = 0;
				for (int k = num; k <= num2; k++)
				{
					for (int l = num3; l <= num4; l++)
					{
						BlockData bounded = w.GetBounded(k, l);
						num10++;
						if (bounded.b || bounded.h < 30f)
						{
							suitable = false;
						}
					}
				}
				if (!suitable)
				{
					TooltipSystem.Hide();
					return;
				}
				if (num9 == 0)
				{
					suitable = false;
					TooltipSystem.Hide();
					return;
				}
				this.z.PreCreateLot(x - num7, z - num8, 4, 4, permanent: true, 1f, num10);
				float value = Random.value;
				sbyte farmStyle;
				if (value > 0.75f)
				{
					farmStyle = -1;
				}
				else if (value > 0.5f)
				{
					farmStyle = -2;
				}
				else
				{
					farmStyle = (sbyte)((!(value > 0.25f)) ? (-4) : (-3));
				}
				for (int m = num; m <= num2; m++)
				{
					for (int n = num3; n <= num4; n++)
					{
						TurnTileToFarm(new Vector2Int(m, n), farmStyle);
					}
				}
				StopVisualize();
			}
		}
		if (!viewOnly)
		{
			StopVisualize();
			sm.Play("LotBuilt");
			costHelper.PayForIt();
			StartCoroutine(bi.UpdateChunks());
		}
	}

	private void StopVisualize()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("NewCube");
		foreach (GameObject gameObject in array)
		{
			if (gameObject.name == "DestroyCube(Clone)" || gameObject.name == "NewCube(Clone)" || gameObject.name == "SquareRed1x(Clone)")
			{
				Object.Destroy(gameObject);
			}
			if (gameObject.name == "FriendlyCube(Clone)")
			{
				Object.Destroy(gameObject);
			}
		}
		bi.One_one.transform.position = new Vector3(-1000f, 1000f, -1000f);
	}

	private void Visualize(Vector2Int pos)
	{
		BlockData blockData = w.blockData[pos.x, pos.y];
		Vector3 position = new Vector3(blockData.wP.x, blockData.h + 0.515f, blockData.wP.y);
		if (blockData.b || blockData.h < 30f)
		{
			suitable = false;
			r.material.color = Color.red;
		}
		Object.Instantiate(whiteSquare, position, Quaternion.identity);
		whiteSquare.name = "NewCube";
		whiteSquare.tag = "NewCube";
		sm.Play("RoadPiece");
	}

	private void TurnTileToFarm(Vector2Int pos, sbyte farmStyle)
	{
		BlockData blockData = w.blockData[pos.x, pos.y];
		if (!blockData.b)
		{
			blockData.b = true;
			blockData.g = true;
			blockData.gT = farmStyle;
			blockData.t = BlockData.Type.GRASS;
			bi.CheckChunks(blockData);
		}
	}
}
