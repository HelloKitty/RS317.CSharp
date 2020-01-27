using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Glader.Essentials;
using GladMMO;

namespace Rs317.GladMMO
{
	[SceneTypeCreateGladMMO(GameSceneType.InstanceServerScene)]
	public sealed class InitializePlayerNameOnSpawnEventListener : BaseSingleEventListenerInitializable<IEntityWorldObjectCreatedEventSubscribable, EntityWorldObjectCreatedEventArgs>
	{
		private INameQueryService NameQueryService { get; }

		private ILog Logger { get; }

		public InitializePlayerNameOnSpawnEventListener(IEntityWorldObjectCreatedEventSubscribable subscriptionService,
			[NotNull] INameQueryService nameQueryService,
			[NotNull] ILog logger) 
			: base(subscriptionService)
		{
			NameQueryService = nameQueryService ?? throw new ArgumentNullException(nameof(nameQueryService));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		protected override void OnEventFired(object source, EntityWorldObjectCreatedEventArgs args)
		{
			//Only doing player name queries.
			if (args.EntityGuid.EntityType != EntityType.Player)
				return;

			Task.Factory.StartNew(async () =>
			{
				try
				{
					ResponseModel<NameQueryResponse, NameQueryResponseCode> responseModel = await NameQueryService.RetrievePlayerNameAsync(args.EntityGuid.RawGuidValue);

					if(responseModel.isSuccessful)
						args.WorldReprensetation.SetName(responseModel.Result.EntityName);
				}
				catch (Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Failed to query name for Player: {args.EntityGuid}. Reason: {e}");

					throw;
				}
			});
		}
	}
}
