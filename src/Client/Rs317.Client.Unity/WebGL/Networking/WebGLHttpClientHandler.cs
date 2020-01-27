using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Rs317.Sharp
{
	public sealed class WebGLHttpClientHandler : HttpClientHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			UnityWebRequest unityRequest = new UnityWebRequest(request.RequestUri, request.Method.Method);

			try
			{
				if(request.Method != HttpMethod.Get)
				{
					unityRequest.uploadHandler = new UploadHandlerRaw(await request.Content.ReadAsByteArrayAsync());
					unityRequest.downloadHandler = new DownloadHandlerBuffer();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to grab contents of {nameof(HttpRequestMessage)} to copy to {nameof(UnityWebRequest)}. Reason: {e}");
				throw;
			}

			try
			{
				unityRequest.SetRequestHeader("Content-Type", request.Content.Headers.ContentType.MediaType);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to grab Content-Type header of {nameof(HttpRequestMessage)} to copy to {nameof(UnityWebRequest)}. Reason: {e}");
				throw;
			}


			try
			{
				await unityRequest.SendWebRequest();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to send adapted {nameof(UnityWebRequest)}. Reason: {e}");
				throw;
			}

			if(!unityRequest.isDone)
				throw new InvalidOperationException($"{nameof(UnityWebRequest)} finished awaiting in invalid state. Not done.");

			//TODO: Just debug logging.
			try
			{
				Debug.Log(unityRequest.downloadHandler.text);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Failed to read {nameof(UnityWebRequest)} response data. Reason: {e}");
				throw;
			}

			return new HttpResponseMessage((HttpStatusCode) unityRequest.responseCode) { Content = GenerateHttpContentFromUnityWebRequest(unityRequest) };
		}

		private static HttpContent GenerateHttpContentFromUnityWebRequest(UnityWebRequest unityRequest)
		{
			ByteArrayContent arrayContent = new ByteArrayContent(unityRequest.downloadHandler.data);
			Debug.Log($"ResponseHeader: {unityRequest.GetResponseHeader("Content-Type")}");
			arrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(unityRequest.GetResponseHeader("Content-Type"));

			return arrayContent;
		}
	}
}
