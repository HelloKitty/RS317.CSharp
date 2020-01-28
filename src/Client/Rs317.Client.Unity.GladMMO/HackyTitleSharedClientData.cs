using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GladMMO;
using GladNet;
using Rs317.GladMMO;

namespace Rs317.Sharp
{
	//Put almost nothing here, it's here ONLY to help with auth and afew shared state objects
	//between the two different GladMMO clients.
	public class HackyTitleSharedClientData
	{
		public static HackyTitleSharedClientData Instance { get; set; }

		public IAuthenticationService AuthService { get; }

		public AuthenticateOnLoginButtonClickEventListener AuthButtonListener { get; }

		public HackyTitleSharedClientData([NotNull] IAuthenticationService authService, [NotNull] AuthenticateOnLoginButtonClickEventListener authButtonListener)
		{
			AuthService = authService ?? throw new ArgumentNullException(nameof(authService));
			AuthButtonListener = authButtonListener ?? throw new ArgumentNullException(nameof(authButtonListener));
		}
	}

	public class HackyInstanceSharedClientData : IPeerRequestSendService<GameClientPacketPayload>
	{
		public INetworkSerializationService SerializerService { get; }

		public static HackyInstanceSharedClientData Instance { get; set; }

		public MessageHandlerService<GameServerPacketPayload, GameClientPacketPayload> MessageHandler { get; }

		public IPeerMessageContextFactory MessageContextFactory { get; }

		private IConnectionService ConnectionService { get; }

		public IPeerPayloadSendService<GameClientPacketPayload> SendService { get; }

		public HackyInstanceSharedClientData([NotNull] INetworkSerializationService serializerService, 
			[JetBrains.Annotations.NotNull] MessageHandlerService<GameServerPacketPayload, GameClientPacketPayload> messageHandler,
			[JetBrains.Annotations.NotNull] IPeerMessageContextFactory messageContextFactory,
			[JetBrains.Annotations.NotNull] IConnectionService connectionService,
			[JetBrains.Annotations.NotNull] IPeerPayloadSendService<GameClientPacketPayload> sendService)
		{
			SerializerService = serializerService ?? throw new ArgumentNullException(nameof(serializerService));
			MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
			MessageContextFactory = messageContextFactory ?? throw new ArgumentNullException(nameof(messageContextFactory));
			ConnectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
			SendService = sendService ?? throw new ArgumentNullException(nameof(sendService));
		}

		public IPeerMessageContext<GameClientPacketPayload> CreateMessageContext()
		{
			return MessageContextFactory.Create(ConnectionService, SendService, this);
		}

		async Task<TResponseType> IPeerRequestSendService<GameClientPacketPayload>.SendRequestAsync<TResponseType>(GameClientPacketPayload request, DeliveryMethod method = DeliveryMethod.ReliableOrdered, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotImplementedException();
		}
	}
}
