using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Common.Logging;
using Glader.Essentials;
using GladMMO;
using Rs317.GladMMMO;
using Rs317.Sharp;

namespace Rs317.GladMMO
{
	public sealed class TitlescreenDependencyModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.RegisterModule<ZoneServerServiceDependencyAutofacModule>();

			//Register all required Authentication/Title modules.
			builder.RegisterModule<AuthenticationDependencyAutofacModule>();
			builder.RegisterModule(new CommonGameDependencyModule(GameSceneType.TitleScreen, "http://192.168.0.12:5000", typeof(GladMMOUnityClient).Assembly));
			builder.RegisterModule<TitleScreenUIDependenciesModule>();

			builder.RegisterType<WebGLZoneDataServiceClient>()
				.As<IZoneDataService>();

			builder.RegisterInstance(new ClientConfiguration(0, 0, true))
				.AsSelf();

			builder.RegisterInstance(new ConsoleLogger(LogLevel.All))
				.AsImplementedInterfaces()
				.SingleInstance();

			builder.RegisterType<DefaultBufferFactory>()
				.AsSelf()
				.AsImplementedInterfaces()
				.SingleInstance();

			builder.RegisterInstance<GameManager>(GladMMOProgram.RootGameManager)
				.As<IGameContextEventQueueable>()
				.As<IGameServiceable>()
				.AsSelf()
				.ExternallyOwned()
				.SingleInstance();

			//LocalZoneDataRepository : IZoneDataRepository
			builder.RegisterType<LocalZoneDataRepository>()
				.As<IZoneDataRepository>()
				.As<IReadonlyZoneDataRepository>();

			builder.RegisterInstance<RsUnityClient>(GladMMOProgram.RootClient)
				.AsImplementedInterfaces()
				.As<RsUnityClient>()
				.OnActivated(args => HackyTitleSharedClientData.Instance = args.Context.Resolve<HackyTitleSharedClientData>())
				.ExternallyOwned();

			builder.RegisterType<HackyTitleSharedClientData>()
				.AsSelf();
		}
	}
}
