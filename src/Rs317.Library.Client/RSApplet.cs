using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Rs317.Sharp
{
	public abstract class RSApplet<TGraphicsType> : IRunnable, IMouseInputQueryable, IInputCallbackSubscriber //Instead of that Java stuff we just implement Windows form.
	{
		protected int gameState;

		protected int delayTime;

		protected int minDelay;

		protected long[] otims;

		public int fps { get; set; }

		public bool debugRequested { get; set; }

		protected int width { get; set; }

		protected int height { get; set; }

		protected IRSGraphicsProvider<TGraphicsType> gameGraphics { get; set; }

		protected BaseRsImageProducer<TGraphicsType> fullGameScreen { get; set; }

		private bool clearScreen;

		protected bool awtFocus { get; set; }

		protected int idleTime { get; set; }

		protected int mouseButton { get; private set; }

		//IMouseInputQueryable
		public int mouseX { get; private set; }

		public int mouseY { get; private set; }

		protected int eventMouseButton;

		protected int eventClickX;

		protected int eventClickY;

		protected long eventClickTime;

		public int clickType { get; protected set; }

		protected int clickX { get; set; }

		protected int clickY { get; set; }

		protected long clickTime { get; set; }

		protected int[] keyStatus { get; private set; }

		private int[] inputBuffer;

		protected int readIndex;

		protected int writeIndex;

		protected IRunnableStarter RunnableStarterStrategy { get; }

		/// <summary>
		/// Indicates the thread id of the main engine thread.
		/// </summary>
		public int GameEngineThreadId { get; private set; }

		/// <summary>
		/// Indicates if the caller is on the main game thread.
		/// </summary>
		public bool IsOnGameEngineThread => GameEngineThreadId == Thread.CurrentThread.ManagedThreadId;

		private object InputSyncObj { get; } = new object();

		protected RSApplet(IRunnableStarter runnableStarterStrategy)
		{
			RunnableStarterStrategy = runnableStarterStrategy ?? throw new ArgumentNullException(nameof(runnableStarterStrategy));
			delayTime = 20;
			minDelay = 1;
			otims = new long[10];
			debugRequested = false;
			clearScreen = true;
			awtFocus = true;
			keyStatus = new int[128];
			inputBuffer = new int[128];
		}

		protected virtual void cleanUpForQuit()
		{
			
		}

		public virtual void createClientFrame(int width, int height)
		{
			this.width = width;
			this.height = height;

			signlink.applet = this;
			gameGraphics = CreateGraphicsProvider();
			fullGameScreen = CreateNewImageProducer(this.width, height, nameof(fullGameScreen));
			RunnableStarterStrategy.StartRunnable(this, 1);
		}

		protected abstract IRSGraphicsProvider<TGraphicsType> CreateGraphicsProvider();

		public void mousePressed(object sender, RsMouseInputEventArgs e)
		{
			lock (InputSyncObj)
			{
				int i = e.ScreenCoordX;
				int j = e.ScreenCoordY;

				idleTime = 0;
				eventClickX = i;
				eventClickY = j;
				eventClickTime = TimeService.CurrentTimeInMilliseconds();
				if(e.IsRightClick)
				{
					eventMouseButton = 2;
					mouseButton = 2;
				}
				else
				{
					eventMouseButton = 1;
					mouseButton = 1;
				}
			}
		}

		public void mouseReleased(object sender, RsMouseInputEventArgs e)
		{
			lock (InputSyncObj)
			{
				idleTime = 0;
				mouseButton = 0;
			}
		}

		public void mouseEntered(object sender, EventArgs e)
		{
		}

		public void mouseExited(object sender, EventArgs e)
		{
			lock (InputSyncObj)
			{
				idleTime = 0;
				mouseX = -1;
				mouseY = -1;
			}
		}

		public void mouseDragged(object sender, RsMousePositionChangeEventArgs e)
		{
			lock (InputSyncObj)
			{
				int i = e.ScreenCoordX;
				int j = e.ScreenCoordY;

				idleTime = 0;
				mouseX = i;
				mouseY = j;
			}
		}

		public void mouseWheelDragged(object sender, RsMousePositionChangeEventArgs e)
		{
			lock (InputSyncObj)
			{
				int mouseXDiff = mouseX - e.ScreenCoordX;
				int mouseYDiff = mouseY - e.ScreenCoordY;

				OnMouseWheelDragged(mouseXDiff, mouseYDiff);
			}
		}

		public void mouseWheelScroll(object sender, float scrollDelta)
		{
			lock (InputSyncObj)
			{
				OnMouseWheelScrolled(scrollDelta);
			}
		}

		protected abstract void OnMouseWheelDragged(int mouseXDiff, int mouseYDiff);

		protected abstract void OnMouseWheelScrolled(float scrollDelta);

		public void mouseMoved(object sender, RsMousePositionChangeEventArgs e)
		{
			lock (InputSyncObj)
			{
				int i = e.ScreenCoordX;
				int j = e.ScreenCoordY;

				idleTime = 0;
				mouseX = i;
				mouseY = j;
			}
		}

		protected virtual void drawLoadingText(int percentage, String s)
		{

		}

		public void keyPressed(object sender, RsKeyEventArgs e)
		{
			idleTime = 0;
			int i = (int) e.RsKeyCode;

			if (i > 0 && i < 128)
				keyStatus[i] = 1;
			if (i > 4)
			{
				inputBuffer[writeIndex] = i;
				writeIndex = writeIndex + 1 & 0x7f;
			}
		}

		public void keyReleased(object sender, RsKeyEventArgs e)
		{
			idleTime = 0;
			int keyCode = e.RsKeyCode;
			int keyChar = e.RsKeyCode;
			if(keyCode < 30)
			{
				keyChar = 0;
			}
			else if(keyCode == 37) // Left
			{
				keyChar = 1;
			}
			else if(keyCode == 39) // Right
			{
				keyChar = 2;
			}
			else if(keyCode == 38) // Up
			{
				keyChar = 3;
			}
			else if(keyCode == 40) // Down
			{
				keyChar = 4;
			}
			else if(keyCode == 17) // CTRL
			{
				keyChar = 5;
			}
			else if(keyCode == 8) // Backspace
			{
				keyChar = 8;
			}
			else if(keyCode == 127) // Delete
			{
				keyChar = 8;
			}
			else if(keyCode == 9) // Meant to be tab but doesn't work
			{
				keyChar = 9;
			}
			else if(keyCode == 10) // Enter / return
			{
				keyChar = 10;
			}
			else if(keyCode >= 112 && keyCode <= 123) // F keys
			{
				keyChar = (1008 + keyCode) - 112;
			}
			else if(keyCode == 36) // Home
			{
				keyChar = 1000;
			}
			else if(keyCode == 35) // End
			{
				keyChar = 1001;
			}
			else if(keyCode == 33) // Page up
			{
				keyChar = 1002;
			}
			else if(keyCode == 34) // Page down
			{
				keyChar = 1003;
			}
			else if(keyChar > 0 && keyChar < 128)
			{
				keyStatus[keyChar] = 1;
			}

			if(keyChar > 0 && keyChar < (char)200)
			{
				this.keyStatus[keyChar] = 0;
			}
		}

		public void keyTyped(object sender, RsKeyEventArgs e)
		{
			idleTime = 0;
			int i = (int) e.KeyChar;
			if (e.KeyChar >= 32 && e.KeyChar <= 126)
			{
				inputBuffer[writeIndex] = i;
				writeIndex = writeIndex + 1 & 0x7f;
			}
		}

		public int readChar(int dummy)
		{
			while (dummy >= 0)
			{
				for (int j = 1; j > 0; j++) ;
			}

			int k = -1;
			if (writeIndex != readIndex)
			{
				k = inputBuffer[readIndex];
				readIndex = readIndex + 1 & 0x7f;
			}

			return k;
		}

		public void focusGained(object sender, EventArgs e)
		{
			awtFocus = true;
			clearScreen = true;
			raiseWelcomeScreen();
		}

		public void focusLost(object sender, EventArgs e)
		{
			awtFocus = false;
			for (int i = 0; i < 128; i++)
				keyStatus[i] = 0;
		}

		public virtual async Task processGameLoop()
		{
		}

		public virtual void processDrawing()
		{
		}

		public virtual void raiseWelcomeScreen()
		{
		}

		public virtual void redraw()
		{
		}

		protected void exit()
		{
			gameState = -2;
			cleanUpForQuit();
		}

		public virtual async Task run()
		{
			await StartGameEngineLoop();
		}

		protected async Task StartGameEngineLoop()
		{
			try
			{
				GameEngineThreadId = Thread.CurrentThread.ManagedThreadId;
				Console.WriteLine($"Loading.");
				drawLoadingText(0, "Loading...");
				startUp();
				int opos = 0;
				int ratio = 256;
				int delay = 1;
				int count = 0;
				int intex = 0;
				for (int otim = 0; otim < 10; otim++)
					otims[otim] = TimeService.CurrentTimeInMilliseconds();

				while (gameState >= 0)
				{
					if (gameState > 0)
					{
						gameState--;
						if (gameState == 0)
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
					if (otims[opos] == 0L)
					{
						ratio = i2;
						delay = j2;
					}
					else if (currentTime > otims[opos])
						ratio = (int) (2560 * delayTime / (currentTime - otims[opos]));

					if (ratio < 25)
						ratio = 25;
					if (ratio > 256)
					{
						ratio = 256;
						delay = (int) (delayTime - (currentTime - otims[opos]) / 10L);
					}

					if (delay > delayTime)
						delay = delayTime;
					otims[opos] = currentTime;
					opos = (opos + 1) % 10;
					if (delay > 1)
					{
						for (int otim = 0; otim < 10; otim++)
							if (otims[otim] != 0L)
								otims[otim] += delay;
					}

					if (delay < minDelay)
						delay = minDelay;
					try
					{
						Thread.Sleep(delay);
					}
					catch (Exception _ex) //TODO: Log
					{
						intex++;
					}

					for (; count < 256; count += ratio)
					{
						//Important to sync input copy otherwise we
						//have a race condition in multi-threaded input
						lock (InputSyncObj)
						{
							clickType = eventMouseButton;
							clickX = eventClickX;
							clickY = eventClickY;
							clickTime = eventClickTime;
							eventMouseButton = 0;
						}

						await processGameLoop();
						readIndex = writeIndex;
					}

					count &= 0xff;
					if (delayTime > 0)
						fps = (1000 * ratio) / (delayTime * 256);
					processDrawing();
					if (debugRequested)
					{
						Console.WriteLine("ntime:" + currentTime);
						for (int i = 0; i < 10; i++)
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

				if (gameState == -1)
					exit();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Encountered Exception in Game Run. Reason: {e.ToString()}");
				throw;
			}
		}

		void setFrameRate(int frameRate)
		{
			delayTime = 1000 / frameRate;
		}

		public virtual void startUp()
		{

		}

		protected abstract BaseRsImageProducer<TGraphicsType> CreateNewImageProducer(int xSize, int ySize, string producerName);
	}
}
