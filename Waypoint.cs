using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
	public Transform next;

	public bool free = true;

	public List<VehicleWP> occupers = new List<VehicleWP>();

	public List<TrainWP> trainsOccupers = new List<TrainWP>();

	public Transform treePos;
}
