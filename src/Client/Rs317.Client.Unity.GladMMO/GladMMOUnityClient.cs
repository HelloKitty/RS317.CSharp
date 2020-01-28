using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GladMMO;
using GladNet;
using Refit;
using Reinterpret.Net;
using Rs317.GladMMMO;
using Rs317.Sharp;
using UnityEngine;

namespace Rs317.GladMMO
{
	public sealed class GladMMOUnityClient : RsUnityClient, IRSClientLoginButtonPressedEventSubscribable
	{
		public GameManager GameManagerService { get; }

		public event EventHandler OnRunescapeLoginButtonPressed;

		public GladMMOUnityClient(ClientConfiguration config, UnityRsGraphics graphicsObject, GameManager gameManagerService) 
			: base(config, graphicsObject, new DefaultRunnableStarterStrategy(), new DefaultRsSocketFactory(new DefaultRunnableStarterStrategy()))
		{
			GameManagerService = gameManagerService;
			this.LoggedIn.OnVariableValueChanged += GameManagerService.OnLoginStateChanged;
		}

		protected override Task<int> ReadPacketHeader(int currentAvailableBytes)
		{
			return Task.FromResult<int>(0);
		}

		protected override async Task HandlePacketRecieveAntiCheatCheck()
		{
			//This prevents the client from disconnecting.
		}

		public override async Task processGameLoop()
		{
			await GameManagerService.Service();
			await base.processGameLoop();
		}

		protected override Task OnLoginButtonClicked()
		{
			loginMessage1 = "";
			loginMessage2 = "Connecting to server...";
			drawLoginScreen(true);

			OnRunescapeLoginButtonPressed?.Invoke(this, EventArgs.Empty);

			return Task.CompletedTask;
		}

		protected override void SendIdlePing()
		{
			//Just stub it out.
		}

		protected override void SendWalkPacket(int clickType, int maxPathSize, int x, int currentIndex, int y)
		{
			//TODO: Don't use unit vectors.
			Vector3[] pathPoints = new Vector3[currentIndex + 1];

			pathPoints[0] = new Vector3(walkingQueueX[currentIndex] + baseX, 0, walkingQueueY[currentIndex] + baseY);

			int pathIndex = 1;
			for(int counter = 1; counter < maxPathSize; counter++, pathIndex++)
			{
				currentIndex--;
				pathPoints[pathIndex] = new Vector3(walkingQueueX[currentIndex] + baseX, 0, walkingQueueY[currentIndex] + baseY);
			}

			HackyInstanceSharedClientData.Instance.SendService.SendMessage(new ClientSetClickToMovePathRequestPayload(new PathBasedMovementData(pathPoints.ToArray(), 100)));
		}
	}
}
