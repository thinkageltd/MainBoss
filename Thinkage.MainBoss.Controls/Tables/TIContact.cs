using System;
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBILibrary.MSSql;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Presentation.MSWindows;
using Thinkage.Libraries.Sql;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.Controls {
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Location Browsers and pickers
	/// </summary>
	public class TIContact : TIGeneralMB3 {
		#region ContactFunctionProvider
		private static object[] ContactFunctionValues = new object[] {
			(int)ViewRecordTypes.ContactFunctions.Contact,
			(int)ViewRecordTypes.ContactFunctions.Requestor,
			(int)ViewRecordTypes.ContactFunctions.BillableRequestor,
			(int)ViewRecordTypes.ContactFunctions.Employee,
			(int)ViewRecordTypes.ContactFunctions.SalesVendor,
			(int)ViewRecordTypes.ContactFunctions.ServiceVendor,
			(int)ViewRecordTypes.ContactFunctions.AccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.SalesServiceVendor,
			(int)ViewRecordTypes.ContactFunctions.SalesAccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.ServiceAccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.SalesServiceAccountsPayableVendor,
			(int)ViewRecordTypes.ContactFunctions.RequestAssignee,
			(int)ViewRecordTypes.ContactFunctions.WorkOrderAssignee,
			(int)ViewRecordTypes.ContactFunctions.PurchaseOrderAssignee,
			(int)ViewRecordTypes.ContactFunctions.User
		};
		private static Key[] ContactFunctionLabels = new Key[] {
			KB.K("Contact"),
			KB.TOi(TId.Requestor),
			KB.TOi(TId.BillableRequestor),
			KB.TOi(TId.Employee),
			KB.K("Sales Vendor"),
			KB.K("Service Vendor"),
			KB.K("Accounts Payable Vendor"),
			KB.K("Sales Service Vendor"),
			KB.K("Sales Accounts Payable Vendor"),
			KB.K("Service Accounts Payable Vendor"),
			KB.K("Sales Service Accounts Payable Vendor"),
			KB.TOi(TId.RequestAssignee),
			KB.TOi(TId.WorkOrderAssignee),
			KB.TOi(TId.PurchaseOrderAssignee),
			KB.TOi(TId.User)
		};
		public static EnumValueTextRepresentations ContactFunctionProvider = new EnumValueTextRepresentations(ContactFunctionLabels, null, ContactFunctionValues);
		#endregion

		#region Contact Group
		// TODO: The following various contact groups should perhaps be changed to follow the model of CurrentStateHistoryGroup,
		// except to allow several paths to contact rows. This would eliminate the need for all the explicit column labels and the combinatorial explosion
		// of methods based on what columns should be shown.
		/// <summary>
		/// Create the Layout Node for a multicolumn layout of one of more rows of Contact information, specifically the Name, Business Phone, and Email for the contact.
		/// </summary>
		/// <param name="nodes">Definitions for the individual rows, typically the return values from ContactRowGroup</param>
		/// <returns></returns>
		internal static TblContainerNode ContactGroupTblLayoutNode(params TblRowNode[] nodes) {
			return TblMultiColumnNode.New(
				new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
				new Key[] { KB.K("Name"), KB.K("Business Phone"), KB.K("Email") },
				nodes
			);
		}
		protected static TblContainerNode ContactGroupPreferredLanguageTblLayoutNode(params TblRowNode[] nodes) {
			return TblMultiColumnNode.New(
				new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
				new Key[] { KB.K("Name"), KB.K("Email"), KB.K("Preferred Language") },
				nodes
			);
		}
		public static TblContainerNode SingleRequestorLanguagePreferenceGroup(DBI_Path pathToRequestor) {
			return TIContact.ContactGroupPreferredLanguageTblLayoutNode(TblRowNode.New(KB.TOi(TId.Requestor), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(pathToRequestor, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(new DBI_Path(dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, dsMB.Path.T.Contact.F.Code))), ECol.AllReadonly),
					TblColumnNode.New(new DBI_Path(pathToRequestor.PathToReferencedRow, new DBI_Path(dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, dsMB.Path.T.Contact.F.Email)), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(new DBI_Path(pathToRequestor.PathToReferencedRow, new DBI_Path(dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, dsMB.Path.T.Contact.F.PreferredLanguage)), new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)
				));
		}
		/// <summary>
		/// Define a single row for a multicolumn contact row group, where the editor should be able to pick a Contact directly
		/// </summary>
		/// <param name="pathToContact">Path from the Tbl root to the Contact record</param>
		/// <param name="nameEcolAttr">ECol attribute to use on the Name entry in the row</param>
		/// <returns></returns>
		internal static TblRowNode ContactGroupRow(DBI_Path pathToContact, ECol nameEcolAttr) {
			return ContactGroupRow(pathToContact, dsMB.Path.T.Contact, nameEcolAttr);
		}
		/// <summary>
		/// Define a single row for a multicolumn contact row group, where the editor should be able to pick a record that refers to and is identified by a Contact record.
		/// </summary>
		/// <param name="pathToRecord">Path from the Tbl root to the picked record</param>
		/// <param name="pathRecordToContact">Path from the picked record to its identifying Contact</param>
		/// <param name="nameEcolAttr">ECol attribute to use on the Name entry in the row</param>
		/// <returns></returns>
		internal static TblRowNode ContactGroupRow(DBI_Path pathToRecord, DBI_PathToRow pathRecordToContact, ECol nameEcolAttr) {
			return TblRowNode.New(pathToRecord.Key(), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(pathToRecord, new NonDefaultCol(), new DCol(Fmt.SetDisplayPath(new DBI_Path(pathRecordToContact, dsMB.Path.T.Contact.F.Code))), nameEcolAttr),
					TblColumnNode.New(new DBI_Path(pathToRecord.PathToReferencedRow, new DBI_Path(pathRecordToContact, dsMB.Path.T.Contact.F.BusinessPhone)), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(new DBI_Path(pathToRecord.PathToReferencedRow, new DBI_Path(pathRecordToContact, dsMB.Path.T.Contact.F.Email)), DCol.Normal, ECol.AllReadonly));
		}
		/// <summary>
		/// Make a multicolumn Contact row group that represents a Requestor, with phone and email information
		/// </summary>
		/// <param name="pathToRequestor"></param>
		/// <param name="editable"></param>
		/// <returns></returns>
		public static readonly object RequestorPickerNodeId = KB.I("RequestorPickerNodeId");
		public static readonly object RequestorEmailFilterNodeId = KB.I("RequestorEmailFilterNodeId");
		internal static TblContainerNode SingleRequestorGroup(DBI_Path pathToRequestor, bool editable) {
			return ContactGroupTblLayoutNode(
					ContactGroupRow(pathToRequestor, dsMB.Path.T.Requestor.F.ContactID.PathToReferencedRow, editable
						? new ECol(ECol.NormalAccess, Fmt.SetId(RequestorPickerNodeId), Fmt.SetBrowserFilter(BTbl.TaggedEqFilter(dsMB.Path.T.Requestor.F.ContactID.F.Email, RequestorEmailFilterNodeId, true)))
						: ECol.AllReadonly)
				);
		}
		#endregion
		#region Requestor Assignee Group
		/// <summary>
		/// Return a List of nodes to add to a TblLayoutNode Array display common Contact information for an Assignee
		/// </summary>
		internal static TblContainerNode SingleContactGroup(DBI_Path pathToContact) {
			return TIContact.ContactGroupTblLayoutNode(
				TIContact.ContactGroupRow(pathToContact, ECol.Normal)
			);
		}
		#endregion
		#region Merge Contact Command
		private class MergeContactCommand : ICommand {
			//DataSources for all fields in the license list we are updating from
			private Libraries.DataFlow.Source SourceContactToMergeTo;
			private readonly Libraries.DataFlow.Source SourceContactToMergeFrom;
			private readonly Libraries.DataFlow.Source SourceContactToMergeFromCode;
			private readonly Libraries.DataFlow.Source SourceContactToMergeFromHidden;
			public MergeContactCommand(BrowseLogic browser, object targetId) {
				Browser = browser;
				Browser.BrowserSelectionPositioner.Changed += new DataChanged(UpdateEnabling);
				Browser.EndingSelectionUpdate += EndSelectionUpdate;
				SourceContactToMergeFrom = Browser.GetTblPathDisplaySource(dsMB.Path.T.Contact.F.Id, -1);
				SourceContactToMergeFromCode = Browser.GetTblPathDisplaySource(dsMB.Path.T.Contact.F.Code, -1);
				SourceContactToMergeFromHidden = Browser.GetTblPathDisplaySource(dsMB.Path.T.Contact.F.Hidden, -1);
				Disabler = new MultiDisablerIfAllEnabled();
				Disabler.Add(PermissionToMergeDisabler);
				Disabler.Add(SingleRecordDisabler);
				Disabler.Add(MultipleRecordDisabler);
				Disabler.Add(MergeTargetDisabler);
				Browser.ControlCreationCompletedNotificationRecipients += () => {
					var c = new Libraries.Presentation.ControlValue(targetId);
					try {
						SourceContactToMergeTo = c.GetSourceForInit(Browser, -1, out bool NeedsContext);
					}
					catch (System.NullReferenceException) { } // the id is not there, occurs when the control is set up for a drop down
				};
				Browser.BrowserSelectionPositioner.Changed += new DataChanged(UpdateEnabling);
			}

			private readonly BrowseLogic Browser;
			private readonly MultiDisablerIfAllEnabled Disabler;
			// The source in the browser for the ID of the row as the source of the new license record
			public void Execute() {
				var ContactsToBeDeleted = new List<string>();
				var mergeTo = (System.Guid)SourceContactToMergeTo.GetValue();
				var toCode = "";
				using (var ds = new dsMB(Browser.DB)) {
					ds.EnsureDataTableExists(dsMB.Schema.T.Contact);
					ds.DB.ViewAdditionalRows(ds, dsMB.Schema.T.Contact, new SqlExpression(dsMB.Path.T.Contact.F.Id).Eq(mergeTo));
					if (ds.T.Contact.Rows.Count != 1)
						return; // row is not there, possible if some else is doing a merge
					var r = (dsMB.ContactRow)ds.T.Contact.Rows[0];
					toCode = ContactAsString(r.F.Hidden, r.F.Code);
				}
				Browser.IterateOverContextRecords(() => {
					var mergeFrom = (System.Guid)SourceContactToMergeFrom.GetValue();
					var fromCode = ContactAsString((DateTime?)SourceContactToMergeFromHidden.GetValue(), (string)SourceContactToMergeFromCode.GetValue());
					ContactsToBeDeleted.Add(ContactAsString((DateTime?)SourceContactToMergeFromHidden.GetValue(), (string)SourceContactToMergeFromCode.GetValue()));
					return true;
				});
				if (ContactsToBeDeleted.Count == 0)
					return; //  some used "clear" on the main Browser.

				if (Ask.Question(buildQuestion(toCode, ContactsToBeDeleted)) != Ask.Result.Yes)
					return;
				using (DBDataSet dsmerge = XAFDataSet.New(Browser.TInfo.Schema.Database, Browser.DB)) {
					dsmerge.EnforceConstraints = false;
					dsmerge.DataSetName = KB.I("BrowseBaseControl.MergeContact.dsMove");
					var dbuser = dsmerge.DB.Session.ConnectionInformation.UserIdentification;
					var winuser = Strings.IFormat("{0}\\{1}", Environment.UserDomainName, Environment.UserName);
					if (string.Compare(dbuser, winuser, true) == 0)
						dbuser = Strings.Format(KB.K("SQL User '{0}'"), dbuser);
					else
						dbuser = Strings.Format(KB.K("SQL user '{0}' (Windows user '{1}')"), dbuser, winuser);

					var batch = new CommandBatchSpecification();
					batch.Commands.Add(new MSSqlLiteralCommandSpecification(mergeSql));
					var pmergeto = batch.CreateNormalParameter(KB.I("keepContact"), SourceContactToMergeTo.TypeInfo);
					var pmergefrom = batch.CreateNormalParameter(KB.I("deleteContact"), SourceContactToMergeFrom.TypeInfo);
					var psubject = batch.CreateNormalParameter(KB.I("subject"), new Thinkage.Libraries.TypeInfo.StringTypeInfo(0, 100, 1, true, true, true));
					var pdescription = batch.CreateNormalParameter(KB.I("description"), new Thinkage.Libraries.TypeInfo.StringTypeInfo(0, 1000, 1, true, true, true));
					pmergeto.Value = mergeTo;
					psubject.Value = Strings.Format(KB.K("Merging contacts"));

					Browser.IterateOverContextRecords(() => {
						var mergeFrom = (System.Guid)SourceContactToMergeFrom.GetValue();
						if (mergeFrom == null || mergeTo == null) return true;
						var fromCode = ContactAsString((DateTime?)SourceContactToMergeFromHidden.GetValue(), (string)SourceContactToMergeFromCode.GetValue());
						pmergefrom.Value = mergeFrom;
						pdescription.Value = Strings.Format(KB.K("{0} merged contact {1} into contact {2}"), dbuser, fromCode, toCode);
						dsmerge.DB.Session.PerformTransaction(true, () => dsmerge.DB.Session.ExecuteCommandBatch(batch));
						return true;
					});
					Browser.RefreshCommand.Execute();
				}
			}
			private static string buildQuestion(string toContact, List<string> fromContacts) {
				var question = new System.Text.StringBuilder();
				question.AppendLine(Strings.Format(KB.K("Merging contacts should only be done when you are the sole user of MainBoss")));
				question.AppendLine();
				if (fromContacts.Count > 1)
					question.AppendLine(Strings.Format(KB.K("Do you want to merge contacts:")));
				else
					question.AppendLine(Strings.Format(KB.K("Do you want to merge contact:")));
				question.AppendLine();
				question.Append(string.Join("," + Environment.NewLine, fromContacts.Take(20)));
				question.AppendLine();
				if (fromContacts.Count > 50)
					question.AppendLine(Strings.Format(KB.K("plus {0} more"), fromContacts.Count - 20));
				question.AppendLine();
				question.AppendLine(Strings.Format(KB.K("into contact:")));
				question.AppendLine();
				question.AppendLine(toContact);
				question.AppendLine();
				question.AppendLine(Strings.Format(KB.K(@"
Contacts from the source of the merge will be deleted including their associated MainBoss user.
The merge will not delete any of the SQL Database Logins or SQL Database Users.
If desired the SQL Database Login and SQL Database User may be manually deleted.
")));
				question.AppendLine();
				question.AppendLine(Strings.Format(KB.K("If you or other users are currently using the contact which is the source of the merge, the merge may cause transient errors in MainBoss, restarting MainBoss will fix the problem")));
				question.AppendLine();
				question.AppendLine(Strings.Format(KB.K("This action cannot be undone")));
				return question.ToString();
			}
			private void UpdateEnabling(DataChangedEvent changeType, Position affectedRecord) {
				if (Browser.InSelectionUpdate) {
					// If we get a notification while the browser is altering the selection, we just note that it happened and we update our
					// enabling once the Selection Update it done.
					// This speeds things up when there is a large change in the selected records (Inverting the selection being pretty much worst-case).
					NeedDeferredUpdate = true;
					return;
				}
				int count = 0;
				Browser.IterateOverContextRecords(() => ++count < 2);
				SingleRecordDisabler.Enabled = count > 0;
#if !DEBUG // merging is dangerous, but be able to merge multiple records make debugging easier
				MultipleRecordDisabler.Enabled = count < 2;
#endif
				try {
					MergeTargetDisabler.Enabled = SourceContactToMergeTo?.GetValue() != null;
				}
				catch (Thinkage.Libraries.TypeInfo.NonnullValueRequiredException) { // someone used "clear" on the control
					MergeTargetDisabler.Enabled = false;
				}
				Enabled = Disabler.Enabled;
			}
			private bool NeedDeferredUpdate = false;

			private void EndSelectionUpdate() {
				if (NeedDeferredUpdate) {
					NeedDeferredUpdate = false;
					UpdateEnabling(DataChangedEvent.Reset, null);
				}
			}
			public void TargetPickerChanged() {
				UpdateEnabling(Libraries.DataFlow.DataChangedEvent.Reset, null);
			}

			string ContactAsString(DateTime? deleted, string code) {
				if (deleted == null)
					return Strings.IFormat("'{0}'", code);
				else
					return Strings.Format(KB.K("'{0}'    deleted on {1}"), code, ((DateTime)deleted).ToString("d"));
			}

			#region Contact Merge SQL Code
			private readonly string mergeSql = @"
	begin transaction MergeContact
		declare @work uniqueidentifier
		declare @now datetime = dbo._DClosestValue(getdate(), 2, 100)
		set @work = (select top 1 id from Requestor where ContactId = @keepContact or ContactId = @deleteContact 
			order by (case when Hidden is null then 0 else 1 end ),(case when ContactID = @keepContact then 0 else 1 end),Hidden desc  )
		update Requestor set ContactID   = @keepContact from Requestor where Id = @work and ContactId != @keepContact
		update Request   set RequestorID = @work from Request	join Requestor on RequestorID = Requestor.Id where RequestorId != @work and (ContactID = @keepContact or ContactId = @deleteContact)
		update WorkOrder set RequestorID = @work from WorkOrder join Requestor on RequestorID = Requestor.Id where RequestorId != @work and (ContactID = @keepContact or ContactId = @deleteContact)
		delete from Requestor where Id !=  @work and (ContactID = @deleteContact or ContactId = @keepContact)
				
		set @work = (select top 1 id from BillableRequestor where ContactId = @keepContact or ContactId = @deleteContact 
			order by (case when Hidden is null then 0 else 1 end ),(case when ContactID = @keepContact then 0 else 1 end),Hidden desc  )
		update BillableRequestor set ContactID = @keepContact from BillableRequestor where Id = @work and ContactId != @keepContact
		update ChargeBack set BillableRequestorID = @work from ChargeBack join BillableRequestor on BillableRequestorID = BillableRequestor.Id where BillableRequestorID != @work and (ContactID = @keepContact or ContactId = @deleteContact)
		delete from BillableRequestor where Id !=  @work and (ContactID = @deleteContact or ContactId = @keepContact)

		set @work = (select top 1 id from Employee where ContactId = @keepContact or ContactId = @deleteContact 
			order by (case when Hidden is null then 0 else 1 end ),(case when ContactID = @keepContact then 0 else 1 end),Hidden desc  )
		update Employee  set ContactID  = @keepContact  from Employee   where @work = id and Employee.ContactID = @deleteContact

		update old set EmployeeID = @work, Hidden = @now
			from LaborInside as old 
			join LaborInside as new on new.Code = old.Code and new.TradeID = old.TradeId
			join Employee on old.EmployeeID = Employee.Id
				where new.id = @work and new.id != old.id and ContactId = @deleteContact and new.Hidden is null and old.Hidden is null

		update old set EmployeeID = @work, Hidden = @now
			from OtherWorkInside as old 
			join OtherWorkInside as new on new.Code = old.Code and new.TradeID = old.TradeId
			join Employee on old.EmployeeID = Employee.Id
				where new.id = @work  and new.id != old.id   and ContactId = @deleteContact and new.Hidden is null and old.Hidden is null

		update old set EmployeeID = @work, Hidden = @now
			from _DLaborInside as old 
			join _DLaborInside as new on new.Code = old.Code and new.TradeID = old.TradeId
			join Employee on old.EmployeeID = Employee.Id
				where new.id = @work    and new.id != old.id and ContactId = @deleteContact and new.Hidden is null and old.Hidden is null

		update old set EmployeeID = @work, Hidden = @now
			from _DOtherWorkInside as old 
			join _DOtherWorkInside as new on new.Code = old.Code and new.TradeID = old.TradeId
			join Employee on old.EmployeeID = Employee.Id
				where new.id = @work    and new.id != old.id  and ContactId = @deleteContact and new.Hidden is null and old.Hidden is null

		update LaborInside       set EmployeeId = @work    from LaborInside       join Employee on EmployeeId = Employee.Id where EmployeeId != @work and (ContactID = @deleteContact or ContactID = @keepContact)
		update OtherWorkInside   set EmployeeId = @work    from OtherWorkInside   join Employee on EmployeeId = Employee.Id where EmployeeId != @work and (ContactID = @deleteContact or ContactID = @keepContact)
		update _DLaborInside     set EmployeeId = @work    from _DLaborInside     join Employee on EmployeeId = Employee.Id where EmployeeId != @work and (ContactID = @deleteContact or ContactID = @keepContact)
		update _DOtherWorkInside set EmployeeId = @work    from _DOtherWorkInside join Employee on EmployeeId = Employee.Id where EmployeeId != @work and (ContactID = @deleteContact or ContactID = @keepContact)
		update ItemIssue         set EmployeeId = @work    from ItemIssue         join Employee on EmployeeId = Employee.Id where EmployeeId != @work and (ContactID = @deleteContact or ContactID = @keepContact)
		update _DItemIssue       set EmployeeId = @work    from _DItemIssue       join Employee on EmployeeId = Employee.Id where EmployeeId != @work and (ContactID = @deleteContact or ContactID = @keepContact)
		update Employee          set Hidden = null where id = @keepContact and Hidden is not null  and (select count(*) from Employee where hidden is null and ContactID = @keepContact) > 0
		delete from Employee where employee.id != @work and (ContactID = @keepContact or ContactID = @deleteContact)

		if( @keepContact = @deleteContact )
		begin 
			commit transaction MergeContact
			return
		end

	-- Update tables that do not have derived records.

		update Vendor set salescontactid = @keepContact where salescontactid = @deletecontact
		update Vendor set servicecontactid = @keepContact where servicecontactid = @deletecontact
		update Vendor set payablescontactid = @keepContact where payablescontactid = @deletecontact
		update _DVendor set salescontactid = @keepContact where salescontactid = @deletecontact
		update _DVendor set servicecontactid = @keepContact where servicecontactid = @deletecontact
		update _DVendor set payablescontactid = @keepContact where payablescontactid = @deletecontact
		update _DUnitRelatedContact set contactid = @keepContact where contactid = @deletecontact
		update _DRequestor set contactid = @keepContact where contactid = @deletecontact
		update _DPurchaseOrderAssignee set contactid = @keepContact where contactid = @deletecontact
		update _DRequestAssignee set contactid = @keepContact where contactid = @deletecontact
		update _DWorkOrderAssignee set contactid = @keepContact where contactid = @deletecontact
		update _DBillableRequestor set ContactID = @keepContact where ContactID = @deleteContact
		update _DEmployee set ContactID = @keepContact where ContactID = @deleteContact

		set @work = (select top 1 id from RequestAssignee where ContactID = @keepContact)
		update RequestAssignee set ContactID = @keepContact from RequestAssignee where @work is null and ContactID = @deleteContact
		delete from new from RequestAssignment as old join  RequestAssignment as new on old.RequestID = new.RequestID join RequestAssignee on ContactID = @deleteContact
			where  new.RequestAssigneeID = @work and  Contactid = @deleteContact and new.LastNotificationDate < old.LastNotificationDate or new.LastNotificationDate is null
		delete from old from RequestAssignment as old join  RequestAssignment as new on old.RequestID = new.RequestID join RequestAssignee on ContactID = @deleteContact
			where  new.RequestAssigneeID = @work and  Contactid = @deleteContact 
		update RequestAssignment set RequestAssigneeId = @work from RequestAssignment join RequestAssignee on  RequestAssigneeID = RequestAssignee.Id where ContactID = @deleteContact 
		delete from RequestAssignee where ContactID = @deleteContact

		set @work = (select top 1 id from WorkOrderAssignee where ContactID = @keepContact)
		update WorkOrderAssignee set ContactID = @keepContact from WorkOrderAssignee where @work is null and ContactID = @deleteContact
		delete from old from WorkOrderAssignment as old join  WorkOrderAssignment as new on old.WorkOrderID = new.WorkOrderID join WorkOrderAssignee on ContactID = @deleteContact
			where  new.WorkOrderAssigneeID = @work and  Contactid = @deleteContact
		update WorkOrderAssignment set WorkOrderAssigneeId = @work from WorkOrderAssignment join WorkOrderAssignee on WorkOrderAssigneeID = WorkOrderAssignee.Id where ContactID = @deleteContact 
		delete from WorkOrderAssignee where ContactID = @deleteContact

		set @work = (select top 1 id from PurchaseOrderAssignee where ContactID = @keepContact)
		update PurchaseOrderAssignee set ContactID = @keepContact from PurchaseOrderAssignee where @work is null and ContactID = @deleteContact
		delete from new from PurchaseOrderAssignment as old join  PurchaseOrderAssignment as new on old.PurchaseOrderID = new.PurchaseOrderID join PurchaseOrderAssignee on ContactID = @deleteContact
			where  new.PurchaseOrderAssigneeID = @work and  Contactid = @deleteContact and new.LastNotificationDate < old.LastNotificationDate or new.LastNotificationDate = null
		delete from old from PurchaseOrderAssignment as old join  PurchaseOrderAssignment as new on old.PurchaseOrderID = new.PurchaseOrderID join PurchaseOrderAssignee on ContactID = @deleteContact
			where  new.PurchaseOrderAssigneeID = @work and  Contactid = @deleteContact 
		update PurchaseOrderAssignment set PurchaseOrderAssigneeId = @work from PurchaseOrderAssignment join PurchaseOrderAssignee on PurchaseOrderAssigneeID = PurchaseOrderAssignee.Id where ContactID = @deleteContact 
		delete from PurchaseOrderAssignee where ContactID = @deleteContact

		delete from a from UnitRelatedContact as a join UnitRelatedContact as b on a.ContactID = @deleteContact and b.Contactid = @keepContact and a.RelationshipID = b.RelationshipID and a.UnitLocationID = b.UnitLocationID 
		update UnitRelatedContact set ContactID = @keepContact where ContactID = @deletecontact

		-- if two records that use the same employee are the same they probably should also be merged.
		delete from Employee where ContactID = @deleteContact
		-- 
		-- User table is treated specially
		-- if deleteConcact has no associated user nothing to do nothing
		-- if keepContact has no associated user, move the user the keepContact
		-- otherwise  move all the user entries of the deleteContact to the KeepContact
		-- The user contact is unique and we can hide but we cannot delete it.

		if( (select count(*) from [User] where ContactID = @keepContact  ) = 0 )
			update [User] set ContactId = @keepContact where ContactId = @deleteContact
		else
			update [User] set Hidden =  @now where ContactId = @deleteContact and hidden is null


		--	Users in general cannot be merged unless they have the same authenticationCredentails.
		drop table if exists #mappings 
		select a.id as keep, b.id as remove into #mappings from [User] as a join [User] as b 
			on (a.Hidden is null or (b.Hidden is not null and a.Hidden > b.Hidden)) and a.ContactID = @keepContact and b.ContactID = @deletecontact
		if( (select count(*) from #mappings ) != 0 ) begin
			update AccountingTransaction        set Userid = keep from AccountingTransaction       join #mappings on remove = Userid
			update _DAccountingTransaction      set Userid = keep from _DAccountingTransaction     join #mappings on remove = Userid
			update PMGenerationBatch            set Userid = keep from PMGenerationBatch           join #mappings on remove = Userid
			update PurchaseOrderStateHistory    set Userid = keep from PurchaseOrderStateHistory   join #mappings on remove = Userid
			update _DPurchaseOrderStateHistory  set Userid = keep from _DPurchaseOrderStateHistory join #mappings on remove = Userid
			update RequestStateHistory          set Userid = keep from RequestStateHistory         join #mappings on remove = Userid
			update _DRequestStateHistory        set Userid = keep from _DRequestStateHistory       join #mappings on remove = Userid
			update WorkOrderStateHistory        set Userid = keep from WorkOrderStateHistory       join #mappings on remove = Userid
			update _DWorkOrderStateHistory      set Userid = keep from _DWorkOrderStateHistory     join #mappings on remove = Userid
	--	    update MeterReading                 set Userid = keep from MeterReading                join #mappings on remove = Userid -- not in 4.2
	--		update _MeterReading                set Userid = keep from MeterReading                join #mappings on remove = Userid -- would never occur
			delete DefaultSettings                           from DefaultSettings                  join #mappings on remove = Userid -- can't preserve, the 'settings' may not move
			update Settings                     set Userid = keep from Settings	                   join #mappings on remove = Userid
				where not exists( select * from Settings as a where a.Userid = keep and Settings.SettingsNameID = a.SettingsNameID )
			delete Settings                     from Settings                                      join #mappings on remove = Userid
		   	update UserRole                     set Userid = keep from UserRole                    join #mappings on remove = Userid
				where not exists( select * from UserRole as a where a.Userid = keep and Userrole.PrincipalID = a.PrincipalID )
			delete UserRole                     from UserRole                                      join #mappings on  remove = Userid
	--		delete _DUserRole                   from UserRole                                      join #mappings on  remove = Userid -- would never occur
			delete from [user]                  from [user]                                        join #mappings on [user].id = remove
		end
		drop table #mappings
		update new  
			set Comment = coalesce(new.Comment+CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+old.Comment, new.Comment, old.Comment),
					BusinessPhone = coalesce(new.BusinessPhone, old.BusinessPhone),
					FaxPhone = coalesce(new.FaxPhone, old.FaxPhone),
					HomePhone = coalesce(new.HomePhone, old.HomePhone),
					MobilePhone = coalesce(new.MobilePhone, old.MobilePhone),
				Email = coalesce(new.Email, old.Email),
				WebURL = coalesce(new.WebURL, old.WebURL),
				PreferredLanguage = coalesce(new.PreferredLanguage, old.PreferredLanguage),
				LDAPGuid = coalesce(new.LDAPGuid, old.LDAPGuid),
				AlternateEmail = ltrim(rtrim(coalesce( new.AlternateEmail+' ','') 
							+ case when new.Email is not null and old.Email is not null and old.Email != new.Email  then old.email + ' ' else '' end
							+ case when old.AlternateEmail is not  null and old.AlternateEmail != new.Email and old.AlternateEmail != new.AlternateEmail then old.AlternateEmail else '' end)),
				[Hidden] = case when new.Hidden is not null and old.Hidden is not null then coalesce(new.Hidden,old.Hidden) else null end
			from Contact as new
			join Contact as old on old.Id = @deleteContact 
			where new.ID = @keepContact

		-- hide the contact to be deleted
		update Contact set Hidden = @now where Hidden is null  and Id = @deleteContact
		insert into DatabaseHistory (Id, EntryDate, Subject, Description) values ( NEWID(), @now, @subject, @description)
		-- if the deleted contact has no user record the contact record can be deleted.
		delete Contact where Contact.id = @deleteContact and not Exists(select * from [User] where [User].ContactID = @deleteContact)
	commit transaction MergeContact
";
			#endregion

			#region IDisablerProperties Members
			public Libraries.Translation.Key Tip {
				get {
					if (Enabled)
						return KB.K("Merge all contact information from the selected contacts into the target contact");
					else
						return Disabler.Tip;
				}
			}
			public bool Enabled {
				get {
					return pEnabled;
				}
				set {
					if (pEnabled != value) {
						pEnabled = value;
						EnabledChanged?.Invoke();
					}
				}
			}
			private bool pEnabled;
			public event IEnabledChangedEvent EnabledChanged;
			SettableDisablerProperties MergeTargetDisabler = new SettableDisablerProperties(null, KB.K("Select a contact to be merged into"), false);
			SettableDisablerProperties SingleRecordDisabler = new SettableDisablerProperties(null, KB.K("Select a contact to be merged from"), false);
			SettableDisablerProperties MultipleRecordDisabler = new SettableDisablerProperties(null, KB.K("Only one Contact at a time can be merged"), true);
			PermissionDisabler PermissionToMergeDisabler = ((PermissionDisabler)Application.Instance.GetInterface<ITblDrivenApplication>().PermissionsManager.GetPermission(Root.Rights.Action.MergeContacts));

			#endregion
			public virtual bool RunElevated { get { return false; } }
		}
		#endregion
		public static DelayedCreateTbl ContactWithMergeTbl;
		public static readonly DelayedCreateTbl ContactFunctionsBrowsetteTblCreator = null;
		static TIContact() {
			#region ContactFunctions
			ContactFunctionsBrowsetteTblCreator = new DelayedCreateTbl(delegate () {
				Key assigneeGroup = KB.K("New Assignee");
				return new CompositeTbl(dsMB.Schema.T.ContactFunctions, TId.ContactFunction,
					new Tbl.IAttr[] {
						new BTbl(BTbl.ListColumn(dsMB.Path.T.ContactFunctions.F.TableEnum))
					},
					dsMB.Path.T.ContactFunctions.F.TableEnum,
					null,               // Table 0 (Contact)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.RequestorID,               // Table 1 (Requestor)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Requestor.F.ContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.BillableRequestorID,       // Table 2 (BillableRequestor)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.BillableRequestor.F.ContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.EmployeeID,                // Table 3 (Employee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Employee.F.ContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID,                  // Table 4 (Sales Vendor)
						CompositeView.ContextFreeInit(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Vendor.F.SalesContactID),
						CompositeView.ContextFreeInit(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Vendor.F.ServiceContactID),
						CompositeView.ContextFreeInit(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.Vendor.F.PayablesContactID)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 5 (Service Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 6 (Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 7 (Sales & Service Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 8 (Sales & Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 9 (Service & Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.VendorID, NoNewMode),// Table 10 (Sales, Service & Accounts Payable Vendor)
					new CompositeView(dsMB.Path.T.ContactFunctions.F.RequestAssigneeID,     // Table 11 (RequestAssignee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.RequestAssignee.F.ContactID),
						CompositeView.NewCommandGroup(assigneeGroup)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.WorkOrderAssigneeID,   // Table 12(WorkOrderAssignee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.WorkOrderAssignee.F.ContactID),
						CompositeView.NewCommandGroup(assigneeGroup)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.PurchaseOrderAssigneeID,// Table 13 (PurchaseOrderRequestAssignee)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.PurchaseOrderAssignee.F.ContactID),
						CompositeView.NewCommandGroup(assigneeGroup)),
					new CompositeView(dsMB.Path.T.ContactFunctions.F.UserID,                // Table 14 (User)
						CompositeView.PathAlias(dsMB.Path.T.ContactFunctions.F.ParentContactID, dsMB.Path.T.User.F.ContactID),
						NoNewMode)
				);
			});
			#endregion
			#region Contact
			Tbl contactCreatorFromDirectoryServiceTbl = new Tbl(dsUserPrincipal.Schema.T.UserPrincipal, TId.UserPrincipalInformation,
				new Tbl.IAttr[] {
					new UseNamedTableSchemaPermissionTbl(dsMB.Schema.T.Contact),
					xyzzy.ContactsDependentGroup,
					new BTbl(
						BTbl.LogicClass(typeof(ContactFromDirectoryServiceBrowseLogic)),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.Name),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.DisplayName),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.BusPhone),
						BTbl.ListColumn(dsUserPrincipal.Path.T.UserPrincipal.F.EmailAddress)
					),
					new ETbl(ETbl.EditorDefaultAccess(false)),
					new CustomSessionTbl(delegate(XAFClient existingDatabaseAccess, DBI_Database schema, out bool callerHasCustody) {
						callerHasCustody = true;
						return new XAFClient(existingDatabaseAccess.ConnectionInfo, new UserFromDirectoryServiceSession(existingDatabaseAccess));
					})
				},
				new TblLayoutNodeArray(
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.Name, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.DisplayName, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.BusPhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.EmailAddress, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.HomePhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.PagerPhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.MobilePhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.FaxPhone, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.WebURL, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.PreferredLanguage, DCol.Normal),
					TblColumnNode.New(dsUserPrincipal.Path.T.UserPrincipal.F.LDAPPath, DCol.Normal)
				)
			);
			DefineTbl(dsMB.Schema.T.Contact, delegate () {
				return new Tbl(dsMB.Schema.T.Contact, TId.Contact,
				new Tbl.IAttr[] {
						ContactsDependentGroup,
						// Note that there was up until Oct 24/08 a ContactBrowseControl which contained #if'ed-off inoperational code to import from Outlook Contacts
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Contact.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Contact.F.BusinessPhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
							BTbl.ListColumn(dsMB.Path.T.Contact.F.MobilePhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
							BTbl.AdditionalVerb(KB.K("Create from Active Directory"),
								delegate(BrowseLogic browserLogic) {
									return new CallDelegateCommand(
										delegate() {
											BrowseForm.NewBrowseForm(browserLogic.CommonUI.UIFactory, browserLogic.DB, contactCreatorFromDirectoryServiceTbl).ShowForm();
										}
									);
								}
							)
						),
						new ETbl(
							ETbl.CustomCommand(delegate(EditLogic editorLogic) {
								if (editorLogic.WillBeEditingDefaults)
									return null;
								Source CommentSource = editorLogic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.Comment, 0);
								var userDirectory = new EditLogic.CommandDeclaration(KB.K("Initialize all from Active Directory"), new FillContactFromUserDirectoryCommand(editorLogic, false));
								var useActiveDirectoryGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								useActiveDirectoryGroup.Add(userDirectory);
								return useActiveDirectoryGroup;
							}),
							ETbl.CustomCommand(delegate(EditLogic editorLogic) {
								if (editorLogic.WillBeEditingDefaults)
									return null;
								var userDirectory = new EditLogic.CommandDeclaration(KB.K("Update all from Active Directory"), new FillContactFromUserDirectoryCommand(editorLogic, true));
								var useActiveDirectoryGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								useActiveDirectoryGroup.Add(userDirectory);
								return useActiveDirectoryGroup;
							})
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.ContactReport; }))
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.Contact.F.Code, DCol.Normal, new ECol(Fmt.SetId(ContactNameId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.BusinessPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.Email, DCol.Normal, new ECol(Fmt.SetId(EmailAddressId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.AlternateEmail, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.HomePhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.PagerPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.MobilePhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.FaxPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.WebURL, DCol.Normal, new ECol(Fmt.SetId(WebUrlAddressId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LocationID.F.Desc, DCol.Normal),
						// TODO: In the following we only really need the version check for the ECol since the edit buffer fetches the entire record and the schema must match even for fields we don't use,
						// but MinimumDBVersionTbl is not an ECol.ICtorArg
						TblColumnNode.New(dsMB.Path.T.Contact.F.Id.L.User.ContactID.F.AuthenticationCredential, ECol.AllReadonly, new MinimumDBVersionTbl(new Version(1, 1, 4, 2)), DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.PreferredLanguage, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LDAPGuid, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.Contact.F.Comment, DCol.Normal, ECol.Normal)
					),
					// NOTE: Currently the Requestors tab appears blank because the only information that the Requestor
					// Tbl entry displays comes from the Contact record, and these fields get removed since this is
					// a browsette on the Contact table.
					BrowsetteTabNode.New(TId.ContactFunction, TId.Contact,
						TblColumnNode.NewBrowsette(ContactFunctionsBrowsetteTblCreator, dsMB.Path.T.ContactFunctions.F.ParentContactID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ContactRelation, TId.Contact,
						TblColumnNode.NewBrowsette(TIRelationship.ContactRelatedRecordsBrowseTbl, dsMB.Path.T.ContactRelatedRecords.F.ThisContactID, DCol.Normal, ECol.Normal))
				), EmailAddressValidator, WebUrlAddressValidator
				);
			});
			RegisterExistingForImportExport(TId.Contact, dsMB.Schema.T.Contact);
			#endregion

			#region Contact with Merge
			ContactWithMergeTbl = new DelayedCreateTbl(() => {
				object targetContactId = KB.I("Target ContentID");
				return new Tbl(dsMB.Schema.T.Contact, TId.Contact,
					new Tbl.IAttr[] {
						ContactsDependentGroup,
						// Note that there was up until Oct 24/08 a ContactBrowseControl which contained #if'ed-off inoperational code to import from Outlook Contacts
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.Contact.F.Code),
							BTbl.ListColumn(dsMB.Path.T.Contact.F.BusinessPhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
							BTbl.ListColumn(dsMB.Path.T.Contact.F.MobilePhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
							BTbl.AdditionalVerb(KB.K("Create from Active Directory"),
								delegate(BrowseLogic browserLogic) {
									return new CallDelegateCommand(
										delegate() {
											BrowseForm.NewBrowseForm(browserLogic.CommonUI.UIFactory, browserLogic.DB, contactCreatorFromDirectoryServiceTbl).ShowForm();
										}
									);
								}
							),
							BTbl.AdditionalVerb(null,
								delegate (BrowseLogic browserLogic) {
									var cmd = new MergeContactCommand(browserLogic, targetContactId);
									browserLogic.Commands.AddCommand(KB.K("Merge into Contact"), null, cmd, null, null,
										TblUnboundControlNode.New(KB.K("Merge to"), dsMB.Schema.T.Contact.F.Id.EffectiveType,
											new DCol(Fmt.SetId(targetContactId),
												Fmt.SetPickFrom(dsMB.Schema.T.Contact, new SettableDisablerProperties(KB.K("Choose the contact which the selected contacts will be merged into"), KB.K("Select a contact to be merged into"), true)),
												Fmt.SetCreated(delegate (IBasicDataControl c) { c.Notify += cmd.TargetPickerChanged; })
											)
										)
									);
									return null;
								}
							)
						),
						new ETbl(
							ETbl.CustomCommand(delegate(EditLogic editorLogic) {
								if (editorLogic.WillBeEditingDefaults)
									return null;
								Source CommentSource = editorLogic.GetPathNotifyingValue(dsMB.Path.T.Contact.F.Comment, 0);
								var userDirectory = new EditLogic.CommandDeclaration(KB.K("Initialize all from Active Directory"), new FillContactFromUserDirectoryCommand(editorLogic, false));
								var useActiveDirectoryGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								useActiveDirectoryGroup.Add(userDirectory);
								return useActiveDirectoryGroup;
							}),
							ETbl.CustomCommand(delegate(EditLogic editorLogic) {
								if (editorLogic.WillBeEditingDefaults)
									return null;
								var userDirectory = new EditLogic.CommandDeclaration(KB.K("Update all from Active Directory"), new FillContactFromUserDirectoryCommand(editorLogic, true));
								var useActiveDirectoryGroup = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								useActiveDirectoryGroup.Add(userDirectory);
								return useActiveDirectoryGroup;
							})
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.ContactReport; }))
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.Contact.F.Code, DCol.Normal, new ECol(Fmt.SetId(ContactNameId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.BusinessPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.Email, DCol.Normal, new ECol(Fmt.SetId(EmailAddressId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.AlternateEmail, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.HomePhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.PagerPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.MobilePhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.FaxPhone, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.WebURL, DCol.Normal, new ECol(Fmt.SetId(WebUrlAddressId))),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LocationID.F.Desc, DCol.Normal),
						// TODO: In the following we only really need the version check for the ECol since the edit buffer fetches the entire record and the schema must match even for fields we don't use,
						// but MinimumDBVersionTbl is not an ECol.ICtorArg
						TblColumnNode.New(dsMB.Path.T.Contact.F.Id.L.User.ContactID.F.AuthenticationCredential, ECol.AllReadonly, new MinimumDBVersionTbl(new Version(1, 1, 4, 2)), DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.PreferredLanguage, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Contact.F.LDAPGuid, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.Contact.F.Comment, DCol.Normal, ECol.Normal)
					),
					// NOTE: Currently the Requestors tab appears blank because the only information that the Requestor
					// Tbl entry displays comes from the Contact record, and these fields get removed since this is
					// a browsette on the Contact table.
					BrowsetteTabNode.New(TId.ContactFunction, TId.Contact,
						TblColumnNode.NewBrowsette(ContactFunctionsBrowsetteTblCreator, dsMB.Path.T.ContactFunctions.F.ParentContactID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.ContactRelation, TId.Contact,
						TblColumnNode.NewBrowsette(TIRelationship.ContactRelatedRecordsBrowseTbl, dsMB.Path.T.ContactRelatedRecords.F.ThisContactID, DCol.Normal, ECol.Normal))
				), EmailAddressValidator, WebUrlAddressValidator
			);
			});
			#endregion
		}
		internal static void DefineTblEntries() {
		}
		private TIContact() {
		}
	}
}
