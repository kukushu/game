using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/AI/RCC AI Waypoints Container")]
public class RCC_AIWaypointsContainer : MonoBehaviour
{
	public List<RCC_Waypoint> waypoints = new List<RCC_Waypoint>();

	private void OnDrawGizmos()
	{
		if (waypoints == null)
		{
			return;
		}
		for (int i = 0; i < waypoints.Count && !(waypoints[i] == null); i++)
		{
			Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
			Gizmos.DrawSphere(waypoints[i].transform.position, 2f);
			Gizmos.DrawWireSphere(waypoints[i].transform.position, 20f);
			if (i < waypoints.Count - 1 && (bool)waypoints[i] && (bool)waypoints[i + 1] && waypoints.Count > 0)
			{
				Gizmos.color = Color.green;
				if (i < waypoints.Count - 1)
				{
					Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
				}
				if (i < waypoints.Count - 2)
				{
					Gizmos.DrawLine(waypoints[waypoints.Count - 1].transform.position, waypoints[0].transform.position);
				}
			}
		}
	}
}
