using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineQueue
{
	private readonly uint maxActive;

	private readonly Func<IEnumerator, Coroutine> coroutineStarter;

	private readonly Queue<IEnumerator> queue;

	public uint numActive;

	public CoroutineQueue(uint maxActive, Func<IEnumerator, Coroutine> coroutineStarter)
	{
		if (maxActive == 0)
		{
			throw new ArgumentException("Must be at least one", "maxActive");
		}
		this.maxActive = maxActive;
		this.coroutineStarter = coroutineStarter;
		queue = new Queue<IEnumerator>();
	}

	public void Run(IEnumerator coroutine)
	{
		if (numActive < maxActive)
		{
			IEnumerator arg = CoroutineRunner(coroutine);
			coroutineStarter(arg);
		}
		else
		{
			queue.Enqueue(coroutine);
		}
	}

	private IEnumerator CoroutineRunner(IEnumerator coroutine)
	{
		numActive++;
		while (coroutine.MoveNext())
		{
			yield return coroutine.Current;
		}
		numActive--;
		if (queue.Count > 0)
		{
			IEnumerator coroutine2 = queue.Dequeue();
			Run(coroutine2);
		}
	}
}
