using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rs317.Sharp
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			try
			{
				Console.WriteLine($"RS2 user client - release #{317} using Rs317.Sharp by Glader");
				await StartClient(0, 0, true);
			}
			catch(Exception exception)
			{
				throw new InvalidOperationException($"Erorr: {exception.Message} \n\n Stack: {exception.StackTrace}");
			}
		}

		private static async Task StartClient(int localWorldId, short portOffset, bool membersWorld)
		{
			Application.SetCompatibleTextRenderingDefault(false);

			Task clientRunningAwaitable = signlink.startpriv("127.0.0.1");
			ClientConfiguration configuration = new ClientConfiguration(localWorldId, (short) (portOffset + 1), membersWorld);

			RsWinForm windowsFormApplication = new RsWinForm(765, 503);
			RsWinFormsClient client1 = new RsWinFormsClient(configuration, windowsFormApplication.CreateGraphics());
			windowsFormApplication.RegisterInputSubscriber(client1);
			client1.createClientFrame(765, 503);

			Application.Run(windowsFormApplication);

			await clientRunningAwaitable
				.ConfigureAwait(false);
		}
	}
}
