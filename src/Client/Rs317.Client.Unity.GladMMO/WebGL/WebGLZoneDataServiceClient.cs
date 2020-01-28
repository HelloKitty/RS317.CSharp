using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GladMMO;
using Newtonsoft.Json;
using Refit;
using UnityEngine;
using UnityEngine.Networking;

namespace Rs317.Sharp
{
	public sealed class WebGLZoneDataServiceClient : IZoneDataService
	{
		private string BaseUrl { get; }

		public WebGLZoneDataServiceClient()
		{
			BaseUrl = ZoneServerServiceDependencyAutofacModule.PrecomputedEndpoint;
		}

		public async Task<ResponseModel<ZoneWorldConfigurationResponse, ZoneWorldConfigurationResponseCode>> GetZoneWorldConfigurationAsync(int zoneId)
		{
			//UnityWebRequest unityRequest = new UnityWebRequest($"{BaseUrl}api/", request.Method.Method);
			throw new NotImplementedException();
		}

		public async Task<ResolveServiceEndpointResponse> GetZoneConnectionEndpointAsync(int zoneId)
		{
			throw new NotImplementedException();
		}

		//[Get("/api/ZoneData/default/endpoint")]
		//[Headers(new[] { "Cache-Control: max-age=300" })]
		public async Task<ResponseModel<ZoneConnectionEndpointResponse, ResolveServiceEndpointResponseCode>> GetAnyZoneConnectionEndpointAsync()
		{
			string uri = $"{BaseUrl}api/ZoneData/default/endpoint";
			Console.WriteLine($"Sending GET to {uri}");

			UnityWebRequest unityRequest = new UnityWebRequest(uri, HttpMethod.Get.Method);
			unityRequest.downloadHandler = new DownloadHandlerBuffer();

			await unityRequest.SendWebRequest();

			if(unityRequest.isHttpError)
				return new ResponseModel<ZoneConnectionEndpointResponse, ResolveServiceEndpointResponseCode>(ResolveServiceEndpointResponseCode.GeneralRequestError);

			//TODO: Just debug logging.
			try
			{
				Debug.Log($"Manual WebGL WebRequest Content: {unityRequest.downloadHandler.text}");
			}
			catch(Exception e)
			{
				Console.WriteLine($"Failed to read {nameof(UnityWebRequest)} response data. Reason: {e}");
				throw;
			}
			
			return JsonConvert.DeserializeObject<ResponseModel<ZoneConnectionEndpointResponse, ResolveServiceEndpointResponseCode>>(unityRequest.downloadHandler.text);
		}
	}
}
