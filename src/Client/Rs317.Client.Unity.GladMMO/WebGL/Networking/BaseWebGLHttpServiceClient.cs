using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GladMMO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Rs317.Sharp
{
	public abstract class BaseWebGLHttpServiceClient
	{
		protected string BaseUrl { get; }

		protected BaseWebGLHttpServiceClient([JetBrains.Annotations.NotNull] string baseUrl)
		{
			if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentException("Value cannot be null or empty.", nameof(baseUrl));

			BaseUrl = baseUrl;
		}

		public async Task<ResponseModel<TResponseType, TResponseCodeType>> SendRequestWithResponseModelAsync<TResponseType, TResponseCodeType>(string relativeUrl, HttpMethod actionType) 
			where TResponseType : class 
			where TResponseCodeType : Enum
		{
			var response = await SendRequestAsync<ResponseModel<TResponseType, TResponseCodeType>>(relativeUrl, actionType);

			if (response == null)
				return new ResponseModel<TResponseType, TResponseCodeType>(default(TResponseCodeType));

			return response;
		}

		public async Task<TResponseType> SendRequestAsync<TResponseType>(string relativeUrl, HttpMethod actionType)
		{
			//TODO: Find a way to cache this so we don't need to compute it every time.
			string uri = $"{BaseUrl}{relativeUrl}";

			UnityWebRequest unityRequest = new UnityWebRequest(uri, actionType.Method);
			unityRequest.downloadHandler = new DownloadHandlerBuffer();

			await unityRequest.SendWebRequest();

			if (unityRequest.isHttpError)
				return default(TResponseType);

			return JsonConvert.DeserializeObject<TResponseType>(unityRequest.downloadHandler.text);
		}
	}
}
