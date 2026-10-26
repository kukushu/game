using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockInteraction : MonoBehaviour
{
	public CostHelper costHelper;

	public GameObject One_one;

	public GameObject cursorManager;

	public GameObject serviceSelector;

	public GameObject lotSelector;

	public GameObject citizenSelector;

	public HashSet<Chunk> updates = new HashSet<Chunk>();

	public ServiceTool serviceTool;

	public SuperTool superTool;

	public Roading roading;

	public Railing railing;

	public Buldoze buldoze;

	public Farming farming;

	public World world;

	public Economy eco;

	public ReflectionProbe reflectionProbe;

	public int x1;

	public int z1;

	public int x2;

	public int z2;

	public int oldx1;

	public int oldz1;

	public int oldx2;

	public int oldz2;

	public Vector3 pos = Vector3.zero;

	public Vector3 mousePos = Vector3.zero;

	public int tamere;

	public Material Trees;

	private bool isTransparent = true;

	public static bool following;

	public GameObject objectToFollow;

	public LayerMask buildingLayer;

	public LayerMask carLayer;

	public SoundManager sm;

	private void Update()
	{
		int index = Indexer.Index;
		if (Input.GetMouseButtonDown(1))
		{
			One_one.transform.position = new Vector3(-1000f, 1000f, -1000f);
			following = false;
		}
		if ((index >= 20 && index <= 24) || index == 10 || index == 11 || index == 17 || index == 9 || index == 3 || index == 4)
		{
			if (!isTransparent)
			{
				Trees.SetFloat("_Cutoff", 0.85f);
				isTransparent = true;
			}
		}
		else if (isTransparent)
		{
			Trees.SetFloat("_Cutoff", 0.2f);
			isTransparent = false;
		}
		if (index > 0)
		{
			if (EventSystem.current.IsPointerOverGameObject())
			{
				return;
			}
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hitInfo, 400f, buildingLayer))
			{
				Vector3 vector = (mousePos = ((!Input.GetMouseButtonDown(0)) ? (hitInfo.point + hitInfo.normal / 2f) : (hitInfo.point - hitInfo.normal / 2f)));
				Vector2 zero = Vector2.zero;
				if (index == 10)
				{
					zero.x = -0.5f;
					zero.y = 0.5f;
				}
				BlockData bounded = world.GetBounded((int)(vector.x + 0.5f + zero.x), (int)(vector.z + 0.5f + zero.y));
				if (bounded == null)
				{
					pos = Vector3.zero;
					return;
				}
				pos = new Vector3(bounded.wP.x, bounded.h, bounded.wP.y);
				x2 = (int)pos.x;
				z2 = (int)pos.z;
				if (Input.GetMouseButtonDown(0))
				{
					if (index == 1 && !bounded.b && !bounded.r && !bounded.p && !bounded.a)
					{
						bounded.FlatHeight(bounded.h + 0.25f);
						if (bounded.h > 30f)
						{
							bounded.t = BlockData.Type.GRASS;
						}
						CheckChunks(bounded);
						sm.Play("Terra");
						costHelper.PayAmount(5);
						StartCoroutine(UpdateChunks());
					}
					if (index == 2 && !bounded.b && !bounded.r && !bounded.p && !bounded.a)
					{
						bounded.FlatHeight(bounded.h - 0.25f);
						if (bounded.h <= 30f)
						{
							bounded.t = BlockData.Type.SAND;
						}
						CheckChunks(bounded);
						sm.Play("Terra");
						costHelper.PayAmount(5);
						StartCoroutine(UpdateChunks());
					}
					if ((index == 9 && tamere == 0) || (index == 23 && tamere == 0) || (index == 3 && tamere == 0) || (index == 4 && tamere == 0) || (index == 55 && tamere == 0) || (index == 56 && tamere == 0) || (index == 26 && tamere == 0))
					{
						x1 = (int)pos.x;
						z1 = (int)pos.z;
						tamere = 3;
					}
					if (index >= 20 && index <= 24 && tamere == 0)
					{
						x1 = (int)pos.x;
						z1 = (int)pos.z;
						tamere = 2;
					}
				}
				if (Input.GetMouseButtonUp(0) && tamere == 3)
				{
					switch (index)
					{
					case 9:
						buldoze.BuldozeZone(x1, z1, x2, z2);
						tamere = 0;
						break;
					case 3:
					case 4:
					case 26:
						buldoze.FlattenZone(x1, z1, x2, z2);
						tamere = 0;
						break;
					case 55:
					case 56:
						buldoze.FlattenZone(x1, z1, x2, z2);
						tamere = 0;
						sm.Play("Tree");
						break;
					case 23:
						farming.FarmingUpdate(viewOnly: false);
						tamere = 0;
						break;
					}
				}
				if (index == 9 || index == 55 || index == 1 || index == 2 || index == 23 || index == 26 || index == 4 || index == 3)
				{
					float y = 0.515f;
					if (RTS_Camera.showingUnderground)
					{
						y = -0.98f;
					}
					Vector3 vector2 = new Vector3(0f, y, 0f);
					One_one.transform.position = pos + vector2;
				}
			}
		}
		else if (index == 0 && World.dataReady)
		{
			bool flag = false;
			if (EventSystem.current.IsPointerOverGameObject())
			{
				flag = true;
			}
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out var hitInfo2, 400f, buildingLayer) && !flag)
			{
				Vector3 vector3 = ((!Input.GetMouseButtonDown(0)) ? (hitInfo2.point + hitInfo2.normal / 2f) : (hitInfo2.point - hitInfo2.normal / 2f));
				BlockData bounded2 = world.GetBounded((int)(vector3.x + 0.5f), (int)(vector3.z + 0.5f));
				if (bounded2 == null)
				{
					return;
				}
				if (!serviceTool.isDisplayed)
				{
					ServiceSelection(bounded2);
				}
				if (!superTool.isDisplayed)
				{
					LotSelection(bounded2, clicked: false);
				}
				if (Input.GetMouseButtonDown(0))
				{
					if (bounded2.sO != null)
					{
						bool flag2 = true;
						if (bounded2.sO.type == ServiceCreator.Type.Airport)
						{
							sm.Play("Airport");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Fire)
						{
							sm.Play("Fire");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Government)
						{
							sm.Play("Gov");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Hospital)
						{
							sm.Play("Hospital");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Police)
						{
							sm.Play("Police");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Prison)
						{
							sm.Play("Prison");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Power)
						{
							sm.Play("Power");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Water)
						{
							sm.Play("Pumping");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Drone)
						{
							sm.Play("Future");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.Port)
						{
							sm.Play("Port");
						}
						else if (bounded2.sO.type == ServiceCreator.Type.School)
						{
							if (bounded2.sO.typeName == "S")
							{
								sm.Play("School");
							}
							else if (bounded2.sO.typeName == "B")
							{
								sm.Play("Library");
							}
							else
							{
								sm.Play("HighSchool");
							}
						}
						else
						{
							flag2 = false;
						}
						if (flag2)
						{
							serviceTool.OpenServiceEditor(bounded2.sO);
						}
					}
					if (bounded2.lO != null)
					{
						LotSelection(bounded2, clicked: true);
						sm.Play("ClickButtonB");
					}
				}
				if (Input.GetMouseButtonDown(1))
				{
					serviceTool.CloseServiceEditor();
					superTool.CloseSuperTool();
					following = false;
					Indexer.sliderOnScreen = false;
				}
				x2 = (int)vector3.x;
				z2 = (int)vector3.z;
			}
			if (Physics.Raycast(ray, out hitInfo2, 400f, carLayer) && hitInfo2.transform.gameObject != null && !flag)
			{
				GameObject gameObject = hitInfo2.transform.gameObject;
				if (gameObject.layer == 13)
				{
					citizenSelector.transform.position = gameObject.transform.position + Vector3.up;
					if (Input.GetMouseButtonDown(0))
					{
						objectToFollow = gameObject;
						following = true;
					}
				}
				else
				{
					citizenSelector.transform.position = new Vector3(-1000f, 1000f, -1000f);
				}
			}
			else
			{
				citizenSelector.transform.position = new Vector3(-1000f, 1000f, -1000f);
			}
		}
		if (following)
		{
			if (objectToFollow == null)
			{
				following = false;
				return;
			}
			world.RTScam.follower.transform.position = objectToFollow.transform.position;
			world.RTScam.LookAt = objectToFollow.transform.position;
		}
	}

	private void LotSelection(BlockData x, bool clicked)
	{
		if (x2 != oldx2 || z2 != oldz2 || superTool.isDisplayed)
		{
			if (x.lO != null)
			{
				oldx2 = x2;
				oldz2 = z2;
				lotSelector.transform.position = x.lO.Anchor + new Vector3(0f, 0.5f, 0f);
				ScaleLotSelector(x.lO);
			}
			else
			{
				lotSelector.transform.position = new Vector3(-1000f, 1000f, -1000f);
			}
		}
		if (clicked && x.lO != null)
		{
			superTool.OpenServiceEditor(x.lO);
		}
	}

	private void ServiceSelection(BlockData x)
	{
		if (x2 != oldx2 || z2 != oldz2)
		{
			if (x.sO != null && x.sO.type != ServiceCreator.Type.Park && x.sO.type != ServiceCreator.Type.TrainStation && x.sO.type != ServiceCreator.Type.Religion && x.sO.type != ServiceCreator.Type.Arcology)
			{
				oldx2 = x2;
				oldz2 = z2;
				serviceSelector.transform.position = x.sO.Anchor;
			}
			else
			{
				serviceSelector.transform.position = new Vector3(-1000f, 1000f, -1000f);
			}
		}
	}

	private void ScaleLotSelector(LotCreator lc)
	{
		if (!(lc == null))
		{
			BoxCollider collider = lc.GetCollider();
			float y = ((!(collider == null)) ? ((1f + collider.center.y) * 1.5f) : 2f);
			float x;
			float z;
			if (lc.lSize == LotCreator.LotSize._2x4)
			{
				x = 1.99f;
				z = 3.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._2x5)
			{
				x = 1.99f;
				z = 4.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._2x6)
			{
				x = 1.99f;
				z = 5.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._3x4)
			{
				x = 2.99f;
				z = 3.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._3x5)
			{
				x = 2.99f;
				z = 4.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._3x6)
			{
				x = 2.99f;
				z = 5.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._4x4)
			{
				x = 3.99f;
				z = 3.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._4x5)
			{
				x = 3.99f;
				z = 4.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._4x6)
			{
				x = 3.99f;
				z = 5.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._6x4)
			{
				x = 5.99f;
				z = 3.99f;
			}
			else if (lc.lSize == LotCreator.LotSize._6x5)
			{
				x = 5.99f;
				z = 4.99f;
			}
			else
			{
				x = 5.99f;
				z = 5.99f;
			}
			lotSelector.transform.localScale = new Vector3(x, y, z);
			lotSelector.transform.eulerAngles = new Vector3(0f, lc.Orientation + 90, 0f);
		}
	}

	public void CheckChunks(BlockData b)
	{
		updates.Add(b.o);
		if (b.pos[0] == 0)
		{
			BlockData ifNotNull = world.GetIfNotNull(b.wP.x - 1, b.wP.y);
			if (ifNotNull == null)
			{
				return;
			}
			updates.Add(ifNotNull.o);
		}
		else if (b.pos[0] == 15)
		{
			BlockData ifNotNull2 = world.GetIfNotNull(b.wP.x + 1, b.wP.y);
			if (ifNotNull2 == null)
			{
				return;
			}
			updates.Add(ifNotNull2.o);
		}
		if (b.pos[1] == 0)
		{
			BlockData ifNotNull3 = world.GetIfNotNull(b.wP.x, b.wP.y - 1);
			if (ifNotNull3 != null)
			{
				updates.Add(ifNotNull3.o);
			}
		}
		else if (b.pos[1] == 15)
		{
			BlockData ifNotNull4 = world.GetIfNotNull(b.wP.x, b.wP.y + 1);
			if (ifNotNull4 != null)
			{
				updates.Add(ifNotNull4.o);
			}
		}
	}

	public IEnumerator UpdateChunks()
	{
		foreach (Chunk c in updates.ToList())
		{
			if (c != null)
			{
				c.Redraw();
				yield return new WaitForEndOfFrame();
				c.UpdateRoads();
				yield return new WaitForEndOfFrame();
			}
		}
		updates.Clear();
	}
}
