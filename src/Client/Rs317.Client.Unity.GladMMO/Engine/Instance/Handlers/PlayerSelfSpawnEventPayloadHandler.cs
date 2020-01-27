using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Glader.Essentials;
using GladNet;

namespace GladMMO
{
	[SceneTypeCreateGladMMO(GameSceneType.InstanceServerScene)]
	public sealed class PlayerSelfSpawnEventPayloadHandler : BaseGameClientGameMessageHandler<PlayerSelfSpawnEventPayload>
	{
		private INetworkEntityVisibilityEventPublisher VisibilityEventPublisher { get; }

		private IFactoryCreatable<NetworkEntityNowVisibleEventArgs, EntityCreationData> VisibileEventFactory { get; }

		public PlayerSelfSpawnEventPayloadHandler(ILog logger, 
			[NotNull] IFactoryCreatable<NetworkEntityNowVisibleEventArgs, EntityCreationData> visibileEventFactory
			, [NotNull] INetworkEntityVisibilityEventPublisher visibilityEventPublisher) 
			: base(logger)
		{
			VisibileEventFactory = visibileEventFactory ?? throw new ArgumentNullException(nameof(visibileEventFactory));
			VisibilityEventPublisher = visibilityEventPublisher ?? throw new ArgumentNullException(nameof(visibilityEventPublisher));
		}

		public override async Task HandleMessage(IPeerMessageContext<GameClientPacketPayload> context, PlayerSelfSpawnEventPayload payload)
		{
			NetworkEntityNowVisibleEventArgs visibilityEvent = VisibileEventFactory.Create(payload.CreationData);

			VisibilityEventPublisher.Publish(visibilityEvent);

			//TODO: We need to make this the first packet, or couple of packets. We don't want to do this inbetween potentially slow operatons.
			await context.PayloadSendService.SendMessageImmediately(new ServerTimeSyncronizationRequestPayload(DateTime.UtcNow.Ticks));
		}
	}
}
