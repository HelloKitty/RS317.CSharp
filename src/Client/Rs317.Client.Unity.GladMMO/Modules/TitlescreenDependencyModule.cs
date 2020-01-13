﻿using System;
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
			//Register all required Authentication/Title modules.
			builder.RegisterModule<AuthenticationDependencyAutofacModule>();
			builder.RegisterModule(new CommonGameDependencyModule(GameSceneType.TitleScreen, "http://192.168.0.12:5000", typeof(GladMMOUnityClient).Assembly));
			builder.RegisterModule<TitleScreenUIDependenciesModule>();

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

			builder.Register<GladMMOUnityClient>(context =>
				{
					//This is done to make sure only 1 is ever created.
					if(GladMMOProgram.RootClient == null)
					{
						GladMMOProgram.RootClient = new GladMMOUnityClient(context.Resolve<ClientConfiguration>(), context.Resolve<UnityRsGraphics>(), GladMMOProgram.RootGameManager);

						return GladMMOProgram.RootClient;
					}
					else
						return GladMMOProgram.RootClient;
				})
				.AsSelf()
				.As<RsUnityClient>()
				.AsImplementedInterfaces()
				.ExternallyOwned();
		}
	}
}
