using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Rs317.Sharp
{
	public sealed class UnityWebRequestAwaiter : INotifyCompletion
	{
		private UnityWebRequestAsyncOperation WebOperation { get; }

		public bool IsCompleted => WebOperation.isDone;

		public UnityWebRequestAwaiter([NotNull] UnityWebRequestAsyncOperation webOperation)
		{
			WebOperation = webOperation ?? throw new ArgumentNullException(nameof(webOperation));
		}

		public void GetResult() { }

		public void OnCompleted([NotNull] Action continuation)
		{
			if(IsCompleted)
			{
				continuation();
				return;
			}

			SynchronizationContext capturedContext = SynchronizationContext.Current;
			WebOperation.completed += operation =>
			{
				if(capturedContext != null)
					capturedContext.Post(_ => continuation(), null);
				else
					continuation();
			};
		}
	}

	public static class UnityWebRequestAwaiterExtensions
	{
		public static UnityWebRequestAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
		{
			return new UnityWebRequestAwaiter(asyncOp);
		}
	}
}
