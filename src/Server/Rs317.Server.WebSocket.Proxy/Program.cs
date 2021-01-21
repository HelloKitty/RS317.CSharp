using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fleck;

namespace Rs317.Sharp
{
	class Program
	{
		static async Task Main(string[] args)
		{
			int mainPort = 43594;

			//https://stackoverflow.com/questions/4926676/mono-https-webrequest-fails-with-the-authentication-or-decryption-has-failed
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
			ServicePointManager.CheckCertificateRevocationList = false;

			X509Certificate2 cert = new X509Certificate2(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Certs", "scape.vrmmo.org.pfx")), "test");

			if(!cert.HasPrivateKey)
				throw new InvalidOperationException($"Cannot use websocket TLS cert without private key.");

			//Connecting to Server: 127.0.0.1:43594
			WebSocketServer server = new WebSocketServer($"wss://0.0.0.0:{mainPort + 1}");
			ConcurrentDictionary<Guid, TcpClient> TcpClientDictionary = new ConcurrentDictionary<Guid, TcpClient>();
			server.ListenerSocket.NoDelay = true;
			server.Certificate = cert;
			server.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11;
			server.Start(socket =>
			{
				socket.OnOpen = async () =>
				{
					Console.WriteLine($"Open! Id: {socket.ConnectionInfo.Id}");
					TcpClient tcpClient = new TcpClient();
					TcpClientDictionary.TryAdd(socket.ConnectionInfo.Id, tcpClient);
					Task.Factory.StartNew(async () =>
					{
						await OnConnectionOpenAsync(tcpClient, mainPort, TcpClientDictionary, socket);
					});
				};

				socket.OnError = e =>
				{
					Console.WriteLine($"Close! Error: {e.Message}");
					socket.Close();
				};

				socket.OnClose = () => { OnConnectionClose(TcpClientDictionary, socket); };

				socket.OnBinary = async (byte[] bytes) => { await OnBinaryMessage(bytes, TcpClientDictionary, socket); };
			});

			if(!server.IsSecure)
				throw new InvalidOperationException($"Cannot support WS from WebGL browser. Must be a secure WSS server.");

			while (true)
				await Task.Delay(10000);
		}

		private static async Task OnBinaryMessage(byte[] bytes, ConcurrentDictionary<Guid, TcpClient> TcpClientDictionary, IWebSocketConnection socket)
		{
			Console.WriteLine($"Client Sent Bytes: {bytes.Length}");

			TcpClient client = TcpClientDictionary[socket.ConnectionInfo.Id];
			int waitCount = 0;
			while (!client.Connected && waitCount < 20)
			{
				await Task.Delay(150);
				waitCount++;
			}

			await client.GetStream().WriteAsync(bytes);
		}

		private static void OnConnectionClose(ConcurrentDictionary<Guid, TcpClient> TcpClientDictionary, IWebSocketConnection socket)
		{
			Console.WriteLine("Close!");
			TcpClientDictionary[socket.ConnectionInfo.Id].GetStream().Close();
			TcpClientDictionary[socket.ConnectionInfo.Id].Close();
			TcpClientDictionary.TryRemove(socket.ConnectionInfo.Id, out var val);
		}

		private static async Task OnConnectionOpenAsync(TcpClient tcpClient, int mainPort, ConcurrentDictionary<Guid, TcpClient> TcpClientDictionary, IWebSocketConnection socket)
		{
			try
			{
				//await tcpClient.ConnectAsync("72.190.177.214", 5006);
				await tcpClient.ConnectAsync("127.0.0.1", mainPort);

				if(!tcpClient.Connected)
					throw new InvalidOperationException($"Failed to connect to RS server.");

				NetworkStream stream = tcpClient.GetStream();
				byte[] buffer = new byte[5000];
				while (tcpClient.Connected && TcpClientDictionary.ContainsKey(socket.ConnectionInfo.Id))
				{
					int byteCountRead = await stream.ReadAsync(buffer);

					if (byteCountRead == 0)
						continue;

					Console.WriteLine($"Server Sent Bytes: {byteCountRead}");

					//TODO: Optimize this.
					await socket.Send(new ArraySegment<byte>(buffer, 0, byteCountRead).ToArray());
				}

				Console.WriteLine($"Server proxy listener stopped.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Listener failed. Reason: {e}");
				throw;
			}
		}
	}
}
