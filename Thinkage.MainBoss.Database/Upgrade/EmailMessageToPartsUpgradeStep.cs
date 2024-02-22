using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.XAF.Database.Service.MSSql;
using Thinkage.Libraries.XAF.Database.Layout;
using Thinkage.Libraries.Translation;
using System.Linq;
using Dart.Mail;
using Thinkage.Libraries.XAF.Database.Service;

namespace Thinkage.MainBoss.Database
{
	public class EmailMessageToPartsUpgradeStep : DataUpgradeStep
	{
		// This step changes previous storage of EmailMessage body text into constituent Plain Text and associated EmailPart records for each MIME part of the EmailMessage
		public EmailMessageToPartsUpgradeStep()
		{
		}
		public override void Perform(Version startingVersion, ISession session, DBI_Database schema, DBVersionHandler handler)
		{
			DBClient db = new DBClient(new DBClient.Connection(session.ConnectionInformation, dsEmailRequest.Schema), session);
			using (dsEmailRequest dsEmail = new dsEmailRequest(db)) {
				dsEmail.EnsureDataTableExists(dsEmailRequest.Schema.T.EmailPart, dsEmailRequest.Schema.T.EmailRequest);
				// Get all the RowIds of all the current rows in the table and put them in a list (this is to avoid reading the entire table into memory at one time; these are mail messages with possible large attachments embedded in them)
				dsEmail.DB.ViewAdditionalRows(dsEmail, dsEmailRequest.Schema.T.EmailRequest, null, new SqlExpression[] { new SqlExpression(dsEmailRequest.Path.T.EmailRequest.F.Id) }, null);
				dsEmailRequest.EmailRequestDataTable t = dsEmail.T.EmailRequest;
				List<Guid> ids = new List<Guid>(t.Rows.Count);
				foreach (dsEmailRequest.EmailRequestRow r in t)
					ids.Add(r.F.Id);

				foreach (Guid id in ids) {
					dsEmail.T.EmailRequest.Clear();
					dsEmail.T.EmailPart.Clear();
					dsEmail.AcceptChanges();
					// Get the row to convert
					dsEmailRequest.EmailRequestRow erequestrow = (dsEmailRequest.EmailRequestRow)dsEmail.DB.EditSingleRow(dsEmail, dsEmailRequest.Schema.T.EmailRequest, new SqlExpression(dsEmailRequest.Path.T.EmailRequest.F.Id).Eq(SqlExpression.Constant(id)));
					if (erequestrow != null) {
						try {
							using (var mail = new EmailMessage(erequestrow.F.MailMessage)) { // Reconstructed from originally stored MessageStream from previous implementations of Service
								if (String.IsNullOrEmpty(mail.FromAddress)) {
									throw new System.Exception(); // no message, just catch it and delete this record
								}
								else {
									erequestrow.F.MailHeader = mail.HeaderText;
									erequestrow.F.MailMessage = mail.Body; // This is replacing the original ENTIRE mail message contents
									erequestrow.F.PreferredLanguage = mail.PreferredLanguage;
									erequestrow.F.RequestorEmailAddress = mail.FromAddress;
									erequestrow.F.RequestorEmailDisplayName = mail.FromName;
									var ParttoEmailPart = new Dictionary<int, dsEmailRequest.EmailPartRow>();
									var partList = EmailPart.PartList(mail);
									foreach (var ep in partList) {
										var p = ep.Part;
										if (p.Length > (int)p.Length)
											continue;
										; // we don't want 
										dsEmailRequest.EmailPartRow part = dsEmail.T.EmailPart.AddNewEmailPartRow();
										part.F.EmailRequestID = erequestrow.F.Id;
										part.F.ContentType = p.ContentType.MediaType;
										part.F.Header = p.Headers.ToString();
										part.F.Name = p.ContentType.Name;
										//	part.F.ContentTypeDisposition = ep.ContentDisposition	-- part of a later upgrade set
										part.F.Header = ep.Headers;
										part.F.Name = p.ContentType.Name;
										part.F.Content = ep.Content;
										part.F.ContentLength = ep.Content == null ? 0 : part.F.Content.Length;
										part.F.Order = (short)ep.Index;
										ParttoEmailPart[ep.Index] = part;
									}
									foreach (var ep in partList)
										if (ep.Parent != -1)
											ParttoEmailPart[ep.Index].F.ParentID = ParttoEmailPart[ep.Parent].F.Id;
								}
							}
						}
						catch (System.Exception) {
							erequestrow.Delete(); // can't skip it, the MailHeader is a required field, and it may not be set; or something else is crap; we just 'throw the message away'
						}
						finally {
							dsEmail.DB.Update(dsEmail, ServerExtensions.UpdateOptions.Normal);
						}
					} // else? report what?
				}
			}
		}
	}
}
