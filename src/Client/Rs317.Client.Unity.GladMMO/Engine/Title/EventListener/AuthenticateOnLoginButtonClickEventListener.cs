using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Common.Logging;
using Glader.Essentials;
using GladMMO;
using Refit;

namespace Rs317.GladMMO
{
	[AdditionalRegisterationAs(typeof(AuthenticateOnLoginButtonClickEventListener))]
	[AdditionalRegisterationAs(typeof(IAuthenticationResultRecievedEventSubscribable))]
	[SceneTypeCreateGladMMO(GameSceneType.TitleScreen)]
	public sealed class AuthenticateOnLoginButtonClickEventListener : BaseSingleEventListenerInitializable<ILoginButtonClickedEventSubscribable>, IAuthenticationResultRecievedEventSubscribable
	{
		private IAuthenticationService AuthService { get; }

		private IUIText UserNameField { get; }

		private IUIText PasswordField { get; }

		private ILog Logger { get; }

		private IGameContextEventQueueable GameQueueable { get; }

		private IAuthTokenRepository AuthTokenRepository { get; }

		public event EventHandler<AuthenticationResultEventArgs> OnAuthenticationResultRecieved;

		//This helps us track auth state so we don't authenticate multiple times
		//if they click multiple times.
		private bool isAuthenticationRequestPending = false;

		public AuthenticateOnLoginButtonClickEventListener(ILoginButtonClickedEventSubscribable subscriptionService, 
			IAuthenticationService authService,
			[KeyFilter(ClientUIDependencyKey.UserName)] IUIText userNameField,
			[KeyFilter(ClientUIDependencyKey.Password)] IUIText passwordField, 
			ILog logger, 
			IGameContextEventQueueable gameQueueable,
			[NotNull] IAuthTokenRepository authTokenRepository) 
			: base(subscriptionService)
		{
			AuthService = authService ?? throw new ArgumentNullException(nameof(authService));
			UserNameField = userNameField;
			PasswordField = passwordField;
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			GameQueueable = gameQueueable ?? throw new ArgumentNullException(nameof(gameQueueable));
			AuthTokenRepository = authTokenRepository ?? throw new ArgumentNullException(nameof(authTokenRepository));
		}

		protected override void OnEventFired(object source, EventArgs args)
		{
			Console.WriteLine($"Auth: {UserNameField.Text}:{PasswordField.Text} IsAlreadyPending: {isAuthenticationRequestPending}");
			
			if (isAuthenticationRequestPending)
				return;

			isAuthenticationRequestPending = true;

			Task.Factory.StartNew(async () =>
			{
				PlayerAccountJWTModel jwtModel = null;

				//TODO: Validate username and password
				//We can't do error code supression with refit anymore, so we have to do this crap.
				try
				{
					jwtModel = await AuthService.TryAuthenticate(new AuthenticationRequestModel(UserNameField.Text, PasswordField.Text));
				}
				catch(ApiException e)
				{
					jwtModel = e.GetContentAs<PlayerAccountJWTModel>();

					if(Logger.IsErrorEnabled)
						Logger.Error($"Encountered Auth Error: {e.Message}");
				}
				catch(Exception e)
				{
					if(Logger.IsErrorEnabled)
						Logger.Error($"Encountered Auth Error: {e.Message}\n\nStack: {e.StackTrace}");
				}
				finally
				{
					try
					{
						if (Logger.IsDebugEnabled)
							Logger.Debug($"Auth Response for User: {UserNameField.Text} Result: {jwtModel?.isTokenValid} OptionalError: {jwtModel?.Error} OptionalErrorDescription: {jwtModel?.ErrorDescription}");

						if (jwtModel != null && jwtModel.isTokenValid)
						{
							UpdatedTokenRepository(jwtModel);

							GameQueueable.Enqueue(() =>
							{
								//Even if it's null, we should broadcast the event.
								OnAuthenticationResultRecieved?.Invoke(this, new AuthenticationResultEventArgs(jwtModel));
							});
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
						throw;
					}
					finally
					{
						//Make this this ALWAYS happens.
						isAuthenticationRequestPending = false;
					}
				}
			});
		}

		public void UpdatedTokenRepository(PlayerAccountJWTModel jwtModel)
		{
			AuthTokenRepository.Update(jwtModel.AccessToken);
		}

		public void DispatchAuthenticationResult(PlayerAccountJWTModel model)
		{
			OnAuthenticationResultRecieved?.Invoke(this, new AuthenticationResultEventArgs(model));
		}
	}
}
