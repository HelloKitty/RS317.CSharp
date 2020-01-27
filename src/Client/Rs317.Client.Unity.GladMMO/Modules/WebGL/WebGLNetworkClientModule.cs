using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common.Logging;
using GladMMO;
using GladNet;
using Reinterpret.Net;

namespace Rs317.Sharp
{
	public sealed class WebGLNetworkClientModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterType<Test>()
				.As<IPeerPayloadSendService<GameClientPacketPayload>>()
				.As<IConnectionService>()
				.As<IManagedNetworkClient<GameClientPacketPayload, GameServerPacketPayload>>()
				.As<INetworkClientManager>()
				.As<INetworkClientDisconnectedEventSubscribable>()
				.SingleInstance();

			builder.RegisterType<DefaultMessageContextFactory>()
				.As<IPeerMessageContextFactory>()
				.SingleInstance();
		}
	}

	public class Test : 
		INetworkClientManager<GameServerPacketPayload, GameClientPacketPayload>, 
		IPeerPayloadSendService<GameClientPacketPayload>, IConnectionService, 
		IManagedNetworkClient<GameClientPacketPayload, GameServerPacketPayload>, 
		INetworkClientManager,
		INetworkClientDisconnectedEventSubscribable
	{
		public bool isConnected { get; private set; }

		private RsUnityClient Client { get; }

		private INetworkSerializationService SerializerService { get; }

		private byte[] PacketBuffer { get; } = new byte[5000];

		public event EventHandler OnNetworkClientDisconnected;

		public Test([NotNull] RsUnityClient client, [NotNull] INetworkSerializationService serializerService)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			SerializerService = serializerService ?? throw new ArgumentNullException(nameof(serializerService));
		}

		public async Task StartHandlingNetworkClient(IManagedNetworkClient<GameClientPacketPayload, GameServerPacketPayload> client)
		{
			isNetworkHandling = true;
		}

		public async Task StopHandlingNetworkClient()
		{
			isNetworkHandling = false;
		}

		public bool isNetworkHandling { get; private set; }

		async Task<SendResult> IPeerPayloadSendService<GameClientPacketPayload>.SendMessage<TPayloadType>(TPayloadType payload, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
		{
			byte[] packet = SerializerService.Serialize(payload);

			((short) packet.Length).Reinterpret(PacketBuffer, 0);

			await Client.socket.write(2, PacketBuffer);
			await Client.socket.write(packet.Length, packet);
			return SendResult.Sent;
		}

		async Task<SendResult> IPeerPayloadSendService<GameClientPacketPayload>.SendMessageImmediately<TPayloadType>(TPayloadType payload, DeliveryMethod method = DeliveryMethod.ReliableOrdered)
		{
			byte[] packet = SerializerService.Serialize(payload);

			((short)packet.Length).Reinterpret(PacketBuffer, 0);

			await Client.socket.write(2, PacketBuffer);
			await Client.socket.write(packet.Length, packet);
			return SendResult.Sent;
		}

		public async Task DisconnectAsync(int delay)
		{
			Client.logout();
			isConnected = false;
			OnNetworkClientDisconnected?.Invoke(this, EventArgs.Empty);
		}

		public async Task<bool> ConnectAsync(string ip, int port)
		{
			await Client.ConnectToGameServer();
			isConnected = true;
			return true;
		}

		public async Task<NetworkIncomingMessage<GameServerPacketPayload>> ReadMessageAsync(CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public async Task<TResponseType> InterceptPayload<TResponseType>(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task WriteAsync(byte[] bytes, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}
