using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Glader.Essentials;
using Rs317.GladMMO;
using Rs317.Sharp;

namespace Rs317.GladMMMO
{
	public sealed class GameManager : IGameContextEventQueueable, IGameServiceable
	{
		private long LastMilliseconds { get; set; } = -1;

		private ConcurrentQueue<Action> EventQueue { get; } = new ConcurrentQueue<Action>();

		private ConcurrentQueue<Func<Task>> EventQueueAsync { get; } = new ConcurrentQueue<Func<Task>>();

		public static IDisposable LifetimeDependencyScope { get; set; }

		//Hacky, but this is mutable when the game state changes.
		private IEnumerable<IGameTickable> CurrentGameTickables { get; set; } = Array.Empty<IGameTickable>();

		//Hacky, but this is mutable when the game state changes.
		private IEnumerable<IGameFixedTickable> CurrentGameFixedTickables { get; set; } = Array.Empty<IGameFixedTickable>();

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void OnLoginStateChanged(object sender, HookableVariableValueChangedEventArgs<bool> e)
		{
			LifetimeDependencyScope?.Dispose();

			//EventQueue.Clear();
			//Not available in netstandard2.0
			//https://github.com/dotnet/corefx/issues/2338
			while(EventQueue.TryDequeue(out var dummy)) { }

			Console.WriteLine($"LoginState Changed");

			try
			{
				//We're now logged in, time to create new tickables and initializables.
				if(e.NewValue)
				{
					//We're going into the game!
					var container = BuildInstanceServerContainer().BeginLifetimeScope();
					InitializeNewContainerContext(container);
				}
				else
				{
					//We're going BACK to the titlescreen.
					var container = BuildTitleScreenContainer().BeginLifetimeScope();
					InitializeNewContainerContext(container);
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine($"Failed GameManager scene change. Reason: {exception}");
				throw;
			}
		}

		private void InitializeNewContainerContext(ILifetimeScope container)
		{
			if (container == null) throw new ArgumentNullException(nameof(container));

			var gameInitializables = container.Resolve<IEnumerable<IGameInitializable>>();
			var gameStartables = container.Resolve<IEnumerable<IGameStartable>>();
			CurrentGameTickables = container.Resolve<IEnumerable<IGameTickable>>();
			CurrentGameFixedTickables = container.Resolve<IEnumerable<IGameFixedTickable>>();

			EventQueueAsync.Enqueue(async () =>
			{
				foreach (var initializable in gameInitializables)
					await initializable.OnGameInitialized();

				foreach (var startable in gameStartables)
					await startable.OnGameStart();
			});

			LifetimeDependencyScope = container;
		}

		public void Enqueue(Action actionEvent)
		{
			EventQueue.Enqueue(actionEvent);
		}

		public void EnqueueAsync(Func<Task> actionEventAsync)
		{
			EventQueueAsync.Enqueue(actionEventAsync);
		}

		public async Task Service()
		{
			//Just do one a tick.
			if (EventQueue.Any())
			{
				if (EventQueue.TryDequeue(out var action))
					action?.Invoke();
			}

			if (EventQueueAsync.Any())
			{
				if (EventQueueAsync.TryDequeue(out var action))
					await action.Invoke();
			}

			foreach(var tickable in CurrentGameTickables)
				tickable.Tick();

			long currentMilliseconds = TimeService.CurrentTimeInMilliseconds();

			if(LastMilliseconds == -1)
				LastMilliseconds = currentMilliseconds;
			else if(currentMilliseconds - 600 >= LastMilliseconds)
			{
				//It's been 600ms, time for another tick.
				LastMilliseconds = currentMilliseconds;

				foreach(var fixedTickable in CurrentGameFixedTickables)
					fixedTickable.OnGameFixedTick();
			}
		}

		public static IContainer BuildTitleScreenContainer()
		{
			//Autofac is needed to create the GladMMO scene.
			ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterModule<TitlescreenDependencyModule>();
			return builder.Build();
		}

		public static IContainer BuildInstanceServerContainer()
		{
			//Autofac is needed to create the GladMMO scene.
			ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterModule<InstanceServerDependencyModule>();
			return builder.Build();
		}
	}
}
