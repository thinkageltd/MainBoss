using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.WebAccess.Models {
	/// <summary>
	/// This is the repository class used to do user authentication.
	/// </summary>
	public class AuthenticationRepository : BaseRepository, Thinkage.MainBoss.WebAccess.Models.interfaces.IAuthenticationRepository {
		#region Constructor and Base Support
		public AuthenticationRepository()
			: base("User") {
		}
		public override void InitializeDataContext() {
			DataContext = new AuthenticationDataContext(Connection.ConnectionString);
		}
		public AuthenticationDataContext DataContext {
			get;
			private set;
		}
		#endregion
		#region User Information
		public IQueryable<AuthenticationEntities.User> Users() {
			return DataContext.User;
		}
		public AuthenticationEntities.Contact GetContactForUser(Guid userID) {
			return DataContext.User.SingleOrDefault<AuthenticationEntities.User>(d => d.Id == userID).Contact;
		}
		public AuthenticationEntities.Requestor GetRequestorForEmailAddress(string emailAddress) {
			try {
				var requestInfo = new AcquireRequestorAddressWithLogging(KB.I("WebAccess"), MB3DB, Thinkage.Libraries.Mail.MailAddress(emailAddress), null); // the last argument should be the language
				if (requestInfo.Exception != null) throw new EmailAddressValidityException(requestInfo.UserText);
				if (requestInfo.RequestorID == null) return null;
				
				IQueryable<AuthenticationEntities.Requestor> x = from r in DataContext.Requestor where r.Id == requestInfo.RequestorID select r;
				return x.SingleOrDefault();
			}
			catch (EmailAddressValidityException) { // Ignore ones we generate ourselves
				throw;
			}
			catch (Exception e) { // catch all others
				throw new EmailAddressValidityException(e.Message, e.InnerException);
			}
		}
		#endregion
	}
}
