using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Common.Logging;
using Glader.Essentials;
using GladMMO;
using Rs317.GladMMMO;
using Rs317.Sharp;

namespace Rs317.GladMMO
{
	public sealed class InstanceServerDependencyModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			//Don't use the typical GladMMO networking for WebGL
			if (!RsUnityPlatform.isWebGLBuild)
				builder.RegisterModule<GameServerNetworkClientAutofacModule>();
			else
				builder.RegisterModule<WebGLNetworkClientModule>();

			builder.RegisterModule(new GameClientMessageHandlerAutofacModule(GameSceneType.InstanceServerScene, this.GetType().Assembly));
			builder.RegisterModule<GladMMONetworkSerializerAutofacModule>();
			builder.RegisterModule<RsGameplayDependencyRegisterationAutofacModule>();
			builder.RegisterModule<CharacterServiceDependencyAutofacModule>();
			builder.RegisterModule<GladMMOClientExplicitEngineInterfaceAutoModule>();

			builder.RegisterInstance<GameManager>(GladMMOProgram.RootGameManager)
				.As<IGameContextEventQueueable>()
				.As<IGameServiceable>()
				.AsSelf()
				.ExternallyOwned()
				.SingleInstance();

			builder.RegisterInstance<RsUnityClient>(GladMMOProgram.RootClient)
				.As<RsUnityClient>()
				.OnActivated(args => HackyInstanceSharedClientData.Instance = args.Context.Resolve<HackyInstanceSharedClientData>())
				.AsImplementedInterfaces()
				.ExternallyOwned();

			builder.RegisterType<HackyInstanceSharedClientData>()
				.AsSelf();

			//Register all required Authentication/Title modules.
			builder.RegisterModule(new CommonGameDependencyModule(GameSceneType.InstanceServerScene, "http://192.168.0.12:5000", typeof(GladMMOUnityClient).Assembly));

			builder.RegisterInstance(new ConsoleLogger(LogLevel.All))
				.AsImplementedInterfaces()
				.SingleInstance();

			//LocalZoneDataRepository : IZoneDataRepository
			builder.RegisterType<LocalZoneDataRepository>()
				.As<IZoneDataRepository>()
				.As<IReadonlyZoneDataRepository>();
		}
	}
}
