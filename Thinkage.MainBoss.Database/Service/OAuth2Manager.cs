using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Thinkage.Libraries;
using Microsoft.Identity.Client;

namespace Thinkage.MainBoss.Database.Service {
	#region OAuth2 Manager
	public abstract class OAuth2ManagerBase {
		public abstract string Authority { get; }
		public abstract string GetAccessToken();
	}
	public class OAuth2ManagerAzure : OAuth2ManagerBase {

		public override string Authority {
			get {
				return Strings.IFormat(Instance, DirectoryTenantID);
			}
		}
		private string AuthorizeAccess {  get { return Authority + KB.I("/oath2/v2.0/authorize"); } }
		private string TokenAccess { get { return Authority + KB.I("/oath2/v2.0/token"); } }
		private string Instance { get; } = "https://login.microsoftonline.com/{0}";
		/// <summary>
		/// The Tenant is:
		/// - either the tenant ID of the Azure AD tenant in which this application is registered (a guid)
		/// or a domain name associated with the tenant
		/// - or 'organizations' (for a multi-tenant application)
		/// </summary>
		private string DirectoryTenantID = "63b20df6-57ca-4b76-84f0-292ae18234aa";
		private string ApplicationClientID = "5faf12fa-8b73-406e-97fc-e6d36b5f15dd";

		// Email permission Scopes
		private string[] EmailPermissionScopes = new string[] { "https://outlook.office.com/IMAP.AccessAsUser.All" }; //, "https://outlook.office.com/POP.AccessAsUser.All", "https://outlook.office.com/SMTP.Send" };
																													  //following are for testing and expire 2021/09/10 Description
		private string ClientSecretDescription { get; } = "Windview MainBoss Service";
		private string ClientSecretID { get; } = "bff26d1d-ef0e-43d2-a7c1-bbe8424bdf23";
		private string ClientSecretValue { get; } = "8~JV-k6vzk1hMBK ~o_4~C3Re5hyd8n.wPW";
		public override string GetAccessToken() {
			return AcquireClientSecretToken().GetAwaiter().GetResult();
		}
		private async Task<string> AcquireClientSecretToken() {

#if THISWILLNOTWORKUNTILJULY2021
			IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ApplicationClientID)
				.WithClientSecret(ClientSecretValue)
				.WithAuthority(new Uri(Authority))
				.Build();
			AuthenticationResult result = null;
			try {
				var builder = app.AcquireTokenForClient(EmailPermissionScopes);
				result = await builder.ExecuteAsync().ConfigureAwait(false);
			}
			catch(MsalServiceException ex) when (ex.Message.Contains("AADSTS70011")) {
				throw ex;
			}
			return result?.AccessToken;
#endif
			return null;
		}
	}
	#endregion
}
