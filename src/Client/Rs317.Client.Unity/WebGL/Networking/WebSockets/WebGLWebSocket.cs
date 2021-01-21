using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Rs317.Sharp
{
#if !IL2CPP
	/// <summary>
	/// WebSocket class bound to JSLIB.
	/// </summary>
	public class WebGLWebSocket : IWebSocket
	{
		//Runtime.dynCall('viii', webSocketState.onMessage, [ instanceId, buffer, dataBuffer.length ]);
		/* WebSocket JSLIB functions */
		[DllImport("__Internal")]
		public static extern int WebSocketConnect(int instanceId);

		[DllImport("__Internal")]
		public static extern int WebSocketClose(int instanceId, int code, string reason);

		[DllImport("__Internal")]
		public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

		[DllImport("__Internal")]
		public static extern int WebSocketGetState(int instanceId);

		/// <summary>
		/// The instance identifier.
		/// </summary>
		protected int instanceId;

		/// <summary>
		/// Occurs when the connection is opened.
		/// </summary>
		public event WebSocketOpenEventHandler OnOpen;

		/// <summary>
		/// Occurs when a message is received.
		/// </summary>
		public event WebSocketMessageEventHandler OnMessage;

		/// <summary>
		/// Occurs when an error was reported from WebSocket.
		/// </summary>
		public event WebSocketErrorEventHandler OnError;

		/// <summary>
		/// Occurs when the socked was closed.
		/// </summary>
		public event WebSocketCloseEventHandler OnClose;

		public event EventHandler<ArraySegment<byte>> OnDataReceived;

		public WebSocketState State => GetState();

		private TaskCompletionSource<bool> PendingRecieveSource { get; set; } = new TaskCompletionSource<bool>();

		private readonly object SyncObj = new object();

		private ITaskDelayFactory TaskDelayFactory { get; }

		/// <summary>
		/// Constructor - receive JSLIB instance id of allocated socket
		/// </summary>
		/// <param name="instanceId">Instance identifier.</param>
		/// <param name="taskDelayFactory"></param>
		public WebGLWebSocket(int instanceId, [NotNull] ITaskDelayFactory taskDelayFactory)
		{
			this.instanceId = instanceId;
			TaskDelayFactory = taskDelayFactory ?? throw new ArgumentNullException(nameof(taskDelayFactory));
		}

		/// <summary>
		/// Destructor - notifies WebSocketFactory about it to remove JSLIB references
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:HybridWebSocket.WebSocket"/> is reclaimed by garbage collection.
		/// </summary>
		~WebGLWebSocket()
		{
			WebGLWebSocketFactory.HandleInstanceDestroy(this.instanceId);
		}

		/// <summary>
		/// Return JSLIB instance ID
		/// </summary>
		/// <returns>The instance identifier.</returns>
		public int GetInstanceId()
		{
			return this.instanceId;
		}

		/// <summary>
		/// Open WebSocket connection
		/// </summary>
		public void Connect()
		{
			int ret = WebSocketConnect(this.instanceId);

			if(ret < 0)
				throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
		}

		/// <summary>
		/// Close WebSocket connection with optional status code and reason.
		/// </summary>
		/// <param name="code">Close status code.</param>
		/// <param name="reason">Reason string.</param>
		public Task Close(WebSocketCloseCode code = WebSocketCloseCode.Normal, string reason = null)
		{
			int ret = WebSocketClose(this.instanceId, (int)code, reason);

			if(ret < 0)
				throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);

			return Task.CompletedTask;
		}

		public Task Receive()
		{
			//I tried very hard to implement async notification
			//in WebGL but it just wasn't working for some reason
			//so we'll delay until the next frame instead if there is not enough data
			//return TaskDelayFactory.Create(1);

			lock (SyncObj)
			{
				//If they're calling Receive they want MORE data.
				//They should only call this if they want more data.
				//Therefore we need to create a new task if this one is already completed.
				if(PendingRecieveSource.Task.IsCompleted)
					PendingRecieveSource = new TaskCompletionSource<bool>();

				return PendingRecieveSource.Task;
			}
		}

		public Task<bool> ConnectAsync(SocketCreationContext connectionInfo)
		{
			//Before connect we need to create a callback to complete the async task
			//if the caller continues before connection was successful then we actually
			//end up in a bad state where it'll try to send before it's connected.
			TaskCompletionSource<bool> connectionResult = new TaskCompletionSource<bool>();

			//TODO: Unregister these.
			//Complete the task depending on task state
			//and the callbacks.
			this.OnOpen += () => connectionResult.SetResult(true);
			this.OnError += msg =>
			{
				if (!connectionResult.Task.IsCompleted)
					connectionResult.SetResult(false);
			};
			this.OnClose += reason =>
			{
				if (!connectionResult.Task.IsCompleted)
					connectionResult.SetResult(false);
			};

			OnMessage += OnMessageRecieved;

			//It already allocated for the specific URL/URI
			Connect();

			return connectionResult.Task;
		}

		private void OnMessageRecieved(ArraySegment<byte> data)
		{
			try
			{
				//When we recieve we need to use this
				//pending task to notify awaiters that we've actually produced
				//new data from an incoming message.
				lock(SyncObj)
				{
					//VERY important to always call/handle this first
					//otherwise you will encounter a race condition, or return before handling
					//due to the return below too.
					//Most importantly there is a race condition for the continutation if we don't
					//have the data received first.
					OnDataReceived?.Invoke(this, data);

					if(PendingRecieveSource.Task.IsCompleted)
						return;

					PendingRecieveSource.SetResult(true);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to handle incoming WebGL Websocket data. Reason: {e}");
				throw;
			}
		}

		/// <summary>
		/// Send binary data over the socket.
		/// </summary>
		/// <param name="data">Payload data.</param>
		public Task Send(byte[] bytes, int offset, int length)
		{
			//TODO: Handle offset and pointer passing.
			int ret = WebSocketSend(this.instanceId, bytes, length);

			if(ret < 0)
				throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Return WebSocket connection state.
		/// </summary>
		/// <returns>The state.</returns>
		public WebSocketState GetState()
		{
			int state = WebSocketGetState(this.instanceId);

			if(state < 0)
				throw WebSocketHelpers.GetErrorMessageFromCode(state, null);

			switch(state)
			{
				case 0:
					return WebSocketState.Connecting;

				case 1:
					return WebSocketState.Open;

				case 2:
					return WebSocketState.Closing;

				case 3:
					return WebSocketState.Closed;

				default:
					return WebSocketState.Closed;
			}
		}

		/// <summary>
		/// Delegates onOpen event from JSLIB to native sharp event
		/// Is called by WebSocketFactory
		/// </summary>
		public void DelegateOnOpenEvent()
		{
			this.OnOpen?.Invoke();
		}

		/// <summary>
		/// Delegates onMessage event from JSLIB to native sharp event
		/// Is called by WebSocketFactory
		/// </summary>
		/// <param name="data">Binary data.</param>
		public void DelegateOnMessageEvent(byte[] data)
		{
			this.OnMessage?.Invoke(new ArraySegment<byte>(data));
		}

		/// <summary>
		/// Delegates onError event from JSLIB to native sharp event
		/// Is called by WebSocketFactory
		/// </summary>
		/// <param name="errorMsg">Error message.</param>
		public void DelegateOnErrorEvent(string errorMsg)
		{
			this.OnError?.Invoke(errorMsg);
		}

		/// <summary>
		/// Delegate onClose event from JSLIB to native sharp event
		/// Is called by WebSocketFactory
		/// </summary>
		/// <param name="closeCode">Close status code.</param>
		public void DelegateOnCloseEvent(int closeCode)
		{
			this.OnClose?.Invoke(WebSocketHelpers.ParseCloseCodeEnum(closeCode));
		}
	}
#endif
}