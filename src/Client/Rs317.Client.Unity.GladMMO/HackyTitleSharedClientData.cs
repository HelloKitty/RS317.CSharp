using System;
using System.Collections.Generic;
using System.Text;
using GladMMO;
using GladNet;
using Rs317.GladMMO;

namespace Rs317.Sharp
{
	//Put almost nothing here, it's here ONLY to help with auth and afew shared state objects
	//between the two different GladMMO clients.
	public class HackyTitleSharedClientData
	{
		public static HackyTitleSharedClientData Instance { get; set; }

		public IAuthenticationService AuthService { get; }

		public AuthenticateOnLoginButtonClickEventListener AuthButtonListener { get; }

		public HackyTitleSharedClientData([NotNull] IAuthenticationService authService, [NotNull] AuthenticateOnLoginButtonClickEventListener authButtonListener)
		{
			AuthService = authService ?? throw new ArgumentNullException(nameof(authService));
			AuthButtonListener = authButtonListener ?? throw new ArgumentNullException(nameof(authButtonListener));
		}
	}

	public class HackyInstanceSharedClientData
	{
		public INetworkSerializationService SerializerService { get; }

		public static HackyInstanceSharedClientData Instance { get; set; }

		public HackyInstanceSharedClientData([NotNull] INetworkSerializationService serializerService)
		{
			SerializerService = serializerService ?? throw new ArgumentNullException(nameof(serializerService));
		}
	}
}
