// There are two incomplete ways of implementing the Assigne To Self commands here.
// Keith's way is to have a custom command in both the editor and browser which query the current user's Assignee-hood when they are executed
// thus precluding having them enable/disable appropriately on this. 
#define KEITHSWAY
// Kevin's way is to make it be a normal Extra New Verb in the browser, and to make a browsette on requests in the editor with no UI and just an exported
// Extra New verb. In some sense this fits the model more cleanly but is has some problems:
// 1 - Currently the Extra New verb cannot be exported to the editor because it is contextual (it depends on the current state of the Request)
// 2 - A special browse Tbl must be created to build a no-ui browser. It has no list columns, its panels refer to edit tbls with no DCols,
//		and no EdtModes are allowed. This still leaves a Refresh button and the unexported (see item 1) Assign to Self button.
// 3 - The no-ui browsette tries to load the Request with an active filter in effect and so might not load it (this may not be a problem because
//		New and InProgress requests cannot become inactive but there is still the cost of the filter on the query).
// 4 - In the editor the no-ui browsette loads a duplicate of the Request instead of fetching its information from the edit buffer.
// 5 - This method has no good way to generate the AssigneeID for the current user. This value is desired both as an Init Source for the new records
//		but also as a value to reference in Conditions on the allowability of the verb overall. There is a related problem that Tbl information coded
//		as a SqlExpression cannot refer to BrowserInitValues
//#define KEVINSWAY
// Both methods suffer from the problem that two users can see an unassigned request. One of them claims it (so it is now assigned) then the other user
// claims it. Because their browser is out of date it still allows this. This will result in the request being assigned to both users and containing
// two state history records.
// This sort of problem may already exist for state transition commands: If two users see an open WO, and one Closes it, then the other Retracts it
// does it appear to work? What sort of mess shows up in the state history? Or did we solve this?
using System.Collections.Generic;
using System.Linq;
using Thinkage.Libraries;
using Thinkage.Libraries.DataFlow;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries.TypeInfo;
using Thinkage.Libraries.XAF.UI;
using Thinkage.MainBoss.Controls.Resources;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.Database.Service;

namespace Thinkage.MainBoss.Controls
{
	/// <summary>
	/// Register Tbl and/or DelayedCreateTbl objects for Requests.
	/// </summary>
	public class TIRequest : TIGeneralMB3 {
		public static EnumValueTextRepresentations ReceiveAcknowledgementEnumText = EnumValueTextRepresentations.NewForBool(KB.K("No acknowledgements are sent"), null, KB.K("Acknowledgements are sent by email"), null);

		#region Common Id objects
		static object RequestAssigneePickerFilterId = KB.I("RequestAssigneePickerFilterId");
		#endregion
		#region Named Tbls
		static DelayedCreateTbl RequestRequestedWorkOrdersTbl;
		public static DelayedCreateTbl RequestEditorTblCreator;
		public static DelayedCreateTbl RequestNewBrowseTbl;
		public static DelayedCreateTbl RequestInProgressBrowseTbl;
		public static DelayedCreateTbl RequestInProgressWithWOBrowseTbl;
		public static DelayedCreateTbl RequestInProgressWithoutWOBrowseTbl;
		public static DelayedCreateTbl RequestClosedBrowseTbl;
		public static DelayedCreateTbl RequestInProgressAssignedToBrowseTbl;
		public static DelayedCreateTbl RequestorForRequestsTblCreator;
		public static DelayedCreateTbl RequestorForRequestsOrWorkOrdersTblCreator;
		public static DelayedCreateTbl RequestorForWorkOrdersTblCreator;
		public static DelayedCreateTbl RequestAssignmentByAssigneeTblCreator;
		public static DelayedCreateTbl RequestEditorByAssigneeTblCreator;
		public static DelayedCreateTbl UnassignedRequestBrowseTbl;
#if KEVINSWAY
		public static DelayedCreateTbl PocketRequestUnassignedBrowseTbl;
#endif
		#endregion
		#region State History with UI definition
		public static DelayedConstruction<StateHistoryUITable> RequestHistoryTable = new DelayedConstruction<StateHistoryUITable>(delegate () {
			return new StateHistoryUITable(MB3Client.RequestHistoryTable, null, FindDelayedEditTbl(dsMB.Schema.T.RequestStateHistory), null);
		});
		#endregion
		#region RequestAssignment
		private static DelayedCreateTbl RequestAssignmentBrowseTbl(bool fixedRequest) {
			return new DelayedCreateTbl(
				delegate() {
					List<BTbl.ICtorArg> BTblAttrs = new List<BTbl.ICtorArg>();
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.RequestAssignment.F.RequestID.F.Number));
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.RequestAssignment.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.Code));
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.RequestAssignment.F.RequestID.F.Subject));
					BTblAttrs.Add(BTbl.ListColumn(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.F.Code));

					return new CompositeTbl(dsMB.Schema.T.RequestAssignment, TId.RequestAssignment,
						new Tbl.IAttr[] {
							RequestsGroup,
							new BTbl(BTblAttrs.ToArray())
						},
						null,
						new CompositeView(RequestAssignmentEditTbl(fixedRequest), dsMB.Path.T.RequestAssignment.F.Id,
							CompositeView.RecognizeByValidEditLinkage(),
							CompositeView.AdditionalViewVerb(KB.K("View Request"), KB.K("View the assigned Request"),  null, dsMB.Path.T.RequestAssignment.F.RequestID, null, null),
							CompositeView.AdditionalViewVerb(KB.K("View Assignee"),  KB.K("View the Request Assignee"), null, dsMB.Path.T.RequestAssignment.F.RequestAssigneeID, null, null)
						)
					);
				}
			);
		}
		private static DelayedCreateTbl RequestAssignmentEditTbl(bool fixedRequest) {
			var creator = new AssignmentDerivationTblCreator(TId.RequestAssignment, dsMB.Schema.T.RequestAssignment);
			if (!fixedRequest) {
				// TODO: This should be 4 checkboxes instead.
				EnumValueTextRepresentations requestState = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Only show New Requests"),
								KB.K("Only show In Progress Requests"),
								KB.K("Only show Transferred Requests"),
								KB.K("Only show Closed Requests"),
								KB.K("Only show Unassigned Requests"),
								KB.K("Show all Requests"),
							},
					null,
					new object[] { 0, 1, 2, 3, 4, 5 }
				);
				object wrFilterChoiceId = creator.AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 5),
					Fmt.SetEnumText(requestState),
					Fmt.SetIsSetting(0)
				);

				creator.AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsNew)),
					wrFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 0)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress)),
					wrFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 1)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders).Gt(0)),
					wrFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 2)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(
					new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsClosed)),
					wrFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 3)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(
					new  SqlExpression(dsMB.Path.T.Request.F.Id).In(
						new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID) },
							null,
							null).SetDistinct(true)).Not()),
					wrFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 4)
				);
				creator.AddPickerFilter(BTbl.ExpressionFilter(
					SqlExpression.Constant(true)),
					wrFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 5)
				);
			}
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestID.F.Number);
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.Code);
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestID.F.Subject);
			creator.CreateBoundPickerControl(KB.I("RequestPickerId"), dsMB.Path.T.RequestAssignment.F.RequestID);

			if (fixedRequest) {
				EnumValueTextRepresentations assigneeCriteria = new EnumValueTextRepresentations(
					new Key[] {
								KB.K("Show prospects for Request Assignee for this Request"),
								KB.K("Show all Assignees not currently assigned to this Request")
							},
					null,
					new object[] {
								0, 1
							}
				);

#if LATER_MAYBE
				// Build an expression that finds all assignees not currently associated with ANY New or In Progress Requests (i.e.
				// select ID from RequestAssignee where ID NOT IN
				// (select RequestAssigneeID from RequestAssignment
				//		join Request on Request.ID = RequestAssignment.RequestID
				//		join RequestStateHistory on RequestStateHistory.ID = Request.CurrentRequestStateHistoryID
				//		join RequestState on RequestState.ID = RequestStateHistory.RequestStateID
				//		where RequestState.FilterAsNew <> 0 or RequestState.FilterAsInProgress <> 0)
				SqlExpression AnyAssigneeNotOnNewOrInProgress = new SqlExpression(dsMB.Path.T.RequestAssignee.F.Id)
						.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID) },
							new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress)
							.Or(new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsNew)),
							null).SetDistinct(true)).Not();
#endif

				object assigneeFilterChoiceId = creator.AddPickerFilterControl(null, new IntegralTypeInfo(false, 0, 1),
					Fmt.SetEnumText(assigneeCriteria),
					Fmt.SetIsSetting(0),
					Fmt.SetId(RequestAssigneePickerFilterId)
				);

				// Probable assignees based on whether its a request, a unit contact, or as reasonable assigne to a workorder that is associated with this request
				// the Unit's contact id matches a workorder assignee contact.
				creator.AddPickerFilter(BTbl.ExpressionFilter(
																new SqlExpression(dsMB.Path.T.RequestAssignee.F.ContactID)
																	.In(new SelectSpecification(
																		null,
																		new SqlExpression[] {
																			new SqlExpression(dsMB.Path.T.RequestAssigneeProspect.F.ContactID),
																		},
																		new SqlExpression(dsMB.Path.T.RequestAssigneeProspect.F.RequestID)
																			.Eq(new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID, 2)	// outer scope 2 refers to the edit buffer contents
																		),
																		null).SetDistinct(true))
																.Or(new SqlExpression(dsMB.Path.T.RequestAssignee.F.ContactID.L.User.ContactID.F.Id)
																	.Eq(new SqlExpression(new UserIDSource())))),
					assigneeFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 0)
				);
				// Build an expression that finds all assignees not current associated with the Request associated with this Request Assignment
				// select ID from RequestAssignee 
				// where ID NOT IN (select RequestAssigneeID from RequestAssignment where RequestID = <ID of current Request associated with RequestAssignment>)
				creator.AddPickerFilter(BTbl.ExpressionFilter(
																new SqlExpression(dsMB.Path.T.RequestAssignee.F.Id)
																	.In(new SelectSpecification(
																		null,
																		new SqlExpression[] { new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID) },
																		new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID).Eq(new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID, 2)),	// outer scope 2 refers to the edit buffer contents
																		null).SetDistinct(true)).Not()),
					assigneeFilterChoiceId,
					o => IntegralTypeInfo.Equals(o, 1)
				);
			}
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.F.Code);
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.F.BusinessPhone);
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.F.MobilePhone);
			creator.AddPickerPanelDisplay(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.F.Email);
			// Label of PickerControl must be same as the underlying Tbl identification of for RequestAssignee as the Tbl identification will
			// be used in any Unique violations to identify the control on the screen (since the actual picker control will have a 'null' label
			creator.CreateBoundPickerControl(KB.I("RequestAssigneePickerId"), dsMB.Path.T.RequestAssignment.F.RequestAssigneeID);

			return new DelayedCreateTbl(
				delegate() {
					return creator.GetTbl(RequestsGroup);
				}
			);
		}
		#endregion
		#region Request Edit
#if KEITHSWAY
		/// <summary>
		/// The 'rules' say if you have RequestFulfillment OR RequestClose "as well as" RequestAssigneSelf, you can use the "Self Assign" operation.
		/// We do this by knowing the RequestAssignSelf role (only) allows 'Create' on the 'UnassignedRequest' view as a table op.
		/// We explicitly create disablers for each of the required table rights. (We 'know' that RequestFulfillment and RequestClose have 'Create' on the RequestStateHistory table)
		/// yech!
		/// </summary>
		/// <returns></returns>
		private static List<IDisablerProperties> SelfAssignDisablers() {
			ITblDrivenApplication app = Application.Instance.GetInterface<ITblDrivenApplication>();
			TableOperationRightsGroup rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild("UnassignedRequest");
			var list = new List<IDisablerProperties>();
			list.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
			rightsGroup = (TableOperationRightsGroup)app.TableRights.FindDirectChild(dsMB.Schema.T.RequestStateHistory.Name);
			list.Add((IDisablerProperties)app.PermissionsManager.GetPermission(rightsGroup.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create)));
			return list;
		}
		private static Key SelfAssignCommand = KB.K("Self Assign");
		private static Key SelfAssignTip = KB.K("Add yourself as an assignee to this Request");
		private static void SelfAssignmentEditor(CommonLogic el, object requestID) {
			object requestAssigneeID;
			using (dsMB ds = new dsMB(el.DB)) {
				var row = ds.DB.ViewAdditionalRow(ds, dsMB.Schema.T.RequestAssignee,
					// Although a User row might be Hidden, such a row could not be the UserRecordID.
					// However, a deleted Contact could still name a User record so we check to ensure that the user's Contact record is not Hidden.
					new SqlExpression(dsMB.Path.T.RequestAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(Application.Instance.GetInterface<Thinkage.Libraries.DBAccess.IApplicationWithSingleDatabaseConnection>().UserRecordID))
						.And(new SqlExpression(dsMB.Path.T.RequestAssignee.F.ContactID.F.Hidden).IsNull()));
				if (row == null)
					throw new GeneralException(KB.K("You are not registered as a Request Assignee"));
				requestAssigneeID = ((dsMB.RequestAssigneeRow)row).F.Id;
			}
			var initList = new List<TblActionNode>();
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestStateHistory.F.RequestID, new ConstantValue(requestID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestStateHistory.F.RequestStateID, new ConstantValue(KnownIds.RequestStateInProgressId)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID, new EditorPathValue(dsMB.Path.T.RequestStateHistory.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestStateHistory.F.PredictedCloseDate, new EditorPathValue(dsMB.Path.T.RequestStateHistory.F.RequestID.F.CurrentRequestStateHistoryID.F.PredictedCloseDate)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestStateHistory.F.Comment, new ConstantValue(Strings.Format(KB.K("Self assigned")))));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestAssignment.F.RequestID, 1, new EditorPathValue(dsMB.Path.T.RequestStateHistory.F.RequestID)));
			initList.Add(Init.OnLoadNew(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID, 1, new ConstantValue(requestAssigneeID)));
			Libraries.Application.Instance.GetInterface<ITblDrivenApplication>().GetInterface<ITblDrivenApplication>().PerformMultiEdit(el.CommonUI.UIFactory, el.DB, TblRegistry.FindDelayedEditTbl(dsMB.Schema.T.RequestStateHistory),
				EdtMode.New,
				new[] { new object[] { } },
				ApplicationTblDefaults.NoModeRestrictions,
				new[] { initList },
				((ICommonUI)el.CommonUI).Form, el.CallEditorsModally,
				null);
		}
#endif
		private static DelayedCreateTbl RequestEditTbl(TblLayoutNodeArray nodes, FeatureGroup featureGroup, bool AssignToSelf) {
			return new DelayedCreateTbl(delegate() {
				List<ETbl.ICtorArg> etblArgs = new List<ETbl.ICtorArg>();
				etblArgs.Add(MB3ETbl.HasStateHistoryAndSequenceCounter(dsMB.Path.T.Request.F.Number, dsMB.Schema.T.RequestSequenceCounter, dsMB.Schema.V.WRSequence, dsMB.Schema.V.WRSequenceFormat, RequestHistoryTable));
				etblArgs.Add(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.Delete));
				etblArgs.Add(ETbl.Print(TIReports.SingleRequestFormReport, dsMB.Path.T.RequestReport.F.RequestID));
				if (AssignToSelf) {
#if KEVINSWAY
					var augmentedNodes = new List<TblLayoutNode>(nodes.ColumnArray);
					augmentedNodes.Add(TblColumnNode.NewBrowsette(PocketRequestUnassignedBrowseTbl, dsMB.Path.T.Request.F.Id, ECol.Normal));
					nodes = new TblLayoutNodeArray(augmentedNodes);
#endif
#if KEITHSWAY
					etblArgs.Add(ETbl.CustomCommand(
							delegate(EditLogic el) {
								var group = new EditLogic.MutuallyExclusiveCommandSetDeclaration();
								group.Add(new EditLogic.CommandDeclaration(
									SelfAssignCommand,
									new MultiCommandIfAllEnabled(
										EditLogic.StateTransitionCommand.NewSameTargetState(el,
											SelfAssignTip,
											delegate()
											{
												SelfAssignmentEditor(el, el.RootRowIDs[0]);
											},
											el.AllStatesWithExistingRecord.ToArray()),
										SelfAssignDisablers().ToArray())
									));
								return group;
							}
					));
#endif
				}
				return new Tbl(dsMB.Schema.T.Request, TId.Request,
					new Tbl.IAttr[] {
						featureGroup,
						new ETbl(etblArgs.ToArray())
					},
					(TblLayoutNodeArray)nodes.Clone(),
					Init.LinkRecordSets(dsMB.Path.T.RequestStateHistory.F.RequestID, 1, dsMB.Path.T.Request.F.Id, 0),
						// Copy the AccessCode from unit if the checkbox is checked
					Init.New(new ControlTarget(AccessCodeId), new EditorPathValue(dsMB.Path.T.Request.F.UnitLocationID.F.RelativeLocationID.F.UnitID.F.AccessCodeID), new Libraries.Presentation.ControlValue(UseUnitAccessCode), TblActionNode.Activity.Disabled, TblActionNode.SelectiveActivity(TblActionNode.Activity.Continuous, EdtMode.New, EdtMode.Edit, EdtMode.Clone)),
						// Arrange for Access Code choices to be readonly if using the Unit's values
					Init.Continuous(new ControlReadonlyTarget(AccessCodeId, BecauseUsingUnitAccessCode), new Libraries.Presentation.ControlValue(UseUnitAccessCode)),
					Init.OnLoadNew(new PathTarget(dsMB.Path.T.RequestStateHistory.F.UserID, 1), new UserIDValue())
				);
			});
		}
		#endregion

		public static readonly object RequestDescriptionID = KB.I("RequestDescriptionID");
		public static readonly object RequestSubjectID = KB.I("RequestSubjectID");
		internal static void DefineTblEntries() {
		}

#if KEVINSWAY
		private static CompositeView AssignToSelfVerb(bool exported) {
			// This makes a new RequestStateHistory record and also a RequestAssignment record using the second recordSet by calling up the Request State History editor.
			// The containing Tbl must be rooted at the Request table.
			var attribs = new List<CompositeView.ICtorArg>();
			attribs.Add(CompositeView.JoinedNewCommand(KB.K("Assign to myself")));
			attribs.Add(NewNoContextFreeNew);
			attribs.Add(NoReentryToNewMode);
			attribs.Add(CompositeView.ContextFreeInit(dsMB.Path.T.Request.F.Id, dsMB.Path.T.RequestStateHistory.F.RequestID));
			attribs.Add(CompositeView.ContextFreeInit(new BrowserCalculatedInitValue(Libraries.TypeInfo.StringTypeInfo.NonNullUniverse, (values) => KB.K("Assigned to self").Translate()), dsMB.Path.T.RequestStateHistory.F.Comment));
			attribs.Add(CompositeView.ContextFreeInit(new ConstantValue(KnownIds.RequestStateInProgressId), dsMB.Path.T.RequestStateHistory.F.RequestStateID));
			attribs.Add(CompositeView.ContextFreeInit(dsMB.Path.T.Request.F.Id, dsMB.Path.T.RequestAssignment.F.RequestID, 1));
			attribs.Add(CompositeView.ContextFreeInit(new UserIDValue(), dsMB.Path.T.RequestAssignment.F.RequestAssigneeID, 1));	// TODO: Must use the RequestAssigneeId (an up-to-date result for assigneeIdExpression)
			attribs.Add(CompositeView.ContextualInit(0,
				//							new[] { new CompositeView.Condition(assigneeIdExpression.IsNotNull(), notAssigneeTip) },	// TODO: the expression cannot be evaluated client-side.
				new CompositeView.Init(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID, new ConstantValue(null))
			));
			attribs.Add(CompositeView.ContextualInit(1,
				//							new[] { new CompositeView.Condition(assigneeIdExpression.IsNotNull(), notAssigneeTip) },	// TODO: the expression cannot be evaluated client-side.
				new CompositeView.Init(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID, dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID)
			));
			if (exported)
				attribs.Add(CompositeView.ExportNewVerb(true));
			return CompositeView.ExtraNewVerb(FindDelayedEditTbl(dsMB.Schema.T.RequestStateHistory),
				attribs.ToArray()
			);
		}
#endif
		static TIRequest() {
			#region RequestAssignee
			DefineTbl(dsMB.Schema.T.RequestAssignee, delegate() {
				return new Tbl(dsMB.Schema.T.RequestAssignee, TId.RequestAssignee,
				new Tbl.IAttr[] {
					RequestsGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.RequestAssignee.F.ContactID.F.Code),
						BTbl.ListColumn(dsMB.Path.T.RequestAssignee.F.ContactID.F.BusinessPhone, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.RequestAssignee.F.ContactID.F.MobilePhone, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.RequestAssignee.F.Id.L.RequestAssigneeStatistics.Id.F.NumNew, NonPerViewColumn),
						BTbl.ListColumn(dsMB.Path.T.RequestAssignee.F.Id.L.RequestAssigneeStatistics.Id.F.NumInProgress, NonPerViewColumn)
					),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete)),
					TIReports.NewRemotePTbl(TIReports.RequestAssigneeReport)
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						SingleContactGroup(dsMB.Path.T.RequestAssignee.F.ContactID),
						TblColumnNode.New(dsMB.Path.T.RequestAssignee.F.ReceiveNotification, new FeatureGroupArg(MainBossServiceAsWindowsServiceGroup), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestAssignee.F.Comment, DCol.Normal, ECol.Normal)
					),
					TblTabNode.New(KB.TOc(TId.RequestAssignment), KB.K("Request Assignments for this Assignee"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
						TblColumnNode.NewBrowsette(RequestAssignmentBrowseTbl(false), dsMB.Path.T.RequestAssignment.F.RequestAssigneeID, DCol.Normal, ECol.Normal))
				));
			});
			RegisterExistingForImportExport(TId.RequestAssignee, dsMB.Schema.T.RequestAssignee);
			#endregion
			#region RequestAssignmentByAssignee
			RequestAssignmentByAssigneeTblCreator = new DelayedCreateTbl(delegate () {
				object assignedToColumnID = KB.I("codeName");
				DelayedCreateTbl assignedToGroup = new DelayedCreateTbl(
					delegate () {
						return new Tbl(dsMB.Schema.T.RequestAssignmentByAssignee, TId.AssignedGroup,
							new Tbl.IAttr[] {
								RequestsAssignmentsGroup,
							},
							new TblLayoutNodeArray(
								TblColumnNode.New(null, dsMB.Path.T.RequestAssignmentByAssignee.F.Id, DCol.Normal)
							)
						);
					}
				);
				Key newAssignmentKey = TId.RequestAssignment.ComposeCommand("New {0}");
				return new CompositeTbl(dsMB.Schema.T.RequestAssignmentByAssignee, TId.RequestAssignmentByAssignee,
					new Tbl.IAttr[] {
						RequestsGroup,
						new BTbl(
							BTbl.PerViewListColumn(KB.K("Assigned to"), assignedToColumnID),
							BTbl.ListColumn(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.Subject),
							BTbl.ListColumn(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders, BTbl.ListColumnArg.Contexts.SearchAndFilter)
						),
						new TreeStructuredTbl(null, 2),
						TIReports.NewRemotePTbl(TIReports.RequestByAssigneeFormReport),
					},
					null,
					// The fake contact row for unassigned work orders; This displays because the XAFDB file specifies a text provider for its own ID field.
					new CompositeView(assignedToGroup, dsMB.Path.T.RequestAssignmentByAssignee.F.Id, ReadonlyView,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.ContactID).IsNull()),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.RequestAssignmentByAssignee.F.Id)
					),
					// Normal contact row for an assignee
					// TODO: The view should return the AssigneeID rather than the ContactID in all row types so we don't need this .L. linkage
					// TODO: We should then have extra verbs for direct Edit/View contact so the user doesn't have to drill into the assignee record.
					new CompositeView(dsMB.Path.T.RequestAssignmentByAssignee.F.ContactID.L.RequestAssignee.ContactID,
						NoNewMode,
						CompositeView.RecognizeByValidEditLinkage(),
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID).IsNull()),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.RequestAssignee.F.ContactID.F.Code)
					),
					// TODO: These views show the Request in the panel. If an explicit assignment exists (RequestAssignmentID != null) there should be a way to delete it.
					// (un)assigned draft Request
					new CompositeView(RequestEditorTblCreator, dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID).IsNotNull()
									.And(SqlExpression.Constant(KnownIds.RequestStateNewId).Eq(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID)))),
						CompositeView.SetParentPath(dsMB.Path.T.RequestAssignmentByAssignee.F.ContactID),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.Request.F.Number),
						CompositeView.IdentificationOverride(TId.NewRequest)
					),
					// (un)assigned open Request
					new CompositeView(RequestEditorTblCreator, dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID,
						NoNewMode,
						CompositeView.AddRecognitionCondition(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID).IsNotNull()
									.And(SqlExpression.Constant(KnownIds.RequestStateInProgressId).Eq(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID)))),
						CompositeView.SetParentPath(dsMB.Path.T.RequestAssignmentByAssignee.F.ContactID),
						BTbl.PerViewColumnValue(assignedToColumnID, dsMB.Path.T.Request.F.Number),
						CompositeView.UseSamePanelAs(2),
						CompositeView.IdentificationOverride(TId.InProgressRequest)
					),
					// Allow creation of new assignments if a Request is selected.
					CompositeView.ExtraNewVerb(RequestAssignmentEditTbl(true),
						CompositeView.JoinedNewCommand(newAssignmentKey),
						NoContextFreeNew,
						CompositeView.ContextualInit(
							new int[] { 2, 3 }, // corresponds to the composite views above on Requests
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.RequestAssignment.F.RequestID), dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID)
						)
					),
					// Allow creation of new assignments if an assignee is selected.
					CompositeView.ExtraNewVerb(RequestAssignmentEditTbl(false),
						CompositeView.JoinedNewCommand(newAssignmentKey),
						NoContextFreeNew,
						CompositeView.ContextualInit(
							new int[] { 1 }, // corresponds to the composite view above on assignees
							new CompositeView.Init(new PathOrFilterTarget(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID), dsMB.Path.T.RequestAssignmentByAssignee.F.ContactID.L.RequestAssignee.ContactID.F.Id)
						)
					)
				);
			}
			);
			#endregion
			#region RequestedWorkOrder
			BTbl.ICtorArg[] RequestedWorkOrdersListColumns = {
					BTbl.ListColumn(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.Number),
					BTbl.ListColumn(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.Subject),
					BTbl.ListColumn(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.Number),
					BTbl.ListColumn(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.UnitLocationID.Key(), dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.UnitLocationID.F.Code),
					BTbl.ListColumn(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.Subject),
			};
			TblLayoutNodeArray nodes = new TblLayoutNodeArray(
				TblGroupNode.New(dsMB.Path.T.RequestedWorkOrder.F.RequestID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.RequestID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Request.F.Number)), ECol.Normal),
					SingleRequestorGroup(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.RequestorID, false),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.Subject, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(KB.K("State"), dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.Code, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.CurrentRequestStateHistoryID.F.PredictedCloseDate, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.RequestID.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.RequestID.F.Description, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)),
				TblGroupNode.New(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrder.F.Number)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.UnitLocationID.Key(), dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.UnitLocationID.F.Code, DCol.Normal),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.Subject, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(KB.K("State"), dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.CurrentWorkOrderStateHistoryID.F.WorkOrderStateID.F.Code, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(dsMB.Path.T.RequestedWorkOrder.F.WorkOrderID.F.Description, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly)
				)
			);
			DefineTbl(dsMB.Schema.T.RequestedWorkOrder, delegate() {
				return new Tbl(dsMB.Schema.T.RequestedWorkOrder, TId.RequestedWorkOrder,
				new Tbl.IAttr[] {
					WorkOrdersAndRequestsGroup,
					new BTbl(RequestedWorkOrdersListColumns),
					new ETbl(ETbl.EditorAccess(false, EdtMode.UnDelete, EdtMode.EditDefault, EdtMode.ViewDefault))
				},
				nodes
				);
			});
			RequestRequestedWorkOrdersTbl = new DelayedCreateTbl(delegate() {
				Key joinedLinkWorkOrder = TId.WorkOrder.ComposeCommand("Link {0}");
				Key joinedCreateWorkOrder = TId.WorkOrder.ComposeCommand("New {0}");
				return new CompositeTbl(dsMB.Schema.T.RequestedWorkOrder, TId.RequestedWorkOrder,
						new Tbl.IAttr[] {
							new BTbl(RequestedWorkOrdersListColumns)
						},
						null,
						CompositeView.ChangeEditTbl(TIGeneralMB3.FindDelayedEditTbl(dsMB.Schema.T.RequestedWorkOrder),
							CompositeView.JoinedNewCommand(joinedLinkWorkOrder),
							CompositeView.ExportNewVerb(true),
							CompositeView.IdentificationOverride(TId.LinkedWorkOrder)),
						CompositeView.ExtraNewVerb(TIWorkOrder.WorkOrderEditorFromRequestTbl,
							CompositeView.ExportNewVerb(true),
							CompositeView.PathAlias(dsMB.Path.T.RequestedWorkOrder.F.RequestID,
								dsMB.Path.T.RequestedWorkOrder.F.RequestID, 2)
						)
					);
			});

			#endregion
			#region RequestState
			DefineTbl(dsMB.Schema.T.RequestState, delegate() {
				return new Tbl(dsMB.Schema.T.RequestState, TId.RequestState,
				new Tbl.IAttr[] {
					RequestsGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.RequestState.F.Code), BTbl.ListColumn(dsMB.Path.T.RequestState.F.Desc)),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.RequestState.F.Code, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestState.F.Desc, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestState.F.Comment, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestState.F.FilterAsNew, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestState.F.FilterAsInProgress, DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestState.F.FilterAsClosed, DCol.Normal)
					),
					BrowsetteTabNode.New(TId.Request, TId.RequestState, 
						TblColumnNode.NewBrowsette(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID, DCol.Normal))
				));
			});
			#endregion
			#region RequestStateHistoryStatus
			DefineTbl(dsMB.Schema.T.RequestStateHistoryStatus, delegate() {
				return new Tbl(dsMB.Schema.T.RequestStateHistoryStatus, TId.RequestStatus,
				new Tbl.IAttr[] {
					RequestsGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.RequestStateHistoryStatus.F.Code),
							BTbl.ListColumn(dsMB.Path.T.RequestStateHistoryStatus.F.Desc)
					),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.RequestStateHistoryStatus.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistoryStatus.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistoryStatus.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Request, TId.RequestStatus, 
						TblColumnNode.NewBrowsette(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID, DCol.Normal, ECol.Normal))
				));
			});
			#endregion
			#region RequestStateHistory
			DefineEditTbl(dsMB.Schema.T.RequestStateHistory, new DelayedCreateTbl(delegate () {
				TblLayoutNodeArray extraNodes;
				List<TblActionNode> inits = StateHistoryInits(MB3Client.RequestHistoryTable, out extraNodes);
				TblLayoutNodeArray RequestStateHistoryNodes = new TblLayoutNodeArray(
						TblGroupNode.New(dsMB.Path.T.RequestStateHistory.F.RequestID, new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
							TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.RequestID.F.Number, DCol.Normal, ECol.AllReadonly),
							TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.Code, new ECol(ECol.AllReadonlyAccess, ECol.ForceErrorsFatal(), Fmt.SetId(TIGeneralMB3.CurrentStateHistoryCodeWhenLoadedId))),
							TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID.F.Code, ECol.AllReadonly),
							SingleRequestorLanguagePreferenceGroup(dsMB.Path.T.RequestStateHistory.F.RequestID.F.RequestorID)
						),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.EffectiveDate, DCol.Normal, new ECol(Fmt.SetId(TIGeneralMB3.StateHistoryEffectiveDateId)), new NonDefaultCol()),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.EntryDate, new NonDefaultCol(), DCol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.UserID.F.ContactID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.RequestStateID.F.Code, new NonDefaultCol(), DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.RequestStateHistoryStatus.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.PredictedCloseDate, new NonDefaultCol(), DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.CommentToRequestor, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestStateHistory.F.Comment, DCol.Normal, ECol.Normal)
				);
				inits.Add(Init.OnLoadNew(dsMB.Path.T.RequestStateHistory.F.PredictedCloseDate, new EditorPathValue(dsMB.Path.T.RequestStateHistory.F.RequestID.F.CurrentRequestStateHistoryID.F.PredictedCloseDate)));
				return new Tbl(dsMB.Schema.T.RequestStateHistory, TId.RequestStateHistory,
					new Tbl.IAttr[] {
						RequestsGroup,
						new ETbl(
							ETbl.EditorDefaultAccess(false),
							ETbl.EditorAccess(true, EdtMode.New, EdtMode.Edit, EdtMode.View, EdtMode.EditDefault, EdtMode.ViewDefault),
							ETbl.NewOnlyOnce(true),
							ETbl.UseNewConcurrency(true),
							ETbl.AllowConcurrencyErrorOverride(false),
							ETbl.RowEditType(dsMB.Path.T.RequestStateHistory.F.RequestID.F.Id, 0, RecordManager.RowInfo.RowEditTypes.EditOnly),
							MB3ETbl.IsStateHistoryTbl(RequestHistoryTable)
						)
					},
					RequestStateHistoryNodes + extraNodes,
					inits.ToArray()
				);
			}));
			DefineBrowseTbl(dsMB.Schema.T.RequestStateHistory, new DelayedCreateTbl(delegate () {
				return new CompositeTbl(dsMB.Schema.T.RequestStateHistory, TId.RequestStateHistory,
					new Tbl.IAttr[] {
						RequestsGroup,
						new BTbl(
							MB3BTbl.IsStateHistoryTbl(RequestHistoryTable),
							BTbl.ListColumn(dsMB.Path.T.RequestStateHistory.F.RequestID.F.Number),
							BTbl.ListColumn(dsMB.Path.T.RequestStateHistory.F.EffectiveDate, BTbl.ListColumnArg.Contexts.SortInitialDescending),
							BTbl.ListColumn(dsMB.Path.T.RequestStateHistory.F.RequestStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.RequestStateHistory.F.Comment)
						)
					},
					null,
					CompositeView.ChangeEditTbl(FindDelayedEditTbl(dsMB.Schema.T.RequestStateHistory), NoNewMode)
				);
			}));
			#endregion
			#region ManageRequestTransition
			DefineTbl(dsMB.Schema.T.ManageRequestTransition, new DelayedCreateTbl(delegate() {
				return new Tbl(dsMB.Schema.T.ManageRequestTransition, TId.WorkOrderRequestTransition,
					new Tbl.IAttr[] {
						new BTbl(
							BTbl.ListColumn(dsMB.Path.T.ManageRequestTransition.F.RequestStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ManageRequestTransition.F.WorkOrderStateID.F.Code),
							BTbl.ListColumn(dsMB.Path.T.ManageRequestTransition.F.ChangeToRequestStateID.F.Code)
							),
							new ETbl(ETbl.EditorDefaultAccess(false), ETbl.EditorAccess(true, EdtMode.View, EdtMode.New, EdtMode.Edit, EdtMode.Delete))
					},
					new TblLayoutNodeArray(
						TblColumnNode.New(dsMB.Path.T.ManageRequestTransition.F.RequestStateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.RequestState.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ManageRequestTransition.F.WorkOrderStateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.WorkOrderState.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ManageRequestTransition.F.ChangeToRequestStateID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.RequestState.F.Code)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.ManageRequestTransition.F.CommentToRequestorUserMessageKeyID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.UserMessageKey.F.Key)),
									new ECol(Fmt.SetPickFrom(TIGeneralMB3.UserMessageKeyWithEditAbilityTblCreator),
									Fmt.SetBrowserFilter(BTbl.EqFilter(dsMB.Path.T.UserMessageKey.F.Context, KB.I(Database.RequestClosePreferenceK.RequestClosePreference))))),
						TblInitSourceNode.New(KB.K("Current Translation"), new DualCalculatedInitValue(TranslationKeyTypeInfo.Universe, delegate(object[] inputs)
						{
							if (inputs[0] == null || inputs[1] == null)
								return null;
							return new Thinkage.Libraries.Translation.SimpleKey(ContextReference.New((string)inputs[0]), (string)inputs[1]);
						}, new DualPathValue(dsMB.Path.T.ManageRequestTransition.F.CommentToRequestorUserMessageKeyID.F.Context), new DualPathValue(dsMB.Path.T.ManageRequestTransition.F.CommentToRequestorUserMessageKeyID.F.Key)),
							Fmt.SetLineCount(5), DCol.Normal, ECol.AllReadonly)
					)
				);
			}));

			#endregion
			#region Request
			BTbl.ICtorArg RequestNumberListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.Number);
			BTbl.ICtorArg RequestRequestorListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.RequestorID.F.ContactID.F.Code);
			BTbl.ICtorArg RequestBusinessPhoneListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.RequestorID.F.ContactID.F.BusinessPhone, BTbl.ListColumnArg.Contexts.ClosedCombo | BTbl.ListColumnArg.Contexts.OpenCombo | BTbl.ListColumnArg.Contexts.SearchAndFilter);
			BTbl.ICtorArg RequestSubjectListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.Subject);
			BTbl.ICtorArg RequestStatusListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateHistoryStatusID.F.Code);
			BTbl.ICtorArg RequestStateAuthorListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.UserID.F.ContactID.F.Code, BTbl.ListColumnArg.Contexts.SearchAndFilter);
			BTbl.ICtorArg OpenEmailCommand = BTbl.AdditionalVerb(KB.K("View Email"),
							delegate (BrowseLogic browserLogic) {
								Libraries.DataFlow.Source emailRequestId = browserLogic.GetTblPathDisplaySource(dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID.F.Id, -1);
								return new MultiCommandIfAllEnabled(new CallDelegateCommand(KB.K("View the Email associated with the request in your Email program"),
									delegate () {
										ServiceUtilities.OpenEmail(browserLogic.DB, (System.Guid)emailRequestId.GetValue());
									}),
								browserLogic.NeedSingleSelectionDisabler,
								new HasEmailDisabler(browserLogic, emailRequestId));
							}
						);

			// The priority column uses an alternative sort key which biases the non-null values:
			// null -> int.MaxValue, int.MinValue -> null, and all others -> value-1
			// This causes null to sort as lower-than-lowest (because a big ranking number means low priority), and also that the default ascending sort puts highest priority at the top of the list.
			BTbl.ICtorArg RequestPriorityListColumnSortValue = BTbl.ListColumn(dsMB.Path.T.Request.F.RequestPriorityID.F.Code.Key(), dsMB.Path.T.Request.F.RequestPriorityID.F.Rank, BTbl.ListColumnArg.Contexts.TaggedValueProvider | BTbl.ListColumnArg.Contexts.SortNormal,
				BTbl.ListColumnArg.WrapSource((originalSource) => new ConvertingSource<int?, int?>(originalSource, dsMB.Path.T.Request.F.RequestPriorityID.F.Rank.ReferencedColumn.EffectiveType,
					(value) => (value.HasValue ? value.Value == int.MinValue ? null : value - 1 : int.MaxValue))));
			BTbl.ICtorArg RequestPriorityListColumn = BTbl.ListColumn(dsMB.Path.T.Request.F.RequestPriorityID.F.Code, BTbl.ListColumnArg.Contexts.List | BTbl.ListColumnArg.Contexts.SearchAndFilter | BTbl.ListColumnArg.Contexts.SortAlternativeValue);

			// We build the layout piecemeal so as to skip the Work Order browsette if we are the Service Desk application.
			// The "Create WO" button should perhaps be done here as another contextual New operation on the browser. This would still
			// leave the editor with a creation problem, however. Another choice would be to have the WR/WO linkage browsette have
			// the special "Create WO" button, and provide some way for a browsette to export commands to the enclosing editor/browser.
			TblLayoutNodeArray RequestDetailNodes = new TblLayoutNodeArray(
					TblColumnNode.New(dsMB.Path.T.Request.F.Number, new NonDefaultCol(), DCol.Normal, ECol.ReadonlyInUpdate),
					TblColumnNode.New(dsMB.Path.T.Request.F.Subject, DCol.Normal, new ECol(Fmt.SetId(RequestSubjectID))),
					TblVariableNode.New(KB.K("Number Format"), dsMB.Schema.V.WRSequenceFormat, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Number Sequence"), dsMB.Schema.V.WRSequence, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					CurrentStateHistoryGroup(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID,
						dsMB.Path.T.RequestStateHistory.F.EffectiveDate,
						dsMB.Path.T.RequestStateHistory.F.RequestStateID.F.Code,
						dsMB.Path.T.RequestStateHistory.F.RequestStateHistoryStatusID.F.Code,
						dsMB.Path.T.RequestStateHistory.F.PredictedCloseDate),
				// Need MIN EntryDate in RequestStateHistory for Create date				TblColumnNode.NewLastColumnBound("Created", dsMB.Path.T.Request.F.CreateDate, DCol.Normal, ECol.Normal ),
					SingleRequestorGroup(dsMB.Path.T.Request.F.RequestorID, true),
					TblColumnNode.New(dsMB.Path.T.Request.F.UnitLocationID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.Location.F.Code)), ECol.Normal),
					TblUnboundControlNode.New(UseUnitAccessCode, BoolTypeInfo.NonNullUniverse, new ECol(Fmt.SetId(UseUnitAccessCode), Fmt.SetIsSetting(false)), new NonDefaultCol()),
					TblColumnNode.New(dsMB.Path.T.Request.F.AccessCodeID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.AccessCode.F.Code)), new ECol(Fmt.SetId(AccessCodeId))),
					TblColumnNode.New(dsMB.Path.T.Request.F.RequestPriorityID, new DCol(Fmt.SetDisplayPath(dsMB.Path.T.RequestPriority.F.Code)), ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Request.F.Description, DCol.Normal, new ECol(Fmt.SetId(RequestDescriptionID))),
					TblColumnNode.New(dsMB.Path.T.Request.F.Comment, DCol.Normal, ECol.Normal),
					TblColumnNode.New(dsMB.Path.T.Request.F.SelectPrintFlag, DCol.Normal, ECol.Normal)
			);
			TblLayoutNodeArray RequestNodes = new TblLayoutNodeArray(
				DetailsTabNode.New(RequestDetailNodes.ColumnArray),
				TblTabNode.New(KB.K("Close Preferences"), KB.K("Options for changing the state of a Request automatically"), new TblLayoutNode.ICtorArg[] { new DefaultOnlyCol(), DCol.Normal, ECol.Normal },
					TblVariableNode.New(KB.K("Requests are automatically changed to a new state when linked to a work order, or when a linked work order state changes"), dsMB.Schema.V.ManageRequestStates, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblVariableNode.New(KB.K("Include Work Order History status code and comments in Comments to Requestor"), dsMB.Schema.V.CopyWSHCommentToRSH, new DefaultOnlyCol(), DCol.Normal, ECol.Normal),
					TblGroupNode.New(KB.TOc(TId.WorkOrderRequestTransition), new TblLayoutNode.ICtorArg[] { new DefaultOnlyCol(), DCol.Normal, ECol.Normal },
						TblColumnNode.NewBrowsetteForDefaults(TblRegistry.FindDelayedBrowseTbl(dsMB.Schema.T.ManageRequestTransition), DCol.Normal, ECol.Normal)
					)
				),
				BrowsetteTabNode.New(TId.RequestAssignment, TId.Request, 
					TblColumnNode.NewBrowsette(RequestAssignmentBrowseTbl(true), dsMB.Path.T.RequestAssignment.F.RequestID, DCol.Normal, ECol.Normal)),
				TblTabNode.New(dsMB.Schema.T.EmailRequest.LabelKey, KB.TOBrowsetteTip(TId.Request, TId.EmailRequest), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal },
					TblColumnNode.New(VisibleReverseLinkagePathKey(dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID, dsMB.Path.T.EmailRequest.F.ReceiveDate), dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID.F.ReceiveDate, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(VisibleReverseLinkagePathKey(dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID, dsMB.Path.T.EmailRequest.F.RequestorEmailAddress), dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID.F.RequestorEmailAddress, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(VisibleReverseLinkagePathKey(dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID, dsMB.Path.T.EmailRequest.F.Subject), dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID.F.Subject, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(VisibleReverseLinkagePathKey(dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID, dsMB.Path.T.EmailRequest.F.MailMessage), dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID.F.MailMessage, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.New(VisibleReverseLinkagePathKey(dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID, dsMB.Path.T.EmailRequest.F.Comment), dsMB.Path.T.Request.F.Id.L.EmailRequest.RequestID.F.Comment, DCol.Normal, ECol.AllReadonly),
					TblColumnNode.NewBrowsette(dsMB.Path.T.EmailPart.F.EmailRequestID.F.RequestID, DCol.Normal, ECol.Normal)
				),
				BrowsetteTabNode.New(TId.WorkOrder, TId.Request,
					TblColumnNode.NewBrowsette(RequestRequestedWorkOrdersTbl, dsMB.Path.T.RequestedWorkOrder.F.RequestID, DCol.Normal, ECol.Normal)),
				BrowsetteTabNode.New(TId.RequestStateHistory, TId.Request, 
					TblColumnNode.NewBrowsette(dsMB.Path.T.RequestStateHistory.F.RequestID, DCol.Normal, ECol.Normal))
			);
			// Editors for various feature groups
			RequestEditorByAssigneeTblCreator = RequestEditTbl(RequestNodes, RequestsGroup, false);
			RequestEditorTblCreator = RequestEditTbl(RequestNodes, RequestsGroup, false);
			DelayedCreateTbl RequestAssignedToEditorTblCreator = RequestEditTbl(RequestNodes, RequestsAssignmentsGroup, false);
			DelayedCreateTbl RequestUnassignedEditorTblCreator = RequestEditTbl(RequestNodes, RequestsAssignmentsGroup, true);
			DefineEditTbl(dsMB.Schema.T.Request, RequestEditorTblCreator);
			// The Request browser is made Composite only to allow specification of an alternative Tbl for editing.
			DefineBrowseTbl(dsMB.Schema.T.Request, delegate() {
				Key NewRequest = TId.Request.ComposeCommand("New {0}");
				return new CompositeTbl(dsMB.Schema.T.Request, TId.Request,
				new Tbl.IAttr[] {
					new BTbl(
						MB3BTbl.HasStateHistory(RequestHistoryTable),
						RequestNumberListColumn, RequestRequestorListColumn, RequestBusinessPhoneListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand

					),
					TIReports.NewRemotePTbl(new DelayedCreateTbl( delegate() { return TIReports.RequestFormReport; }))
				},
				null,	// no record type
				CompositeView.ChangeEditTbl(RequestEditorTblCreator, CompositeView.JoinedNewCommand(NewRequest),
					CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateNewId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID))),
					CompositeView.IdentificationOverride(TId.NewRequest)),
				CompositeView.ChangeEditTbl(RequestEditorTblCreator, OnlyViewEdit, CompositeView.JoinedNewCommand(NewRequest),
					CompositeView.UseSamePanelAs(0),
					CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateInProgressId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID))),
					CompositeView.IdentificationOverride(TId.InProgressRequest)),
				CompositeView.ChangeEditTbl(RequestEditorTblCreator, OnlyViewEdit, CompositeView.JoinedNewCommand(NewRequest),
					CompositeView.UseSamePanelAs(0),
					CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateClosedId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID))),
					CompositeView.IdentificationOverride(TId.ClosedRequest)),
				CompositeView.AdditionalEditDefault(FindDelayedEditTbl(dsMB.Schema.T.RequestStateHistory))
				);
			});

			RequestNewBrowseTbl = new DelayedCreateTbl(delegate() {
			return new CompositeTbl(dsMB.Schema.T.Request, TId.NewRequest,
				new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							//BTbl.EqFilter(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsNew, true),
							// We use ExpressionFilter instead since it does not turn into an Init directive in the new-mode editor.
							// DefaultVisibility of the other filtered Request browsers allow New mode.
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsNew).IsTrue()),
							RequestNumberListColumn, RequestRequestorListColumn, RequestBusinessPhoneListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl(() => TIReports.RequestNewFormReport))
				},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestEditorTblCreator)
				);
			});

			RequestInProgressBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.Request, TId.InProgressRequest,
					new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress, true),
							RequestNumberListColumn, RequestPriorityListColumnSortValue, RequestPriorityListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl(() => TIReports.RequestInProgressFormReport))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestEditorTblCreator, OnlyViewEdit)
				);
			});

			SqlExpression UserRequestAssignmentsInProgress = new SqlExpression(dsMB.Path.T.Request.F.Id)
				.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID) },
							new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestAssigneeID.F.ContactID.L.User.ContactID.F.Id).Eq(new SqlExpression(new UserIDSource()))
									.And(new SqlExpression(dsMB.Path.T.RequestAssignment.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress).IsTrue()),
							null).SetDistinct(true));

			RequestInProgressAssignedToBrowseTbl = new DelayedCreateTbl(delegate()
			{
				return new CompositeTbl(dsMB.Schema.T.Request, TId.InProgressRequest,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("AssignedRequest"),
						RequestsAssignmentsGroup,
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							BTbl.ExpressionFilter(UserRequestAssignmentsInProgress),
							BTbl.ListColumn(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.EffectiveDate),
							RequestNumberListColumn, RequestPriorityListColumnSortValue, RequestPriorityListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl( () =>TIReports.RequestInProgressAndAssignedFormReport))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestAssignedToEditorTblCreator, OnlyViewEdit)
				);
			});

			SqlExpression UnassignedRequestAssignments = new SqlExpression(dsMB.Path.T.Request.F.Id)
				// TODO: There are more efficient ways of doing this that do not involve the RequestAssignmentByAssignee view.
				// e.g. NOT IN(select distinct RequestAssignement.RequestID from RequestAssignment where RequestAssignement.ContactID.Hidden is null)
				// or COUNT(select * from RequestAssignment where RequestAssignement.ContactID.Hidden is null and RequestAssignement.RequestID == outer Request.ID) == 0
				.In(new SelectSpecification(
							null,
							new SqlExpression[] { new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID) },
							new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.ContactID).Eq(SqlExpression.Constant(KnownIds.UnassignedID))
									.And(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress)
										.Or(new SqlExpression(dsMB.Path.T.RequestAssignmentByAssignee.F.RequestID.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsNew))),
							null).SetDistinct(true));
			UnassignedRequestBrowseTbl = new DelayedCreateTbl(delegate()
			{
				var assigneeIdExpression = SqlExpression.ScalarSubquery(new SelectSpecification(dsMB.Schema.T.RequestAssignee, new[] { new SqlExpression(dsMB.Path.T.RequestAssignee.F.Id) }, new SqlExpression(dsMB.Path.T.RequestAssignee.F.ContactID.L.User.ContactID.F.Id).Eq(SqlExpression.Constant(Application.Instance.GetInterface<IApplicationWithSingleDatabaseConnection>().UserRecordID)), null));
				var notAssigneeTip = KB.K("You are not registered as a Request Assignee");
				return new CompositeTbl(dsMB.Schema.T.Request, TId.UnassignedRequest,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("UnassignedRequest"),
						RequestsAssignmentsGroup,
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							BTbl.ExpressionFilter(UnassignedRequestAssignments),
							BTbl.ListColumn(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.EffectiveDate),
							RequestNumberListColumn, RequestPriorityListColumnSortValue, RequestPriorityListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
#if KEITHSWAY
							,
							BTbl.AdditionalVerb(SelfAssignCommand,
								delegate(BrowseLogic browserLogic) {
									List<IDisablerProperties> disablers = SelfAssignDisablers();
									disablers.Insert(0,browserLogic.NeedSingleSelectionDisabler);
									return new MultiCommandIfAllEnabled(new CallDelegateCommand(SelfAssignTip,
										delegate() {
											SelfAssignmentEditor(browserLogic, browserLogic.BrowserSelectionPositioner.CurrentPosition.Id);
										}),
										disablers.ToArray()
									);
								}
							)
#endif
						)
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestUnassignedEditorTblCreator, OnlyViewEdit,
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateNewId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID))),
						CompositeView.IdentificationOverride(TId.NewRequest)),
					CompositeView.ChangeEditTbl(RequestUnassignedEditorTblCreator, OnlyViewEdit,
						CompositeView.UseSamePanelAs(0),
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateInProgressId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID))),
						CompositeView.IdentificationOverride(TId.InProgressRequest))
#if KEVINSWAY
					,
					AssignToSelfVerb(false)
#endif
				);
			});
#if KEVINSWAY
			PocketRequestUnassignedBrowseTbl = new DelayedCreateTbl(delegate() {
				var EmptyNewRequestEditTbl = new DelayedCreateTbl(new Tbl(dsMB.Schema.T.Request, TId.Request, new Tbl.IAttr[] { new ETbl(ETbl.EditorDefaultAccess(false)) }, new TblLayoutNodeArray()));
				var EmptyInProgressRequestEditTbl = new DelayedCreateTbl(new Tbl(dsMB.Schema.T.Request, TId.Request, new Tbl.IAttr[] { new ETbl(ETbl.EditorDefaultAccess(false)) }, new TblLayoutNodeArray()));
				return new CompositeTbl(dsMB.Schema.T.Request, TId.UnassignedRequest,
					new Tbl.IAttr[] {
						new UseNamedTableSchemaPermissionTbl("UnassignedRequest"),
						new BTbl()
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(EmptyNewRequestEditTbl,
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateNewId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID)))),
					CompositeView.ChangeEditTbl(EmptyInProgressRequestEditTbl,
						CompositeView.AddRecognitionCondition(SqlExpression.Constant(KnownIds.RequestStateInProgressId).Eq(new SqlExpression(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID)))),
					AssignToSelfVerb(false)
				);
			});
#endif

			RequestClosedBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.Request, TId.ClosedRequest,
					new Tbl.IAttr[] {
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsClosed, true),
						RequestNumberListColumn, RequestRequestorListColumn, RequestBusinessPhoneListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl(() => TIReports.RequestClosedFormReport))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestEditorTblCreator, OnlyViewEdit)
				);
			});
			RequestInProgressWithWOBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.Request, TId.InProgressRequestWithLinkedWorkOrder,
					new Tbl.IAttr[] {
						WorkOrdersAndRequestsGroup,
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress, true),
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders).Gt(0)),
							RequestNumberListColumn, RequestPriorityListColumnSortValue, RequestPriorityListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl(() =>TIReports.RequestInProgressWithWOFormReport))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestEditorTblCreator, OnlyViewEdit)
				);
			});
			RequestInProgressWithoutWOBrowseTbl = new DelayedCreateTbl(delegate() {
				return new CompositeTbl(dsMB.Schema.T.Request, TId.InProgressRequestWithNoLinkedWorkOrder,
					new Tbl.IAttr[] {
						WorkOrdersAndRequestsGroup,
						new BTbl(
							MB3BTbl.HasStateHistory(RequestHistoryTable),
							BTbl.EqFilter(dsMB.Path.T.Request.F.CurrentRequestStateHistoryID.F.RequestStateID.F.FilterAsInProgress, true),
							BTbl.ExpressionFilter(new SqlExpression(dsMB.Path.T.Request.F.Id.L.RequestExtras.RequestID.F.CountOfLinkedWorkOrders).Eq(0)),
							RequestNumberListColumn, RequestPriorityListColumnSortValue, RequestPriorityListColumn, RequestStatusListColumn, RequestSubjectListColumn, RequestStateAuthorListColumn, OpenEmailCommand
						),
						TIReports.NewRemotePTbl(new DelayedCreateTbl(() => TIReports.RequestInProgressWithoutWOFormReport ))
					},
					null,	// no record type
					CompositeView.ChangeEditTbl(RequestEditorTblCreator, OnlyViewEdit)
				);
			});

			#endregion
			#region Requestor
			BTbl.ICtorArg[] RequestorListColumns = {
				BTbl.ListColumn(dsMB.Path.T.Requestor.F.ContactID.F.Code),
				BTbl.ListColumn(dsMB.Path.T.Requestor.F.ContactID.F.BusinessPhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter),
				BTbl.ListColumn(dsMB.Path.T.Requestor.F.ContactID.F.MobilePhone, BTbl.ListColumnArg.Contexts.List|BTbl.ListColumnArg.Contexts.SearchAndFilter)
			};
			TblLayoutNodeArray requestorNodes = new TblLayoutNodeArray(
					DetailsTabNode.New(
						ContactGroupTblLayoutNode(ContactGroupRow(dsMB.Path.T.Requestor.F.ContactID, ECol.Normal)),
						TblColumnNode.New(dsMB.Path.T.Requestor.F.ContactID.F.PreferredLanguage, DCol.Normal, ECol.AllReadonly),
						TblColumnNode.New(dsMB.Path.T.Requestor.F.ReceiveAcknowledgement, new FeatureGroupArg(MainBossServiceAsWindowsServiceGroup), new DCol(Fmt.SetLabelPositioning(Fmt.LabelPositioning.BlankOnSide), Fmt.SetEnumText(ReceiveAcknowledgementEnumText)), ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.Requestor.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Request, TId.Requestor, 
						TblColumnNode.NewBrowsette(dsMB.Path.T.Request.F.RequestorID, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.WorkOrder, TId.Requestor, 
						TblColumnNode.NewBrowsette(dsMB.Path.T.WorkOrder.F.RequestorID, DCol.Normal, ECol.Normal))
			);
			RequestorForRequestsTblCreator = new DelayedCreateTbl( delegate()
			{
				return new Tbl(dsMB.Schema.T.Requestor, TId.Requestor,
				new Tbl.IAttr[] {
					RequestorsGroup,
					new BTbl(RequestorListColumns),
					new ETbl(),
//					new ImageTbl(Images.Requestor, KB.TOi(TId.Requestor)),
					TIReports.NewRemotePTbl(TIReports.RequestorReport)
				},
				(TblLayoutNodeArray)requestorNodes.Clone()
				);
			});
			RequestorForRequestsOrWorkOrdersTblCreator = new DelayedCreateTbl(delegate()
			{
				return new Tbl(dsMB.Schema.T.Requestor, TId.Requestor,
				new Tbl.IAttr[] {
					new BTbl(RequestorListColumns),
					new ETbl(),
//					new ImageTbl(Images.Requestor, KB.TOi(TId.Requestor)),
					TIReports.NewRemotePTbl(TIReports.RequestorReport)
				},
				(TblLayoutNodeArray)requestorNodes.Clone()
				);
			});
			DefineTbl(dsMB.Schema.T.Requestor, RequestorForRequestsOrWorkOrdersTblCreator);
			RegisterExistingForImportExport(TId.Requestor, dsMB.Schema.T.Requestor);

			// Separate Tbl for putting in the WorkOrders group to avoid a Requestors node appearing under a WorkOrder control panel node when in Request Only Mode
			RequestorForWorkOrdersTblCreator = new DelayedCreateTbl(delegate()
			{
				return new Tbl(dsMB.Schema.T.Requestor, TId.Requestor,
				new Tbl.IAttr[] {
					WorkOrdersGroup,
					new BTbl(RequestorListColumns),
					new ETbl(),
//					new ImageTbl(Images.Requestor, KB.TOi(TId.Requestor)),
					TIReports.NewRemotePTbl(TIReports.RequestorReport)
				},
				requestorNodes
				);
			});
			#endregion
			#region RequestPriority
			DefineTbl(dsMB.Schema.T.RequestPriority, delegate() {
				return new Tbl(dsMB.Schema.T.RequestPriority, TId.RequestPriority,
				new Tbl.IAttr[] {
					RequestsGroup,
					new BTbl(BTbl.ListColumn(dsMB.Path.T.RequestPriority.F.Code),
							BTbl.ListColumn(dsMB.Path.T.RequestPriority.F.Desc),
							BTbl.ListColumn(dsMB.Path.T.RequestPriority.F.Rank, BTbl.ListColumnArg.Contexts.SortInitialAscending|NonPerViewColumn)
					),
					new ETbl(),
					TIReports.NewCodeDescPTbl()
				},
				new TblLayoutNodeArray(
					DetailsTabNode.New(
						TblColumnNode.New(dsMB.Path.T.RequestPriority.F.Code, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestPriority.F.Desc, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestPriority.F.Rank, DCol.Normal, ECol.Normal),
						TblColumnNode.New(dsMB.Path.T.RequestPriority.F.Comment, DCol.Normal, ECol.Normal)),
					BrowsetteTabNode.New(TId.Request, TId.RequestPriority,
						TblColumnNode.NewBrowsette(dsMB.Path.T.Request.F.RequestPriorityID, DCol.Normal, ECol.Normal) )
				));
			});
			#endregion
		}
		private class HasEmailDisabler : Thinkage.Libraries.Presentation.BrowseLogic.GeneralConditionDisabler {
			public HasEmailDisabler(BrowseLogic browser, Libraries.DataFlow.Source emailRequestId)
				: base(browser, KB.K("The request has no associated Email Message")
					, () => emailRequestId.GetValue() != null) { }
		}

	}
}
