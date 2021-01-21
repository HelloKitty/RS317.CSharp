using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using AOT;
using JetBrains.Annotations;

namespace Rs317.Sharp
{
#if !IL2CPP
	/// <summary>
	/// Class providing static access methods to work with JSLIB WebSocket or WebSocketSharp interface
	/// </summary>
	public class WebGLWebSocketFactory : IRsSocketFactory
	{
		/* Map of websocket instances */
		public static Dictionary<Int32, WebGLWebSocket> instances = new Dictionary<Int32, WebGLWebSocket>();

		/* Delegates */
		public delegate void OnOpenCallback(int instanceId);

		public delegate void OnMessageCallback(int instanceId, System.IntPtr msgPtr, int msgSize);

		public delegate void OnErrorCallback(int instanceId, System.IntPtr errorPtr);

		public delegate void OnCloseCallback(int instanceId, int closeCode);

		/* WebSocket JSLIB callback setters and other functions */
		[DllImport("__Internal")]
		public static extern int WebSocketAllocate(string url);

		[DllImport("__Internal")]
		public static extern void WebSocketFree(int instanceId);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnOpen(OnOpenCallback callback);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnMessage(OnMessageCallback callback);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnError(OnErrorCallback callback);

		[DllImport("__Internal")]
		public static extern void WebSocketSetOnClose(OnCloseCallback callback);

		/* If callbacks was initialized and set */
		public static bool isInitialized = false;

		static WebGLWebSocketFactory()
		{
			Initialize();
		}

		/*
		 * Initialize WebSocket callbacks to JSLIB
		 */
		public static void Initialize()
		{
			WebSocketSetOnOpen(DelegateOnOpenEvent);
			WebSocketSetOnMessage(DelegateOnMessageEvent);
			WebSocketSetOnError(DelegateOnErrorEvent);
			WebSocketSetOnClose(DelegateOnCloseEvent);

			isInitialized = true;
		}

		/// <summary>
		/// Called when instance is destroyed (by destructor)
		/// Method removes instance from map and free it in JSLIB implementation
		/// </summary>
		/// <param name="instanceId">Instance identifier.</param>
		public static void HandleInstanceDestroy(int instanceId)
		{
			instances.Remove(instanceId);
			WebSocketFree(instanceId);
		}

		[MonoPInvokeCallback(typeof(OnOpenCallback))]
		public static void DelegateOnOpenEvent(int instanceId)
		{
			WebGLWebSocket instanceRef;

			if(instances.TryGetValue(instanceId, out instanceRef))
			{
				instanceRef.DelegateOnOpenEvent();
			}
		}

		[MonoPInvokeCallback(typeof(OnMessageCallback))]
		public static void DelegateOnMessageEvent(int instanceId, System.IntPtr msgPtr, int msgSize)
		{
			WebGLWebSocket instanceRef;

			if(instances.TryGetValue(instanceId, out instanceRef))
			{
				byte[] msg = new byte[msgSize];
				Marshal.Copy(msgPtr, msg, 0, msgSize);

				instanceRef.DelegateOnMessageEvent(msg);
			}
		}

		[MonoPInvokeCallback(typeof(OnErrorCallback))]
		public static void DelegateOnErrorEvent(int instanceId, System.IntPtr errorPtr)
		{
			WebGLWebSocket instanceRef;

			if(instances.TryGetValue(instanceId, out instanceRef))
			{

				string errorMsg = Marshal.PtrToStringAuto(errorPtr);
				instanceRef.DelegateOnErrorEvent(errorMsg);

			}
		}

		[MonoPInvokeCallback(typeof(OnCloseCallback))]
		public static void DelegateOnCloseEvent(int instanceId, int closeCode)
		{
			WebGLWebSocket instanceRef;

			if(instances.TryGetValue(instanceId, out instanceRef))
			{
				instanceRef.DelegateOnCloseEvent(closeCode);
			}
		}

		private ITaskDelayFactory TaskDelayFactory { get; }

		public WebGLWebSocketFactory([NotNull] ITaskDelayFactory taskDelayFactory)
		{
			TaskDelayFactory = taskDelayFactory ?? throw new ArgumentNullException(nameof(taskDelayFactory));
		}

		public IRsSocket Create(SocketCreationContext context)
		{
			if(!isInitialized)
				Initialize();

			//WebGL from browser will use HTTPS so it required WSS
			string url = $"wss://{context.Endpoint}:{context.Port}";
			int instanceId = WebSocketAllocate(url);

			WebGLWebSocket socket = new WebGLWebSocket(instanceId, TaskDelayFactory);
			instances.Add(instanceId, socket);

			socket.OnOpen += () => SocketOnOpen(socket);

			//Now we make an adapter for the IRsSocket interface.
			return new WebSocketRsSocketClientAdapter(socket, false);
		}

		private void SocketOnOpen([NotNull] IWebSocket openedSocket)
		{
			if (openedSocket == null) throw new ArgumentNullException(nameof(openedSocket));

			//WebGL socket doesn't seem to be capable of pushing in bytes into the ready
			//onrecieve method when they are reieved causing the client to just hang after
			//logging in because it then expects to be preemptively notified of available
			//bytes but this will be always zero with explicit calls to receieve.
		}
	}
#endif
}