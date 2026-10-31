using System;using System.Collections.Generic;
namespace HarborCity.Diagnostics {
 // Derived timer primitive: stable ordinal keys, one pending wake per entity.
 // No business state, actor IDs, events, delegates, or save protocol lives here.
 internal sealed class CityWakeQueue {
  readonly long[] due;readonly int[] heap,positions;int count;
  internal long ScheduleCalls,Changes,Cancellations,Wakes;
  internal int Count=>count;
  internal long LogicalBytes=>due.LongLength*8+heap.LongLength*4+positions.LongLength*4;
  internal CityWakeQueue(int capacity){if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));due=new long[capacity];heap=new int[capacity];positions=new int[capacity];for(int i=0;i<capacity;i++)positions[i]=-1;}
  void Check(int ordinal){if((uint)ordinal>=(uint)due.Length)throw new ArgumentOutOfRangeException(nameof(ordinal));}
  bool Less(int a,int b)=>due[a]<due[b]||due[a]==due[b]&&a<b;
  void Swap(int a,int b){int x=heap[a];heap[a]=heap[b];heap[b]=x;positions[heap[a]]=a;positions[heap[b]]=b;}
  int Up(int at){while(at>0){int parent=(at-1)/2;if(!Less(heap[at],heap[parent]))break;Swap(at,parent);at=parent;}return at;}
  void Down(int at){while(true){int left=at*2+1;if(left>=count)return;int right=left+1,best=right<count&&Less(heap[right],heap[left])?right:left;if(!Less(heap[best],heap[at]))return;Swap(at,best);at=best;}}
  internal void Schedule(int ordinal,long tick){Check(ordinal);if(tick<0)throw new ArgumentOutOfRangeException(nameof(tick));ScheduleCalls++;int at=positions[ordinal];if(at>=0&&due[ordinal]==tick)return;Changes++;due[ordinal]=tick;if(at<0){at=count++;heap[at]=ordinal;positions[ordinal]=at;}Down(Up(at));}
  internal bool Cancel(int ordinal){Check(ordinal);int at=positions[ordinal];if(at<0)return false;Cancellations++;RemoveAt(at);return true;}
  void RemoveAt(int at){int removed=heap[at];positions[removed]=-1;count--;if(at==count)return;heap[at]=heap[count];positions[heap[at]]=at;Down(Up(at));}
  internal void DrainDue(long tick,List<int> ready){if(ready==null)throw new ArgumentNullException(nameof(ready));ready.Clear();while(count>0&&due[heap[0]]<=tick){ready.Add(heap[0]);RemoveAt(0);Wakes++;}ready.Sort();} // Current Members order, not due/actor-ID order.
  internal static long FirstTick(double deadline,long minimum,double stepSeconds){const long max=1000000000000L;if(minimum<0||minimum>max||double.IsNaN(deadline)||double.IsInfinity(deadline)||stepSeconds<=0||double.IsNaN(stepSeconds)||double.IsInfinity(stepSeconds))throw new ArgumentOutOfRangeException();if(deadline<=minimum*stepSeconds)return minimum;if(deadline>max*stepSeconds)return long.MaxValue;long tick=Math.Max(minimum,(long)Math.Ceiling(deadline/stepSeconds));while(tick>minimum&&(tick-1)*stepSeconds>=deadline)tick--;while(tick<=max&&tick*stepSeconds<deadline)tick++;return tick>max?long.MaxValue:tick;}
 }
}
