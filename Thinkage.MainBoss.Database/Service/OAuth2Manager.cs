using Microsoft.Identity.Client;
using System;
using System.Security.Cryptography.X509Certificates;
using Thinkage.Libraries;

namespace Thinkage.MainBoss.Database.Service {
	public abstract class OAuth2ManagerBase {
		public abstract string GetAccessToken();
		public static X509Certificate2 GetCertificateFromStore(string certName) {
			// Get the certificate store for the current user.
			X509Store store = new X509Store(StoreLocation.CurrentUser);
			try {
				store.Open(OpenFlags.ReadOnly);

				// TODO: Do we need any other validity checks, e.g. proper CA chain? Currently it seems not. We may not even need a valid date.
				// Self-signed certs can be CA's by putting them in the Trusted Root Certificates.
				X509Certificate2Collection signingCerts = store.Certificates
					.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
				if (signingCerts.Count == 0)
					throw new GeneralException(KB.K("Unable to locate certificate '{0}'"), certName);
				signingCerts = signingCerts.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
				if (signingCerts.Count == 0)
					throw new GeneralException(KB.K("Unable to locate certificate '{0}' with current valid date"), certName);
				// Return the first certificate in the collection, has the right name and is current.
				return signingCerts[0];
			}
			finally {
				store.Close();
			}
		}
	}
	public class OAuth2ManagerAzure : OAuth2ManagerBase {
		// Certificate requirements are at https://learn.microsoft.com/en-us/azure/active-directory/develop/howto-create-self-signed-certificate
		public OAuth2ManagerAzure(string clientID, string emailAddress, X509Certificate2 certificate)
			: this(ConfidentialClientApplicationBuilder.Create(clientID).WithCertificate(certificate), emailAddress) {
		}
		public OAuth2ManagerAzure(string clientID, string emailAddress, string clientSecret)
			: this(ConfidentialClientApplicationBuilder.Create(clientID).WithClientSecret(clientSecret), emailAddress) {
		}
		private OAuth2ManagerAzure(ConfidentialClientApplicationBuilder appBuilder, string emailAddress) {
			var cca = appBuilder
				//.WithLogging(MyLoggingMethod, LogLevel.Verbose, enablePiiLogging: true, enableDefaultPlatformLogging: false)
				//.WithHttpClientFactory(new HttpSnifferClientFactory())
				.WithAuthority(new Uri(Authority(emailAddress)))
				.Build();
			Builder = cca.AcquireTokenForClient(EmailPermissionScopes);
		}
		private readonly AcquireTokenForClientParameterBuilder Builder;
		// We infer the Authority URI from the email address.
		// TODO: Do we have to worry about "pretty" email addresses like "Fred Frederickson <ffred@abc.com>" ?
		// Can there be other @ signa in the email address?
		// Can it be the case that the domain part of the email address is NOT the name of the Tanant on AAD?
		private string Authority(string emailAddress) {
			string[] parts = emailAddress.Split('@');
			return Strings.IFormat(Instance, parts[1]);
		}
		private string Instance { get; } = "https://login.microsoftonline.com/{0}";
		// Email permission Scopes
		private string[] EmailPermissionScopes = new string[] {
			"https://outlook.office365.com/.default"
		};

		public override string GetAccessToken() {
			// Because we have not called Builder.WithForcedRefresh(true) the following call will use a cached Access Token if it is still valid.
			return Builder.ExecuteAsync().ConfigureAwait(false).GetAwaiter().GetResult()?.AccessToken;
		}
		private static void MyLoggingMethod(LogLevel level, string message, bool containsPii) {
			Console.WriteLine(message);
		}
	}
}
