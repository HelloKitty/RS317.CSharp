using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GladMMO;
using GladNet;
using Reinterpret.Net;
using Rs317.GladMMMO;
using UnityEngine;

namespace Rs317.Sharp
{
	public sealed class GladMMORsUnityWebGLClient : RsUnityWebGLClient, IRSClientLoginButtonPressedEventSubscribable
	{
		private GameManager GameManagerService { get; }

		public event EventHandler OnRunescapeLoginButtonPressed;

		/// <summary>
		/// Thread specific buffer used to deserialize the packet header bytes into.
		/// </summary>
		protected byte[] PacketPayloadReadBuffer { get; } = new byte[5000];

		public GladMMORsUnityWebGLClient(ClientConfiguration config, 
			UnityRsGraphics graphicsObject, 
			[JetBrains.Annotations.NotNull] MonoBehaviour clientMonoBehaviour, 
			IRsSocketFactory socketFactory, 
			ITaskDelayFactory taskDelayFactory,
			[JetBrains.Annotations.NotNull] GameManager rootGameManager) 
			: base(config, graphicsObject, clientMonoBehaviour, socketFactory, taskDelayFactory)
		{
			GameManagerService = rootGameManager ?? throw new ArgumentNullException(nameof(rootGameManager));
			this.LoggedIn.OnVariableValueChanged += GameManagerService.OnLoginStateChanged;
		}

		protected override Task<int> ReadPacketHeader(int currentAvailableBytes)
		{
			return Task.FromResult<int>(0);
		}

		protected override async Task HandlePacketRecieveAntiCheatCheck()
		{
			//This prevents the client from disconnecting.
		}

		public override async Task processGameLoop()
		{
			await GameManagerService.Service();
			await base.processGameLoop();
		}

		protected override async Task OnLoginButtonClicked()
		{
			loginMessage1 = "";
			loginMessage2 = "Connecting to server...";
			drawLoginScreen(true);

			PlayerAccountJWTModel model = await HackyTitleSharedClientData.Instance.AuthService.TryAuthenticate(new AuthenticationRequestModel("Admin", "Test69!"));
			HackyTitleSharedClientData.Instance.AuthButtonListener.UpdatedTokenRepository(model);
			HackyTitleSharedClientData.Instance.AuthButtonListener.DispatchAuthenticationResult(model);
		}

		protected override async Task HandleIncomingPacketsAsync()
		{
			int currentAvailableBytes = socket.available();

			if (currentAvailableBytes < 2)
				return;

			//Read ALL packets until we're out of bytes this tick.
			while (currentAvailableBytes > 2)
			{
				//2 bytes for packet size
				await socket.read(PacketPayloadReadBuffer, 2);
				currentAvailableBytes -= 2;

				//See: GladMMOUnmanagedNetworkClient for understanding of how the networking internally works.
				//We read from the payload buffer 2 bytes, it's the size.
				int payloadSize = PacketPayloadReadBuffer.Reinterpret<short>(0);

				//We need to read enough bytes to deserialize the payload
				await socket.read(PacketPayloadReadBuffer, payloadSize);
				currentAvailableBytes -= payloadSize;

				//Deserialize the bytes starting from the begining but ONLY read up to the payload size. We reuse this buffer and it's large
				//so if we don't specify the length we could end up with an issue.
				var payload = HackyInstanceSharedClientData.Instance.SerializerService.Deserialize<GameServerPacketPayload>(PacketPayloadReadBuffer, 0, payloadSize);

				var incomingMessage = new NetworkIncomingMessage<GameServerPacketPayload>(new HeaderlessPacketHeader(payloadSize), payload);

				Console.WriteLine($"GladMMO Packet: {payload.GetType().Name}");
			}
		}

		protected override Task SendPendingPacketBufferAsync()
		{
			return Task.CompletedTask;
		}

		protected override void SendIdlePing()
		{
			//Just stub it out.
		}
	}
}
