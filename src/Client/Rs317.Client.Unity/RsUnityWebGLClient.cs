using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using JetBrains.Annotations;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Rs317.Sharp
{
	public class RsUnityWebGLClient : RsUnityClient
	{
		/// <summary>
		/// Initializes the ability for callbacks to be processed for
		/// browser page visibility.
		/// </summary>
		[DllImport("__Internal")]
		public static extern void InitializePageVisibilityCallbacks();

		[DllImport("__Internal")]
		public static extern void SetOnPageVisible(OnVisibilityChangeCallback callback);

		[DllImport("__Internal")]
		public static extern void SetOnPageInvisible(OnVisibilityChangeCallback callback);

		public delegate void OnVisibilityChangeCallback();

		private ITaskDelayFactory TaskDelayFactory { get; }

		private MonoBehaviour ClientMonoBehaviour { get; }

		public RsUnityWebGLClient(ClientConfiguration config, UnityRsGraphics graphicsObject, [NotNull] MonoBehaviour clientMonoBehaviour, ITaskDelayFactory taskDelayFactory) 
			: this(config, graphicsObject, clientMonoBehaviour, new WebGLTcpClientRsSocketFactory(), taskDelayFactory)
		{

		}

		public RsUnityWebGLClient(ClientConfiguration config, UnityRsGraphics graphicsObject, [NotNull] MonoBehaviour clientMonoBehaviour, IRsSocketFactory socketFactory, ITaskDelayFactory taskDelayFactory)
			: base(config, graphicsObject, new WebGLRunnableStarterStrategy(), socketFactory)
		{
			if(config == null) throw new ArgumentNullException(nameof(config));

			ClientMonoBehaviour = clientMonoBehaviour ?? throw new ArgumentNullException(nameof(clientMonoBehaviour));

			//Only need to override this for WebGL.
			Sprite.ExternalLoadImageHook += ExternalLoadImageHook;
			TaskDelayFactory = taskDelayFactory;

			//This is a hack to make sure that WebGL doesn't cut off frames in the background
			//causing disconnections.
			Time.maximumDeltaTime = 20.0f;

			//Can't do this in the editor.
			if (!RsUnityPlatform.isInEditor)
			{
				SetOnPageVisible(DelegateOnVisibilityChangeVisible);
				SetOnPageInvisible(DelegateOnVisibilityChangeInvisible);
				InitializePageVisibilityCallbacks();
			}

			//WebGL probably forces vsync already but we should ensure it.
			//QualitySettings.vSyncCount = 1;
		}

		[MonoPInvokeCallback(typeof(OnVisibilityChangeCallback))]
		public static void DelegateOnVisibilityChangeVisible()
		{
			Console.WriteLine("Page Visible Called");
			ShouldClientRender = true;
		}

		[MonoPInvokeCallback(typeof(OnVisibilityChangeCallback))]
		public static void DelegateOnVisibilityChangeInvisible()
		{
			Console.WriteLine("Page Invisible Called");
			ShouldClientRender = false;
		}

		private LoadedImagePixels ExternalLoadImageHook(byte[] arg)
		{
			//LoadImage will replace with with incoming image size.
			Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
			ImageConversion.LoadImage(tex, arg);
			tex = FlipTexture(tex);
			int[] pixels = tex.GetPixels32().Select(ColorUtils.ColorToRGB).ToArray();

			return new LoadedImagePixels(tex.height, tex.width, pixels);
		}

		Texture2D FlipTexture(Texture2D original)
		{
			Texture2D flipped = new Texture2D(original.width, original.height);

			int xN = original.width;
			int yN = original.height;

			for(int i = 0; i < xN; i++)
			{
				for(int j = 0; j < yN; j++)
				{
					flipped.SetPixel(i, yN - j - 1, original.GetPixel(i, j));
				}
			}
			flipped.Apply();

			return flipped;
		}

		protected override IRSGraphicsProvider<UnityRsGraphics> CreateGraphicsProvider()
		{
			return new UnityRsGraphicsProvider(GraphicsObject);
		}

		protected override BaseRsImageProducer<UnityRsGraphics> CreateNewImageProducer(int xSize, int ySize, string producerName)
		{
			return new UnityRsImageProducer(xSize, ySize, producerName, gameGraphics);
		}

		public override void createClientFrame(int width, int height)
		{
			this.width = width;
			this.height = height;

			signlink.applet = this;
			gameGraphics = CreateGraphicsProvider();
			fullGameScreen = CreateNewImageProducer(this.width, height, nameof(fullGameScreen));

			//Creates the Unity engine component that will start the client's
			//GameEngine loop.
			AsyncStartRunnableComponent component = new UnityEngine.GameObject("Applet Container").AddComponent<AsyncStartRunnableComponent>();
			component.RunnableObject = this;
		}

		protected override void StartOnDemandFetcher(Archive archiveVersions)
		{
			//Webgl requires a non-blocking archive loading system.
			/*onDemandFetcher = new OnDemandFetcher();
			onDemandFetcher.start(archiveVersions, this);*/
			WebGLOnDemandFetcher fetcher = new WebGLOnDemandFetcher(TaskDelayFactory);
			onDemandFetcher = fetcher;
			fetcher.start(archiveVersions, this);
		}

		protected override void StartFlameDrawing()
		{
			Debug.Log($"Starting flames drawing.");
			shouldDrawFlames = true;
			currentlyDrawingFlames = true;
			new UnityEngine.GameObject("Async: Flames").AddComponent<AsyncTaskAwaitableComponent>().SetTask = DrawFlamesAsync();
		}

		public override async Task run()
		{
			if(!shouldDrawFlames)
			{
				await AppletRunCoroutine();
			}
		}

		private async Task DrawFlamesAsync()
		{
			Debug.Log($"Starting flame drawing.");
			drawingFlames = true;

			while(currentlyDrawingFlames)
			{
				flameCycle++;

				if (ShouldClientRender)
				{
					calcFlamesPosition();
					calcFlamesPosition();
					doFlamesDrawing();
				}

				await TaskDelayFactory.Create(25);
			}

			drawingFlames = false;
		}

		protected override void StopDrawingFlames()
		{
			currentlyDrawingFlames = false;
		}

		public async Task StartupCoroutine()
		{
			if(!wasClientStartupCalled)
				wasClientStartupCalled = true;
			else
				throw new InvalidOperationException($"Failed. Cannot call startup on Client multiple times.");

			drawLoadingText(20, "Starting up");

			if(clientRunning)
			{
				rsAlreadyLoaded = true;
				return;
			}

			clientRunning = true;
			bool validHost = true;
			String s = getDocumentBaseHost();
			if(s.EndsWith("jagex.com"))
				validHost = true;
			if(s.EndsWith("runescape.com"))
				validHost = true;
			if(s.EndsWith("192.168.1.2"))
				validHost = true;
			if(s.EndsWith("192.168.1.229"))
				validHost = true;
			if(s.EndsWith("192.168.1.228"))
				validHost = true;
			if(s.EndsWith("192.168.1.227"))
				validHost = true;
			if(s.EndsWith("192.168.1.226"))
				validHost = true;
			if(s.EndsWith("127.0.0.1"))
				validHost = true;
			if(!validHost)
			{
				genericLoadingError = true;
				return;
			}

			if(signlink.cache_dat != null)
			{
				for(int i = 0; i < 5; i++)
					caches[i] = new FileCache(signlink.cache_dat, signlink.cache_idx[i], i + 1);
			}

			connectServer();
			archiveTitle = requestArchive(1, "title screen", "title", expectedCRCs[1], 25);
			fontSmall = new GameFont("p11_full", archiveTitle, false);
			fontPlain = new GameFont("p12_full", archiveTitle, false);
			fontBold = new GameFont("b12_full", archiveTitle, false);
			drawLogo();
			loadTitleScreen();
			Archive archiveConfig = requestArchive(2, "config", "config", expectedCRCs[2], 30);
			Archive archiveTextures = requestArchive(6, "textures", "textures", expectedCRCs[6], 45);
			//Archive archiveWord = requestArchive(7, "chat system", "wordenc", expectedCRCs[7], 50);
			Archive archiveSounds = requestArchive(8, "sound effects", "sounds", expectedCRCs[8], 55);
			tileFlags = new byte[4, 104, 104];
			intGroundArray = CollectionUtilities.Create3DJaggedArray<int>(4, 105, 105);
			worldController = new WorldController(intGroundArray);
			for(int z = 0; z < 4; z++)
				currentCollisionMap[z] = new CollisionMap();

			minimapImage = new Sprite(512, 512);
			Archive archiveVersions = requestArchive(5, "update list", "versionlist", expectedCRCs[5], 60);
			drawLoadingText(60, "Connecting to update server");
			StartOnDemandFetcher(archiveVersions);
			Animation.init(onDemandFetcher.getAnimCount());
			Model.init(onDemandFetcher.fileCount(0), onDemandFetcher);

			songChanging = true;
			onDemandFetcher.request(2, nextSong);
			while(onDemandFetcher.immediateRequestCount() > 0)
			{
				processOnDemandQueue(false);

				await TaskDelayFactory.Create(1);

				if(onDemandFetcher.failedRequests > 3)
				{
					loadError();
					return;
				}
			}

			drawLoadingText(65, "Requesting animations");
			int fileRequestCount = onDemandFetcher.fileCount(1);
			for(int id = 0; id < fileRequestCount; id++)
				onDemandFetcher.request(1, id);

			if (!await ProcessAnimationsAsync(fileRequestCount)) return;

			drawLoadingText(70, "Requesting models");
			fileRequestCount = onDemandFetcher.fileCount(0);
			for(int id = 0; id < fileRequestCount; id++)
			{
				int modelId = onDemandFetcher.getModelId(id);
				if((modelId & 1) != 0)
					onDemandFetcher.request(0, id);
			}

			fileRequestCount = onDemandFetcher.immediateRequestCount();
			while(onDemandFetcher.immediateRequestCount() > 0)
			{
				int remaining = fileRequestCount - onDemandFetcher.immediateRequestCount();
				if(remaining > 0)
					drawLoadingText(70, "Loading models - " + (remaining * 100) / fileRequestCount + "%");
				processOnDemandQueue();

				await TaskDelayFactory.Create(1);
			}

			if(caches[0] != null)
			{
				drawLoadingText(75, "Requesting maps");
				onDemandFetcher.request(3, onDemandFetcher.getMapId(0, 47, 48));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(1, 47, 48));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(0, 48, 48));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(1, 48, 48));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(0, 49, 48));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(1, 49, 48));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(0, 47, 47));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(1, 47, 47));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(0, 48, 47));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(1, 48, 47));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(0, 48, 148));
				onDemandFetcher.request(3, onDemandFetcher.getMapId(1, 48, 148));
				fileRequestCount = onDemandFetcher.immediateRequestCount();
				while(onDemandFetcher.immediateRequestCount() > 0)
				{
					int remaining = fileRequestCount - onDemandFetcher.immediateRequestCount();
					if(remaining > 0)
						drawLoadingText(75, "Loading maps - " + (remaining * 100) / fileRequestCount + "%");
					processOnDemandQueue(false);

					await TaskDelayFactory.Create(1);
				}
			}

			fileRequestCount = onDemandFetcher.fileCount(0);
			for(int id = 0; id < fileRequestCount; id++)
			{
				int modelId = onDemandFetcher.getModelId(id);
				byte priority = 0;
				if((modelId & 8) != 0)
					priority = 10;
				else if((modelId & 0x20) != 0)
					priority = 9;
				else if((modelId & 0x10) != 0)
					priority = 8;
				else if((modelId & 0x40) != 0)
					priority = 7;
				else if((modelId & 0x80) != 0)
					priority = 6;
				else if((modelId & 2) != 0)
					priority = 5;
				else if((modelId & 4) != 0)
					priority = 4;
				if((modelId & 1) != 0)
					priority = 3;
				if(priority != 0)
					onDemandFetcher.setPriority(priority, 0, id);
			}

			//Don't need to even preload.
			await ((WebGLOnDemandFetcher)onDemandFetcher).preloadRegionsAsync(membersWorld);

			//Remove low memory check.
			int count = onDemandFetcher.fileCount(2);
			for(int id = 1; id < count; id++)
				if(onDemandFetcher.midiIdEqualsOne(id))
					onDemandFetcher.setPriority((byte)1, 2, id);

			//We don't unpack media here on WebGL because it
			//causes memory problems.

			drawLoadingText(83, "Unpacking textures");
			Rasterizer.unpackTextures(archiveTextures);
			Rasterizer.calculatePalette(0.80000000000000004D);
			Rasterizer.resetTextures();
			drawLoadingText(86, "Unpacking config");
			AnimationSequence.unpackConfig(archiveConfig);
			GameObjectDefinition.load(archiveConfig);
			FloorDefinition.load(archiveConfig);
			ItemDefinition.load(archiveConfig);
			EntityDefinition.load(archiveConfig);
			IdentityKit.load(archiveConfig);
			SpotAnimation.load(archiveConfig);
			Varp.load(archiveConfig);
			VarBit.load(archiveConfig);
			ItemDefinition.membersWorld = membersWorld;

			//Removed low memory check
			drawLoadingText(90, "Unpacking sounds");

			//Sound loading disabled in WebGL.
			byte[] soundData = archiveSounds.decompressFile("sounds.dat");
			Effect.load(new Default317Buffer(soundData));
		}

		private async Task<bool> ProcessAnimationsAsync(int fileRequestCount)
		{
			while (onDemandFetcher.immediateRequestCount() > 0)
			{
				int remaining = fileRequestCount - onDemandFetcher.immediateRequestCount();
				if (remaining > 0)
					drawLoadingText(65, "Loading animations - " + (remaining * 100) / fileRequestCount + "%");

				try
				{
					processOnDemandQueue();
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to process animation demand queue. Reason: {e.Message} \n Stack: {e.StackTrace}");
					signlink.reporterror($"Failed to process animation demand queue. Reason: {e.Message} \n Stack: {e.StackTrace}");
					throw;
				}

				//We can't force load more because if we constantly loop thw webgl main thread has no
				//way to load the resources we're waiting for.
				await TaskDelayFactory.Create(1);

				if (onDemandFetcher.failedRequests > 3)
				{
					loadError();
					return false;
				}
			}

			return true;
		}

		//Returns true when finished.
		public async Task HandlePendingInterfaceUnpackingAsync()
		{
			Archive archiveInterface = requestArchive(3, "interface", "interface", 0, 35);
			Archive archiveMedia = requestArchive(4, "2d graphics", "media", 0, 40);

			GameFont fontFancy = new GameFont("q8_full", archiveTitle, true);
			GameFont[] fonts = new GameFont[] { fontSmall, fontPlain, fontBold, fontFancy };

			drawLoadingText(95, "Unpacking interfaces");

			RSInterface.InitializeUnpackFields(archiveInterface);
			await TaskDelayFactory.Create(1);

			long currentMemory = GC.GetTotalMemory(false);
			//There are so many interfaces that we'd be waiting a minute if we unpacked them 1 per frame.
			for(int i = 0; i < int.MaxValue; i++)
			{
				//This will break us out eventually.
				if(!RSInterface.unpack(archiveInterface, fonts, archiveMedia, false))
					break;

				//If we've doubled the allocated memory, we should early break from this.
				if(GC.GetTotalMemory(false) > currentMemory * 1.5)
					await TaskDelayFactory.Create(1);

				if (i % 200 == 0)
					await TaskDelayFactory.Create(1);
			}

			await TaskDelayFactory.Create(1);
		}

		private async Task PostLoadEngineInitializationAsync()
		{
			drawLoadingText(100, "Preparing game engine");
			for(int _y = 0; _y < 33; _y++)
			{
				int firstXOfLine = 999;
				int lastXOfLine = 0;
				for(int _x = 0; _x < 34; _x++)
				{
					if(minimapBackgroundImage.pixels[_x + _y * minimapBackgroundImage.width] == 0)
					{
						if(firstXOfLine == 999)
							firstXOfLine = _x;
						continue;
					}

					if(firstXOfLine == 999)
						continue;
					lastXOfLine = _x;
					break;
				}

				compassHingeSize[_y] = firstXOfLine;
				compassWidthMap[_y] = lastXOfLine - firstXOfLine;
			}

			for(int _y = 5; _y < 156; _y++)
			{
				int min = 999;
				int max = 0;
				for(int _x = 25; _x < 172; _x++)
				{
					if(minimapBackgroundImage.pixels[_x + _y * minimapBackgroundImage.width] == 0
					   && (_x > 34 || _y > 34))
					{
						if(min == 999)
							min = _x;
						continue;
					}

					if(min == 999)
						continue;
					max = _x;
					break;
				}

				minimapLeft[_y - 5] = min - 25;
				minimapLineWidth[_y - 5] = max - min;
			}

			Rasterizer.setBounds(479, 96);
			chatboxLineOffsets = Rasterizer.lineOffsets;
			Rasterizer.setBounds(190, 261);
			sidebarOffsets = Rasterizer.lineOffsets;
			Rasterizer.setBounds(512, 334);
			viewportOffsets = Rasterizer.lineOffsets;

			int[] ai = new int[9];
			for(int i8 = 0; i8 < 9; i8++)
			{
				int k8 = 128 + i8 * 32 + 15;
				int l8 = 600 + k8 * 3;
				int i9 = Rasterizer.SINE[k8];
				ai[i8] = l8 * i9 >> 16;
			}

			WorldController.setupViewport(500, 800, 512, 334, ai);

			GameObject.clientInstance = this;
			EntityDefinition.clientInstance = this;

			//TODO: Disabled censor
			//Censor.load(archiveWord);
			mouseDetection = new MouseDetection(this, TaskDelayFactory);
			startRunnable(mouseDetection, 10);
		}

		internal async Task LoadMediaContentAsync()
		{
			Archive archiveMedia = requestArchive(4, "2d graphics", "media", 0, 40);
			Default317Buffer metadataBuffer = new Default317Buffer(archiveMedia.decompressFile("index.dat"));

			inventoryBackgroundImage = new IndexedImage(archiveMedia, "invback", 0, metadataBuffer);
			chatBackgroundImage = new IndexedImage(archiveMedia, "chatback", 0, metadataBuffer);
			minimapBackgroundImage = new IndexedImage(archiveMedia, "mapback", 0, metadataBuffer);
			backBase1Image = new IndexedImage(archiveMedia, "backbase1", 0, metadataBuffer);
			backBase2Image = new IndexedImage(archiveMedia, "backbase2", 0, metadataBuffer);
			backHmid1Image = new IndexedImage(archiveMedia, "backhmid1", 0, metadataBuffer);
			for(int icon = 0; icon < 13; icon++)
				sideIconImage[icon] = new IndexedImage(archiveMedia, "sideicons", icon, metadataBuffer);

			minimapCompassImage = new Sprite(archiveMedia, "compass", 0, metadataBuffer);
			minimapEdgeImage = new Sprite(archiveMedia, "mapedge", 0, metadataBuffer);
			minimapEdgeImage.trim();

			await TaskDelayFactory.Create(1);
			try
			{
				for(int i = 0; i < 100; i++)
					mapSceneImage[i] = new IndexedImage(archiveMedia, "mapscene", i, metadataBuffer);

				await TaskDelayFactory.Create(1);
			}
			catch(Exception _ex)
			{
				signlink.reporterror($"Unexpected Exception: {_ex.Message} \n\n Stack: {_ex.StackTrace}");
			}

			try
			{
				for(int i = 0; i < 100; i++)
					mapFunctionImage[i] = new Sprite(archiveMedia, "mapfunction", i, metadataBuffer);

				await TaskDelayFactory.Create(1);
			}
			catch(Exception _ex)
			{
				signlink.reporterror($"Unexpected Exception: {_ex.Message} \n\n Stack: {_ex.StackTrace}");
			}

			try
			{
				for(int i = 0; i < 20; i++)
					hitMarkImage[i] = new Sprite(archiveMedia, "hitmarks", i, metadataBuffer);

				await TaskDelayFactory.Create(1);
			}
			catch(Exception _ex)
			{
				signlink.reporterror($"Unexpected Exception: {_ex.Message} \n\n Stack: {_ex.StackTrace}");
			}

			try
			{
				for(int i = 0; i < 20; i++)
					headIcons[i] = new Sprite(archiveMedia, "headicons", i, metadataBuffer);

				await TaskDelayFactory.Create(1);
			}
			catch(Exception _ex)
			{
				signlink.reporterror($"Unexpected Exception: {_ex.Message} \n\n Stack: {_ex.StackTrace}");
				throw;
			}

			mapFlag = new Sprite(archiveMedia, "mapmarker", 0, metadataBuffer);
			mapMarker = new Sprite(archiveMedia, "mapmarker", 1, metadataBuffer);
			for(int i = 0; i < 8; i++)
				crosses[i] = new Sprite(archiveMedia, "cross", i, metadataBuffer);

			mapDotItem = new Sprite(archiveMedia, "mapdots", 0, metadataBuffer);
			mapDotNPC = new Sprite(archiveMedia, "mapdots", 1, metadataBuffer);
			mapDotPlayer = new Sprite(archiveMedia, "mapdots", 2, metadataBuffer);
			mapDotFriend = new Sprite(archiveMedia, "mapdots", 3, metadataBuffer);
			mapDotTeam = new Sprite(archiveMedia, "mapdots", 4, metadataBuffer);
			scrollBarUp = new IndexedImage(archiveMedia, "scrollbar", 0, metadataBuffer);
			scrollBarDown = new IndexedImage(archiveMedia, "scrollbar", 1, metadataBuffer);
			redStone1 = new IndexedImage(archiveMedia, "redstone1", 0, metadataBuffer);
			redStone2 = new IndexedImage(archiveMedia, "redstone2", 0, metadataBuffer);
			redStone3 = new IndexedImage(archiveMedia, "redstone3", 0, metadataBuffer);
			redStone1_2 = new IndexedImage(archiveMedia, "redstone1", 0, metadataBuffer);
			redStone1_2.flipHorizontally();
			redStone2_2 = new IndexedImage(archiveMedia, "redstone2", 0, metadataBuffer);
			redStone2_2.flipHorizontally();
			redStone1_3 = new IndexedImage(archiveMedia, "redstone1", 0, metadataBuffer);
			redStone1_3.flipVertically();
			redStone2_3 = new IndexedImage(archiveMedia, "redstone2", 0, metadataBuffer);
			redStone2_3.flipVertically();
			redStone3_2 = new IndexedImage(archiveMedia, "redstone3", 0, metadataBuffer);
			redStone3_2.flipVertically();
			redStone1_4 = new IndexedImage(archiveMedia, "redstone1", 0, metadataBuffer);
			redStone1_4.flipHorizontally();
			redStone1_4.flipVertically();
			redStone2_4 = new IndexedImage(archiveMedia, "redstone2", 0, metadataBuffer);
			redStone2_4.flipHorizontally();
			redStone2_4.flipVertically();
			for(int i = 0; i < 2; i++)
				modIcons[i] = new IndexedImage(archiveMedia, "mod_icons", i, metadataBuffer);

			await TaskDelayFactory.Create(1);

			Sprite sprite = new Sprite(archiveMedia, "backleft1", 0, metadataBuffer);
			backLeftIP1 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backLeftIP1));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backleft2", 0, metadataBuffer);
			backLeftIP2 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backLeftIP2));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backright1", 0, metadataBuffer);
			backRightIP1 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backRightIP1));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backright2", 0, metadataBuffer);
			backRightIP2 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backRightIP2));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backtop1", 0, metadataBuffer);
			backTopIP1 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backTopIP1));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backvmid1", 0, metadataBuffer);
			backVmidIP1 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backVmidIP1));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backvmid2", 0, metadataBuffer);
			backVmidIP2 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backVmidIP2));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backvmid3", 0, metadataBuffer);
			backVmidIP3 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backVmidIP3));
			sprite.drawInverse(0, 0);
			sprite = new Sprite(archiveMedia, "backhmid2", 0, metadataBuffer);
			backVmidIP2_2 = CreateNewImageProducer(sprite.width, sprite.height, nameof(backVmidIP2_2));
			sprite.drawInverse(0, 0);

			await TaskDelayFactory.Create(1);

			int randomRed = (int)(StaticRandomGenerator.Next() * 21D) - 10;
			int randomGreen = (int)(StaticRandomGenerator.Next() * 21D) - 10;
			int randomBlue = (int)(StaticRandomGenerator.Next() * 21D) - 10;
			int randomColour = (int)(StaticRandomGenerator.Next() * 41D) - 20;
			for(int i = 0; i < 100; i++)
			{
				if(mapFunctionImage[i] != null)
					mapFunctionImage[i].adjustRGB(randomRed + randomColour, randomGreen + randomColour,
						randomBlue + randomColour);
				if(mapSceneImage[i] != null)
					mapSceneImage[i].mixPalette(randomRed + randomColour, randomGreen + randomColour,
						randomBlue + randomColour);
			}
		}

		private async Task AppletRunCoroutine()
		{
			Console.WriteLine($"Loading.");
			drawLoadingText(0, "Loading...");

			await StartupCoroutine();
			await LoadMediaContentAsync();

			await HandlePendingInterfaceUnpackingAsync();

			await PostLoadEngineInitializationAsync();

			drawLoadingText(100, "Finished Loading");

			int opos = 0;
			int ratio = 256;
			int delay = 1;
			int count = 0;
			int intex = 0;
			for(int otim = 0; otim < 10; otim++)
				otims[otim] = TimeService.CurrentTimeInMilliseconds();

			while(gameState >= 0)
			{
				if(gameState > 0)
				{
					gameState--;
					if(gameState == 0)
					{
						exit();
						return;
					}
				}

				int i2 = ratio;
				int j2 = delay;
				ratio = 300;
				delay = 1;
				long currentTime = TimeService.CurrentTimeInMilliseconds();
				if(otims[opos] == 0L)
				{
					ratio = i2;
					delay = j2;
				}
				else if(currentTime > otims[opos])
					ratio = (int)(2560 * delayTime / (currentTime - otims[opos]));

				if(ratio < 25)
					ratio = 25;
				if(ratio > 256)
				{
					ratio = 256;
					delay = (int)(delayTime - (currentTime - otims[opos]) / 10L);
				}

				if(delay > delayTime)
					delay = delayTime;
				otims[opos] = currentTime;
				opos = (opos + 1) % 10;
				if(delay > 1)
				{
					for(int otim = 0; otim < 10; otim++)
						if(otims[otim] != 0L)
							otims[otim] += delay;

				}

				if(delay < minDelay)
					delay = minDelay;

				//Always await, mindelay is at least one ms and that'll be 1 frame minimum for us
				//if we never yield frame time then critical things in WebGL enviroment can't run
				await TaskDelayFactory.Create(delay);

				for(; count < 256; count += ratio)
				{
					clickType = eventMouseButton;
					clickX = eventClickX;
					clickY = eventClickY;
					clickTime = eventClickTime;
					eventMouseButton = 0;
					await processGameLoop();
					readIndex = writeIndex;
				}

				count &= 0xff;
				if(delayTime > 0)
					fps = (1000 * ratio) / (delayTime * 256);
				processDrawing();
				if(debugRequested)
				{
					Console.WriteLine("ntime:" + currentTime);
					for(int i = 0; i < 10; i++)
					{
						int otim = ((opos - i - 1) + 20) % 10;
						Console.WriteLine("otim" + otim + ":" + otims[otim]);
					}

					Console.WriteLine("fps:" + fps + " ratio:" + ratio + " count:" + count);
					Console.WriteLine("del:" + delay + " deltime:" + delayTime + " mindel:" + minDelay);
					Console.WriteLine("intex:" + intex + " opos:" + opos);
					debugRequested = false;
					intex = 0;
				}
			}

			if(gameState == -1)
				exit();
		}
	}
}
