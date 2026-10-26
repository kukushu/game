using System;
using UnityEngine;

public class CheckpointController : MonoBehaviour
{
	public Checkpoint[] CheckpointsList;

	public LookAtTargetController Arrow;

	private Checkpoint CurrentCheckpoint;

	private int CheckpointId;

	private void Start()
	{
		if (CheckpointsList.Length != 0)
		{
			for (int i = 0; i < CheckpointsList.Length; i++)
			{
				CheckpointsList[i].gameObject.SetActive(value: false);
			}
			CheckpointId = 0;
			SetCurrentCheckpoint(CheckpointsList[CheckpointId]);
		}
	}

	private void SetCurrentCheckpoint(Checkpoint checkpoint)
	{
		if (CurrentCheckpoint != null)
		{
			CurrentCheckpoint.gameObject.SetActive(value: false);
			Checkpoint currentCheckpoint = CurrentCheckpoint;
			currentCheckpoint.CheckpointActivated = (Action)Delegate.Remove(currentCheckpoint.CheckpointActivated, new Action(CheckpointActivated));
		}
		CurrentCheckpoint = checkpoint;
		Checkpoint currentCheckpoint2 = CurrentCheckpoint;
		currentCheckpoint2.CheckpointActivated = (Action)Delegate.Combine(currentCheckpoint2.CheckpointActivated, new Action(CheckpointActivated));
		Arrow.Target = CurrentCheckpoint.transform;
		CurrentCheckpoint.gameObject.SetActive(value: true);
	}

	private void CheckpointActivated()
	{
		CheckpointId++;
		if (CheckpointId >= CheckpointsList.Length)
		{
			CurrentCheckpoint.gameObject.SetActive(value: false);
			Checkpoint currentCheckpoint = CurrentCheckpoint;
			currentCheckpoint.CheckpointActivated = (Action)Delegate.Remove(currentCheckpoint.CheckpointActivated, new Action(CheckpointActivated));
			Arrow.gameObject.SetActive(value: false);
		}
		else
		{
			SetCurrentCheckpoint(CheckpointsList[CheckpointId]);
		}
	}

	private void Update()
	{
	}
}
