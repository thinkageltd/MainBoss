using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database.Service {
	#region OAuth2 Manager
	public abstract class OAuth2ManagerBase {
		public abstract string GetAccessTokenUsingCertificate(string emailAddress, string certificateName, string clientID);
		public abstract string GetAccessTokenUsingClientSecret(string emailAddress, string clientSecret, string clientID);
	}
	public class OAuth2ManagerAzure : OAuth2ManagerBase {

		public string Authority(string emailAddress) {
			string[] parts = emailAddress.Split('@');
			return Strings.IFormat(Instance, parts[1]);
		}
		private string Instance { get; } = "https://login.microsoftonline.com/{0}";
		// the following are for testing and reflect Windview Software Solutions TenantID and ApplicationClientId registered as an AppRegistration with the client secret
		//
		// and the appropriate Service Principal defined in Exchange Online (per https://learn.microsoft.com/en-us/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth#use-client-credentials-grant-flow-to-authenticate-imap-and-pop-connections)
		// and mailbox full access permissions granted to the Service Principal for the username configured as the receiver of requests.

		//following are for testing
		// not required but documented only to validate entries in Azure Active directory to make sure we are using the proper 'secret'
		//		private string ClientSecretDescription { get; } = "MainBoss Service 4 Secret";
		//		private string ClientSecretID { get; } = "20dead10-51b9-4e90-819e-1d512858398f";

		// Certificate requirements are at https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-self-signed-certificate
		private static X509Certificate2 GetCertificateFromStore(string certName) {
			// Get the certificate store for the current user.
			X509Store store = new X509Store(StoreLocation.CurrentUser);
			try {
				store.Open(OpenFlags.ReadOnly);

				// Place all certificates in an X509Certificate2Collection object.
				X509Certificate2Collection certCollection = store.Certificates;
				// TODO: Do we need any other validity checks, e.g. proper CA chain? Self-signed certs can be CA's by putting them in the Trusted Root Certificates.
				X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
				X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
				if (signingCert.Count == 0)
					return null;
				// Return the first certificate in the collection, has the right name and is current.
				return signingCert[0];
			}
			finally {
				store.Close();
			}
		}
		// Email permission Scopes
		private string[] EmailPermissionScopes = new string[] {
			"https://outlook.office365.com/.default"
		};

		public override string GetAccessTokenUsingCertificate(string emailAddress, string certificateName, string clientID) {
			var certificate = GetCertificateFromStore(certificateName);
			if (certificate == null)
				throw new GeneralException(KB.K("Unable to locate certificate '{0}'"), certificateName);
			IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientID)
				.WithCertificate(certificate)
				//.WithLogging(MyLoggingMethod, LogLevel.Verbose, enablePiiLogging: true, enableDefaultPlatformLogging: false)
				//.WithHttpClientFactory(new HttpSnifferClientFactory())
				.WithAuthority(new Uri(Authority(emailAddress)))
				.Build();
			return AcquireClientSecretToken(app).GetAwaiter().GetResult();
		}
		public override string GetAccessTokenUsingClientSecret(string emailAddress, string clientSecret, string clientID) {
			// TODO: Keep this app around so subsequen requests try to re-use a still-valid token in its cache, or perhaps to renew it if possible
			IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientID)
				.WithClientSecret(clientSecret)
				//.WithLogging(MyLoggingMethod, LogLevel.Info, enablePiiLogging: true, enableDefaultPlatformLogging: false)
				//.WithHttpClientFactory(new HttpSnifferClientFactory())
				.WithAuthority(new Uri(Authority(emailAddress)))
				.Build();
			return AcquireClientSecretToken(app).GetAwaiter().GetResult();
		}
		private async Task<string> AcquireClientSecretToken(IConfidentialClientApplication app) {
			AuthenticationResult result = null;
			try {
				var builder = app.AcquireTokenForClient(EmailPermissionScopes);
				result = await builder.ExecuteAsync().ConfigureAwait(false);
			}
			// TODO: What is the point of this try/catch? It just re-throws and all other exceptions aren't caught.
			catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011")) {
				throw ex;
			}
			return result?.AccessToken;
		}
		void MyLoggingMethod(LogLevel level, string message, bool containsPii) {
			Console.WriteLine(message);
		}
	}

	#endregion
#if DEBUG
	#region HttpSnifferClientFactory
	class HttpSnifferClientFactory : IMsalHttpClientFactory {
		readonly HttpClient _httpClient;

		public IList<Tuple<HttpRequestMessage, HttpResponseMessage>> RequestsAndResponses { get; }

		public HttpSnifferClientFactory() {
			RequestsAndResponses = new List<Tuple<HttpRequestMessage, HttpResponseMessage>>();

			var recordingHandler = new RecordingHandler((req, res) => {
				RequestsAndResponses.Add(new Tuple<HttpRequestMessage, HttpResponseMessage>(req, res));
				Console.WriteLine($"HTTP Request:\n{req}");
				Console.WriteLine($"HTTP Response:\n{res}");
			});
			_httpClient = new HttpClient(recordingHandler);
		}

		public HttpClient GetHttpClient() {
			return _httpClient;
		}
	}
	class RecordingHandler : HttpClientHandler {
		// This derives from HttpClientHandler, but the original example used [System.Net.Http???.]DelegatingHandler and set the delegate to a new HttpClientHandler.
		public delegate void handler(HttpRequestMessage request, HttpResponseMessage response);
		public RecordingHandler(handler h) {
			this.h = h;
		}
		private readonly handler h;
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) {
			var response = await base.SendAsync(request, cancellationToken);
			h(request, response);
			return response;
		}
	}
	#endregion
#endif
}