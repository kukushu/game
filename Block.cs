using UnityEngine;

public static class Block
{
	private enum Cubeside
	{
		TOP,
		LEFT,
		RIGHT,
		FRONT,
		BACK
	}

	public static void Create(BlockData.Type b, byte[] cPos, float rPos, GameObject p, Chunk o, Vector2Int wPos, bool _isGrass, World w, byte bm)
	{
		BlockData blockData = w.blockData[wPos.x, wPos.y];
		if (blockData == null)
		{
			blockData = new BlockData();
			w.blockData[wPos.x, wPos.y] = blockData;
		}
		blockData.t = b;
		blockData.o = o;
		blockData.pos[0] = cPos[0];
		blockData.pos[1] = cPos[1];
		blockData.h = rPos;
		blockData.wP = wPos;
		blockData.bio = bm;
		blockData.g = _isGrass;
		blockData.s1 = (byte)Random.Range(0, 100);
		blockData.s2 = (byte)Random.Range(0, 100);
	}

	private static void CreateQuad(Cubeside side, bool _isCliff, BlockData bd, float border, World w)
	{
		Vector3[] vertices = new Vector3[4];
		Vector3[] normals = new Vector3[4];
		bool flag = false;
		Vector2 vector;
		Vector2 vector2;
		Vector2 vector3;
		Vector2 vector4;
		if (side == Cubeside.TOP)
		{
			if ((bd.b && bd.h <= 30f) || bd.t == BlockData.Type.SAND)
			{
				vector = UVs.blockUVsCity[3, 0];
				vector2 = UVs.blockUVsCity[3, 1];
				vector3 = UVs.blockUVsCity[3, 2];
				vector4 = UVs.blockUVsCity[3, 3];
			}
			else
			{
				int num = UVs.numbers[Mathf.RoundToInt((float)(int)bd.s1 * 0.1f)];
				vector = UVs.blockUVsCity[num, 0];
				vector2 = UVs.blockUVsCity[num, 1];
				vector3 = UVs.blockUVsCity[num, 2];
				vector4 = UVs.blockUVsCity[num, 3];
			}
		}
		else if (_isCliff || bd.b)
		{
			vector = UVs.blockUVsCity[1, 0];
			vector2 = UVs.blockUVsCity[1, 1];
			vector3 = UVs.blockUVsCity[1, 2];
			vector4 = UVs.blockUVsCity[1, 3];
		}
		else if (bd.t != BlockData.Type.SAND)
		{
			vector = UVs.blockUVsCity[7, 0];
			vector2 = UVs.blockUVsCity[7, 1];
			vector3 = UVs.blockUVsCity[7, 2];
			vector4 = UVs.blockUVsCity[7, 3];
		}
		else
		{
			vector = UVs.blockUVsCity[8, 0];
			vector2 = UVs.blockUVsCity[8, 1];
			vector3 = UVs.blockUVsCity[8, 2];
			vector4 = UVs.blockUVsCity[8, 3];
		}
		if (!bd.b && bd.h < (float)SettingsStatic.smoothLevel + 0.25f)
		{
			float h = w.GetBounded(bd.wP.x + 1, bd.wP.y).h;
			float h2 = w.GetBounded(bd.wP.x, bd.wP.y + 1).h;
			float h3 = w.GetBounded(bd.wP.x - 1, bd.wP.y).h;
			float h4 = w.GetBounded(bd.wP.x, bd.wP.y - 1).h;
			if (h != h4 || h2 != h3)
			{
				flag = true;
			}
			switch (side)
			{
			case Cubeside.TOP:
				vertices = ((!bd.b) ? new Vector3[4]
				{
					new Vector3(0.5f, GetHeight(bd, w, 6), -0.5f),
					new Vector3(0.5f, GetHeight(bd, w, 5), 0.5f),
					new Vector3(-0.5f, GetHeight(bd, w, 4), 0.5f),
					new Vector3(-0.5f, GetHeight(bd, w, 7), -0.5f)
				} : new Vector3[4]
				{
					UVs.p6,
					UVs.p5,
					UVs.p4,
					UVs.p7
				});
				normals = new Vector3[4]
				{
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.up
				};
				break;
			case Cubeside.LEFT:
				vertices = new Vector3[4]
				{
					new Vector3(-0.5f, GetHeight(bd, w, 7), -0.5f),
					new Vector3(-0.5f, GetHeight(bd, w, 4), 0.5f),
					new Vector3(UVs.p0.x, UVs.p0.y - border, UVs.p0.z),
					new Vector3(UVs.p3.x, UVs.p3.y - border, UVs.p3.z)
				};
				normals = new Vector3[4]
				{
					Vector3.left,
					Vector3.left,
					Vector3.left,
					Vector3.left
				};
				break;
			case Cubeside.RIGHT:
				vertices = new Vector3[4]
				{
					new Vector3(0.5f, GetHeight(bd, w, 5), 0.5f),
					new Vector3(0.5f, GetHeight(bd, w, 6), -0.5f),
					new Vector3(UVs.p2.x, UVs.p2.y - border, UVs.p2.z),
					new Vector3(UVs.p1.x, UVs.p1.y - border, UVs.p1.z)
				};
				normals = new Vector3[4]
				{
					Vector3.right,
					Vector3.right,
					Vector3.right,
					Vector3.right
				};
				break;
			case Cubeside.FRONT:
				vertices = new Vector3[4]
				{
					new Vector3(-0.5f, GetHeight(bd, w, 4), 0.5f),
					new Vector3(0.5f, GetHeight(bd, w, 5), 0.5f),
					new Vector3(UVs.p1.x, UVs.p1.y - border, UVs.p1.z),
					new Vector3(UVs.p0.x, UVs.p0.y - border, UVs.p0.z)
				};
				normals = new Vector3[4]
				{
					Vector3.forward,
					Vector3.forward,
					Vector3.forward,
					Vector3.forward
				};
				break;
			case Cubeside.BACK:
				vertices = new Vector3[4]
				{
					new Vector3(0.5f, GetHeight(bd, w, 6), -0.5f),
					new Vector3(-0.5f, GetHeight(bd, w, 7), -0.5f),
					new Vector3(UVs.p3.x, UVs.p3.y - border, UVs.p3.z),
					new Vector3(UVs.p2.x, UVs.p2.y - border, UVs.p2.z)
				};
				normals = new Vector3[4]
				{
					Vector3.back,
					Vector3.back,
					Vector3.back,
					Vector3.back
				};
				break;
			}
		}
		else
		{
			switch (side)
			{
			case Cubeside.TOP:
				vertices = UVs.vTop;
				normals = UVs.nTop;
				break;
			case Cubeside.LEFT:
				vertices = UVs.vLeft;
				if (border != 0f)
				{
					vertices = new Vector3[4]
					{
						UVs.p7,
						UVs.p4,
						new Vector3(UVs.p0.x, UVs.p0.y - border, UVs.p0.z),
						new Vector3(UVs.p3.x, UVs.p3.y - border, UVs.p3.z)
					};
				}
				normals = UVs.nLeft;
				break;
			case Cubeside.RIGHT:
				vertices = UVs.vRight;
				if (border != 0f)
				{
					vertices = new Vector3[4]
					{
						UVs.p5,
						UVs.p6,
						new Vector3(UVs.p2.x, UVs.p2.y - border, UVs.p2.z),
						new Vector3(UVs.p1.x, UVs.p1.y - border, UVs.p1.z)
					};
				}
				normals = UVs.nRight;
				break;
			case Cubeside.FRONT:
				vertices = UVs.vFront;
				if (border != 0f)
				{
					vertices = new Vector3[4]
					{
						UVs.p4,
						UVs.p5,
						new Vector3(UVs.p1.x, UVs.p1.y - border, UVs.p1.z),
						new Vector3(UVs.p0.x, UVs.p0.y - border, UVs.p0.z)
					};
				}
				normals = UVs.nFront;
				break;
			case Cubeside.BACK:
				vertices = UVs.vBack;
				if (border != 0f)
				{
					vertices = new Vector3[4]
					{
						UVs.p6,
						UVs.p7,
						new Vector3(UVs.p3.x, UVs.p3.y - border, UVs.p3.z),
						new Vector3(UVs.p2.x, UVs.p2.y - border, UVs.p2.z)
					};
				}
				normals = UVs.nBack;
				break;
			}
		}
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = new Vector2[4] { vector4, vector3, vector2, vector };
		if (flag)
		{
			mesh.triangles = UVs.invertTriangles;
		}
		else
		{
			mesh.triangles = UVs.triangles;
		}
		if ((bd.t != BlockData.Type.SAND && !_isCliff) || (bd.b && bd.h > 30f))
		{
			int num2 = 0;
			TerrainColor.ColorWizardry(w.rZ * (World.chunkSize * World.worldSize) + bd.wP.y);
			if (bd.lO != null)
			{
				int lClass = (int)bd.lO.lClass;
				if (bd.lO.Stage == 0 || lClass == 7)
				{
					num2 = 4;
				}
				else
				{
					switch (lClass)
					{
					case 2:
					case 4:
					case 6:
						num2 = 3;
						break;
					case 1:
					case 3:
						num2 = 2;
						break;
					default:
						num2 = 1;
						break;
					}
				}
			}
			else if (bd.b && bd.h > 30f)
			{
				num2 = 1;
			}
			if (!bd.b || (num2 == 0 && bd.h > 30f))
			{
				mesh.colors32 = TerrainColor.GrassColor(bd.h + (float)bd.v[1] / 80f, mesh, 0, (float)(int)bd.bio / 100f);
			}
			else if (side == Cubeside.TOP && bd.h > 30f)
			{
				mesh.colors32 = TerrainColor.GrassColor(bd.h + (float)bd.v[1] / 80f, mesh, num2, (float)(int)bd.bio / 100f);
			}
			else if (num2 == 7)
			{
				mesh.colors32 = TerrainColor.GrassColor(bd.h + (float)bd.v[1] / 80f, mesh, num2, (float)(int)bd.bio / 100f);
			}
			else if (bd.b && bd.h <= 30f)
			{
				mesh.colors32 = TerrainColor.SandColor(bd.h, mesh);
			}
		}
		else if (bd.t == BlockData.Type.SAND || (bd.b && bd.h <= 30f))
		{
			mesh.colors32 = TerrainColor.SandColor(bd.h, mesh);
		}
		GameObject gameObject = new GameObject("Quad");
		gameObject.transform.position = new Vector3((int)bd.pos[0], bd.h, (int)bd.pos[1]);
		gameObject.transform.parent = bd.o.chunk.transform;
		((MeshFilter)gameObject.AddComponent(typeof(MeshFilter))).mesh = mesh;
		bd.o.meshTerrain.Add(mesh);
	}

	public static void Draw(BlockData bd, World W)
	{
		BlockData ifNotNull = W.GetIfNotNull(bd.wP.x, bd.wP.y + 1);
		BlockData ifNotNull2 = W.GetIfNotNull(bd.wP.x, bd.wP.y - 1);
		BlockData ifNotNull3 = W.GetIfNotNull(bd.wP.x + 1, bd.wP.y);
		BlockData ifNotNull4 = W.GetIfNotNull(bd.wP.x - 1, bd.wP.y);
		if (bd.h < (float)SettingsStatic.smoothLevel + 0.25f)
		{
			CreateQuad(Cubeside.TOP, _isCliff: false, bd, 0f, W);
			if (ifNotNull != null)
			{
				if (ifNotNull.h < bd.h)
				{
					if (ifNotNull.b)
					{
						if (ifNotNull.h <= bd.h - 0.25f)
						{
							CreateQuad(Cubeside.FRONT, _isCliff: false, bd, 0f, W);
						}
						else
						{
							CreateQuad(Cubeside.FRONT, _isCliff: true, bd, 0f, W);
						}
					}
				}
				else if (!bd.b && ifNotNull.b)
				{
					CreateQuad(Cubeside.FRONT, _isCliff: false, bd, 0f, W);
				}
			}
			else
			{
				CreateQuad(Cubeside.FRONT, _isCliff: true, bd, bd.h - 28f, W);
			}
			if (ifNotNull2 != null)
			{
				if (ifNotNull2.h < bd.h)
				{
					if (ifNotNull2.b)
					{
						if (ifNotNull2.h <= bd.h - 0.25f)
						{
							CreateQuad(Cubeside.BACK, _isCliff: false, bd, 0f, W);
						}
						else
						{
							CreateQuad(Cubeside.BACK, _isCliff: true, bd, 0f, W);
						}
					}
				}
				else if (!bd.b && ifNotNull2.b)
				{
					CreateQuad(Cubeside.BACK, _isCliff: false, bd, 0f, W);
				}
			}
			else
			{
				CreateQuad(Cubeside.BACK, _isCliff: true, bd, bd.h - 28f, W);
			}
			if (ifNotNull4 != null)
			{
				if (ifNotNull4.h < bd.h)
				{
					if (ifNotNull4.b)
					{
						if (ifNotNull4.h <= bd.h - 0.25f)
						{
							CreateQuad(Cubeside.LEFT, _isCliff: false, bd, 0f, W);
						}
						else
						{
							CreateQuad(Cubeside.LEFT, _isCliff: true, bd, 0f, W);
						}
					}
				}
				else if (!bd.b && ifNotNull4.b)
				{
					CreateQuad(Cubeside.LEFT, _isCliff: false, bd, 0f, W);
				}
			}
			else
			{
				CreateQuad(Cubeside.LEFT, _isCliff: true, bd, bd.h - 28f, W);
			}
			if (ifNotNull3 != null)
			{
				if (ifNotNull3.h < bd.h)
				{
					if (ifNotNull3.b)
					{
						if (ifNotNull3.h <= bd.h - 0.25f)
						{
							CreateQuad(Cubeside.RIGHT, _isCliff: false, bd, 0f, W);
						}
						else
						{
							CreateQuad(Cubeside.RIGHT, _isCliff: true, bd, 0f, W);
						}
					}
				}
				else if (!bd.b && ifNotNull3.b)
				{
					CreateQuad(Cubeside.RIGHT, _isCliff: false, bd, 0f, W);
				}
			}
			else
			{
				CreateQuad(Cubeside.RIGHT, _isCliff: true, bd, bd.h - 28f, W);
			}
			return;
		}
		CreateQuad(Cubeside.TOP, _isCliff: false, bd, 0f, W);
		if (ifNotNull != null)
		{
			if (ifNotNull.h < bd.h)
			{
				if (ifNotNull.h <= bd.h - 0.75f)
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 180);
				}
				else
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 180, colorize: true);
				}
			}
		}
		else
		{
			CreateQuad(Cubeside.FRONT, _isCliff: true, bd, bd.h - 28f, W);
		}
		if (ifNotNull2 != null)
		{
			if (ifNotNull2.h < bd.h)
			{
				if (ifNotNull2.h <= bd.h - 0.75f)
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 0);
				}
				else
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 0, colorize: true);
				}
			}
		}
		else
		{
			CreateQuad(Cubeside.BACK, _isCliff: true, bd, bd.h - 28f, W);
		}
		if (ifNotNull4 != null)
		{
			if (ifNotNull4.h < bd.h)
			{
				if (ifNotNull4.h <= bd.h - 0.75f)
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 90);
				}
				else
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 90, colorize: true);
				}
			}
		}
		else
		{
			CreateQuad(Cubeside.LEFT, _isCliff: true, bd, bd.h - 28f, W);
		}
		if (ifNotNull3 != null)
		{
			if (ifNotNull3.h < bd.h)
			{
				if (ifNotNull3.h <= bd.h - 0.75f)
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 270);
				}
				else
				{
					VegetationCreator.CreateCliff(bd.o.chunk, bd, 270, colorize: true);
				}
			}
		}
		else
		{
			CreateQuad(Cubeside.RIGHT, _isCliff: true, bd, bd.h - 28f, W);
		}
	}

	private static float GetHeight(BlockData bd, World w, int corner)
	{
		float num = (float)SettingsStatic.smoothLevel + 0.25f;
		float h = bd.h;
		float num2 = h;
		float h2;
		float h3;
		float h4;
		switch (corner)
		{
		case 4:
			h2 = w.GetBounded(bd.wP.x - 1, bd.wP.y).h;
			h3 = w.GetBounded(bd.wP.x - 1, bd.wP.y + 1).h;
			h4 = w.GetBounded(bd.wP.x, bd.wP.y + 1).h;
			break;
		case 5:
			h2 = w.GetBounded(bd.wP.x, bd.wP.y + 1).h;
			h3 = w.GetBounded(bd.wP.x + 1, bd.wP.y + 1).h;
			h4 = w.GetBounded(bd.wP.x + 1, bd.wP.y).h;
			break;
		case 6:
			h2 = w.GetBounded(bd.wP.x + 1, bd.wP.y).h;
			h3 = w.GetBounded(bd.wP.x + 1, bd.wP.y - 1).h;
			h4 = w.GetBounded(bd.wP.x, bd.wP.y - 1).h;
			break;
		default:
			h2 = w.GetBounded(bd.wP.x - 1, bd.wP.y).h;
			h3 = w.GetBounded(bd.wP.x - 1, bd.wP.y - 1).h;
			h4 = w.GetBounded(bd.wP.x, bd.wP.y - 1).h;
			break;
		}
		if (h2 > num2)
		{
			num2 = h2;
		}
		if (h3 > num2)
		{
			num2 = h3;
		}
		if (h4 > num2)
		{
			num2 = h4;
		}
		if (num > 31f)
		{
			return num2 - h + 0.5f;
		}
		if (num2 > num)
		{
			return num - h + 0.5f;
		}
		return num2 - h + 0.5f;
	}
}
