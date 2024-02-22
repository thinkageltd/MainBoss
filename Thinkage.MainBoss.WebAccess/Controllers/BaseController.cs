using System;
using System.Data.SqlClient;
using System.Web.Mvc;
using Thinkage.Libraries;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.MVC;
using Thinkage.Libraries.MVC.Models;
using Thinkage.Libraries.XAF.Database.Service;
using Thinkage.MainBoss.WebAccess.Models;

namespace Thinkage.MainBoss.WebAccess.Controllers {
	abstract public class ErrorHandlingController : Controller {
		protected override void OnException(ExceptionContext filterContext) {
			base.OnException(filterContext);
			if (filterContext.ExceptionHandled)
				return;
		}
		/// <summary>
		/// Set to messages that need to be conveyed during error processing in other forms
		/// </summary>
		public string ResultMessage {
			get;
			set;
		}
		/// <summary>
		/// Add all RuleViolations to the ModelState error collection
		/// </summary>
		/// <param name="model"></param>
		protected void CollectRuleViolations(ISimpleModelValidation model) {
			foreach (var issue in model.GetRuleViolations()) {
				ModelState.AddModelError(issue.PropertyName, issue.ErrorMessage);
			}
		}
		public void SetCancelURL() {
			if (Request.UrlReferrer != null && !String.IsNullOrEmpty(Request.UrlReferrer.AbsoluteUri))
				SetCancelURL(Request.UrlReferrer.AbsoluteUri);
		}
		public void RemoveCancelURL() {
			if (TempData.ContainsKey("CancelURL"))
				TempData.Remove("CancelURL");
		}
		public void SetCancelURL(string uri) {
			RemoveCancelURL();
			if (!String.IsNullOrEmpty(uri))
				TempData.Add("CancelURL", uri);
		}
		public void SetHomeURL(string uri) {
			if (TempData.ContainsKey("HomeURL"))
				TempData.Remove("HomeURL");
			if (!String.IsNullOrEmpty(uri))
				TempData.Add("HomeURL", uri);
		}
	}
	#region BaseController for WebAccess
	/// <summary>
	/// An exception to indicate there are Errors in the input text of a Form that were detected at time of doing the value conversion.
	/// </summary>
	/// <summary>
	/// BaseController of all our controllers for common error handling
	/// </summary>
	abstract public class BaseController : ErrorHandlingController {
		[Serializable]
		protected class ValueProviderException : System.Exception {
			[NonSerialized]
			public readonly RuleViolationCollector RulesViolated;
			public ValueProviderException(RuleViolationCollector rulesViolated) {
				RulesViolated = rulesViolated;
			}
		}
		/// <summary>
		/// Define ActionResult Views to be processed during an OnException event handling
		/// </summary>
		/// <returns></returns>
		protected delegate ActionResult OnExceptionView();
		protected virtual T NewRepository<T>() where T : BaseRepository, new() {
			Repository = new T();
			return (T)Repository;
		}
		protected BaseRepository Repository {
			get;
			private set;
		}
		protected override void OnException(ExceptionContext filterContext) {
			base.OnException(filterContext);
			if (filterContext.ExceptionHandled)
				return;

			if (filterContext.Exception is NoPermissionException) {
				filterContext.ExceptionHandled = true;
				//				string controllerName = (string)filterContext.RouteData.Values["controller"];
				//				string actionName = (string)filterContext.RouteData.Values["action"];
				//				HandleErrorInfo model = new HandleErrorInfo(filterContext.Exception, controllerName, actionName);
				View("NoPermission", filterContext.Exception).ExecuteResult(this.ControllerContext);
			}
		}
		protected override void OnAuthorization(AuthorizationContext filterContext) {
			base.OnAuthorization(filterContext);
			if (filterContext.Result == null)
				((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).ReAuthenticateMainBossUser();
		}
	}
	#endregion
	#region BaseController for nonForm controllers (no input forms are used)
	abstract public class BaseController<TModel> : BaseController where TModel : class, new() {
	}
	#endregion
	#region BaseController for updating a model that has a RulesViolationCollector
	/// <summary>
	/// BaseController with Update support.
	/// </summary>
	/// <typeparam name="TModel">The model which will be updated from the FormCollection</typeparam>
	abstract public class BaseControllerWithRulesViolationCheck<TModel> : BaseController<TModel> where TModel : class, new() {
		public delegate ActionResult ReturnResult();

		protected TModel UpdateFromForm(TModel model, FormCollectionWithType collection) {
			collection.SetTypeMapping(Repository.Form);
			UpdateModel<TModel>(model, collection.ToValueProvider());
			if (!Repository.Form.IsValid)
				throw new ValueProviderException(Repository.Form);
			return model;
		}
		/// <summary>
		/// Controllers must be able to return a TModel for exception processing to pass to a View that will be used if rule validation caused a problem
		/// </summary>
		/// <returns></returns>
		protected abstract TModel GetModelForModelStateErrors();
		virtual protected ViewResult GetViewForModelStateError(TModel model) {
			// Default is to return the View we came from; however, we allow overrides to decide.
			return View(model);
		}
		#region Exception Interpretation
		// return true if a View model needs to be constructed (THIS MAY GO AWAY HOPEFULLY)
		protected bool InterpretException(System.Exception e) {
			// First do the property Exceptions
			if (e is ValueProviderException || e is InvalidOperationException) {
				if (e is ValueProviderException) { // Additional errors that we caught beyond what UpdateModel caught
					CollectRuleViolations((e as ValueProviderException).RulesViolated);
				}
				return true;
			}
			if (e is GeneralException) {
				string message = Thinkage.Libraries.Exception.FullMessage(e);
				if (message.StartsWith("EffectiveDate-")) { // field name in trigger message 
					message = message.Substring(14); // remove "EffectiveDate-
					ModelState.AddModelError("EffectiveDate", message);
				}
				else
					ModelState.AddModelError(String.Empty, message);
				return true;
			}
			// Then global (ResultMessage) type exceptions
			if (e is System.Data.DBConcurrencyException) {
				var rowInError = (e as System.Data.DBConcurrencyException).Row;
				foreach (var columnInError in rowInError.GetColumnsInError()) {
					ModelState.AddModelError(columnInError.ColumnName, rowInError.GetColumnError(columnInError));
				}
				ResultMessage = Thinkage.Libraries.Exception.FullMessage(e);
				ModelState.AddModelError(String.Empty, ResultMessage);
			}
			if (e is ColumnRelatedDBException) {
				var vuc = e as ColumnRelatedDBException;
				System.Exception columnMappedException = vuc;
				if (vuc.InterpretedErrorCode == InterpretedDbExceptionCodes.ViolationUniqueConstraint) {
					// For Unique Constraint violations, we try to list the related fields in a custom message, using the column names is the best we can do in the web version for now
					var mappedLabels = new System.Collections.Generic.List<string>();
					foreach (string s in vuc.ColumnsInError)
						mappedLabels.Add(s);

					if (mappedLabels.Count == 0)
						columnMappedException = new GeneralException(KB.K("The value entered already exists. Only one instance of this value is allowed."));
					else if (mappedLabels.Count == 1)
						columnMappedException = new GeneralException(KB.K("The value entered for '{0}' already exists. Only one instance of this value is allowed."), mappedLabels[0]);
					else {
						// Note that the argument value for {0} will have quotes and commas inserted between each label.
						Thinkage.Libraries.Translation.Key message = KB.K("The combination of the values entered for '{0}' already exists. Only one instance of that combination is allowed.");
						System.Text.StringBuilder sb = new System.Text.StringBuilder();
						int addcomma = 0;
						foreach (string messageLabel in mappedLabels) {
							if (addcomma++ > 0)
								sb.Append(KB.I("', '"));
							sb.Append(messageLabel);
						}
						columnMappedException = new GeneralException(message, sb);
					}
					ResultMessage = Thinkage.Libraries.Exception.FullMessage(columnMappedException);
					ModelState.AddModelError(String.Empty, ResultMessage);
				}
			}
			return false;
		}

		#endregion
		protected override void OnException(ExceptionContext filterContext) {
			base.OnException(filterContext);
			if (filterContext.ExceptionHandled)
				return;

			if (InterpretException(filterContext.Exception)) {
				filterContext.ExceptionHandled = true;
				ViewResult v = GetViewForModelStateError(GetModelForModelStateErrors());
				v.ExecuteResult(this.ControllerContext);
			}
#if FUTURE
			InterpretedDbException ie = DB.InterpretException(e) as InterpretedDbException;
			// TODO: Because we (yet again) reinterpret the error, we are losing the original error message from looped containment errors.
			if (ie != null) {
				if (ie.InterpretedErrorCode == InterpretedDbExceptionCodes.ViolationUniqueConstraint) {
					SqlClient.ColumnRelatedDBException vuc = ie as SqlClient.ColumnRelatedDBException;
					List<EditControlInfo> colsInError = MapColumnsInError(vuc);
					if (colsInError.Count == 0)
						e = new GeneralException(e, ErrorMessageComposer.K("The value entered already exists. Only one instance of this value is allowed."));
					else if (colsInError.Count == 1)
						e = new GeneralException(e, ErrorMessageComposer.K("The value entered for '{0}' already exists. Only one instance of this value is allowed."), colsInError[0].tblLayoutNode.Label.Translate());
					else {
						// Note that the argument value for {0} will have multiple quotes (') added to each label
						Key message = ErrorMessageComposer.K("The combination of the values entered for '{0}' already exists. Only one instance of that combination is allowed.");
						System.Text.StringBuilder sb = new System.Text.StringBuilder();
						int addcomma = 0;
						foreach (EditControlInfo ci in colsInError) {
							if (addcomma++ > 0)
								sb.Append(KB.I("', '"));
							if (ci.tblLayoutNode.Label == null) { // may be in a picker container control; use the associated BrowseControl Tbl Identification
								// TODO: Perhaps the enclosing 'TblGroupNode' or some other identifable container label should be used; There didn't appear to be a convenient way of finding a
								// enclosing container's label.
								Thinkage.Libraries.WinControls.BrowseControl bc = ci.CheckedControl as Thinkage.Libraries.WinControls.BrowseControl;
								System.Diagnostics.Debug.Assert(bc != null, "Unable to interpret colsInError with empty Label");
								sb.Append(KB.K(bc.TblIdentification).Translate());
							}
							else
								sb.Append(ci.tblLayoutNode.Label.Translate());
						}
						e = new GeneralException(e, message, sb.ToString());
					}
					foreach (EditControlInfo ci in colsInError)
						if (ci.feedbackProvider != null)
							ci.feedbackProvider.SetFeedback(FeedbackProvider.Classifications.Warning, e.Message);
					throw e;
				}
				throw ie;
			}
#endif
		}
	}

#if FUTURE
		private List<EditControlInfo> MapColumnsInError(SqlClient.ColumnRelatedDBException vc) {
			// TODO: This should use the DataPanel.GetLabelForPath method to map row & column to control labels.

			// This method is specialized to the single purpose for which it is used, and so it does things that are not of general interest, such as
			// ignoring control infos whose leaf node has no Label, and only giving the first ControlInfo found for each column in error.
			List<EditControlInfo> controlInfoInError = new List<EditControlInfo>();
			DBI_Table tableInError = dataSet.DBISchema.Tables[vc.TableInError];
			if (tableInError != dataSet.DBISchema.Tables[vc.RowInError.Table.TableName])
				// We compare the schema objects so we don't have to worry about case distinction in table names.
				// I'm not sure when this can happen.
				return controlInfoInError;

			// Determine the recordSet index and path to the row in error.
			DBI_Path pathToRowInError;
			int recordSetInError = recordManager.PathToRow(tableInError.InternalIdColumn[vc.RowInError], tableInError, out pathToRowInError);
			if (recordSetInError < 0)
				// The RecordManager could not find the record in its buffer.
				return controlInfoInError;

			// Calculate the paths in the edit buffer of all the columns in error.
			List<DBI_Path> unprocessedPathsInError = new List<DBI_Path>();
			for (int i = vc.ColumnsInError.Length; --i >= 0; )
				// TODO: tableInError.Columns[xxx] could return null in the above trigger-confusion case, so unprocessedPathsInError could contain
				// a null or the path to a linking field.
				unprocessedPathsInError.Add(new DBI_Path(pathToRowInError, tableInError.Columns[vc.ColumnsInError[i]]));

			// Now map these to control infos.
			foreach (EditControlInfo ci in EditControlInfoCollection) {
				TblColumnNode columnNode = ci.tblLayoutNode as TblColumnNode;
				if (columnNode == null || columnNode.Label == null)
					continue;

				DBI_Path boundPath = columnNode.Path;
				for (int i = unprocessedPathsInError.Count; --i >= 0; )
					if (boundPath == unprocessedPathsInError[i]) {
						controlInfoInError.Add(ci);
						unprocessedPathsInError.RemoveAt(i);
					}
			}
			return controlInfoInError;
		}
#endif
	#endregion
}
