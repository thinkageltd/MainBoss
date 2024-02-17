using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Thinkage.Libraries;
using Thinkage.Libraries.Collections;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Receiving.
	/// </summary>
	public class TIMainBossService : TIGeneralMB3 {
		#region ServiceConfigurationApplication Interface
		internal protected abstract class ServiceConfigurationApplicationCommand {
			// Static so that in event our servicecontrol object is reclaimed, we can still detect the existence of the manage process
			// that may be running from an earlier incarnation.
			static System.Diagnostics.Process manageProcess = null;
			protected ServiceConfigurationApplicationCommand([Invariant] string executable, [Invariant] string commandLine) {
				Executable = executable;
				CommandLine = commandLine;
			}
			private readonly string Executable;
			private readonly string CommandLine;
			public void RunCommand() {
				try {
					if (manageProcess != null) {
						manageProcess.Refresh();
						if (!manageProcess.HasExited) {
							Thinkage.Libraries.MSWindows.WinUser.SetForegroundWindow(manageProcess.MainWindowHandle);
							return;
						}
					}
				}
				catch (System.Exception) {
					// something not okay with our previous incarnation; just make a new one.
				}
				manageProcess = new System.Diagnostics.Process();
				string currentExecutingPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				manageProcess.StartInfo.Arguments = CommandLine;
				manageProcess.StartInfo.FileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(currentExecutingPath), Executable);
				manageProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
				try {
					manageProcess.Start();
					try {
						manageProcess.WaitForExit(30 * 1000);
					}
					catch (System.Exception) {
						// ignore spurious messages on WaitForInputIdle on remote desktop connection to Vista computers.
					}
				}
				catch (System.Exception ex) {
					Thinkage.Libraries.Exception.AddContext(ex, new Thinkage.Libraries.MessageExceptionContext(KB.K("executing {0}"), manageProcess.StartInfo.FileName));
					throw;
				}
			}
			static protected string BuildSqlConnectString(XAFClient db) {
				var ci = ((MB3Client)db).ConnectionInfo;
				var cs = new SqlConnectionStringBuilder();
				try {
					cs.DataSource = ci.DBServer;
					cs.InitialCatalog = ci.DBName;
					cs.Authentication = (SqlAuthenticationMethod)ci.DBCredentials.Type;
					if (ci.DBCredentials.Type != AuthenticationMethod.WindowsAuthentication) {
						cs.UserID = ci.DBCredentials.Username;
						cs.Password = ci.DBCredentials.Password;
					}
				}
				catch (System.Exception e) {
					throw new GeneralException(e, KB.K("Could not construct a valid SQL Server Connection string"));
				}
				return cs.ConnectionString;
			}

		}
		internal class ServiceCommand : ServiceConfigurationApplicationCommand {
			public ServiceCommand([Invariant]string command, [Invariant]string serviceName, XAFClient db) :
				base(KB.I(ServiceCommonBrowseLogic.ServiceConfigurationCommand), Strings.IFormat("{0} /ServiceCode:\"{1}\" /Connection:{2}"
					, command, serviceName, ServiceConfiguration.EscapeArg(BuildSqlConnectString(db)))) {
			}
		}
		internal class ClearEventLog : ServiceConfigurationApplicationCommand {
			public ClearEventLog(string serviceName) :
				base(KB.I(ServiceCommonBrowseLogic.ServiceConfigurationCommand), "/ClearEventLog") {
			}
		}
		internal class ManualServiceExecution : ServiceConfigurationApplicationCommand {
			public ManualServiceExecution([Invariant]string command, [Invariant]string serviceName, XAFClient db) :
				base(KB.I("Thinkage.MainBoss.Service.exe"), Strings.IFormat("/{0}  /ServiceCode:\"{1}\" /Connection:{2}"
					, command, serviceName, ServiceConfiguration.EscapeArg(BuildSqlConnectString(db)))) {
			}
		}
		public class ProcessEmailServiceCommand {
			private readonly UIFactory UIFactory;
			private readonly XAFClient DB;
			private readonly bool Trace;
			private readonly bool SendingEmailOk;
			public ProcessEmailServiceCommand(UIFactory uiFactory, XAFClient db, bool trace, bool sendRejections) {
				UIFactory = uiFactory;
				DB = db;
				Trace = trace;
				SendingEmailOk = sendRejections;
			}
			public void RunCommand() {
				IProgressDisplay ipdo = UIFactory.CreateProgressDisplay(KB.K("Processing Email"), SendingEmailOk ? 3 : 1, false);
				try {
					ipdo.Update(KB.K("Creating Requests from Email."), 1);
					var log = new LogAndDatabaseAndError(DB.ConnectionInfo);
					RequestProcessor.DoAllRequestProcessing(log, (MB3Client)DB, Trace, Trace, SendingEmailOk);
					if (SendingEmailOk) {
						ipdo.Update(KB.K("Notifying Requestors"), 2);
						RequestorNotificationProcessor.DoAllRequestorNotifications(log, (MB3Client)DB, Trace, Trace);
						ipdo.Update(KB.K("Notifying Assignees"), 3);
						AssignmentNotificationProcessor.DoAllAssignmentNotifications(log, (MB3Client)DB, Trace, Trace);
					}
					log.LogClose(KB.K("Retrieving Email completed").Translate());
				}
				finally {
					ipdo.Complete();
				}
			}
		}

		#endregion

		#region NodeIds
		private static readonly object IncomingMailServerTypeId = KB.I("IncomingMailServerTypeId");
		private static readonly object IncomingMailEncryptionId = KB.I("IncomingMailEncryptionId");
		private static readonly object IncomingMailServerPortId = KB.I("IncomingMailServerPortId");
		private static readonly object OutgoingMailServerAuthenticationId = KB.I("OutgoingMailServerAuthenticationId");
		private static readonly object OutgoingMailServerUserDomainId = KB.I("OutgoingMailServerUserDomainId");
		private static readonly object ReturnEmailAddressId = KB.I("ReturnEmailAddressId");
		private static readonly object MainBossRemoteURLId = KB.I("MainBossRemoteURLId");
		private static readonly object AllowPattern = KB.I("AllowPattern");
		private static readonly object RejectPattern = KB.I("RejectPattern");
		private static readonly object ServiceCodeID = KB.I("ServiceCodeID");
		private static readonly object OutgoingMailServerUsernameId = KB.I("OutgoingMailServerUsernameId");
		private static readonly object OutgoingMailServerPasswordId = KB.I("OutgoingMailServerPasswordId");
		protected static readonly Key BecauseOutgoingMailServerAuthenticationIsNotCustom = KB.K("Readonly because the Outgoing SMTP server authentication is not using the specified SMTP domain, username and password");
		private static readonly EditorCalculatedInitValue.CalculationDelegate OutgoingMailServerAuthenticationControlReadonly = delegate (object[] inputs) {
			return (inputs[0] != null && (DatabaseEnums.SMTPCredentialType)(int)IntegralTypeInfo.AsNativeType(inputs[0], typeof(int)) != DatabaseEnums.SMTPCredentialType.CUSTOM);
		};
		#endregion

		#region Record-type Providers
		public static EnumValueTextRepresentations EmailRequestStateProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Not Processed"),
				KB.K("Completed"),
				KB.K("Email rejected: Senders email address doesn't match any Requestor"),
				KB.K("Error"),
				KB.K("Email rejected: Senders email address doesn't match any Contact"),
				KB.K("Senders email address matches multiple Requestors"),
				KB.K("Senders email address matches multiple Contacts"),
				KB.K("Contact could not be created for senders email address"),
				KB.K("Senders email address doesn't match any Requestor"),
				KB.K("Senders email address doesn't match any Contact"),
				KB.K("Email rejected: Senders email address matches multiple Requestors"),
				KB.K("Email rejected: Senders email address matches multiple Contacts"),
				KB.K("Email rejected: Senders Contact could not be created since another Contact exists with the same code"),
				KB.K("Email to be rejected by a MainBoss user"),
				KB.K("Email was rejected by a MainBoss user"),
			},
			null,
			new object[] {
				(int)DatabaseEnums.EmailRequestState.UnProcessed,
				(int)DatabaseEnums.EmailRequestState.Completed,
				(int)DatabaseEnums.EmailRequestState.RejectNoRequestor,
				(int)DatabaseEnums.EmailRequestState.Error,
				(int)DatabaseEnums.EmailRequestState.RejectNoContact,
				(int)DatabaseEnums.EmailRequestState.AmbiguousRequestor,
				(int)DatabaseEnums.EmailRequestState.AmbiguousContact,
				(int)DatabaseEnums.EmailRequestState.AmbiguousContactCreation,
				(int)DatabaseEnums.EmailRequestState.NoRequestor,
				(int)DatabaseEnums.EmailRequestState.NoContact,
				(int)DatabaseEnums.EmailRequestState.RejectAmbiguousRequestor,
				(int)DatabaseEnums.EmailRequestState.RejectAmbiguousContact,
				(int)DatabaseEnums.EmailRequestState.RejectAmbiguousContactCreation,
				(int)DatabaseEnums.EmailRequestState.ToBeRejected,
				(int)DatabaseEnums.EmailRequestState.RejectManual,
			}
		);

		public static EnumValueTextRepresentations EmailServerTypeProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Automatically determined"),
				KB.K("POP3"),
				KB.K("IMAP4"),
				KB.K("POP3S"),
				KB.K("IMAP4S"),
			},
			new Key[] {
				KB.K("Try in order POP3 with TLS, IMAP4 with TLS, POP3S, IMAP4S, POP3 and IMAP4"),
				KB.K("Post Office Protocol version 3"),
				KB.K("Internet Message Access Protocol version 4 revision 1"),
				KB.K("Post Office Protocol version 3 over SSL"),
				KB.K("Internet Message Access Protocol version 4 revision 1 over SSL"),
			},
			new object[] {
				(int)DatabaseEnums.MailServerType.Any,
				(int)DatabaseEnums.MailServerType.POP3,
				(int)DatabaseEnums.MailServerType.IMAP4,
				(int)DatabaseEnums.MailServerType.POP3S,
				(int)DatabaseEnums.MailServerType.IMAP4S,
			}, 5
		);
		public static EnumValueTextRepresentations EmailServerEncryptionProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("When Available"),
				KB.K("Require Encryption"),
				KB.K("Require Encryption with a Valid Certificate"),
				KB.K("No Encryption"),
			},
			new Key[] {
				KB.K("Use encryption if available"),
				KB.K("Require the use of encryption"),
				KB.K("Require encryption and a valid certificate"),
				KB.K("Do not use encryption"),
			},
			new object[] {
				(int)DatabaseEnums.MailServerEncryption.AnyAvailable,
				(int)DatabaseEnums.MailServerEncryption.RequireEncryption,
				(int)DatabaseEnums.MailServerEncryption.RequireValidCertificate,
				(int)DatabaseEnums.MailServerEncryption.None
			}, 4
		);

		public static EnumValueTextRepresentations SMTPCredentialTypeProvider = new EnumValueTextRepresentations(
			new Key[] {
				KB.K("Anonymous"),
				KB.K("Use the default network credential"),
				KB.K("Using the specified SMTP domain, username and password"),
			},
			new Key[] {
				KB.K("Access the SMTP server anonymously"),
				KB.K("Use the credentials of the user that is currently logged-in"),
				KB.K("Use the credentials given in the fields: Outgoing SMTP Domain, Username and Password"),
			},
			new object[] {
				(int)DatabaseEnums.SMTPCredentialType.ANONYMOUS,
				(int)DatabaseEnums.SMTPCredentialType.DEFAULT,
				(int)DatabaseEnums.SMTPCredentialType.CUSTOM,
			}
		);
		#endregion

		public static DelayedCreateTbl MainBossServiceManageTblCreator = null;
		private static DelayedCreateTbl MainBossServiceConfigurationEditTblCreator = null;

		#region EmailRequestTbl
		private static Tbl EmailRequestBrowseTbl() {
			var layout = new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.RequestID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Request.F.Number)), ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.ReceiveDate, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.RequestorEmailDisplayName, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.RequestorEmailAddress, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.Subject, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.ProcessingState, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.MailMessage, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.MailHeader, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailRequest.F.Comment, DCol.Normal, ECol.AllReadonly)
				),
				BrowsetteTabNode.New(TId.EmailPart, TId.EmailRequest,
					TblColumnNode.NewBrowsette(dsMB.Path.T.EmailPart.F.EmailRequestID, DCol.Normal, ECol.Normal))
				);

			var editTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.EmailRequest, TId.EmailRequest,
					new Tbl.IAttr[] {
						MainBossServiceGroup,
						new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.Delete), ETbl.Print(TIReports.SingleEmailRequestReport, dsMB.Path.T.EmailRequest.F.Id))
					},
					layout
				);
			});
			return new CompositeTbl(dsMB.Schema.T.EmailRequest, TId.EmailRequest,
				new Tbl.IAttr[] {
					MainBossServiceGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.EmailRequest.F.ReceiveDate),
						BTbl.ListColumn(dsMB.Path.T.EmailRequest.F.RequestorEmailDisplayName),
						BTbl.ListColumn(dsMB.Path.T.EmailRequest.F.ProcessingState),
						BTbl.ListColumn(dsMB.Path.T.EmailRequest.F.RequestID.F.Number),
						BTbl.AdditionalVerb(KB.K("Email Message"),
							delegate(BrowseLogic browserLogic) {
								Libraries.DataFlow.Source emailRequestId = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailRequest.F.Id, -1);
								var group = browserLogic.Commands.CreateNestedNode(KB.K("Email"),null);
#if DEBUG
								group.AddCommand(KB.K("DEBUG View Email Message Structure"), null, new MultiCommandIfAllEnabled(new CallDelegateCommand(
									delegate() {
										ServiceUtilities.DebugEmailMessage(browserLogic.DB, false, (Guid)emailRequestId.GetValue());
									}),
									browserLogic.NeedSingleSelectionDisabler
									),null);

								group.AddCommand(KB.K("DEBUG View RFC822 Email Message"), null, new MultiCommandIfAllEnabled(new CallDelegateCommand(
									delegate() {
										ServiceUtilities.DebugEmailMessage(browserLogic.DB, true, (Guid)emailRequestId.GetValue());
									}),
									browserLogic.NeedSingleSelectionDisabler
									),null);
#endif
								group.AddCommand(KB.K("View Email"), null, new MultiCommandIfAllEnabled( new CallDelegateCommand(KB.K("View the Message in your Email program"),
									delegate() {
										ServiceUtilities.OpenEmail(browserLogic.DB, (Guid)emailRequestId.GetValue() );
									}),
									browserLogic.NeedSingleSelectionDisabler
								),null);
								group.AddCommand(KB.K("Save Email Message"), null, new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Save the Email Message to a file"),
									delegate() {
										try {
											var message = EmailMessage.EmailRequestToRFC822(browserLogic.DB, true, (Guid)emailRequestId.GetValue());
											using (var saveFile = new System.Windows.Forms.SaveFileDialog()) {
												saveFile.RestoreDirectory = true;
												saveFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
												saveFile.DefaultExt = ".eml";
												saveFile.Filter =  "Mail Messages (*.eml)|*.eml|All files (*.*)|*.*";
												saveFile.FilterIndex = 0;
												saveFile.AddExtension = true;
												if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
													System.IO.File.WriteAllText(saveFile.FileName, message );
												}
											}
										}
										catch( System.Exception e) {
											throw new GeneralException(e, KB.K("Cannot save Email Message to specified file"));
										}
									}),
									browserLogic.NeedSingleSelectionDisabler
									),null);
								group.AddCommand(KB.K("Import Email Message"), null, new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("Insert a new Email Message from a file"),
									delegate() {
										try {
											using (var openfile = new System.Windows.Forms.OpenFileDialog()) {
												openfile.RestoreDirectory = true;
												openfile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
												openfile.DefaultExt = ".eml";
												openfile.Filter =  "Mail Messages (*.eml)|*.eml|All files (*.*)|*.*";
												openfile.FilterIndex = 0;
												openfile.AddExtension = true;
												if (openfile.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
													var message = System.IO.File.ReadAllText(openfile.FileName);
													var id = ServiceUtilities.EmailRequestFromEmail(browserLogic.DB, message, openfile.FileName);
#if DEBUG
													ServiceUtilities.DebugEmailMessage(browserLogic.DB, false, (Guid)emailRequestId.GetValue());
#endif
												}
											}
										}
										catch( System.Exception e) {
											throw new GeneralException(e, KB.K("Cannot import Email Message"));
										}
									}),
									browserLogic.NeedSingleSelectionDisabler
									),null);
									return null;
								}
							),
							BTbl.AdditionalVerb(KB.K("Reject Email Request"),
							delegate(BrowseLogic browserLogic) {
								Libraries.DataFlow.Source emailRequestState = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailRequest.F.ProcessingState, -1);
								return new MultiCommandIfAllEnabled( new CallDelegateCommand(KB.K("Send a rejection message to the sender"),
									delegate() {
										RequestFromEmailActions.Reject((MB3Client)browserLogic.DB, browserLogic.BrowseContextRecordIds);
										//for (Thinkage.Libraries.DataFlow.Position p = browserLogic.BrowserSelectionPositioner.StartPosition.Next; !p.IsEnd; p = p.Next)
										//	RejectEmailAction(browserLogic, p.Id);
										browserLogic.SetAllOutOfDate();
									}),
									new IsRejectedDisabler(browserLogic, emailRequestState),
									new IsProcessedDisabler(browserLogic, emailRequestState)
								);
							}
						),
						BTbl.AdditionalVerb(KB.K("Process Email Request"),
							delegate(BrowseLogic browserLogic) {
								Libraries.DataFlow.Source emailRequestState = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailRequest.F.ProcessingState, -1);
								return new MultiCommandIfAllEnabled( new CallDelegateCommand(KB.K("Create a request for this Email Message"),
									delegate() {
										RequestFromEmailActions.Create((MB3Client)browserLogic.DB, browserLogic.BrowseContextRecordIds, false);
										browserLogic.SetAllOutOfDate();
									}),
									new IsProcessedDisabler(browserLogic, emailRequestState)

								);
							}
						),
						BTbl.AdditionalVerb(KB.K("Create Requestor"),
							delegate(BrowseLogic browserLogic) {
								Libraries.DataFlow.Source emailRequestState = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailRequest.F.ProcessingState, -1);
								return new MultiCommandIfAllEnabled( new CallDelegateCommand(KB.K("Create a requestor for the sender email address and create a request"),
									delegate() {
										RequestFromEmailActions.Create((MB3Client)browserLogic.DB, browserLogic.BrowseContextRecordIds, true);
										browserLogic.SetAllOutOfDate();
									}),
									new NoRequestorDisabler(browserLogic, emailRequestState),
									new IsProcessedDisabler(browserLogic, emailRequestState)
								);
							}
						)
					),
					TIReports.NewRemotePTbl(TIReports.EmailRequestReport)
				},
				null,
				CompositeView.ChangeEditTbl(editTbl),
				CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.Request),
					NoNewMode,
					CompositeView.ContextualInit(0, new[] {
						new CompositeView.Init(new ControlTarget(TIRequest.RequestSubjectID), new BrowserCalculatedInitValue(dsMB.Path.T.Request.F.Subject.ReferencedColumn.EffectiveType,
								delegate(object[] inputs) {
									var content = (string)inputs[0];
									if (content == null)
										return null;
									return content;
								}, new BrowserPathValue(dsMB.Path.T.EmailRequest.F.Subject))),
						new CompositeView.Init(new ControlTarget(TIRequest.RequestDescriptionID), new BrowserCalculatedInitValue(dsMB.Path.T.Request.F.Description.ReferencedColumn.EffectiveType,
								delegate(object[] inputs) {
									var content = (string)inputs[0];
									if (content == null)
										return null;
									return content;
								}, new BrowserPathValue(dsMB.Path.T.EmailRequest.F.MailMessage))),

						//TODO: The following filter shows Requestors that have a NULL email address field in the contact record; the user might expect the email value to be set in the contact record (but there is no structure to permit this)
						//TODO: THe email address field in the Contact record might be more than a simple EMAIL address (it might be in the form "Display Name <display@email.com>" in which case the filter will not find it.
						//		we are considering separating the Display Name and Address into distinct fields in the Contact and EmailRequest records.
						new CompositeView.Init(new InSubBrowserTarget(TIGeneralMB3.RequestorPickerNodeId, new BrowserFilterTarget(TIGeneralMB3.RequestorEmailFilterNodeId)), dsMB.Path.T.EmailRequest.F.RequestorEmailAddress)
					})) //
			);
		}
		#endregion
		#region Email Extra Commands
		private class IsRejectedDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public IsRejectedDisabler(BrowseLogic browser, Libraries.DataFlow.Source emailRequestState)
				: base(browser, KB.K("The email request has been already been rejected")
					, () => ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.NoRequestor
						|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.NoContact
						|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.AmbiguousRequestor
						|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.AmbiguousContact
						|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.AmbiguousContactCreation
						|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.UnProcessed
				) { }
		}
		private class IsProcessedDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public IsProcessedDisabler(BrowseLogic browser, Libraries.DataFlow.Source emailRequestState)
				: base(browser, KB.K("The email request has been already processed")
					, () => ((short)emailRequestState.GetValue()) != (short)DatabaseEnums.EmailRequestState.Completed) { }
		}

		private class NoRequestorDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public NoRequestorDisabler(BrowseLogic browser, Libraries.DataFlow.Source emailRequestState)
				: base(browser, KB.K("The email request does not have a problem with the requestor")
					, () => ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.NoRequestor
								|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.NoContact
								|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.RejectNoRequestor
								|| ((short)emailRequestState.GetValue()) == (short)DatabaseEnums.EmailRequestState.RejectNoContact
					) { }
		}
		private class PartHasContentDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public PartHasContentDisabler(BrowseLogic browser, Libraries.DataFlow.Source emailPartContentLength)
				: base(browser, KB.K("There is no content to save"), () => (int)emailPartContentLength.GetValue() > 0) { }
		}
		private class IsEmailAttachmentDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public IsEmailAttachmentDisabler(BrowseLogic browser, Libraries.DataFlow.Source emailPartType)
				: base(browser, KB.K("Only valid for an Email Attachment"), () => (string)emailPartType.GetValue() == "message/rfc822") { }
		}

		static System.CodeDom.Compiler.TempFileCollection temporaryFiles = new System.CodeDom.Compiler.TempFileCollection();
		#region EmailPartTblCreator
		private static CompositeTbl EmailPartTbl() {
			var unknownViewerTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.EmailPart, TId.EmailPart,
					new Tbl.IAttr[] {
					MainBossServiceGroup,
					new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.Delete))
				},
					new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Order, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentType, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Header, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Name, DCol.Normal, ECol.Normal)
					)
				);
			});
			var plainTextViewerTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.EmailPart, TId.EmailPart,
					new Tbl.IAttr[] {
					MainBossServiceGroup,
					new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.Delete))
				},
					new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Order, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentType, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Header, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentLength, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Name, DCol.Normal, ECol.Normal),
					TblGroupNode.New(KB.K("Text Content"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblInitSourceNode.New(null, new DualCalculatedInitValue(StringTypeInfo.Universe, delegate (object[] inputs) {
							var content = (byte[])inputs[1];
							if (content == null)
								return null;
							try {
								return EmailMessage.ContentEncoding((string)inputs[0]).GetString(content);
							}
							catch (System.Exception e) {
								var s = new StringBuilder();
								s.AppendLine(Strings.Format(KB.K("Content is not printable")));
								s.Append(Thinkage.Libraries.Exception.FullMessage(e));
								return s.ToString();
							}
						}, new DualPathValue(dsMB.Path.T.EmailPart.F.ContentEncoding), new DualPathValue(dsMB.Path.T.EmailPart.F.Content)), DCol.Normal, ECol.AllReadonly)
						)
					)
				);
			});
			var htmlViewerTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.EmailPart, TId.EmailPart,
					new Tbl.IAttr[] {
					MainBossServiceGroup,
					new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.Delete))
				},
					new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Order, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentType, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Header, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentLength, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Name, DCol.Normal, ECol.Normal),
					TblGroupNode.New(KB.K("HTML Content"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblInitSourceNode.New(null, new DualCalculatedInitValue(StringTypeInfo.Universe, delegate (object[] inputs) {
							var content = (byte[])inputs[1];
							if (content == null)
								return null;
							try {
								return EmailMessage.ContentEncoding((string)inputs[0]).GetString(content);
							}
							catch (System.Exception e) {
								var s = new StringBuilder();
								s.AppendLine(Strings.Format(KB.K("Content is not printable")));
								s.Append(Thinkage.Libraries.Exception.FullMessage(e));
								return s.ToString();
							}
						}, new DualPathValue(dsMB.Path.T.EmailPart.F.ContentEncoding), new DualPathValue(dsMB.Path.T.EmailPart.F.Content)), DCol.Html, new ECol(ECol.AllReadonlyAccess, Fmt.SetUsage(DBI_Value.UsageType.Html))))
					)
				);
			});
			var imageViewerTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.EmailPart, TId.EmailPart,
					new Tbl.IAttr[] {
					MainBossServiceGroup,
					new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.Delete))
				},
					new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Order, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentType, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Header, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentLength, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Name, DCol.Normal, ECol.Normal),
					TblGroupNode.New(KB.K("Image Content"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.New(null, dsMB.Path.T.EmailPart.F.Content, DCol.Image, new ECol(ECol.AllReadonlyAccess, Fmt.SetUsage(DBI_Value.UsageType.Image)))
					)
					)
				);
			});
			var AttachedMailMessageViewerTbl = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.EmailPart, TId.EmailPart,
					new Tbl.IAttr[] {
					MainBossServiceGroup,
					new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.Delete))
				},
					new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Order, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentType, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Header, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.ContentLength, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.EmailPart.F.Name, DCol.Normal, ECol.Normal),
					TblGroupNode.New(KB.K("Mail Message Content"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblInitSourceNode.New(null, new DualCalculatedInitValue(StringTypeInfo.Universe, delegate (object[] inputs) {
							var content = (byte[])inputs[1];
							if (content == null)
								return null;
							try {
								var mm = new EmailMessage(EmailMessage.ContentEncoding((string)inputs[0]).GetString(content));
								return mm.MessageAsText(false);
							}
							catch (System.Exception e) {
								var s = new StringBuilder();
								s.AppendLine(Strings.Format(KB.K("Content is not printable")));
								s.Append(Thinkage.Libraries.Exception.FullMessage(e));
								return s.ToString();
							}
						}, new DualPathValue(dsMB.Path.T.EmailPart.F.ContentEncoding), new DualPathValue(dsMB.Path.T.EmailPart.F.Content)), DCol.Normal, ECol.AllReadonly))
					)
				);
			});
			return new CompositeTbl(dsMB.Schema.T.EmailPart, TId.EmailPart,
				new Tbl.IAttr[] {
					MainBossServiceGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.EmailPart.F.Order, BTbl.ListColumnArg.Contexts.SortInitialAscending),
						BTbl.ListColumn(dsMB.Path.T.EmailPart.F.ContentType),
						BTbl.ListColumn(dsMB.Path.T.EmailPart.F.ContentLength),
						BTbl.ListColumn(dsMB.Path.T.EmailPart.F.Name),
						BTbl.AdditionalVerb(KB.K("Save Email Part to a file"),
							delegate(BrowseLogic browserLogic) {
								Libraries.DataFlow.Source emailPartContent = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailPart.F.Content, -1);
								Libraries.DataFlow.Source emailPartContentLength = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailPart.F.ContentLength, -1);
								Libraries.DataFlow.Source emailPartEncoding = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailPart.F.ContentEncoding, -1);
								Libraries.DataFlow.Source emailPartType = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailPart.F.ContentType, -1);
								Libraries.DataFlow.Source emailPartName = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailPart.F.Name, -1);
								Libraries.DataFlow.Source emailPartFileName = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.EmailPart.F.FileName, -1);
								var group = browserLogic.Commands.CreateNestedNode(KB.K("Email Part"),null);
								group.AddCommand(KB.K("View Email Part"), null, new MultiCommandIfAllEnabled( new CallDelegateCommand(KB.K("View the Email Part in an external program"),
									delegate() {
										var name = ((string)emailPartFileName.GetValue())??(string)emailPartName.GetValue();
										var type = (string)emailPartType.GetValue();
										// will save as text (unless we know better)
										var ext = ".txt";
										if( type == "text/html")
											ext = ".htm";
										else if( type == "message/rfc822")
											ext = ".eml";
										else if( !string.IsNullOrWhiteSpace(name) ) {
											var l = name.LastIndexOf('.');
											if( l >= 0 && l < name.Length-1)
												ext = "";
										}
										string fileName = Strings.IFormat("{0}{1}{2}{3}{4}",System.IO.Path.GetTempPath(), Guid.NewGuid().ToString(), name == null ? "": "_", name, ext);
										if( ServiceUtilities.TemporaryFiles == null )
											ServiceUtilities.TemporaryFiles = new System.CodeDom.Compiler.TempFileCollection();
										temporaryFiles.AddFile(fileName,false);
										try {
											if( type == "text/plain" || type == "text/html") {
												var b = EmailMessage.ContentEncoding((string)emailPartEncoding.GetValue()).GetString((byte[])emailPartContent.GetValue());
												System.IO.File.WriteAllText(fileName, b);
											}
											else
												System.IO.File.WriteAllBytes(fileName,(byte[])emailPartContent.GetValue() );
										}
										catch( System.Exception e) {
											System.IO.File.Delete(fileName);
											throw new GeneralException(e, KB.K("Email Part is not viewable"));
										}
										try {
											// note the temp file will not be delete
											// you can't wait for the process to end,
											// since the default is to use the same process if it is out there
											// and the processs that is out there still needs the temp file.
											using(System.Diagnostics.Process.Start(fileName)) { }
										}
										catch( System.Exception e) {
											if( fileName != null ) System.IO.File.Delete(fileName);
											throw new GeneralException(e, KB.K("Cannot view Email Part"));
										}
									}),
									new PartHasContentDisabler(browserLogic, emailPartContentLength),
									browserLogic.NeedSingleSelectionDisabler
								),null);
								group.AddCommand( KB.K("Save Email Part"), null,new MultiCommandIfAllEnabled( new CallDelegateCommand(
									delegate() {
										try {
											using (var saveFile = new System.Windows.Forms.SaveFileDialog()) {
												saveFile.FileName = ((string)emailPartFileName.GetValue())??(string)emailPartName.GetValue();
												saveFile.RestoreDirectory = true;
												saveFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
												var type = (string) emailPartType.GetValue();
												// will save as text (unless we know better)
												saveFile.DefaultExt = ".txt";
												saveFile.AddExtension = true;
												saveFile.Filter =  "Mail Messages (*.eml)|*.eml|All files (*.*)|*.*";
												saveFile.FilterIndex = 0;
												if( type == "text/html") {
													saveFile.DefaultExt = ".htm";
													saveFile.AddExtension = true;
												}
												else if( type == "message/rfc822") {
													saveFile.DefaultExt = ".eml";
													saveFile.AddExtension = true;
												}
												else if( !string.IsNullOrWhiteSpace(saveFile.FileName) ) {
													var l = saveFile.FileName.LastIndexOf('.');
													if( l >= 0 && l < saveFile.FileName.Length-1) {
														saveFile.DefaultExt = saveFile.FileName.Substring(l);
														saveFile.AddExtension = true;
													}
												}
												if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
													if( type == "text/plain" || type == "text/html") {
														var b = EmailMessage.ContentEncoding((string)emailPartEncoding.GetValue()).GetString((byte[])emailPartContent.GetValue());
														System.IO.File.WriteAllText(saveFile.FileName, b);
													}
													else
														System.IO.File.WriteAllBytes(saveFile.FileName,(byte[])emailPartContent.GetValue() );
												}
											}
										}
										catch( System.Exception e) {
											throw new GeneralException(e, KB.K("Cannot save Email Part to a file"));
										}
									}),
									new PartHasContentDisabler(browserLogic, emailPartContentLength),
									browserLogic.NeedSingleSelectionDisabler
								),null);
								group.AddCommand(KB.K("Convert to Email Request"), null, new MultiCommandIfAllEnabled(new CallDelegateCommand(
									delegate() {
										try {
											var message = EmailMessage.ContentEncoding((string)emailPartEncoding.GetValue()).GetString((byte[])emailPartContent.GetValue());
											var id = ServiceUtilities.EmailRequestFromEmail(browserLogic.DB, message,null);
										}
										catch( System.Exception e) {
											throw new GeneralException(e, KB.K("Cannot convert Email Message"));
										}
									}),
									new IsEmailAttachmentDisabler(browserLogic, emailPartType),
									browserLogic.NeedSingleSelectionDisabler
								),null);
								return null;
							}
						)
			),
					new TreeStructuredTbl(dsMB.Path.T.EmailPart.F.ParentID, 3, uint.MaxValue)
				},
				null,
				// The order of the Composite Views matter; The recognition conditions are searched from the END of the list of views to the beginning, so any 'default' recognition condition should be FIRST in the list
				new CompositeView(
					unknownViewerTbl, dsMB.Path.T.EmailPart.F.Id, CompositeView.AddRecognitionCondition(SqlExpression.Constant(true))
					),
				new CompositeView(
					imageViewerTbl, dsMB.Path.T.EmailPart.F.Id, CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.EmailPart.F.ContentType).Like(SqlExpression.Constant("image%")))
					// TODO: If recognition condition added for image, need a another final catchall CompositeView;
					),
				new CompositeView(
					AttachedMailMessageViewerTbl, dsMB.Path.T.EmailPart.F.Id, CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.EmailPart.F.ContentType).Eq(SqlExpression.Constant("message/rfc822")))
					),
				new CompositeView(
					plainTextViewerTbl, dsMB.Path.T.EmailPart.F.Id, CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.EmailPart.F.ContentType).Eq(SqlExpression.Constant("text/plain")))
					),
				new CompositeView(
					htmlViewerTbl, dsMB.Path.T.EmailPart.F.Id, CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.EmailPart.F.ContentType).Eq(SqlExpression.Constant("text/html")))
					)
			);
		}
		#endregion
		#region Common nodes to both the MainBoss Service Status and Manage forms.
		// TODO: Make this Tbl a CompositeTbl with ImageTbl override for each EntryType record ; the images should be the same as used in the system Event log viewer. Need the current panel contents in a separate
		// edit Tbl referenced by each of the CompositeView declarations.
		private static Tbl ServiceLogBrowsetteTbl() {
			DelayedCreateTbl panelTblCreator = new DelayedCreateTbl(delegate () {
				return new Tbl(dsMB.Schema.T.ServiceLog, TId.ServiceLog,
					new Tbl.IAttr[] {
						MainBossServiceAdminGroup
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.ServiceLog.F.EntryDate, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceLog.F.EntryType, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.ServiceLog.F.Message, DCol.Normal)
					)
				);
			});

			return new CompositeTbl(dsMB.Schema.T.ServiceLog, TId.ServiceLog,
				new Tbl.IAttr[] {
					MainBossServiceAdminGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.ServiceLog.F.EntryDate.Key(), new BrowserCalculatedInitValue(
							new StructTypeInfo( false, dsMB.Path.T.ServiceLog.F.EntryDate.ReferencedColumn.EffectiveType, dsMB.Path.T.ServiceLog.F.EntryVersion.ReferencedColumn.EffectiveType ),
							delegate(object[] a)
							{
								return a;
							},
							new BrowserPathValue(dsMB.Path.T.ServiceLog.F.EntryDate), new BrowserPathValue(dsMB.Path.T.ServiceLog.F.EntryVersion)
							),null,BTbl.ListColumnArg.Contexts.TaggedValueProvider | BTbl.ListColumnArg.Contexts.SortNormal),
						BTbl.ListColumn(dsMB.Path.T.ServiceLog.F.EntryDate.Key(), dsMB.Path.T.ServiceLog.F.EntryDate, BTbl.ListColumnArg.Contexts.SortAlternativeValue | BTbl.ListColumnArg.Contexts.SortInitialMask | BTbl.ListColumnArg.Contexts.SortDescendingMask),
						BTbl.ListColumn(dsMB.Path.T.ServiceLog.F.EntryType),
						BTbl.ListColumn(dsMB.Path.T.ServiceLog.F.Message)
						)
					},
					null,
					new CompositeView(panelTblCreator, dsMB.Path.T.ServiceLog.F.Id,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq(SqlExpression.Constant((int)DatabaseEnums.ServiceLogEntryType.Error))),
						CompositeView.IdentificationOverride(TId.ServiceLogError)),
					new CompositeView(panelTblCreator, dsMB.Path.T.ServiceLog.F.Id,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq(SqlExpression.Constant((int)DatabaseEnums.ServiceLogEntryType.Warn))),
						CompositeView.IdentificationOverride(TId.ServiceLogWarning)),
					new CompositeView(panelTblCreator, dsMB.Path.T.ServiceLog.F.Id,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq(SqlExpression.Constant((int)DatabaseEnums.ServiceLogEntryType.Info))),
						CompositeView.IdentificationOverride(TId.ServiceLogInformation)),
					new CompositeView(panelTblCreator, dsMB.Path.T.ServiceLog.F.Id,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq(SqlExpression.Constant((int)DatabaseEnums.ServiceLogEntryType.Activity))),
						CompositeView.IdentificationOverride(TId.ServiceLogActivity)),
					new CompositeView(panelTblCreator, dsMB.Path.T.ServiceLog.F.Id,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.ServiceLog.F.EntryType).Eq(SqlExpression.Constant((int)DatabaseEnums.ServiceLogEntryType.Trace))),
						CompositeView.IdentificationOverride(TId.ServiceLogTrace))
					);
		}
		#endregion
		#region MainBossServiceManageTbl
		private static Tbl MainBossServiceManageTbl() {
			return new Tbl(dsMB.Schema.T.ServiceConfiguration, TId.MainBossService,
				new Tbl.IAttr[] {
					MainBossServiceAdminGroup,
					new BTbl(
						new MB3BTbl.WithManageServiceLogicArg(typeof(MainBossServiceDefinition),
#if DEBUG
							new ManageServiceBrowseLogic.ServiceActionCommand("DEBUG: Process Requestor Notifications", "Notify status changes to waiting requestors", ApplicationServiceRequests.PROCESS_REQUESTOR_NOTIFICATIONS),
							new ManageServiceBrowseLogic.ServiceActionCommand("DEBUG: Process Email Requests", "Retrieve any new Email requests", ApplicationServiceRequests.PROCESS_REQUESTS_INCOMING_EMAIL),
							new ManageServiceBrowseLogic.ServiceActionCommand("DEBUG: Process Assignment Notifications", "Notify status changes to assignees", ApplicationServiceRequests.PROCESS_ASSIGNMENT_NOTIFICATIONS),
							new ManageServiceBrowseLogic.ServiceActionCommand("DEBUG: Stop Service", "Stop the Windows Service for MainBoss", ApplicationServiceRequests.TERMINATE_ALL),
#endif
							new ManageServiceBrowseLogic.ServiceActionCommand("Refresh Service", "Notify the Windows Service for MainBoss to update itself from the configuration record", ApplicationServiceRequests.REREAD_CONFIG),
							new ManageServiceBrowseLogic.ServiceActionCommand("Notify Service", "Notify the Windows Service for MainBoss to do all service actions immediately", ApplicationServiceRequests.PROCESS_ALL),
							new ManageServiceBrowseLogic.ServiceActionCommand("Pause Service", "Pause the Windows Service for MainBoss. While the service is paused it will not do any processing", ApplicationServiceRequests.PAUSE_SERVICE),
							new ManageServiceBrowseLogic.ServiceActionCommand("Resume Service", "Restart the Windows Service for MainBoss when it is paused", ApplicationServiceRequests.RESUME_SERVICE)
						),
						BTbl.SetDummyBrowserPanelControl(new TblLayoutNodeArray(
							// This custom container places its only child centered in a panel
							TblBarePanelNode.New(null,
								delegate(ICommonUIBase ui, UIPanel panel) {
									PanelHelper.SetupCenterOverlayPanel(panel);
									panel.Arranger.VerticalInfiniteGrowth = true;
								},
								new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblUnboundControlNode.New(null,
									DCol.Normal,
									Fmt.SetCreator((CommonLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) =>
										logicObject.CommonUI.UIFactory.CreateLabel(KB.K("You must create a MainBoss Service Configuration record.  Expand this control panel and use the Configuration control panel to create the MainBoss Service Configuration Record."))),
									Fmt.SetBorder(UIBorders.Solid),
									Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Center)
								)
							)
						))
					)
				},
			new TblLayoutNodeArray(
				TblGroupNode.New(KB.K("Service Association"), new TblLayoutNode.ICtorArg[] { DCol.Normal },
				TblColumnNode.New(KB.K("Service Name"), dsMB.Path.T.ServiceConfiguration.F.Code, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ServiceMachineName, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ServiceAccountName, DCol.Normal),
				TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SqlUserid, DCol.Normal),

#if DEBUG
				TblColumnNode.New(KB.T("DEBUG: Version"), dsMB.Path.T.ServiceConfiguration.F.InstalledServiceVersion, DCol.Normal),
#endif 
				TblUnboundControlNode.New(KB.K("Status"), new StringTypeInfo(0, 1024, 3, true, true, true),
					new DCol(Fmt.SetCreatedT<ServiceCommonBrowseLogic>(delegate (ServiceCommonBrowseLogic browseLogic, IBasicDataControl ctrl) {
						ctrl.Value = Strings.Format(KB.K("Status Pending"));
						browseLogic.ServiceStatus = ctrl;
					})))
				),
					TblColumnNode.NewBrowsette(new DelayedCreateTbl(ServiceLogBrowsetteTbl), DCol.Normal)
				)
			);
		}
		#endregion
		#region Helper Classes
		private static Fmt.ICtorArg getPasswordCreatorAttribute() {
			// This returns a password control using a special TypeEditTextHandler to encrypt/decrypt the password; the bound value is the encrypted password.
			return Fmt.SetEditTextHandler((type) => new EncryptingEditTextHandler(type));
		}
		private class EncryptingEditTextHandler : TypeEditTextHandler {
			public EncryptingEditTextHandler(TypeInfo tinfo) {
				pTypeInfo = (BlobTypeInfo)tinfo;
				object minlen = pTypeInfo.SizeType.NativeMinLimit(typeof(int));
				if (minlen != null) {
					System.Diagnostics.Debug.Assert((int)minlen % 2 == 0);
					minlen = (int)minlen / 2;
				}
				object maxlen = pTypeInfo.SizeType.NativeMaxLimit(typeof(int));
				if (maxlen != null) {
					System.Diagnostics.Debug.Assert((int)maxlen % 2 == 0);
					maxlen = (int)maxlen / 2;
				}
				innerHandler = new StringTypeInfo(minlen, maxlen, 0, pTypeInfo.AllowNull, true, true).GetTypeEditTextHandler(Thinkage.Libraries.Application.InstanceFormatCultureInfo);
			}
			private BlobTypeInfo pTypeInfo;
			private TypeEditTextHandler innerHandler;

			public string FormatForEdit(object val) {
				if (val != null)
					val = ServicePassword.Decode((Byte[])val);
				return innerHandler.FormatForEdit(val);
			}
			public object ParseEditText(string str) {
				object result = innerHandler.ParseEditText(str);
				if (result != null)
					result = ServicePassword.Encode((string)result);
				return result;
			}
			public Thinkage.Libraries.TypeInfo.TypeInfo GetTypeInfo() {
				return pTypeInfo;
			}
			public string Format(object val) {
				if (val != null)
					val = ServicePassword.Decode((Byte[])val);
				return innerHandler.Format(val);
			}
			public System.Drawing.StringAlignment PreferredAlignment {
				get {
					return System.Drawing.StringAlignment.Near;
				}
			}
			public SizingInformation SizingInformation {
				get {
					return innerHandler.SizingInformation;
				}
			}
		}
		public static string EmailViewer = null;
		#endregion
		#region MainBoss Service Configuration
		private static Tbl MainBossServiceConfigurationBrowseTbl() {
			return new CompositeTbl(dsMB.Schema.T.ServiceConfiguration, TId.MainBossServiceConfiguration,
				new Tbl.IAttr[] {
					MainBossServiceAdminGroup,
					new BTbl(
						BTbl.ListColumn(KB.K("Service Name"), dsMB.Path.T.ServiceConfiguration.F.Code), BTbl.ListColumn(dsMB.Path.T.ServiceConfiguration.F.Desc),
						BTbl.LogicClass(typeof(ServiceConfigurationBrowseLogic)),
						BTbl.SetDummyBrowserPanelControl(new TblLayoutNodeArray(
							// This custom container places its only child centered in a panel
							TblBarePanelNode.New(null,
								delegate(ICommonUIBase ui, UIPanel panel) {
									PanelHelper.SetupCenterOverlayPanel(panel);
									panel.Arranger.VerticalInfiniteGrowth = true;
								},
								new TblLayoutNode.ICtorArg[] { DCol.Normal },
								TblUnboundControlNode.New(null,
									DCol.Normal,
									Fmt.SetCreator((CommonLogic logicObject, TblLeafNode leafNode, TypeInfo controlTypeInfo, IDisablerProperties enabledDisabler, IDisablerProperties writeableDisabler, ref Key label, Fmt fmt, Settings.Container settingsContainer) =>
										logicObject.CommonUI.UIFactory.CreateLabel(KB.K("You must create or select a MainBoss Service Configuration record."))),
									Fmt.SetBorder(UIBorders.Solid),
									Fmt.SetHorizontalAlignment(System.Drawing.StringAlignment.Center)
								)
							)
						))
					)
				},
				null,
				CompositeView.ChangeEditTbl(MainBossServiceConfigurationEditTblCreator)
			//,
			//	CompositeView.AdditionalVerb(KB.K("Set Service Parameters"),
			//	Thinkage.MainBoss.Controls.ManageServiceBrowseLogic.SetServiceParmeters
			//))
			);
		}
		// Special TBL used to fixup the ServiceName/ServiceMachineName values in a ServiceConfiguration record to match the installed service on the machine.
		private static DelayedCreateTbl FixUpMainBossServiceConfigurationEditTblCreator = new DelayedCreateTbl(
			delegate () {
				return new Tbl(dsMB.Schema.T.ServiceConfiguration, TId.MainBossServiceConfiguration,
					new Tbl.IAttr[] {
						MainBossServiceAdminGroup,
						new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete))
					},
					new TblLayoutNodeArray()
				);
			});

		private static Tbl MainBossServiceConfigurationEditTbl() {
			return new Tbl(dsMB.Schema.T.ServiceConfiguration, TId.MainBossServiceConfiguration,
				new Tbl.IAttr[] {
					MainBossServiceAdminGroup,
					new ETbl(
						ETbl.SetAllowMultiRecordEditing(false),
						ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault)
					)
				},
				new TblLayoutNodeArray(
				DetailsTabNode.New(
					TblColumnNode.New(KB.K("Service Name"), dsMB.Path.T.ServiceConfiguration.F.Code, DCol.Normal, new ECol(Fmt.SetId(ServiceCodeID))),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.Desc, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.AutomaticallyCreateRequestors, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.AutomaticallyCreateRequestorsFromLDAP, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.AutomaticallyCreateRequestorsFromEmail, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.AcceptAutoCreateEmailPattern, DCol.Normal, new ECol(Fmt.SetId(AllowPattern))),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.RejectAutoCreateEmailPattern, DCol.Normal, new ECol(Fmt.SetId(RejectPattern))),
					//TblGroupNode.New(KB.K("Service Installed On"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					//	TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ServiceMachineName, DCol.Normal, ECol.AllReadonly),
					//	TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ServiceName, DCol.Normal, ECol.AllReadonly)
					//),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.Comment, DCol.Normal, ECol.Normal)
				),
				TblTabNode.New(KB.K("Incoming Mail"), KB.K("Settings for retrieving mail from a server"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ProcessRequestorIncomingEmail, DCol.Normal, ECol.Normal),
					TblGroupNode.New(dsMB.Path.T.ServiceConfiguration.F.MailServerType, new TblLayoutNode.ICtorArg[] {ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.MailServerType, new ECol(Fmt.SetId(IncomingMailServerTypeId)))
					),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.MailServerType, DCol.Normal),
					TblGroupNode.New(dsMB.Path.T.ServiceConfiguration.F.Encryption, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.Encryption, new ECol(Fmt.SetId(IncomingMailEncryptionId)))
					),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.Encryption, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.MailServer, DCol.Normal, ECol.Normal),
					TblColumnNode.New(KB.K("Override Default Port"), dsMB.Path.T.ServiceConfiguration.F.MailPort, DCol.Normal, new ECol(Fmt.SetId(IncomingMailServerPortId))),
					TblColumnNode.New(KB.K("Override Default MailBox"), dsMB.Path.T.ServiceConfiguration.F.MailboxName, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.MailUserName, DCol.Normal, ECol.Normal),
					TblColumnNode.New(KB.K("Mail User's Password"), dsMB.Path.T.ServiceConfiguration.F.MailEncryptedPassword, new ECol(getPasswordCreatorAttribute())),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.WakeUpInterval, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.MaxMailSize, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ManualProcessingTimeAllowance, DCol.Normal, ECol.Normal)
				),
				TblTabNode.New(KB.K("Outgoing Mail"), KB.K("Settings for sending email notifications through a mail server"), new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(MainBossServiceAsWindowsServiceGroup), DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ProcessNotificationEmail, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ReturnEmailAddress, DCol.Normal, new ECol(Fmt.SetId(ReturnEmailAddressId))),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.ReturnEmailDisplayName, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.HtmlEmailNotification, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.MainBossRemoteURL, DCol.Normal, new ECol(Fmt.SetId(MainBossRemoteURLId))),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.NotificationInterval, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPServer, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPPort, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPUseSSL, DCol.Normal, ECol.Normal),
					TblGroupNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPCredentialType, new TblLayoutNode.ICtorArg[] { ECol.Normal },
						TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPCredentialType, new ECol(Fmt.SetId(OutgoingMailServerAuthenticationId)))
					),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPCredentialType, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPUserDomain, DCol.Normal, new ECol(Fmt.SetId(OutgoingMailServerUserDomainId))),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPUserName, DCol.Normal, new ECol(Fmt.SetId(OutgoingMailServerUsernameId))),
					TblColumnNode.New(dsMB.Path.T.ServiceConfiguration.F.SMTPEncryptedPassword, new ECol(getPasswordCreatorAttribute(), Fmt.SetId(OutgoingMailServerPasswordId)))
				),
				TblTabNode.New(KB.TOc(TId.Message), KB.K("User custom text for email messages"), new TblLayoutNode.ICtorArg[] { new FeatureGroupArg(MainBossServiceAsWindowsServiceGroup), DCol.Normal, ECol.Normal },
					TblColumnNode.NewBrowsette(TIGeneralMB3.UserMessageKeyPickerTblCreator, DCol.Normal, ECol.Normal, Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.UserMessageKey.F.Context, KB.I("MainBossService"))))
				)),
				new Check1<string>(
					delegate (string code) {
						if (code != null && code.Any(e => !char.IsLetterOrDigit(e)))
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Service Name cannot have any whitespace or any non alphanumeric characters")));
						return null;
					}
				).Operand1(ServiceCodeID),
				new Check1<string>(
					delegate (string returnEmailAddress) {
						// Make sure the email address entered is a valid SMTP EmailAddress
						try {
							if (returnEmailAddress == null)
								return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(new GeneralException(KB.K("No Email Address will prevent notifications from being sent.")));
							else
								Thinkage.Libraries.Mail.MailAddress(returnEmailAddress);
						}
						catch (System.Exception errorException) {
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.T(errorException.Message)));
						}
						return null;
					}
				).Operand1(ReturnEmailAddressId),
				new Check1<string>(
					delegate (string url) {
						if (url != null && !url.StartsWith(KB.I("http"), StringComparison.InvariantCultureIgnoreCase))
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewWarningAll(new GeneralException(KB.K("URL should begin with 'http'")));
						return null;
					}
				).Operand1(MainBossRemoteURLId),
				new Check1<string>(
					delegate (string pattern) {
						if (pattern == null)
							return null;
						try { var reg = new Regex(pattern); }
						catch (ArgumentException) {
							return EditLogic.ValidatorAndCorrector.ValidatorStatus.NewErrorAll(new GeneralException(KB.K("Syntax error in regular expression")));
						}
						return null;
					}).Operand1(AllowPattern),
				new Check1<string>(
					delegate (string pattern) {
						if (pattern == null)
							return null;
						try { var reg = new Regex(pattern); }
						catch (ArgumentException) {
						}
						return null;
					}).Operand1(RejectPattern),
				Init.Continuous(new ControlReadonlyTarget(OutgoingMailServerUserDomainId, BecauseOutgoingMailServerAuthenticationIsNotCustom)
						, new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse, OutgoingMailServerAuthenticationControlReadonly, new ControlValue(OutgoingMailServerAuthenticationId))),
				Init.Continuous(new ControlReadonlyTarget(OutgoingMailServerUsernameId, BecauseOutgoingMailServerAuthenticationIsNotCustom)
						, new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse, OutgoingMailServerAuthenticationControlReadonly, new ControlValue(OutgoingMailServerAuthenticationId))),
				Init.Continuous(new ControlReadonlyTarget(OutgoingMailServerPasswordId, BecauseOutgoingMailServerAuthenticationIsNotCustom)
						, new EditorCalculatedInitValue(BoolTypeInfo.NonNullUniverse, OutgoingMailServerAuthenticationControlReadonly, new ControlValue(OutgoingMailServerAuthenticationId))),
				Init.OnLoadNew(new PathTarget(dsMB.Path.T.ServiceConfiguration.F.Code), new ConstantValue(KB.I("MainBossService"))),
				Init.OnLoadNew(new PathTarget(dsMB.Path.T.ServiceConfiguration.F.Encryption), new ConstantValue(DatabaseEnums.MailServerEncryption.RequireEncryption))
				);
		}
		#endregion

		#endregion
		// TODO: Expand the following 5 Tbl-creator functions in-line as they are only called here and their names are just pollution.
		static TIMainBossService() {
			MainBossServiceManageTblCreator = new DelayedCreateTbl(delegate () { return MainBossServiceManageTbl(); });
			MainBossServiceConfigurationEditTblCreator = new DelayedCreateTbl(delegate () { return MainBossServiceConfigurationEditTbl(); });
		}
		internal static void DefineTblEntries() {
			DefineBrowseTbl(dsMB.Schema.T.EmailPart, delegate () {
				return EmailPartTbl();
			});
			DefineBrowseTbl(dsMB.Schema.T.EmailRequest, delegate () {
				return EmailRequestBrowseTbl();
			});
			DefineBrowseTbl(dsMB.Schema.T.ServiceConfiguration, delegate () {
				return MainBossServiceConfigurationBrowseTbl();
			});
			#region Request Acknowledgment Status
#if DEBUG
			//TODO: The following should be tree structured, with the Request as the parent, and the history as the sub nodes
			DefineTbl(dsMB.Schema.T.RequestAcknowledgement, delegate () {
				return new Tbl(dsMB.Schema.T.RequestAcknowledgement, TId.PendingAcknowledgment,
				new Tbl.IAttr[] {
					MainBossServiceAdminGroup,
					new BTbl(
						BTbl.ListColumn(dsMB.Path.T.RequestAcknowledgement.F.RequestID.F.RequestorID.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.RequestAcknowledgement.F.RequestID.F.Number),
						BTbl.ListColumn(dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.F.EntryDate)
					)
					//,	new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View))		// We don't allow editor access because fetching lookup records causes notifications that upset RecordManager
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						// In lieu of being able to "edit" the record, we use readonly pickers in the panel so you can drill down to the Request and Requestor.
						TblColumnNode.New(dsMB.Path.T.RequestAcknowledgement.F.RequestID.F.RequestorID.F.ContactID, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.RequestAcknowledgement.F.RequestID.F.RequestorID, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestAcknowledgement.F.RequestID, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.RequestAcknowledgement.F.RequestStateHistoryID.F.EntryDate, DCol.Normal, ECol.AllReadonly)
					)
				));
			});
#endif
			#endregion
		}
		private TIMainBossService() {
		}
	}
}
