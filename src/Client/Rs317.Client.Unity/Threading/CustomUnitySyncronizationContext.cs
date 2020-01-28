// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine.Scripting;

//Based on: https://github.com/Unity-Technologies/UnityCsReference/blob/32bd3a1c008265df4cd53b556446fab60f964834/Runtime/Export/Scripting/UnitySynchronizationContext.cs
namespace Rs317.Sharp
{
	internal sealed class CustomUnitySynchronizationContext : SynchronizationContext
	{
		private const int kAwqInitialCapacity = 20;
		private readonly List<WorkRequest> m_AsyncWorkQueue;
		private readonly List<WorkRequest> m_CurrentFrameWork = new List<WorkRequest>(kAwqInitialCapacity);
		private readonly int m_MainThreadID;
		private int m_TrackedCount = 0;

		private CustomUnitySynchronizationContext(int mainThreadID)
		{
			m_AsyncWorkQueue = new List<WorkRequest>(kAwqInitialCapacity);
			m_MainThreadID = mainThreadID;
		}

		private CustomUnitySynchronizationContext(List<WorkRequest> queue, int mainThreadID)
		{
			m_AsyncWorkQueue = queue;
			m_MainThreadID = mainThreadID;
		}

		// Send will process the call synchronously. If the call is processed on the main thread, we'll invoke it
		// directly here. If the call is processed on another thread it will be queued up like POST to be executed
		// on the main thread and it will wait. Once the main thread processes the work we can continue
		public override void Send(SendOrPostCallback callback, object state)
		{
			if(m_MainThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId)
			{
				callback(state);
			}
			else
			{
				//For WebGL this is ok, since the above is always true. We'll never be waiting.
				using(var waitHandle = new ManualResetEvent(false))
				{
					lock(m_AsyncWorkQueue)
					{
						m_AsyncWorkQueue.Add(new WorkRequest(callback, state, waitHandle));
					}
					waitHandle.WaitOne();
				}
			}
		}

		public override void OperationStarted() { Interlocked.Increment(ref m_TrackedCount); }
		public override void OperationCompleted() { Interlocked.Decrement(ref m_TrackedCount); }

		// Post will add the call to a task list to be executed later on the main thread then work will continue asynchronously
		public override void Post(SendOrPostCallback callback, object state)
		{
			lock(m_AsyncWorkQueue)
			{
				m_AsyncWorkQueue.Add(new WorkRequest(callback, state));
			}
		}

		// CreateCopy returns a new UnitySynchronizationContext object, but the queue is still shared with the original
		public override SynchronizationContext CreateCopy()
		{
			lock (m_AsyncWorkQueue)
			{
				return new CustomUnitySynchronizationContext(m_AsyncWorkQueue, m_MainThreadID);
			}
		}

		// Exec will execute tasks off the task list
		private void Exec()
		{
			lock(m_AsyncWorkQueue)
			{
				m_CurrentFrameWork.AddRange(m_AsyncWorkQueue);
				m_AsyncWorkQueue.Clear();
			}

			foreach(var work in m_CurrentFrameWork)
				work.Invoke();

			m_CurrentFrameWork.Clear();
		}

		private bool HasPendingTasks()
		{
			lock (m_AsyncWorkQueue)
			{
				return m_AsyncWorkQueue.Count != 0 || m_TrackedCount != 0;
			}
		}

		// SynchronizationContext must be set before any user code is executed. This is done on
		// Initial domain load and domain reload at MonoManager ReloadAssembly
		public static void InitializeSynchronizationContext()
		{
			SynchronizationContext.SetSynchronizationContext(new CustomUnitySynchronizationContext(System.Threading.Thread.CurrentThread.ManagedThreadId));
		}

		// All requests must be processed on the main thread where the full Unity API is available
		// See ScriptRunDelayedTasks in PlayerLoopCallbacks.h
		public static void ExecuteTasks()
		{
			//UnityEngine.Debug.Log($"SyncContext Null: {SynchronizationContext.Current == null} Type: {SynchronizationContext.Current?.GetType().Name}");
			var context = (CustomUnitySynchronizationContext)SynchronizationContext.Current;

			if (!context.HasPendingTasks())
				return;

			context?.Exec();
		}

		private struct WorkRequest
		{
			private readonly SendOrPostCallback m_DelagateCallback;
			private readonly object m_DelagateState;
			private readonly ManualResetEvent m_WaitHandle;

			public WorkRequest(SendOrPostCallback callback, object state, ManualResetEvent waitHandle = null)
			{
				m_DelagateCallback = callback;
				m_DelagateState = state;
				m_WaitHandle = waitHandle;
			}

			public void Invoke()
			{
				try
				{
					m_DelagateCallback(m_DelagateState);
				}
				catch(Exception e)
				{
					UnityEngine.Debug.LogException(e);;
				}

				m_WaitHandle?.Set();
			}
		}
	}
}