using System;
using System.Collections.Generic;
using Thinkage.Libraries.MVC.Models;

namespace Thinkage.MainBoss.WebAccess.Models {
	[Serializable]
	internal class EmailAddressValidityException : Exception {
		public EmailAddressValidityException(string message, Exception ex)
			: base(message, ex) {
		}
		public EmailAddressValidityException(string message)
			: base(message) {
		}
		public EmailAddressValidityException() {
		}
	}
	/// <summary>
	/// A model for use on the home page
	/// </summary>
	public class EmailValidationModel : SimpleModelValidation {
		[System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = null, ErrorMessageResourceName = "Invalid", ErrorMessageResourceType = typeof(ValidationResource.EmailAddress))]
		[System.ComponentModel.DataAnnotations.Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ValidationResource.EmailAddress))]
		public string EmailAddress {
			get {
				return pEmailAddress;
			}
			set {
				pEmailAddress = value;
				RequestorID = null;
				EmailAddressException = null;
				if (!String.IsNullOrEmpty(pEmailAddress)) {
					var authentication = new AuthenticationRepository();
					try {
						var requestor = authentication.GetRequestorForEmailAddress(pEmailAddress);
						if (requestor != null)
							RequestorID = requestor.Id;
					}
					catch (EmailAddressValidityException e) {
						EmailAddressException = e;
					}
				}
			}
		}
		private string pEmailAddress;
		private EmailAddressValidityException EmailAddressException;
		#region ValidationResource
		/// <summary>
		/// Following the conventions of System.ComponentModel.DataAnnotations for providing error messages
		/// </summary>
		private class ValidationResource {
			internal class EmailAddress {
				public static string Required {
					get {
						return FieldValidationResource.Required;
					}
				}
				public static string Invalid {
					get {
						return KB.K("The email address is not valid").Translate();
					}
				}
			}
		#endregion
		}

		// The RequestorID associated with this Email Address (if authorized)
		public Guid? RequestorID {
			get;
			set;
		}

		public void SetRequestorID() {
			if (String.IsNullOrEmpty(EmailAddress))
				return;
		}

		public override IEnumerable<RuleViolation> GetRuleViolations() {
			string propertyCheck = "EmailAddress";
			if (EmailAddressException != null) {
				if (EmailAddressException.InnerException != null)
					yield return new RuleViolation(Thinkage.Libraries.Exception.FullMessage(EmailAddressException), propertyCheck);
				else
					yield return new RuleViolation(EmailAddressException.Message, propertyCheck);
			}
			else {
				if (String.IsNullOrEmpty(EmailAddress))
					yield return new RuleViolation("Email address is required", propertyCheck);
				else if (!RequestorID.HasValue)
					yield return new RuleViolation(KB.K("There is no MainBoss Requestor with this email address.").Translate(), propertyCheck);
			}
			yield break;
		}
	}
}
