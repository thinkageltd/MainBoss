using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Thinkage.MainBoss.WebAccess.Models {
	public class RequestStateHistoryModel : RequestEntities.RequestStateHistory, Thinkage.Libraries.MVC.Models.ISimpleModelValidation {
		/// <summary>
		/// The CurrentRequestStateHistoryID associated with the Request in question
		/// </summary>
		[System.ComponentModel.DataAnnotations.Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Thinkage.Libraries.MVC.Models.FieldValidationResource))]
		public Guid CurrentStateHistoryID {
			get;
			set;
		}
		public Thinkage.Libraries.Translation.Key CurrentStateCode {
			get;
			set;
		}
		public string CurrentStatusCode {
			get;
			set;
		}
		public string LastComment {
			get;
			set;
		}
		public string LastRequestorComment {
			get;
			set;
		}
		/// <summary>
		/// The identifying Request number associated with this history record
		/// </summary>
		public string RequestNumber {
			get;
			set;
		}
		/// <summary>
		/// The identifying Request Subject associated with this history record
		/// </summary>
		public string RequestSubject {
			get;
			set;
		}
		/// <summary>
		/// The identifying Request Description associated with this history record (to inform the Requestor when they make a comment)
		/// </summary>
		public string RequestDescription {
			get;
			set;
		}
		[System.Runtime.Serialization.IgnoreDataMember]
		public IEnumerable<SelectListItem> RequestStateHistoryStatusPickList {
			get;
			set;
		}

		// Setup to do EmailAddress validation for Requestors to make comments. We proxy what is necessary from the EmailValidationModel that we create for our use.

		private readonly EmailValidationModel RequestorEmailValidation = new EmailValidationModel();
		public string EmailAddress {
			get {
				return RequestorEmailValidation.EmailAddress;
			}
			set {
				RequestorEmailValidation.EmailAddress = value;
			}
		}
		public Guid? RequestorID {
			get {
				return RequestorEmailValidation.RequestorID;
			}
		}

		#region ISimpleModelValidation Members

		public bool IsValid {
			get {
				return EmailAddress == null || RequestorEmailValidation.IsValid;
			}
		}

		public IEnumerable<Libraries.MVC.Models.RuleViolation> GetRuleViolations() {
			return RequestorEmailValidation.GetRuleViolations();
		}

		#endregion
	}
}
