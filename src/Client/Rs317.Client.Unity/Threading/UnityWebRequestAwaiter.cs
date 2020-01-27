using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Rs317.Sharp
{
	public sealed class UnityWebRequestAwaiter : INotifyCompletion
	{
		private UnityWebRequestAsyncOperation WebOperation { get; }

		public bool IsCompleted => WebOperation.isDone;

		private Action AwaiterContinuation;

		public UnityWebRequestAwaiter([NotNull] UnityWebRequestAsyncOperation webOperation)
		{
			WebOperation = webOperation ?? throw new ArgumentNullException(nameof(webOperation));
			WebOperation.completed += WebOperationOnCompleted;
		}

		private void WebOperationOnCompleted(AsyncOperation obj)
		{
			WebOperation.completed -= WebOperationOnCompleted;

			//It completed immediately, meaning that the continuation is
			//not set if this happens to be null.
			AwaiterContinuation?.Invoke();
			AwaiterContinuation = null;
		}

		public void GetResult() { }

		public void OnCompleted([NotNull] Action continuation)
		{
			if (WebOperation.isDone)
				continuation.Invoke();
			else
				AwaiterContinuation = continuation;
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
