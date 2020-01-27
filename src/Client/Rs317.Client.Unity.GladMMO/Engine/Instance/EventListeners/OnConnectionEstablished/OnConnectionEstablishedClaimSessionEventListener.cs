using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Glader.Essentials;
using GladNet;
using Rs317.GladMMO;

namespace GladMMO
{
	[SceneTypeCreateGladMMO(GameSceneType.InstanceServerScene)]
	public sealed class OnConnectionEstablishedClaimSessionEventListener : BaseSingleEventListenerInitializable<INetworkConnectionEstablishedEventSubscribable>
	{
		private IPeerPayloadSendService<GameClientPacketPayload> SendService { get; }

		private IReadonlyAuthTokenRepository AuthTokenRepository { get; }

		private ILocalCharacterDataRepository CharacterDataRepository { get; }

		private ICharacterService CharacterService { get; }

		private ILog Logger { get; }

		private IGameContextEventQueueable EventQueueable { get; }

		public OnConnectionEstablishedClaimSessionEventListener(INetworkConnectionEstablishedEventSubscribable subscriptionService,
			[NotNull] IPeerPayloadSendService<GameClientPacketPayload> sendService,
			[NotNull] IReadonlyAuthTokenRepository authTokenRepository,
			[NotNull] ILocalCharacterDataRepository characterDataRepository,
			[NotNull] ICharacterService characterService,
			[JetBrains.Annotations.NotNull] ILog logger,
			[JetBrains.Annotations.NotNull] IGameContextEventQueueable eventQueueable) 
			: base(subscriptionService)
		{
			SendService = sendService ?? throw new ArgumentNullException(nameof(sendService));
			AuthTokenRepository = authTokenRepository ?? throw new ArgumentNullException(nameof(authTokenRepository));
			CharacterDataRepository = characterDataRepository ?? throw new ArgumentNullException(nameof(characterDataRepository));
			CharacterService = characterService ?? throw new ArgumentNullException(nameof(characterService));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			EventQueueable = eventQueueable ?? throw new ArgumentNullException(nameof(eventQueueable));
		}

		protected override void OnEventFired(object source, EventArgs args)
		{
			//Once connection to the instance server is established
			//we must attempt to claim out session on to actually fully enter.
			EventQueueable.Enqueue(async () =>
			{
				try
				{
					CharacterListResponse listResponse = await CharacterService.GetCharacters();

					await CharacterService.TryEnterSession(listResponse.CharacterIds.First());

					CharacterDataRepository.UpdateCharacterId(listResponse.CharacterIds.First());

					//TODO: When it comes to community servers, we should not expose the sensitive JWT to them. We need a better way to deal with auth against untrusted instance servers
					await SendService.SendMessage(new ClientSessionClaimRequestPayload(AuthTokenRepository.RetrieveWithType(), listResponse.CharacterIds.First()));
				}
				catch(Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Failed to authenticate to instance server as character. Reason: {e.ToString()}");

					throw;
				}
			});
		}
	}
}
