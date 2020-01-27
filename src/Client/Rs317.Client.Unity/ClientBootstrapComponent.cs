using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Glader.Essentials;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace Rs317.Sharp
{
	/// <summary>
	/// This is the component that should be attached in the scene.
	/// It's what actually starts the entire RSClient within Unity3D.
	/// </summary>
	public class ClientBootstrapComponent : MonoBehaviour
	{
		[FormerlySerializedAs(nameof(GraphicsObject))]
		[SerializeField]
		private UnityRsGraphics _GraphicsObject;

		protected UnityRsGraphics GraphicsObject => _GraphicsObject;

		[SerializeField]
		private UnityRsInputDispatcherComponent InputObject;

		[SerializeField]
		public string Endpoint = "127.0.0.1";

		[SerializeField]
		public short PortOffset = 0;

		[SerializeField]
		public float ResolutionMultiplier = 1.0f;

		[Preserve] //important to keep in AOT builds.
		private void AOTSetup()
		{
			//ImageSharp doesn't work on WebGL.
			SixLabors.ImageSharp.Advanced.AotCompilerTools.Seed<Rgba32>();
		}

		//Called on scene start, which starts the underlying client.
		protected virtual async Task Start()
		{
			//Important for cross-thread interaction for creating "images".
			UnityAsyncHelper.InitializeSyncContext();
			Texture.allowThreadedTextureCreation = true;
			UnitySystemConsoleRedirector.Redirect();
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Debug.LogError($"Unhandled Exception: {args.ExceptionObject.ToString()}");

			//765, 503 default size.
			Screen.SetResolution((int) (765 * ResolutionMultiplier), (int) (503 * ResolutionMultiplier), Screen.fullScreenMode);

			try
			{
				await StartClient(0, 0, true);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to start client. Reason: {e.ToString()}");
				throw;
			}
		}

		private async Task StartClient(int localWorldId, short portOffset, bool membersWorld)
		{
			Debug.Log($"Starting client.");

			try
			{
				await signlink.startpriv(Endpoint, new Unity3DResourceCacheLoader());
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to load signlink/cache. Reason: {e}");
				throw;
			}

			//Get back onto the main thread.
			if (!RsUnityPlatform.isWebGLBuild)
				await new UnityYieldAwaitable();

			//WebGL port is + 1 off normal port for Websocket proxy.
			if (RsUnityPlatform.isWebGLBuild)
				portOffset++;

			ClientConfiguration configuration = new ClientConfiguration(localWorldId, portOffset, membersWorld);

			RsUnityClient client1 = CreateRsClient(configuration);
			InputObject.InputSubscribable = client1;
			GraphicsObject.GameStateHookable = client1;
			client1.createClientFrame((int) (765 * ResolutionMultiplier), (int) (503 * ResolutionMultiplier));

			Debug.Log($"Client frame created.");
		}

		protected virtual RsUnityClient CreateRsClient(ClientConfiguration configuration)
		{
			if (RsUnityPlatform.isWebGLBuild)
			{
				//Used for Task.Delay in WebGL (Task.Delay doesn't work in WebGL directly)
				WebGLUnityTaskDelayFactory delayFactory = new UnityEngine.GameObject("Task Delayer").AddComponent<WebGLUnityTaskDelayFactory>();

				if (RsUnityPlatform.isInEditor)
					return new RsUnityWebGLClient(configuration, GraphicsObject, this, new DefaultWebSocketClientFactory(), delayFactory);
				else
					return new RsUnityWebGLClient(configuration, GraphicsObject, this, new WebGLWebSocketFactory(delayFactory), delayFactory);
			}
			else if(RsUnityPlatform.isPlaystationBuild)
				return new RsUnityPS4Client(configuration, GraphicsObject);
			else if(RsUnityPlatform.isAndroidMobileBuild)
				return new RsUnityAndroidClient(configuration, GraphicsObject);
			else
			{
				IRunnableStarter runnableStarter = new DefaultRunnableStarterStrategy();
				return new RsUnityClient(configuration, GraphicsObject, runnableStarter, new DefaultRsSocketFactory(runnableStarter));
			}
		}
	}
}
