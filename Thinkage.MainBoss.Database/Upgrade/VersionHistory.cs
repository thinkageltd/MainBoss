using System;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.Xml;
using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary.MSSql;

namespace Thinkage.MainBoss.Database {
	public class MBUpgrader {
#if DEBUG
		public static void CheckUpgradeSteps() {
			int stepCount;
			System.Diagnostics.Debug.WriteLine("Verifying upgrade steps");
			System.Diagnostics.Debug.Indent();
			DBUpgrader.RollbackSchema(UpgradeInformation, new Version(1, 0, 1, 0), null, out stepCount);
			System.Diagnostics.Debug.Unindent();
			Thinkage.Libraries.Diagnostics.Debug.WriteFormattedLine(null, "Done: {0} steps", stepCount);
		}
#else
		public static void CheckUpgradeSteps() {
		}
#endif
		#region Delegate to return the "old" schema for RemoveSchemaObject upgrade steps
		// The Remove-object steps require a delegate that fetches the "old" schema to allow the post-step schema to be rolled back
		// to the pre-step state.
		// We currently only keep one "old" schema, Dummies.xaf.
		// This delegate loads the schema the first time it is called, and caches the result forever. We should probably make ourselves
		// an class structure for which this would be instance data so it would be discarded at the end of the Upgrade.
		// We may also want to encapsulate this better once we have more than one "old" schema.
		private static readonly SchemaUpgradeStep.GetOldSchema GetOriginalSchema = delegate () {
			if(pOriginalSchema == null) {
				pOriginalSchema = new DBI_Database();
				pOriginalSchema.LoadFromXml("manifest://localhost/Thinkage.MainBoss.Database.Upgrade.Dummies.xaf", new ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly()));
			}
			return pOriginalSchema;
		};
		private static DBI_Database pOriginalSchema = null;
		private static DBI_Database GetPreviousSchema([Thinkage.Libraries.Translation.Invariant]string name) {
			var schema = new DBI_Database();
			schema.LoadFromXml(Thinkage.Libraries.Strings.IFormat("manifest://localhost/Thinkage.MainBoss.Database.Upgrade.PreviousSchema.{0}.xaf", name), new ManifestXmlResolver(System.Reflection.Assembly.GetExecutingAssembly()));
			return schema;
		}
#if LATER_InlineOldSchemas
		// the idea of this is to stop adding to the massive Dummies file, and just have each "Remove" step specify in-line what it is removing by giving a string of XML containing the schema.
		// This does not work right now, though, because Statics.ReadXmlDocument requires that the resolver's GetEntity method return a Stream, and all we can give is a TextReader.
		// This is not intractable, since once it has the result of the resolving, it calls XmlReader.Create which has an alternative signature which accepts a TextReader (and otherwise same arguments)
		private class StringXmlResolver : System.Xml.XmlUrlResolver {
			public StringXmlResolver(string contents) {
				Contents = contents;
			}
			private readonly string Contents;
			/// <summary>
			/// Override for <see cref="XmlUrlResolver.GetEntity"/>.
			/// </summary>
			/// <param name="absoluteUri">URI to be resolved. This resolver recognizes the <c>manifest</c> URI scheme,
			///		while all others are passed to <see cref="XmlUrlResolver.GetEntity"/>.</param>
			/// <param name="role">Ignored on <c>manifest</c> URI scheme, otherwise passed to <see cref="XmlUrlResolver.GetEntity"/>.</param>
			/// <param name="ofObjectToReturn">Ignored on <c>manifest</c> URI scheme, otherwise passed to <see cref="XmlUrlResolver.GetEntity"/>.</param>
			/// <returns></returns>
			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {
				if (absoluteUri.Scheme == KB.I("string")) {
					if (!absoluteUri.IsLoopback)
						throw new GeneralException(KB.K("Cannot load string resources from remote (non-local) Urls"));
					if (absoluteUri.AbsolutePath != KB.I("/"))
						throw new GeneralException(KB.K("Cannot load string resources from Urls with non-null path"));
					return new System.IO.StringReader(Contents);
				}
				return base.GetEntity(absoluteUri, role, ofObjectToReturn);
			}
		}
		private static SchemaUpgradeStep.GetOldSchema OldSchema(string xafText) {
			return delegate() {
				var schema = new DBI_Database();
				schema.LoadFromXml("string://localhost/", new StringXmlResolver(@"
									<?xml version='1.0' encoding='UTF-16'>
									<database xmlns='http://www.thinkage.ca/XmlNamespaces/XAF'>"
									+xafText
									+@"</database>"), null);
				return schema;
			};
		}
#endif
		#endregion
		#region Upgrade steps - static UpgraderInformation definition for mainboss (belongs way elsewhere)
		private static UpgradeStep GetServiceMessageStep([Thinkage.Libraries.Translation.Invariant]string messageName) {
			return new SqlUpgradeStep(
				Strings.IFormat(@"
					INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
						VALUES (NEWID(), '{0}', 'Thinkage.MainBoss.Database' + NCHAR(167) + '{0}_Comment', 'MainBossService')
					INSERT INTO UserMessageTranslation (Id, [UserMessageKeyID], [LanguageLCID], [Translation])
						SELECT TOP 1 NEWID(), UMK.ID, 127, SC.{0}
									FROM UserMessageKey as UMK, ServiceConfiguration as SC
									WHERE UMK.[KEY] = '{0}'
					", messageName));
		}
		// The table of version upgraders, in the order they should be applied. TODO: Although this is a static in DBUpgrader, no code here directly
		// refers to it. The only references are way up at or near the Application level. The entire object really belongs somewhere like DatabaseLayout.
		// All of the steps are implicitly numbered, with each subscript corresponding to one of the 4 parts of the
		// Version information.
		// Upgraders[a][b][c][d] is the step that produces database version a.b.c.d
		// Note that the DB version numbers do not need to synchronize with the program version numbers at all.
		// I expect that we will however keep the major and minor numbers in sync, change the release number
		// when we do an "internal" release and normally just advance the revision numbers.
		public static readonly DBVersionInformation UpgradeInformation = new DBVersionInformation(new UpgradeStep[][][][] {
			new UpgradeStep[][][] {},	// 0.x.x.x
			new UpgradeStep[][][] {		// 1.x.x.x
				#region 1.0.x.x for MainBoss versions 3.0 to 3.4
				new UpgradeStep[][] {	// 1.0.x.x
					new UpgradeStep[] {	// 1.0.0.x
					// All these steps predate MB3.0 during development; we no longer have any development databases we care about that predate MB 3.0 release.
					},
					new UpgradeStep[] { // 1.0.1.x Reserved for steps included in released versions of MB3.0
						#region 1.0.1.0 -
						// MB 3.0.0 uses db version 1.0.1.0
						new UpgradeStepSequence( //1.0.1.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.0.0.249'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.0.0.249'")
						),
						new UpgradeStepSequence( // 1.0.1.1 - fix divide by zero in mbfn_CalculateHourlyCost
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_CalculateHourlyCost"),
							new AddExtensionObjectUpgradeStep("mbfn_CalculateHourlyCost")
						),
						new UpgradeStepSequence( // 1.0.1.2 - Allow Demandxxx quantities to be nullable
							new SetColumnRequiredUpgradeStep("DemandItem.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborInside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkInside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandItemTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborInsideTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutsideTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkInsideTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutsideTemplate.Quantity")
						),
						new UpgradeStepSequence( // 1.0.1.3 - Correct ItemCountValue trigger to use proper .ID field for performance improvement
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValue_Updates_ActualItemLocationEtAl")
						),
						// MB 3.0.1 uses db version 1.0.1.4 but still allows MB 3.0.0 to run
						new UpgradeStepSequence( // 1.0.1.4 - Added fields to UnitReport, WorkOrderReports and their inheriting views.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ManPowerScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Specification_Summary"),
							new AddExtensionObjectUpgradeStep("mbfn_Specification_Summary"),
							new AddExtensionObjectUpgradeStep("mbfn_Attachment_Summary"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("UnitSparePartsReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("UnitReplacementScheduleReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("ManPowerScheduleReport")
						),
						new UpgradeStepSequence( // 1.0.1.5 - added @R support for pop3s and imaps server types; consolidate a couple of data db variables.
							new AddVariableUpgradeStep("ATRMailServerType"),
							new SqlUpgradeStep(@"
							declare @mailservertype as int;
							set @mailservertype =
							(select case
							when dbo._vgetATRMailUseIMAP4() = 0 and dbo._vgetATRMailAttemptTLS() = 1 then 2
							when dbo._vgetATRMailUseIMAP4() = 1 and dbo._vgetATRMailAttemptTLS() = 0 then 3
							when dbo._vgetATRMailUseIMAP4() = 1 and dbo._vgetATRMailAttemptTLS() = 1 then 5
							else 0
							end);
							exec dbo._vsetATRMailServerType @mailservertype;
							"),
							new RemoveVariableUpgradeStep(GetOriginalSchema,"ATRMailUseIMAP4"),
							new RemoveVariableUpgradeStep(GetOriginalSchema,"ATRMailAttemptTLS")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.0.2.x
						#region 1.0.2.0 - 1.0.2.9
						new UpgradeStepSequence( // 1.0.2.0 - Add chargeback form (report)
							new AddTableUpgradeStep("ChargebackFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.1 - fix divide by zero in mbfn_CalculateHourlyCost
							new Version(1,0,2,0), 	// Already upgraded in the 1.0.1.x stream as 1.0.1.1, only needed if we start >= 1.0.2.x
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_CalculateHourlyCost"),
							new AddExtensionObjectUpgradeStep("mbfn_CalculateHourlyCost")
						),
						new UpgradeStepSequence( // 1.0.2.2 - views: renamed ManpowerScheduleReport to LaborForecastReport and added MaterialForecastReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "ManpowerScheduleReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.2.3 - Allow Demandxxx quantities to be nullable
							new Version(1,0,2,0), 	// Already upgraded in the 1.0.1.x stream as 1.0.1.2, only needed if we start >= 1.0.2.x
							new SetColumnRequiredUpgradeStep("DemandItem.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborInside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkInside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutside.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandItemTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborInsideTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutsideTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkInsideTemplate.Quantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutsideTemplate.Quantity")
						),
						new UpgradeStepSequence( // 1.0.2.4 - Correct ItemCountValue trigger to use proper .ID field for performance improvement
							new Version(1,0,2,0),	// Already upgraded in the 1.0.1.x stream as 1.0.1.3, only needed if we start >= 1.0.2.x
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValue_Updates_ActualItemLocationEtAl")
						),
						new UpgradeStepSequence( // 1.0.2.5 - - Added fields to UnitReport, WorkOrderReports and their inheriting views.
							new Version(1,0,2,0), 	// Already upgraded in the 1.0.1.x stream as 1.0.1.4, only needed if we start >= 1.0.2.x
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Specification_Summary"),
							// mbfn_Attachment_Summary was added in 1.0.1.4 and will not exist in newer databases that didn't get that upgrade step; so we do a drop if exists here.
							new SqlUpgradeStep(@"
							if object_id('mbfn_Attachment_Summary', 'FN') is not null
								drop function mbfn_Attachment_Summary;
							"),
							new AddExtensionObjectUpgradeStep("mbfn_Specification_Summary"),
							new AddExtensionObjectUpgradeStep("mbfn_Attachment_Summary"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("UnitSparePartsReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("UnitReplacementScheduleReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("ChargebackFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.6 - added @R support for pop3s and imaps server types; consolidate a couple of data db variables.
							new Version(1,0,2,0),	// Already upgraded in the 1.0.1.x stream as 1.0.1.5, only needed if we start >= 1.0.2.x
							new AddVariableUpgradeStep("ATRMailServerType"),
							new SqlUpgradeStep(@"
							declare @mailservertype as int;
							set @mailservertype =
							(select case
							when dbo._vgetATRMailUseIMAP4() = 0 and dbo._vgetATRMailAttemptTLS() = 1 then 2 -- DatabaseEnums.MailServerType.POP3STLS
							when dbo._vgetATRMailUseIMAP4() = 1 and dbo._vgetATRMailAttemptTLS() = 0 then 3 -- DatabaseEnums.MailServerType.IMAP4
							when dbo._vgetATRMailUseIMAP4() = 1 and dbo._vgetATRMailAttemptTLS() = 1 then 5 -- DatabaseEnums.MailServerType.IMAP4STARTTLS
							else 0  -- DatabaseEnums.MailServerType.POP3
							end);
							exec dbo._vsetATRMailServerType @mailservertype;
							"),
							new RemoveVariableUpgradeStep(GetOriginalSchema,"ATRMailUseIMAP4"),
							new RemoveVariableUpgradeStep(GetOriginalSchema,"ATRMailAttemptTLS")
						),
						new UpgradeStepSequence( // 1.0.2.7 - Modernize the ReceiptActivity and ItemReceiving views
							new RemoveTableUpgradeStep(GetOriginalSchema, "ReceiptActivity"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceiving"),
							new AddTableUpgradeStep("ItemReceiving"),
							new AddTableUpgradeStep("ItemReceivingReport"),
							new AddTableUpgradeStep("ReceiptActivity")
						),
						new UpgradeStepSequence( // 1.0.2.8 - Add Comment field to Requestor table similar to Billable Requestor
							new AddColumnUpgradeStep("Requestor.Comment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new AddTableUpgradeStep("RequestorReport"),
							new AddTableUpgradeStep("BillableRequestorReport")
						),
						new UpgradeStepSequence( //1.0.2.9 - Change VendorID field to PurchaseOrderID field in Receipt table
							new AddColumnUpgradeStep("Receipt.PurchaseOrderID"),
							// Now need to convert existing (old) receipts to ones linked to purchase orders
							new SqlUpgradeStep(@"
-- Set the Purchase order linkage in each receipt to *one of* the linked PO's
 WITH PurchaseOrderReceiptLinkage(PurchaseOrderID, ReceiptID) AS (
	SELECT PurchaseOrderID, ReceiptID FROM ReceiveItemPO JOIN POLineItem ON ReceiveItemPO.POLineItemID = POLineItem.ID JOIN POLine ON POLineItem.POLineID = POLine.ID
	union
	SELECT PurchaseOrderID, ReceiptID FROM ReceiveMiscellaneousPO JOIN POLineMiscellaneous ON ReceiveMiscellaneousPO.POLineMiscellaneousID = POLineMiscellaneous.ID JOIN POLine ON POLineMiscellaneous.POLineID = POLine.ID
	union
	SELECT PurchaseOrderID, ReceiptID FROM ActualLaborOutsidePO JOIN POLineLabor ON ActualLaborOutsidePO.POLineLaborID = POLineLabor.ID JOIN POLine ON POLineLabor.POLineID = POLine.ID
	union
	SELECT PurchaseOrderID, ReceiptID FROM ActualOtherWorkOutsidePO JOIN POLineOtherWork ON ActualOtherWorkOutsidePO.POLineOtherWorkID = POLineOtherWork.ID JOIN POLine ON POLineOtherWork.POLineID = POLine.ID
)
UPDATE Receipt SET PurchaseOrderID = (SELECT TOP 1 PurchaseOrderID FROM PurchaseOrderReceiptLinkage WHERE ReceiptID = Receipt.ID) -- ORDER BY required to use TOP?
-- Check for Receipts with NO PO linkage. ALternatively, ignore this condition and the SetColumnRequiredUpgradeStep will fail.
IF EXISTS(SELECT * FROM Receipt WHERE PurchaseOrderID IS NULL) BEGIN
	insert into DatabaseHistory (id, EntryDate, Subject, Description)
		values (newid(), dbo._DClosestValue(getdate(),2,100), 'Receipts Deleted', 'Receipts that could not be matched to Purchase Orders were deleted in upgrade step 1.0.2.9');
	DELETE Receipt where PurchaseOrderID IS NULL
END
-- Add a temporary working column to the receipts table. This will be null for the receipts already joined to the PO.
ALTER TABLE Receipt ADD OriginalReceiptID UNIQUEIDENTIFIER NULL
--	FOREIGN KEY (OriginalReceiptID) REFERENCES Receipt
"),
							new SqlUpgradeStep(@"
-- Insert duplicates of the receipts for all the additional PO linkages that each requires, making the temp fields link to the original receipt.
WITH PurchaseOrderReceiptLinkage(PurchaseOrderID, ReceiptID) AS (
	SELECT PurchaseOrderID, ReceiptID FROM ReceiveItemPO JOIN POLineItem ON ReceiveItemPO.POLineItemID = POLineItem.ID JOIN POLine ON POLineItem.POLineID = POLine.ID
	union
	SELECT PurchaseOrderID, ReceiptID FROM ReceiveMiscellaneousPO JOIN POLineMiscellaneous ON ReceiveMiscellaneousPO.POLineMiscellaneousID = POLineMiscellaneous.ID JOIN POLine ON POLineMiscellaneous.POLineID = POLine.ID
	union
	SELECT PurchaseOrderID, ReceiptID FROM ActualLaborOutsidePO JOIN POLineLabor ON ActualLaborOutsidePO.POLineLaborID = POLineLabor.ID JOIN POLine ON POLineLabor.POLineID = POLine.ID
	union
	SELECT PurchaseOrderID, ReceiptID FROM ActualOtherWorkOutsidePO JOIN POLineOtherWork ON ActualOtherWorkOutsidePO.POLineOtherWorkID = POLineOtherWork.ID JOIN POLine ON POLineOtherWork.POLineID = POLine.ID
)
INSERT INTO Receipt(ID, OriginalReceiptID, PurchaseOrderID, Waybill, VendorID, Hidden, [Desc], Comment, EffectiveDate)
	SELECT NEWID(), R.ID, EL.PurchaseOrderID, R.Waybill, R.VendorID, R.Hidden, R.[Desc], R.Comment, R.EffectiveDate
		FROM Receipt AS R
		JOIN (
			SELECT DISTINCT L.PurchaseOrderID, L.ReceiptID
			FROM PurchaseOrderReceiptLinkage AS L
			JOIN Receipt AS R ON R.ID = L.ReceiptID
			WHERE R.PurchaseOrderID != L.PurchaseOrderID
		) AS EL ON EL.ReceiptID = R.ID
-- Update all the receive line items to link to the correct Receipt.
-- The JOINs link the ReceiveLine's to all of the clones of their original Receipt record, and the WHERE clause selects the right receipt,
-- i.e. the one tied to the PO that the ReceiveLine is receiving against.
-- Unfortunately we have no common base table for all receiving so we must do this 4 times, once for each PO receiving line item type that exists.
UPDATE ReceiveItemPO SET ReceiptID = RNew.ID
	FROM ReceiveItemPO
	JOIN POLineItem ON ReceiveItemPO.POLineItemID = POLineItem.ID
	JOIN POLine ON POLineItem.POLineID = POLine.ID
	JOIN Receipt AS RNew ON ReceiveItemPO.ReceiptID = RNew.ID
	WHERE RNew.PurchaseOrderID = POLine.PurchaseOrderID
UPDATE ReceiveMiscellaneousPO SET ReceiptID = RNew.ID
	FROM ReceiveMiscellaneousPO
	JOIN POLineMiscellaneous ON ReceiveMiscellaneousPO.POLineMiscellaneousID = POLineMiscellaneous.ID
	JOIN POLine ON POLineMiscellaneous.POLineID = POLine.ID
	JOIN Receipt AS RNew ON ReceiveMiscellaneousPO.ReceiptID = RNew.ID
	WHERE RNew.PurchaseOrderID = POLine.PurchaseOrderID
UPDATE ActualLaborOutsidePO SET ReceiptID = RNew.ID
	FROM ActualLaborOutsidePO
	JOIN POLineLabor ON ActualLaborOutsidePO.POLineLaborID = POLineLabor.ID
	JOIN POLine ON POLineLabor.POLineID = POLine.ID
	JOIN Receipt AS RNew ON ActualLaborOutsidePO.ReceiptID = RNew.ID
	WHERE RNew.PurchaseOrderID = POLine.PurchaseOrderID
UPDATE ActualOtherWorkOutsidePO SET ReceiptID = RNew.ID
	FROM ActualOtherWorkOutsidePO
	JOIN POLineOtherWork ON ActualOtherWorkOutsidePO.POLineOtherWorkID = POLineOtherWork.ID
	JOIN POLine ON POLineOtherWork.POLineID = POLine.ID
	JOIN Receipt AS RNew ON ActualOtherWorkOutsidePO.ReceiptID = RNew.ID
	WHERE RNew.PurchaseOrderID = POLine.PurchaseOrderID
-- Drop the work column

ALTER TABLE Receipt DROP COLUMN OriginalReceiptID
							"),
							// Final cleanup remove old column
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Receipt.VendorID"),
							new SetColumnRequiredUpgradeStep("Receipt.PurchaseOrderID")
						),
						#endregion
						#region 1.0.2.10 - 1.0.2.19
						new UpgradeStepSequence( // 1.0.2.10 - Correct view to provide proper ParentID for correction records.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivity"),
							new AddTableUpgradeStep("ItemActivity")
						),
						new UpgradeStepSequence( // 1.0.2.11 - Updated ReceiptActivity view includes PO lines from linked PO
							new RemoveTableUpgradeStep(GetOriginalSchema, "ReceiptActivity"),
							new AddTableUpgradeStep("ReceiptActivity")
						),
						new UpgradeStepSequence( // 1.0.2.12 - Add SelectPrintFlag to WO, PO and Request
							new AddColumnUpgradeStep("WorkOrder.SelectPrintFlag"),
							new AddColumnUpgradeStep("PurchaseOrder.SelectPrintFlag"),
							new AddColumnUpgradeStep("WorkOrderTemplate.SelectPrintFlag"),
							new AddColumnUpgradeStep("PurchaseOrderTemplate.SelectPrintFlag"),
							new AddColumnUpgradeStep("Request.SelectPrintFlag"),
							new SqlUpgradeStep(@"
								UPDATE _DWorkOrder SET SelectPrintFlag = 1
								UPDATE _DPurchaseOrder SET SelectPrintFlag = 1
								UPDATE _DRequest Set SelectPrintFlag = 1
								UPDATE WorkOrder SET SelectPrintFlag = 0
								UPDATE Request SET SelectPrintFlag = 0
								UPDATE PurchaseOrder SET SelectPrintFlag = 0
							"),
							new SetColumnRequiredUpgradeStep("WorkOrder.SelectPrintFlag"),
							new SetColumnRequiredUpgradeStep("PurchaseOrder.SelectPrintFlag"),
							new SetColumnRequiredUpgradeStep("Request.SelectPrintFlag")
						),
						new UpgradeStepSequence( // 1.0.2.13 - Add the SelectPrintFlag to work order report views
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),

							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("ChargebackFormReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport")
						),
						new UpgradeStepSequence( // 1.0.2.14 - Add SelectPrintFlag to purchasing report views
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("ItemOnOrderReport"),
							new AddTableUpgradeStep("ItemReceivingReport")
						),
						new UpgradeStepSequence( // 1.0.2.15 - Add SelectPrintFlag to Request report views
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport")
						),
						new UpgradeStepSequence( // 1.0.2.16 - Add the SelectPrintFlag to work order template/purchase order template report views
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateReport"),

							new AddTableUpgradeStep("WorkOrderTemplateReport"),
							new AddTableUpgradeStep("PurchaseOrderTemplateReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.2.17 - Adjust the defaults for WorkOrderTemplate/PurchaseOrderTemplate for SelectPrintFlag
							new SqlUpgradeStep(@"
								UPDATE _DWorkOrderTemplate SET SelectPrintFlag = 1
								UPDATE _DPurchaseOrderTemplate SET SelectPrintFlag = 1
								UPDATE WorkOrderTemplate SET SelectPrintFlag = 1 where ContainingWorkOrderTemplateID is null
								UPDATE PurchaseOrderTemplate SET SelectPrintFlag = 1
							")
						),
						new UpgradeStepSequence( // 1.0.2.18 - Changed PurchaseOrderLine to include all receiving, force ReceiptID nonnull.
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderLine"),
							new AddTableUpgradeStep("PurchaseOrderLine"),
							new SetColumnRequiredUpgradeStep("ReceiveItemPO.ReceiptID"),
							new SetColumnRequiredUpgradeStep("ReceiveMiscellaneousPO.ReceiptID"),
							new SetColumnRequiredUpgradeStep("ActualLaborOutsidePO.ReceiptID"),
							new SetColumnRequiredUpgradeStep("ActualOtherWorkOutsidePO.ReceiptID")
						),
						new UpgradeStepSequence( // 1.0.2.19 - Adjust the PurchaseOrderState and transitions for Draft/Issued/Closed/Voided
							new AddColumnUpgradeStep("PurchaseOrderState.FilterAsDraft"),
							new SqlUpgradeStep(@"
								UPDATE PurchaseOrderState SET FilterAsDraft = FilterAsOpen
								UPDATE PurchaseOrderState SET [DESC] = 'Purchase Order being drafted', [CODE] = 'Draft' WHERE  [CODE] = 'Open'
								UPDATE PurchaseOrderStateTransition SET [OperationHint] = 'Withdraw Issued Purchase Order back to Draft status' WHERE Rank = 3
								UPDATE PurchaseOrderStateTransition SET [Operation] = 'ReActivate PO' WHERE Rank = 4
								INSERT INTO PurchaseOrderStateTransition ([ID],[Operation], [OperationHint], [FromState], [ToState], [Rank])
									SELECT NEWID(), 'Draft PO', 'Change a Voided Purchase Order back to Draft status', (SELECT ID FROM PurchaseOrderState where CODE = 'Voided'),
											(SELECT ID From PurchaseOrderState where CODE = 'Draft'), 6
							"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "DatabaseStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderState.FilterAsOpen"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("ItemOnOrderReport"),
							new AddTableUpgradeStep("ItemReceivingReport"),
							new AddTableUpgradeStep("DatabaseStatus")
						),
						#endregion
						#region 1.0.2.20 - 1.0.2.29
						new UpgradeStepSequence( // 1.0.2.20 - Alter fields in Receipt table
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "Receipt.Waybill"),
							new AddColumnUpgradeStep("Receipt.EntryDate"),
							new AddColumnUpgradeStep("Receipt.Reference"),
							new SqlUpgradeStep(@"
								UPDATE Receipt SET EntryDate = EffectiveDate
								delete Receipt where Hidden is not null
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Receipt.EffectiveDate"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Receipt.Hidden"),
							new AddUniqueConstraintUpgradeStep("Receipt.EntryDate")
						),
						new UpgradeStepSequence( // 1.0.2.21 - add PO state flags controlling editing
							new AddColumnUpgradeStep("PurchaseOrderState.CanModifyOrder"),
							new AddColumnUpgradeStep("PurchaseOrderState.CanModifyReceiving"),
							new AddColumnUpgradeStep("PurchaseOrderState.CanHaveReceiving"),
							new SqlUpgradeStep(@"
								UPDATE PurchaseOrderState SET CanModifyOrder = 1, CanModifyReceiving = 0, CanHaveReceiving = 1 WHERE  [CODE] = 'Draft'
								UPDATE PurchaseOrderState SET CanModifyOrder = 0, CanModifyReceiving = 1, CanHaveReceiving = 1 WHERE  [CODE] = 'Issued'
								UPDATE PurchaseOrderState SET CanModifyOrder = 0, CanModifyReceiving = 0, CanHaveReceiving = 1 WHERE  [CODE] = 'Closed'
								UPDATE PurchaseOrderState SET CanModifyOrder = 0, CanModifyReceiving = 0, CanHaveReceiving = 0 WHERE  [CODE] = 'Voided'
							"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.CanModifyOrder"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.CanModifyReceiving"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.CanHaveReceiving")
						),
						new UpgradeStepSequence( // 1.0.2.22 - Change PurchaseOrderLine view to add WorkOrderExpenseModelEntry value for receiving
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderLine"),
							new AddTableUpgradeStep("PurchaseOrderLine")
						),
						new UpgradeStepSequence( // 1.0.2.23 - Correct initial values for trigger-calculated fields
							// Note that ActualItemLocation.OnHand and .TotalCost had an ad hoc solution, code in AlterColumns.sql set their values to zero in the defaults.
							// As a result we don't have to correct these fields in the defaults or actual records, although we still must correct the column nullability.
							// COrrect the null values in the defaults table.
							// We start off by correcting mbfn_ActualItemLocation_OnOrder so that the result is zero if there are no POLineItem records on active PO's
							// for the ItemLocation. Likewise we fix mbfn_ActualItemLocation_OnReserve so the result is zero if there are no DemandItem records on active WO's.
							// We also take the opportunity to remove an unneeded joined table in mbtg_POLineItem_Updates_ActualItemLocation
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_OnOrder"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_POLineItem_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_POLineItem_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualItemLocation_OnOrder"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_OnReserve"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualItemLocation_OnReserve"),
							new SqlUpgradeStep(@"
								UPDATE _DActualItemLocation SET OnOrder = 0, OnReserve = 0
								UPDATE _DChargeback SET TotalCost = 0
								UPDATE _DDemand SET ActualCost = 0
								UPDATE _DDemandItem SET ActualQuantity = 0
								UPDATE _DDemandLaborInside SET ActualQuantity = dbo._INew(0, 0, 0, 0, 0)
								UPDATE _DDemandLaborOutside SET ActualQuantity = dbo._INew(0, 0, 0, 0, 0), OrderQuantity = dbo._INew(0, 0, 0, 0, 0)
								UPDATE _DDemandLaborOutsideTemplate SET OrderQuantity = dbo._INew(0, 0, 0, 0, 0)
								UPDATE _DDemandOtherWorkInside SET ActualQuantity = 0
								UPDATE _DDemandOtherWorkOutside SET ActualQuantity = 0, OrderQuantity = 0
								UPDATE _DDemandOtherWorkOutsideTemplate SET OrderQuantity = 0
								UPDATE _DItem SET Available = 0, OnHand = 0, OnOrder = 0, OnReserve = 0, TotalCost = 0
								UPDATE _DPOLine SET ReceiveCost = 0
								UPDATE _DPOLineItem SET ReceiveQuantity = 0
								UPDATE _DPOLineLabor SET ReceiveQuantity = dbo._INew(0, 0, 0, 0, 0)
								UPDATE _DPOLineMiscellaneous SET ReceiveQuantity = 0
								UPDATE _DPOLineOtherWork SET ReceiveQuantity = 0
								UPDATE _DWorkOrder SET TemporaryStorageEmpty = 1, TotalActual = 0, TotalDemand = 0
								UPDATE _DWorkOrderTemplate SET DemandCount = 0
							"),
							// Correct values in the actual records where child records have never been created
							// These are split into individual steps to ensure each getts the maximum opportunity of SQL request Timeout window. Large databases
							// with lots of data pose problems on time consuming steps.
							new SqlUpgradeStep("UPDATE ActualItemLocation SET OnOrder = 0 WHERE OnOrder IS NULL"),
							new SqlUpgradeStep("UPDATE ActualItemLocation SET OnReserve = 0 WHERE OnReserve IS NULL"),
							new SqlUpgradeStep("UPDATE Chargeback SET TotalCost = 0 WHERE TotalCost IS NULL"),
							new SqlUpgradeStep("UPDATE Demand SET ActualCost = 0 WHERE ActualCost IS NULL"),
							new SqlUpgradeStep("UPDATE DemandItem SET ActualQuantity = 0 WHERE ActualQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandLaborInside SET ActualQuantity = dbo._INew(0, 0, 0, 0, 0) WHERE ActualQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandLaborOutside SET ActualQuantity = dbo._INew(0, 0, 0, 0, 0) WHERE ActualQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandLaborOutside SET OrderQuantity = dbo._INew(0, 0, 0, 0, 0) WHERE OrderQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandLaborOutsideTemplate SET OrderQuantity = dbo._INew(0, 0, 0, 0, 0) WHERE OrderQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandOtherWorkInside SET ActualQuantity = 0 WHERE ActualQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandOtherWorkOutside SET ActualQuantity = 0 WHERE ActualQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandOtherWorkOutside SET OrderQuantity = 0 WHERE OrderQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE DemandOtherWorkOutsideTemplate SET OrderQuantity = 0 WHERE OrderQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE Item SET Available = 0 WHERE Available IS NULL"),
							new SqlUpgradeStep("UPDATE Item SET OnHand = 0 WHERE OnHand IS NULL"),
							new SqlUpgradeStep("UPDATE Item SET OnOrder = 0 WHERE OnOrder IS NULL"),
							new SqlUpgradeStep("UPDATE Item SET OnReserve = 0 WHERE OnReserve IS NULL"),
							new SqlUpgradeStep("UPDATE Item SET TotalCost = 0 WHERE TotalCost IS NULL"),
							new SqlUpgradeStep("UPDATE POLine SET ReceiveCost = 0 WHERE ReceiveCost IS NULL"),
							new SqlUpgradeStep("UPDATE POLineItem SET ReceiveQuantity = 0 WHERE ReceiveQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE POLineLabor SET ReceiveQuantity = dbo._INew(0, 0, 0, 0, 0) WHERE ReceiveQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE POLineMiscellaneous SET ReceiveQuantity = 0 WHERE ReceiveQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE POLineOtherWork SET ReceiveQuantity = 0 WHERE ReceiveQuantity IS NULL"),
							new SqlUpgradeStep("UPDATE WorkOrder SET TemporaryStorageEmpty = 1 WHERE TemporaryStorageEmpty IS NULL"),
							new SqlUpgradeStep("UPDATE WorkOrder SET TotalActual = 0 WHERE TotalActual IS NULL"),
							new SqlUpgradeStep("UPDATE WorkOrder SET TotalDemand = 0 WHERE TotalDemand IS NULL"),
							new SqlUpgradeStep("UPDATE WorkOrderTemplate SET DemandCount = 0 WHERE DemandCount IS NULL"),
							// Set all the columns non-null.
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ActualItemLocation.Available"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ActualItemLocation.UnitCost"),
							new SetColumnRequiredUpgradeStep("ActualItemLocation.OnHand"),
							new SetColumnRequiredUpgradeStep("ActualItemLocation.OnOrder"),
							new SetColumnRequiredUpgradeStep("ActualItemLocation.OnReserve"),
							new SetColumnRequiredUpgradeStep("ActualItemLocation.TotalCost"),
							new AddColumnUpgradeStep("ActualItemLocation.Available"),
							new AddColumnUpgradeStep("ActualItemLocation.UnitCost"),
							new ChangeToComputedColumnUpgradeStep("ActualItemLocation.Available", "(OnHand + OnOrder - OnReserve)"),
							new ChangeToComputedColumnUpgradeStep("ActualItemLocation.UnitCost", "dbo.mbfn_CalculateUnitCost(TotalCost, OnHand, 1)"),
							new SetColumnRequiredUpgradeStep("Chargeback.TotalCost"),
							new SetColumnRequiredUpgradeStep("Demand.ActualCost"),
							new SetColumnRequiredUpgradeStep("DemandItem.ActualQuantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborInside.ActualQuantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutside.ActualQuantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutside.OrderQuantity"),
							new SetColumnRequiredUpgradeStep("DemandLaborOutsideTemplate.OrderQuantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkInside.ActualQuantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutside.ActualQuantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutside.OrderQuantity"),
							new SetColumnRequiredUpgradeStep("DemandOtherWorkOutsideTemplate.OrderQuantity"),
							new SetColumnRequiredUpgradeStep("Item.Available"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Item.UnitCost"),
							new SetColumnRequiredUpgradeStep("Item.OnHand"),
							new SetColumnRequiredUpgradeStep("Item.OnOrder"),
							new SetColumnRequiredUpgradeStep("Item.OnReserve"),
							new SetColumnRequiredUpgradeStep("Item.TotalCost"),
							new AddColumnUpgradeStep("Item.UnitCost"),
							new ChangeToComputedColumnUpgradeStep("Item.UnitCost", "dbo.mbfn_CalculateUnitCost(TotalCost, OnHand, 1)"),
							new SetColumnRequiredUpgradeStep("POLine.ReceiveCost"),
							new SetColumnRequiredUpgradeStep("POLineItem.ReceiveQuantity"),
							new SetColumnRequiredUpgradeStep("POLineLabor.ReceiveQuantity"),
							new SetColumnRequiredUpgradeStep("POLineMiscellaneous.ReceiveQuantity"),
							new SetColumnRequiredUpgradeStep("POLineOtherWork.ReceiveQuantity"),
							new SetColumnRequiredUpgradeStep("WorkOrder.TemporaryStorageEmpty"),
							new SetColumnRequiredUpgradeStep("WorkOrder.TotalActual"),
							new SetColumnRequiredUpgradeStep("WorkOrder.TotalDemand"),
							new SetColumnRequiredUpgradeStep("WorkOrderTemplate.DemandCount")
						),
						new UpgradeStepSequence( // 1.0.2.24 - Remove Quantity from PurchaseOrderTemplateLine view and add new WorkOrderPurchaseOrderLinkage view
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateLine"),
							new AddTableUpgradeStep("PurchaseOrderTemplateLine"),
							new AddTableUpgradeStep("WorkOrderPurchaseOrderLinkage")
						),
						new UpgradeStepSequence( // 1.0.2.25 - Revise WorkOrderPurchaseOrderLinkage view for TreeStructured browsettes
							new AddUniqueConstraintUpgradeStep("WorkOrderPurchaseOrder.WorkOrderID"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderPurchaseOrderLinkage"),
							new AddTableUpgradeStep("WorkOrderPurchaseOrderLinkage"),
							new AddTableUpgradeStep("WorkOrderPurchaseOrderView"),
							new AddTableUpgradeStep("WorkOrderLinkedPurchaseOrdersTreeview"),
							new AddTableUpgradeStep("PurchaseOrderLinkedWorkOrdersTreeview")
						),
						new UpgradeStepSequence( // 1.0.2.26 - Fix PurchaseOrder Form Report
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddVariableUpgradeStep("POFormAdditionalBlankLines"),
							new AddVariableUpgradeStep("POFormAdditionalInformation"),
							new SqlUpgradeStep( // set default values for new variables.
							@"
							UPDATE __Variables SET VALUE = CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(max),'Authorized by ___________________________________')) where NAME = 'POFormAdditionalInformation';
							UPDATE __Variables SET VALUE = CONVERT(VARBINARY(MAX), 6) WHERE NAME = 'POFormAdditionalBlankLines';
							")
						),
						new UpgradeStepSequence( // 1.0.2.27 - Rename Rank in POLine/POLineTemplate, and change XID to include
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "POLine.Rank"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "POLineTemplate.Rank"),
							new AddColumnUpgradeStep("POLine.LineNumber"),
							new AddColumnUpgradeStep("POLineTemplate.LineNumberRank"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.28 - Remove unused mbfn_PurchaseOrder_CreationDate, add PurchaseOrder.HasReceiving
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_CreationDate"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_HasReceiving"),
							new AddColumnUpgradeStep("PurchaseOrder.HasReceiving"),
							new SqlUpgradeStep(@"
								UPDATE PurchaseOrder SET HasReceiving = dbo.mbfn_PurchaseOrder_HasReceiving(ID)
								UPDATE _DPurchaseOrder SET HasReceiving = 0
							"),
							new SetColumnRequiredUpgradeStep("PurchaseOrder.HasReceiving"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemPO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemPO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualLaborOutsidePO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualLaborOutsidePO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualOtherWorkOutsidePO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualOtherWorkOutsidePO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveMiscellaneousPO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveMiscellaneousPO_Updates_Corrected")
						),
						new UpgradeStepSequence( // 1.0.2.29 - change PurchaseOrderFormReport details and layout
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddVariableUpgradeStep("POInvoiceContactID"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						#endregion
						#region 1.0.2.30 - 1.0.2.39
						new UpgradeStepSequence( //1.0.2.30 - change type range of ItemPrice quantity/cost and POLine LineNumber to positive values only
							new SqlUpgradeStep(@"
								UPDATE ItemPrice Set Quantity = ABS(Quantity), Cost = ABS(Cost)
							")
							// Change in types did not affect the underlying sql types of the fields and there is no specific upgrade step to change the underlying type of a SQL column anyway
						),
						new UpgradeStepSequence( // 1.0.2.31 - make POLineLabor quantity (and template) required similar to other POLine derived record quantities
							new SqlUpgradeStep(@"
								UPDATE POLineLabor SET Quantity = dbo._INew(0,0,1,0,0) where Quantity is null
								UPDATE POLineLaborTemplate SET Quantity = dbo._INew(0,0,1,0,0) where Quantity is null
							"),
							new SetColumnRequiredUpgradeStep("POLineLabor.Quantity"),
							new SetColumnRequiredUpgradeStep("POLineLaborTemplate.Quantity")
						),
						new UpgradeStepSequence( // 1.0.2.32 - make ReportText in Specification updated via trigger on changes to data and/or report form layout in SpecificationForm
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Specification.ReportText"),
							new AddColumnUpgradeStep("Specification.ReportText"),
							new AddExtensionObjectUpgradeStep("mbtg_SpecificationData_Updates_Specification"),
							new AddExtensionObjectUpgradeStep("mbtg_SpecificationForm_Updates_Specification"),
							new SqlUpgradeStep(@"
								UPDATE Specification SET ReportText = dbo.mbfn_Specification_ReportText(ID)
							")
						),
						new UpgradeStepSequence( // 1.0.2.33 - Change PO Draft mode so POLineItems count as OnOrder immediately
							new SqlUpgradeStep(@"UPDATE PurchaseOrderState SET OrderCountsActive = 1 WHERE Code = 'Draft'")
						),
						new UpgradeStepSequence( // 1.0.2.34 - Remove EntryDate from POLine (not required); make effective date of PurchaseOrderReport the date of the current state history transition
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "POLine.EntryDate"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("ItemOnOrderReport"),
							new AddTableUpgradeStep("ItemReceivingReport")
						),
						new UpgradeStepSequence( // 1.0.2.35 - Remove Cost from POLineTemplate base record (not required)
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateLine"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "POLineTemplate.Cost"),
							new AddTableUpgradeStep("PurchaseOrderTemplateLine")
						),
						new UpgradeStepSequence( // 1.0.2.36 - Fix WorkOrderFormReport to put in InsideLabor/OutsideLabor code even if the associated Employee/Vendor code is null
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.37 Add TotalPurchase, TotalReceive to PurchaseOrder
							new AddColumnUpgradeStep("PurchaseOrder.TotalPurchase"),
							new AddColumnUpgradeStep("PurchaseOrder.TotalReceive"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_TotalPurchase"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_TotalReceive"),
							new AddExtensionObjectUpgradeStep("mbtg_POLine_Updates_PurchaseOrder"),
							new SqlUpgradeStep(@"
								update _DPurchaseOrder SET TotalPurchase = 0
								update _DPurchaseOrder SET TotalReceive= 0
								update PurchaseOrder SET TotalPurchase = dbo.mbfn_PurchaseOrder_TotalPurchase(PurchaseOrder.ID) from PurchaseOrder
								update PurchaseOrder SET TotalReceive = dbo.mbfn_PurchaseOrder_TotalReceive(PurchaseOrder.ID) from PurchaseOrder
							"),
							new SetColumnRequiredUpgradeStep("PurchaseOrder.TotalPurchase"),
							new SetColumnRequiredUpgradeStep("PurchaseOrder.TotalReceive")
						),
						new UpgradeStepSequence( // 1.0.2.38 Add TotalReceive to Receipt
							new AddColumnUpgradeStep("Receipt.TotalReceive"),
							new AddExtensionObjectUpgradeStep("mbfn_Receipt_TotalReceive"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemPO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemPO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualLaborOutsidePO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualLaborOutsidePO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualOtherWorkOutsidePO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualOtherWorkOutsidePO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveMiscellaneousPO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveMiscellaneousPO_Updates_Corrected"),
							new SqlUpgradeStep(@"
								update _DReceipt SET TotalReceive = 0
								update Receipt SET TotalReceive = dbo.mbfn_Receipt_TotalReceive(Receipt.ID)
							"),
							new SetColumnRequiredUpgradeStep("Receipt.TotalReceive")
						),
						new UpgradeStepSequence( // 1.0.2.39 Remove PurchaseLineText from POLineTemplate; fix previous 2.9 upgrade step moving text to underlying OtherWorkOutside PO Text
							new SqlUpgradeStep(@"
									UPDATE OtherWorkOutside SET PurchaseOrderText = POLT.PurchaseOrderText
										from
									POLineOtherWorkTemplate as POW
										join POLineTemplate as POLT on POLT.ID = POW.PoLineTemplateID
										join DemandOtherWorkOutsideTemplate as D on D.ID = POW.DemandOtherWorkOutsideTemplateID
										join OtherWorkOutside as OW on OW.ID = D.OtherWorkOutsideID
									UPDATE Miscellaneous SET PurchaseOrderText = POLT.PurchaseOrderText
										from
									POLineMiscellaneousTemplate as POMT
										join POLineTemplate as POLT on POLT.ID = POMT.PoLineTemplateID
										join Miscellaneous as M on M.ID = POMT.MiscellaneousID
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "POLineTemplate.PurchaseOrderText")
						),
						#endregion
						#region 1.0.2.40 - 1.0.2.49
						new UpgradeStepSequence( // 1.0.2.40 - Add PO Template lines to PurcharOrderTemplateReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateReport"),
							new AddTableUpgradeStep("PurchaseOrderTemplateReport")
						),
						new UpgradeStepSequence( // 1.0.2.41 - Fix triggers maintaining OnOrder amounts in ActualItemLocation
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_PurchaseOrderStateHistory_Updates_PurchaseOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_PurchaseOrderStateHistory_Updates_PurchaseOrderEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_PurchaseOrderState_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_PurchaseOrderState_Updates_ActualItemLocation"),
							new SqlUpgradeStep(@"
								  update ActualItemLocation
									 set OnOrder = dbo.mbfn_ActualItemLocation_OnOrder(ActualItemLocation.ID)
										from ActualItemLocation
										join POLineItem on POLineItem.ItemLocationID = ActualItemLocation.ItemLocationID
							")
						),
						new UpgradeStepSequence( // 1.0.2.42 - Add POFormTitle variable to allow saving of default title for POForm report; Repeat previous upgrade step for any databases created since 1.0.2.25 as the DatabaseCreation Code had not been altered to set the defaults
							new AddVariableUpgradeStep("POFormTitle"),
							new SqlUpgradeStep(@"
							UPDATE __Variables SET VALUE = CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(max),'P u r c h a s e  O r d e r')) where NAME = 'POFormTitle' and VALUE IS NULL ;
							UPDATE __Variables SET VALUE = CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(max),'Authorized by ___________________________________')) where NAME = 'POFormAdditionalInformation' and VALUE IS NULL;
							UPDATE __Variables SET VALUE = CONVERT(VARBINARY(MAX), 6) WHERE NAME = 'POFormAdditionalBlankLines' and VALUE IS NULL;
								")
						),
						new UpgradeStepSequence( // 1.0.2.43 - Add WorkOrderTemplatePurchaseOrderTemplateLinkage views for TreeStructured browsettes
							new AddUniqueConstraintUpgradeStep("WorkOrderTemplatePurchaseOrderTemplate.WorkOrderTemplateID"),
							new AddTableUpgradeStep("WorkOrderTemplatePurchaseOrderTemplateLinkage"),
							new AddTableUpgradeStep("WorkOrderTemplatePurchaseOrderTemplateView"),
							new AddTableUpgradeStep("WorkOrderTemplateLinkedPurchaseOrderTemplatesTreeView"),
							new AddTableUpgradeStep("PurchaseOrderTemplateLinkedWorkOrderTemplatesTreeView")
						),
						new UpgradeStepSequence( // 1.0.2.44 - Add WorkOrderTemplateID to TemplateItemLocation and revise views that need it
						),
						new UpgradeStepSequence( // 1.0.2.45 - Add explicit Action.Administration permissions to all users (granting it)
							new SqlUpgradeStep(@"
								INSERT INTO [Permission] (ID, UserID, PermissionPathPattern, [Grant])
									SELECT newid(), [User].ID, 'Action.Administration', 1
										FROM [User]
										WHERE NOT EXISTS(SELECT * from [Permission] WHERE UserID = [User].ID AND LOWER(PermissionPathPattern) = 'action.administration')
							")
						),
						new UpgradeStepSequence( // 1.0.2.46 - Add duration/work-days to W/O form report.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.47 - Change Session table handling
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnexpiredSession"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveClient"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Session.Expiry"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Session.SessionID"),
							new AddColumnUpgradeStep("Session.Creation"),
							// upgrading doesn't have a session record, and we have the database exclusive so no session records should be active; Delete any stale ones now
							new SqlUpgradeStep(@"
								DROP PROCEDURE mbfn_RemoveExpiredSessions
								DROP PROCEDURE mbfn_RefreshSessionWithInterval
								DELETE Session
							"),
							new SetColumnRequiredUpgradeStep("Session.Creation")
						),
						// 1.0.2.48 - Change "Default Work Order Expense Model" to "Default Expense Model" in the Work Order Expense model table
						new SqlUpgradeStep(@"
							UPDATE WorkOrderExpenseModel SET Code = 'Default Expense Model', [DESC] = 'Default Expense Model' where Code = 'Default Work Order Expense Model'
							UPDATE WorkOrderExpenseCategory SET Code = 'Default Expense Category', [DESC] = 'Default Expense Category' where Code = 'Default Work Order Expense Category'
						"),
						 new UpgradeStepSequence( // 1.0.2.49 - Add MiscellaneousWorkOrderCost support
							new AddTableUpgradeStep("MiscellaneousWorkOrderCost"),
							new AddTableUpgradeStep("DemandMiscellaneousWorkOrderCost"),
							new AddTableUpgradeStep("ActualMiscellaneousWorkOrderCost"),
							new AddTableUpgradeStep("WorkOrderMiscellaneous"),
 							new AddTableUpgradeStep("WorkOrderMiscellaneousTreeview"),
							new AddTableUpgradeStep("DemandMiscellaneousWorkOrderCostTemplate"),
							new AddTableUpgradeStep("WorkOrderTemplateMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderTemplateMiscellaneousTreeview"),
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultMiscellaneousExpenseCategoryID"),
							new SqlUpgradeStep(@"
								declare @x uniqueidentifier
								select @x = ID from WorkOrderExpenseCategory where [CODE] = 'Default Expense Category'
								UPDATE WorkOrderExpenseModel SET DefaultMiscellaneousExpenseCategoryID = @x
								UPDATE _DDemand SET WorkOrderExpenseCategoryID = @x, ActualCost = 0.0 where WorkOrderExpenseCategoryID is null
								UPDATE _DDemandTemplate SET WorkOrderExpenseCategoryID = @x, EstimateCost = 1 where WorkOrderExpenseCategoryID is null
							"),
							new SqlUpgradeStep(@"
								declare @x uniqueidentifier
								select @x = NEWID()
								INSERT INTO CostCenter (ID, Code, [DESC], Hidden) SELECT @x, 'Default Miscellaneous Work Order Costs', 'Default Miscellaneous Work Order Costs', null
								UPDATE _DMiscellaneousWorkOrderCost SET CostCenterID = @x
							"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualMiscellaneousWorkOrderCost_CorrectedCost"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandMiscellaneousWorkOrderCost_ActualCost"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualMiscellaneousWorkOrderCost_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualMiscellaneousWorkOrderCost_Updates_DemandMiscellaneousWorkOrderCostEtAl")
						 ),
						#endregion
						#region 1.0.2.50 - 1.0.2.59
						new UpgradeStepSequence( // 1.0.2.50 Extend OtherWorkInside to include the Employee and/or Trade
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "OtherWorkInside.Code"),
							new AddColumnUpgradeStep("OtherWorkInside.EmployeeID"),
							new AddColumnUpgradeStep("OtherWorkInside.TradeID"),
							new AddUniqueConstraintUpgradeStep("OtherWorkInside.Code"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInsideTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside"),
							new AddTableUpgradeStep("WorkOrderInside"),
							new AddTableUpgradeStep("WorkOrderInsideTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateInsideTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateInside"),
							new AddTableUpgradeStep("WorkOrderTemplateInside"),
							new AddTableUpgradeStep("WorkOrderTemplateInsideTreeview")
						),
						new UpgradeStepSequence( // 1.0.2.51 - Add Miscellaneous Cost to W/O form report.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.52 - Add W/O Resource lines and P/O Template lines to W/O template report.
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateReport"),
							new AddTableUpgradeStep("WorkOrderTemplateReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("WorkOrderTemplateDetailedReport")
						),
						new UpgradeStepSequence( // 1.0.2.53 - Add report for MiscellaneousWorkOrderCost
							new AddTableUpgradeStep("MiscellaneousWorkOrderCostReport")
						),
						// 1.0.2.54 - Add ItemPricing view for providing enhanced price quoting
						new AddTableUpgradeStep("ItemPricing"),
						new UpgradeStepSequence( // 1.0.2.55 - Reconstruct the TemplateTemporaryStorage table removed at 1.0.0.344
							// When this step was added 1.0.2.44 was removed; users with multiple tasks using the same item+location could not get past
							// it, and users who did pass that step merely have an additional field in SQL not in the xaf schema.
							new AddTableUpgradeStep("TemplateTemporaryStorage"),
							// Steps have been removed; They were adding/deleting dependencies and the upgrader no longer handles dependencies.
							new AddExtensionObjectUpgradeStep("mbtg_SetNewTemplateTemporaryStorageContainment"),
							new AddExtensionObjectUpgradeStep("mbtg_SetUpdatedTemplateTemporaryStorageContainment"),
							new AddExtensionObjectUpgradeStep("mbtg_ClearDeletedTemplateTemporaryStorageContainment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateLinkedPurchaseOrderTemplatesTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateLinkedWorkOrderTemplatesTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplatePurchaseOrderTemplateView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplatePurchaseOrderTemplateLinkage"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndScheduledWorkOrderAndLocation"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateItemsTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateItems"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItemsTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationContainment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationDerivationsAndItemLocations"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationDerivations"),
							new AddTableUpgradeStep("LocationDerivations"),
							new AddTableUpgradeStep("LocationAndContainers"),
							new AddTableUpgradeStep("LocationDerivationsAndItemLocations"),
							new AddTableUpgradeStep("ItemLocationContainment"),
							new AddTableUpgradeStep("ItemLocationAndContainers"),
							new AddTableUpgradeStep("WorkOrderItems"),
							new AddTableUpgradeStep("WorkOrderItemsTreeview"),
							new AddTableUpgradeStep("WorkOrderTemplateItems"),
							new AddTableUpgradeStep("WorkOrderTemplateItemsTreeview"),
							new AddTableUpgradeStep("PMGenerationDetailAndScheduledWorkOrderAndLocation"),
							new AddTableUpgradeStep("PMGenerationDetailAndContainers"),
							new AddTableUpgradeStep("WorkOrderTemplatePurchaseOrderTemplateLinkage"),
							new AddTableUpgradeStep("WorkOrderTemplatePurchaseOrderTemplateView"),
							new AddTableUpgradeStep("WorkOrderTemplateLinkedPurchaseOrderTemplatesTreeview"),
							new AddTableUpgradeStep("PurchaseOrderTemplateLinkedWorkOrderTemplatesTreeview"),
							new SqlUpgradeStep(@"
								if COLUMNPROPERTY(OBJECT_ID('dbo.TemplateItemLocation'), 'WorkOrderTemplateId', 'ColumnId') IS NULL BEGIN
									-- This database did not go through 1.0.2.44; add a nullable WorkOrderTemplateId column so the remainder of this upgrade is
									-- semantically valid.
									-- later tests for nullability of this column will distinguish the 2 classes of database
									ALTER TABLE TemplateItemLocation ADD WorkOrderTemplateId UNIQUEIDENTIFIER
								END
							"),
							// The existing TemplateItemLocation records can have several forms at this point:
							// For 3.0 customers (and anyone else who has not done old 1.0.2.44), they do not contain a WorkOrderTemplateId,
							// and there may be some that are not referenced by any DemandItemTemplate. For 3.0 customers they will also never
							// be referenced by POLineItemTemplate records (2.9 convert did not create them, and no Purchasing license keys were given out),
							// but in-hosue data might.
							// For in-house databases that went through the old 1.0.2.44 step, the table will now contain a WorkOrderTemplateId field,
							// and each TemplateItemLocation will only be referenced by a single Task's DemandItemTemplate records (or it may be unreferenced)
							// There could now be TemplateItemLocation records referenced by POLineTemplate
							new SqlUpgradeStep(@"
								-- This query produces all the distinct Location/WOTemplate combinations
								-- It is used to populate the TemplateTemporaryStorage table and base records.
								if COLUMNPROPERTY(OBJECT_ID('dbo.TemplateItemLocation'), 'WorkOrderTemplateId', 'AllowsNull') = 0 BEGIN
									-- The DB upgraded through original version 1.0.2.44 step.
									-- Use the TemplateItemLocation.WorkOrderTemplateId field to synthesize the new information.
									SELECT DISTINCT CAST(null AS UNIQUEIDENTIFIER) AS BaseId, IL.LocationID as ContainingLocationId, TIL.WorkOrderTemplateID
										INTO NewLocationData
										FROM
											ItemLocation AS IL
										JOIN
											TemplateItemLocation AS TIL on TIL.ItemLocationID = IL.ID
								END
								ELSE BEGIN
									-- This DB did not take the old 1.0.2.44 step. Synthesize the information from the DemandItemTemplates. Unreferenced
									-- TemplateItemLocation records will go away (but old 1.0.2.44 did this anyway) but they contain no information of value.
									-- Several DemandItemTemplate records on the same WorkOrderTemplate may refer to the same TemplateItemLocation so we
									-- use SELECT DISTINCT to only get one record for each (ContainingLocationId, WorkOrderTemplateId) combination.
									-- We ignore any TemplateItemLocation that contains a WorkOrderTemplateId but no DemandItemTemplate referring to it.
									-- These can only exist in-house for a DB upgraded with the old 1.0.2.44 step followed by the user creating but not using
									-- a TemplateItemLocation. TODO: Fix this.
									SELECT DISTINCT CAST(null AS UNIQUEIDENTIFIER) AS BaseId, IL.LocationID as ContainingLocationId, DT.WorkOrderTemplateID
										INTO NewLocationData
										FROM
											DemandItemTemplate AS DIT
										JOIN
											DemandTemplate AS DT ON DT.ID = DIT.DemandTemplateID
										JOIN
											ItemLocation AS IL ON IL.ID = DIT.ItemLocationID
										JOIN
											TemplateItemLocation AS TIL on TIL.ItemLocationID = IL.ID
								END
							"),
							new SqlUpgradeStep(@"
								-- We now assign id's to use for the base Location record. We couldn't do it when creating the table because of the DISTINCT.
								UPDATE NewLocationData SET BaseId = newid()
								-- Create the base Location records
								INSERT INTO Location (Id)
									SELECT BaseId
										FROM NewLocationData
								-- Create the derived TemplateTemporaryStorage records
								INSERT INTO TemplateTemporaryStorage (Id, LocationId, ContainingLocationId, WorkOrderTemplateId)
									SELECT newid(), BaseId, ContainingLocationId, WorkOrderTemplateID
										FROM NewLocationData
								-- This query produces all the distinct Item/Temp location combinations.
								-- It is use to make fresh TemplateItemLocation records and base records.
								-- The delete method has always been delete so no Hidden records should exist for this derived type.
								if COLUMNPROPERTY(OBJECT_ID('dbo.TemplateItemLocation'), 'WorkOrderTemplateId', 'AllowsNull') = 0 BEGIN
									-- The DB upgraded through original version 1.0.2.44 step.
									-- Use the TemplateItemLocation.WorkOrderTemplateId field to synthesize the new information.
									SELECT newid() AS baseId, IL.Id as originId, TTS.WorkOrderTemplateID, IL.ItemID, TTS.LocationID, IL.ItemPriceId
										INTO NewItemLocationData
										FROM
											ItemLocation AS IL
										JOIN
											TemplateItemLocation AS TIL ON TIL.ItemLocationID = IL.ID
										JOIN
											TemplateTemporaryStorage AS TTS ON TTS.ContainingLocationID = IL.LocationID AND TTS.WorkOrderTemplateID = TIL.WorkOrderTemplateID
								END
								ELSE BEGIN
									-- This DB did not take the old 1.0.2.44 step. Synthesize the information from the DemandItemTemplates.
									-- Several DemandItemTemplate records on the same WorkOrderTemplate may refer to the same TemplateItemLocation so we
									-- In the underlying table ItemID and LocationID form a unique key, and multiple LocationId's will never join to a single TTS record
									-- so (IL.ItemID, TTS.LocationID) form a unique set and SELECT DISTINCT is not needed.
									-- TODO: TemplateItemLocations with no Demand but with the 1.0.2.44 WorkOrderTemplateId linkage
									SELECT newid() AS baseId, IL.Id as originId, TTS.WorkOrderTemplateID, IL.ItemID, TTS.LocationID, IL.ItemPriceId
										INTO NewItemLocationData
										FROM
											DemandItemTemplate AS DIT
										JOIN
											DemandTemplate AS DT ON DT.ID = DIT.DemandTemplateID
										JOIN
											ItemLocation AS IL ON IL.ID = DIT.ItemLocationID
										JOIN
											TemplateItemLocation AS TIL ON TIL.ItemLocationID = IL.ID
										JOIN
											TemplateTemporaryStorage AS TTS ON TTS.ContainingLocationID = IL.LocationID AND TTS.WorkOrderTemplateID = DT.WorkOrderTemplateID
								END
							"),
							new SqlUpgradeStep(@"
								-- Hide all the existing TemplateItemLocations
								UPDATE ItemLocation
									SET Hidden = dbo._DClosestValue(getdate(),2,100)
									FROM ItemLocation
									JOIN TemplateItemLocation ON ItemLocation.Id = TemplateItemLocation.ItemLocationId
								-- Create the base ItemLocation records
								INSERT INTO ItemLocation (Id, ItemId, LocationId, ItemPriceId)
									SELECT BaseId, ItemId, LocationId, ItemPriceId
										FROM NewItemLocationData
								-- Create the derived TemplateItemLocation records; we need to keep the WorkOrderTemplateId only to satisfy the nonnull constraint that still exists
								INSERT INTO TemplateItemLocation (Id, ItemLocationId, WorkOrderTemplateId)
										SELECT newid(), BaseId, WorkOrderTemplateId
										FROM NewItemLocationData
								if COLUMNPROPERTY(OBJECT_ID('dbo.TemplateItemLocation'), 'WorkOrderTemplateId', 'AllowsNull') = 0 BEGIN
									-- The DB upgraded through original version 1.0.2.44 step. The TemplateItemLocation records were already task-specific so there
									-- is one new TemplateItemLocation for each old one and a simple join to the new-data table will suffice.
									-- Now the DemandItemTemplate records must be redirected to their new targets.
									UPDATE DemandItemTemplate
										SET ItemLocationID = NILD.baseid
										FROM DemandItemTemplate
										JOIN NewItemLocationData as NILD ON NILD.Originid = DemandItemTemplate.ItemLocationId
									-- Use the TemplateItemLocation.WorkOrderTemplateId field to relink the POLineItemTemplate records
									UPDATE POLineItemTemplate
										SET ItemLocationID = NILD.baseid
										FROM POLineItemTemplate
										JOIN NewItemLocationData as NILD ON NILD.Originid = POLineItemTemplate.ItemLocationId
								END
								ELSE BEGIN
									-- The DB was not upgraded through original version 1.0.2.44 step. The TemplateItemLocation records are shared across tasks.
									-- As a result any POLineItemTemplate references cannot be redirected (and will generate referential errors when we try deleting
									-- the old TemplateItemLocations), and DemandItemTemplate records must be associated using their WorkOrderTemplateID
									-- Now the DemandItemTemplate records must be redirected to their new targets.
									UPDATE DemandItemTemplate
										SET ItemLocationID = NILD.baseid
										FROM DemandItemTemplate
										JOIN DemandTemplate AS DT ON DT.Id = DemandItemTemplate.DemandTemplateID
										JOIN NewItemLocationData as NILD ON NILD.Originid = DemandItemTemplate.ItemLocationId AND NILD.WorkOrderTemplateID = DT.WorkOrderTemplateID
								END
								-- Drop the TemplateItemLocation.WorkOrderTemplateId field
								DECLARE @cmd NVARCHAR(MAX)
								SELECT @cmd = 'ALTER TABLE TemplateItemLocation DROP CONSTRAINT ' + CONSTRAINT_NAME FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE
									WHERE TABLE_SCHEMA = 'dbo'
										AND TABLE_NAME = 'TemplateItemLocation'
										AND COLUMN_NAME = 'WorkOrderTemplateId'
								EXEC sp_executesql @cmd
								ALTER TABLE TemplateItemLocation DROP COLUMN WorkOrderTemplateId

								-- Delete all the hidden TemplateItemLocations (delete the base record, the derived one cascade-deletes)
								DELETE FROM ItemLocation
									FROM ItemLocation
									JOIN TemplateItemLocation AS TIL ON TIL.ItemLocationID = ItemLocation.ID
									WHERE ItemLocation.Hidden IS NOT NULL
								-- Drop the work tables
								DROP TABLE NewLocationData
								DROP TABLE NewItemLocationData
							")
						),
						// 1.0.2.56 - Remove WorkOrderTemplateID field left in previous upgrade step from Default table; clear any Defaults for TemplateItemLocation on removal of top level browser
						new SqlUpgradeStep(@"
							UPDATE _DItemLocation
								SET ItemPriceID = null, LocationID = null, ItemId = null
									from _DTemplateItemLocation as DTIL join _DItemLocation on _DItemLocation.ID = DTIL.ItemLocationID
							if COLUMNPROPERTY(OBJECT_ID('dbo._DTemplateItemLocation'), 'WorkOrderTemplateId', 'ColumnID') IS NOT NULL BEGIN
								-- Drop the TemplateItemLocation.WorkOrderTemplateId field
								DECLARE @cmd NVARCHAR(MAX)
								SELECT @cmd = 'ALTER TABLE _DTemplateItemLocation DROP CONSTRAINT ' + CONSTRAINT_NAME FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE
									WHERE TABLE_SCHEMA = 'dbo'
										AND TABLE_NAME = '_DTemplateItemLocation'
										AND COLUMN_NAME = 'WorkOrderTemplateId'
								EXEC sp_executesql @cmd
								ALTER TABLE _DTemplateItemLocation DROP COLUMN WorkOrderTemplateId
							END
						"),
						// 1.0.2.57 - Add specification form fields to the specification form listing report
						new AddTableUpgradeStep("SpecificationFormReport"),
						new UpgradeStepSequence( // 1.0.2.58 - Fix functions calculating OrderQuantity in Demand records to handle null (deletion of purchase records)
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandOtherWorkOutside_OrderQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandOtherWorkOutside_OrderQuantity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandOtherWorkOutsideTemplate_OrderQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandOtherWorkOutsideTemplate_OrderQuantity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandLaborOutside_OrderQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandLaborOutside_OrderQuantity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandLaborOutsideTemplate_OrderQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandLaborOutsideTemplate_OrderQuantity")
						),
						new UpgradeStepSequence( // 1.0.2.59 - Added more detail to request state history report
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport")
						),
						#endregion
						#region 1.0.2.60 - 1.0.2.66
						new UpgradeStepSequence( // 1.0.2.60 - Add cascade delete to LocationContainment 'ContainedLocationID' so deletion of TemporaryStorage etc. work properly.
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ClearDeletedTemplateTemporaryStorageContainment"),
							new RemoveForeignConstraintUpgradeStep(GetOriginalSchema, "LocationContainment.ContainedLocationID"),
							new AddForeignConstraintUpgradeStep( "LocationContainment.ContainedLocationID")
						),
						new UpgradeStepSequence( // 1.0.2.61 - correct PMGenerationDetailTreeView after incorrect reintroduction of TemplateTemporaryStorage
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndScheduledWorkOrderAndLocation"),
							new AddTableUpgradeStep("PMGenerationDetailAndScheduledWorkOrderAndLocation"),
							new AddTableUpgradeStep("PMGenerationDetailAndContainers")
						),
						new UpgradeStepSequence( // 1.0.2.62 - Meter reading EffectiveReading now calculated with trigger at insert time of reading
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_SetCurrentMeterReading"),
							new AddExtensionObjectUpgradeStep("mbtg_SetCurrentMeterReading"),
							new SqlUpgradeStep(@"
								UPDATE _DMeterReading SET EffectiveReading = 0
							")
						),
						new UpgradeStepSequence( // 1.0.2.63 - Remove P/O lines from resources section in WorkOrderFormReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.2.64 - Change Meter reading EffectiveReading trigger calculation to only apply to NEW records.
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_SetCurrentMeterReading"),
							new AddExtensionObjectUpgradeStep("mbtg_SetCurrentMeterReading")
						),
						new UpgradeStepSequence( // 1.0.2.65 - Change URL's to max length rather than 256
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							// We do not record any schema change here. As a result any previous upgrade step on Attachment.Path or Contact.WebURL
							// may run with a SQL size of 256 but a XAF schema size of MAX.
							new SqlUpgradeStep(@"
								ALTER TABLE Attachment ALTER COLUMN Path NVARCHAR(MAX) NOT NULL
								ALTER TABLE _DAttachment ALTER COLUMN Path NVARCHAR(MAX) NULL
								ALTER TABLE Contact ALTER COLUMN WebURL NVARCHAR(MAX) NULL
								ALTER TABLE _DContact ALTER COLUMN WebURL NVARCHAR(MAX) NULL
							"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("EmployeeReport"),
							new AddTableUpgradeStep("BillableRequestorReport"),
							new AddTableUpgradeStep("RequestorReport")
						),
						// MB 3.1.0 uses db version 1.0.2.66
						new UpgradeStepSequence( //1.0.2.66
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.9'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.9'")
						),
						// If a check-in includes a DB upgrade it should mention the new DB version in the checkin comment
						// and if the steps force a new minimum app version this should also appear in the checkin comment.
						#endregion
					},
					new UpgradeStep[] { // 1.0.3.x Reserved for steps included in released versions of MB3.1
						#region 1.0.3.0 - 1.0.3.18
						// MB 3.1.0 uses db version 1.0.3.0
						new UpgradeStepSequence( //1.0.3.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.9'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.9'")
						),
						new UpgradeStepSequence( //1.0.3.1
							new AddUniqueConstraintUpgradeStep("License.License")
						),
						new UpgradeStepSequence( //1.0.3.2
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.14'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.14'")
						),
						new UpgradeStepSequence( //1.0.3.3 Add Hidden to the unique constraint on a meter reading so you can delete and re add meterreadings at the same time stamp
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "MeterReading.MeterID"),
							new AddUniqueConstraintUpgradeStep("MeterReading.MeterID")
						),
						new UpgradeStepSequence( //1.0.3.4
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.15'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.15'")
						),
						new UpgradeStepSequence( //1.0.3.5 - Exclude receiving and item transfers from workorder form report.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.16'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.16'")
						),
						new UpgradeStepSequence( //1.0.3.6 -- Fix totalling functions related to nonPO/PO and missing 'union all' constructs
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Receipt_TotalReceive"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandOtherWorkOutside_ActualCost"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandOtherWorkOutside_OrderQuantity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandOtherWorkOutside_ActualQuantity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandLaborOutside_ActualCost"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandLaborOutside_OrderQuantity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DemandLaborOutside_ActualQuantity"),

							new AddExtensionObjectUpgradeStep("mbfn_Receipt_TotalReceive"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandOtherWorkOutside_ActualCost"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandOtherWorkOutside_OrderQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandOtherWorkOutside_ActualQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandLaborOutside_ActualCost"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandLaborOutside_OrderQuantity"),
							new AddExtensionObjectUpgradeStep("mbfn_DemandLaborOutside_ActualQuantity"),
							new SqlUpgradeStep(@"
								UPDATE Receipt SET TotalReceive = coalesce(dbo.mbfn_Receipt_TotalReceive(Receipt.ID),0)
								update DemandOtherWorkOutside
									  set ActualQuantity = coalesce(dbo.mbfn_DemandOtherWorkOutside_ActualQuantity(DemandOtherWorkOutside.ID),0),
										  OrderQuantity = coalesce(dbo.mbfn_DemandOtherWorkOutside_OrderQuantity(DemandOtherWorkOutside.ID),0)
								update Demand
									  set ActualCost = coalesce(dbo.mbfn_DemandOtherWorkOutside_ActualCost(DemandOtherWorkOutside.ID),0)
									  from Demand join DemandOtherWorkOutside on Demand.ID = DemandOtherWorkOutside.DemandID
								update DemandLaborOutside
									  set ActualQuantity = coalesce(dbo.mbfn_DemandLaborOutside_ActualQuantity(DemandLaborOutside.ID),0),
										  OrderQuantity = coalesce(dbo.mbfn_DemandLaborOutside_OrderQuantity(DemandLaborOutside.ID),0)
								update Demand
									  set ActualCost = coalesce(dbo.mbfn_DemandLaborOutside_ActualCost(DemandLaborOutside.ID),0)
									  from Demand join DemandLaborOutside on Demand.ID = DemandLaborOutside.DemandID
							"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.17'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.17'")
						),
						new UpgradeStepSequence( //1.0.3.7 -- Add @Requests service version; set a 'fake' version as if it had been originally present since 3.0
							new AddVariableUpgradeStep("ATRServiceVersion"),
							new SqlUpgradeStep(@"
								DECLARE @RequestsServicePresent NVARCHAR(max)
								SELECT @RequestsServicePresent = dbo._vgetATRServiceName()
								IF @RequestsServicePresent IS NOT NULL
								BEGIN
									exec dbo._vsetATRServiceVersion '3.0.0.0'
								END
							"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.1.0.18'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.1.0.18'")
						),
						// MB3.1, Update 1 uses 1.0.3.8
						new UpgradeStepSequence( // 1.0.3.8 -- Add manual index to PMGenerationDetail to speed up workorder generation
							// We must first remove any possible existing index that a user may have placed on the Table following following our FAQ instructions.
							// We cannot rely on the name of the index either; the code below is from the 3.2 version of BuildSqlForColumnIndexDelete
							new SqlUpgradeStep(@"declare @cmd nvarchar(max);
									-- drop non primary indexes on PMGenerationDetail.ScheduledWorkOrderID
									  set @cmd = 
											 ( select distinct 'DROP INDEX ['+ ndxs.name + '] ON dbo.[' + 'PMGenerationDetail' + ']'
												from sys.indexes as ndxs
												join sys.index_columns as icols on icols.object_id = ndxs.object_id
												join sys.tables as tables on tables.object_id = ndxs.object_id
												join sys.columns as tcols on tcols.object_id = ndxs.object_id
												join sys.schemas as schemas on schemas.schema_id = tables.schema_id
												where tables.name = 'PMGenerationDetail'
														and tcols.name = 'ScheduledWorkOrderId'
														and tcols.column_id = icols.column_id
														and icols.index_id = ndxs.index_id
														and ndxs.is_primary_key = 0
														and ndxs.is_unique_constraint = 0
														and schemas.name = 'dbo'
											   for xml path(''), TYPE).value('.','varchar(max)')
									if @cmd is not null exec sp_executesql @cmd;
							"),
							new SqlUpgradeStep(@"
								CREATE NONCLUSTERED INDEX [ScheduledWorkOrderID] ON [dbo].[PMGenerationDetail] (
									[ScheduledWorkOrderID] ASC
								)WITH (STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF,
								 DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
								 ON [PRIMARY]
						")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.0.4.x Versions of MB3.2
						#region 1.0.4.0 - 1.0.4.9
						// MB 3.2.0 uses db version 1.0.4.0
						new UpgradeStepSequence( //1.0.4.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.0.0'"),
							new AddColumnUpgradeStep("Contact.UserID"),
							new AddExtensionObjectUpgradeStep("mbtg_CheckUniqueUser"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new AddTableUpgradeStep("ContactReport")
						),
						new UpgradeStepSequence( // 1.0.4.1 - Update report views that use the updated ContactReport views.
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new AddTableUpgradeStep("EmployeeReport"),
							new AddTableUpgradeStep("BillableRequestorReport"),
							new AddTableUpgradeStep("RequestorReport")
						),
						new UpgradeStepSequence( // 1.0.4.2 -- Add RequestAssignee infractructure
							new AddTableUpgradeStep("RequestAssignee"),
							new AddTableUpgradeStep("RequestAssignment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRoles"),
							new AddTableUpgradeStep("ContactRoles")
						),
						new UpgradeStepSequence( // 1.0.4.3 -- Add RequestAssigneeStatistics view and related linkage in RequestAssignee
							new AddTableUpgradeStep("RequestAssigneeStatistics"),
							new AddColumnUpgradeStep("RequestAssignee.RequestAssigneeStatisticsID"),
							new ChangeToComputedColumnUpgradeStep("RequestAssignee.RequestAssigneeStatisticsID", "[ID]")
						),
						new UpgradeStepSequence( // 1.0.4.4 -- Add AttentionStatus view
							new AddTableUpgradeStep("AttentionStatus")
						),
						new UpgradeStepSequence( // 1.0.4.5 -- Add changes for Draft WorkOrder state
							new SqlUpgradeStep(@"
								UPDATE PurchaseOrderState SET [DESC] = 'Purchase Order completed' where [DESC] = 'Purchase Order complete'
								UPDATE WorkOrderStateTransition SET OperationHint = 'Change a Closed Work Order to Open' where OperationHint = 'Change a Closed Work Order to Open.'
							"),
							new AddColumnUpgradeStep("WorkOrderState.FilterAsDraft"),
							new SqlUpgradeStep(@"
								UPDATE WorkOrderState SET FilterAsDraft = 0
								DECLARE @WOOpenState uniqueidentifier
								DECLARE @WODraftState uniqueidentifier
								DECLARE @WOVoidState uniqueidentifier
								DECLARE @WOClosedState uniqueidentifier

								SELECT @WOOpenState = ID from WorkOrderState where Code = 'Open'
								SELECT @WOVoidState = ID from WorkOrderState where Code = 'Voided'
								SELECT @WOClosedState = ID from WorkOrderState where Code = 'Closed'
								Set @WODraftState = NEWID()

								INSERT into WorkOrderState (ID, Code, [Desc], Comment, DemandCountsActive, TemporaryStorageActive, CanModifyDemands, CanModifyNonDemands,
												CanModifyChargebacks, CanModifyMainFields, CanModifyWorkInterval, AffectsFuturePMGeneration, FilterAsOpen, FilterAsClosed, FilterAsVoid,
												FilterAsDraft)
								values
									(@WODraftState, 'Draft', 'Work Order being drafted', null, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1);

								UPDATE WorkOrderStateTransition SET Rank = 5, FromState = @WODraftState where Rank = 3
								UPDATE WorkOrderStateTransition SET Rank = 4 where Rank = 2
								UPDATE WorkOrderStateTransition SET Rank = 2 where Rank = 1
								INSERT INTO WorkOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, Rank) values
									(NEWID(), 'Draft WO', 'Change a Voided Work Order back to Draft status', @WOVoidState, @WODraftState, 6);
								INSERT INTO WorkOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, Rank) values
									(NEWID(), 'Open WO', 'Change a Draft Work Order to Open', @WODraftState, @WOOpenState, 1);
								INSERT INTO WorkOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, Rank) values
									(NEWID(), 'Suspend WO', 'Change an Open Work Order back to Draft status', @WOOpenState, @WODraftState, 3);


								UPDATE _DWorkOrderStateHistory SET WorkOrderStateID = @WODraftState where WorkOrderStateID = @WOOpenState
							"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),

							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("ChargebackFormReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport")
						),
						new UpgradeStepSequence( // 1.0.4.6 - Change the SpecificationForm table so WriteDependencies can limit field editing rather than hot code
							new RemoveColumnUpgradeStep(GetOriginalSchema, "SpecificationForm.SpecificationCount"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_SpecificationForm_SpecificationCount"),
							new AddExtensionObjectUpgradeStep("mbfn_SpecificationForm_EditAllowed"),
							new AddColumnUpgradeStep("SpecificationForm.EditAllowed"),
							new ChangeToComputedColumnUpgradeStep("SpecificationForm.EditAllowed","dbo.mbfn_SpecificationForm_EditAllowed(ID)")
						),
						new UpgradeStepSequence( // 1.0.4.7 - Add ContactID to User table and related triggers
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_CheckUniqueUser"),
							new AddColumnUpgradeStep("User.ContactID"),
							new AddExtensionObjectUpgradeStep("mbfn_Contact_UserID"),
							new AddExtensionObjectUpgradeStep("mbtg_User_Updates_Contact"),
							new SqlUpgradeStep(@"
								UPDATE [User] set ContactID = Contact.ID from Contact where Contact.UserID = [User].ID
								-- Establish linkages to existing Contacts who match our UserName
								UPDATE [User] set ContactID = Contact.ID from Contact where Contact.Code = [User].UserName and (Contact.Hidden = [User].Hidden or (Contact.Hidden IS NULL and [User].Hidden IS NULL))
								INSERT INTO Contact (ID, Code, Hidden, Comment)
										select NEWID(), [User].UserName, [User].Hidden, 'This contact was created in upgrade step 1.0.4.7 for linking to Users of this organization'
										from [User]
										WHERE [User].ContactID IS NULL and [User].ScopeName IS NULL
								UPDATE [User] set ContactID = Contact.ID from Contact where Contact.Code = [User].UserName and (Contact.Hidden = [User].Hidden or (Contact.Hidden IS NULL and [User].Hidden IS NULL)) and [User].ContactID IS NULL and [User].ScopeName is NULL
								INSERT INTO Contact (ID, Code, Hidden, Comment)
										select NEWID(), [User].ScopeName + '\' + [User].UserName, [User].Hidden, 'This contact was created in upgrade step 1.0.4.7 for linking to Users of this organization'
										from [User]
										WHERE [User].ContactID IS NULL and [User].ScopeName IS NOT NULL
								UPDATE [User] set ContactID = Contact.ID from Contact where Contact.Code = [User].ScopeName + '\' + [User].UserName and  (Contact.Hidden = [User].Hidden or (Contact.Hidden IS NULL and [User].Hidden IS NULL)) and [User].ContactID IS NULL
							"),
							new SetColumnRequiredUpgradeStep("User.ContactID"),
							new AddUniqueConstraintUpgradeStep("User.ContactID")
						),
						new UpgradeStepSequence( // 1.0.4.8 - Add Material History Report, set Correction flags in the other resourse history reports
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport")
						),
						new UpgradeStepSequence( // 1.0.4.9 - Add UserID field to AccountingTransaction
							new AddColumnUpgradeStep("AccountingTransaction.UserID"),
							new SqlUpgradeStep(@"
								UPDATE AccountingTransaction SET UserID = @UserID
							"),
							new SetColumnRequiredUpgradeStep("AccountingTransaction.UserID")
						),
						#endregion
						#region 1.0.4.10 - 1.0.4.19
						new UpgradeStepSequence( // 1.0.4.10 - Add the Role, UserPermission, UserRole and Principal Base tables in preparation to moving to Role based security.
							new AddTableUpgradeStep("Principal"),
							new AddTableUpgradeStep("Role"),
							new AddTableUpgradeStep("UserRole"),
							new AddTableUpgradeStep("UserPermission")
						),
						new UpgradeStepSequence( // 1.0.4.11 - Change the UserPermission to include a link to Permission, and exclude Hidden User/Role related permission records
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserPermission"),
							new AddTableUpgradeStep("UserPermission")
						),
						new UpgradeStepSequence( // 1.0.4.12 - Add the two roles we will need to provide basic access to the database for the administrator and non administrator
							new SqlUpgradeStep(@"
								DECLARE @Principal_AdministratorId uniqueidentifier
								DECLARE @Principal_AllBasicRightsId uniqueidentifier
								DECLARE @Role_AdministratorId uniqueidentifier
								DECLARE @Role_AllBasicRightsId uniqueidentifier
								SET @Principal_AdministratorId = '4D61696E-426F-7373-0000-000000000000'
								SET @Principal_AllBasicRightsId = '4D61696E-426F-7373-0000-000000000001'
								SET @Role_AdministratorId = '4D61696E-426F-7373-0001-000000000000'
								SET @Role_AllBasicRightsId = '4D61696E-426F-7373-0001-000000000001'

								INSERT INTO Principal (ID, [Desc]) VALUES (@Principal_AdministratorId, 'This role provides administrator rights')
								INSERT INTO Principal (ID, [Desc]) VALUES (@Principal_AllBasicRightsId, 'This role provides all basic rights')
								INSERT INTO Role (ID, PrincipalID, Code) VALUES (@Role_AdministratorId, @Principal_AdministratorId, 'Administrator')
								INSERT INTO Role (ID, PrincipalID, Code) VALUES (@Role_AllBasicRightsId, @Principal_AllBasicRightsId, 'All Basic Rights')

								INSERT INTO UserRole (ID, RoleID, UserId)
									SELECT NEWID(), @Role_AdministratorId, UserId from Permission where PermissionPathPattern = 'Action.Administration' and [Grant] = 1
								INSERT INTO UserRole (ID, RoleID, UserID)
									SELECT NEWID(), @Role_AllBasicRightsId, ID from [User]
							")
						),
						new UpgradeStepSequence( // 1.0.4.13 - Make the User table into a derived table from Principal
							new AddColumnUpgradeStep("User.PrincipalID"), // This is a BASE linkage; SetColumnRequiredUpgradeStep and constraints and linkages will be set by AddColumnUpgradeStep
							new RemoveColumnUpgradeStep(GetOriginalSchema, "User.Desc"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "User.Comment")
						),
						new UpgradeStepSequence( // 1.0.4.14 - Transition to UserPermissions NOW
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserPermission"),
							new AddColumnUpgradeStep("Permission.PrincipalID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Permission.Grant"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Permission.UserID"),
							new SqlUpgradeStep(@"
								DECLARE @Principal_AdministratorId uniqueidentifier
								DECLARE @Principal_AllBasicRightsId uniqueidentifier
								DECLARE @Role_AdministratorId uniqueidentifier
								DECLARE @Role_AllBasicRightsId uniqueidentifier
								SET @Principal_AdministratorId = '4D61696E-426F-7373-0000-000000000000'
								SET @Principal_AllBasicRightsId = '4D61696E-426F-7373-0000-000000000001'
								SET @Role_AdministratorId = '4D61696E-426F-7373-0001-000000000000'
								SET @Role_AllBasicRightsId = '4D61696E-426F-7373-0001-000000000001'
								DELETE Permission 
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AdministratorId, 'Action.Administration')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Table.*.*')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.Administration')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.GenerateWorkOrders')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ViewWorkOrderMiscellaneousCost')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ViewUnitValueCost')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ViewAccounting')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.EditAccounting')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.CloseWorkRequest')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ReopenWorkRequest')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.CreateWorkOrderFromWorkRequest')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.CloseWorkOrder')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ReopenWorkOrder')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.IssuePurchaseOrder')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.WithdrawPurchaseOrder')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ClosePurchaseOrder')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.ReopenPurchaseOrder')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.UpgradeDatabase')
							"),
							new SetColumnRequiredUpgradeStep("Permission.PrincipalID"),
							new AddTableUpgradeStep("UserPermission")
						),
						new UpgradeStepSequence( // 1.0.4.15  Remove the unused IsGroup column from the database
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_MaintainSecurity"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserOrGroup"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "User.IsGroup"),
							new AddExtensionObjectUpgradeStep("mbtg_MaintainSecurity")
						),
						new UpgradeStepSequence( // 1.0.4.16 Transition to ViewCost.* for all basic rights; remove old Action.ViewUnitValueCost, etc.
							new SqlUpgradeStep(@"
								DECLARE @Principal_AdministratorId uniqueidentifier
								DECLARE @Principal_AllBasicRightsId uniqueidentifier
								DECLARE @Role_AdministratorId uniqueidentifier
								DECLARE @Role_AllBasicRightsId uniqueidentifier
								SET @Principal_AdministratorId = '4D61696E-426F-7373-0000-000000000000'
								SET @Principal_AllBasicRightsId = '4D61696E-426F-7373-0000-000000000001'
								SET @Role_AdministratorId = '4D61696E-426F-7373-0001-000000000000'
								SET @Role_AllBasicRightsId = '4D61696E-426F-7373-0001-000000000001'
								DELETE FROM PERMISSION where PermissionPathPattern = 'Action.ViewUnitValueCost' or PermissionPathPattern = 'Action.ViewWorkOrderMiscellaneousCost'
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'ViewCost.*')
							")
						 ),
						 new UpgradeStepSequence( // 1.0.4.17 remove erroneous Action.Administration from All rights
							 new SqlUpgradeStep(@"
								DECLARE @Principal_AllBasicRightsId uniqueidentifier
								SET @Principal_AllBasicRightsId = '4D61696E-426F-7373-0000-000000000001'

								DELETE FROM Permission where PrincipalID = @Principal_AllBasicRightsId and PermissionPathPattern ='Action.Administration'
							")
						 ),
						 new UpgradeStepSequence( // 1.0.4.18 Add CompanyInformation view to allow definition of permissions associated with this schema
							 new AddTableUpgradeStep("CompanyInformation"),
 							new SqlUpgradeStep(@"
 								DECLARE @Principal_AdministratorId uniqueidentifier
								DECLARE @Principal_AllBasicRightsId uniqueidentifier
								SET @Principal_AdministratorId = '4D61696E-426F-7373-0000-000000000000'
								SET @Principal_AllBasicRightsId = '4D61696E-426F-7373-0000-000000000001'
								UPDATE Permission SET PermissionPathPattern = 'Table.*.*' where PrincipalID = @Principal_AdministratorId
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AdministratorId, 'ViewCost.*')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AdministratorId, 'Action.*')
							")
						 ),
						 new UpgradeStepSequence( // 1.0.4.19 Convert Action.Administration to Table.*.* for the Administrator role and add all other permissions for Administrator
							new SqlUpgradeStep(@"
								DECLARE @Principal_AdministratorId uniqueidentifier
								DECLARE @Principal_AllBasicRightsId uniqueidentifier
								SET @Principal_AdministratorId = '4D61696E-426F-7373-0000-000000000000'
								SET @Principal_AllBasicRightsId = '4D61696E-426F-7373-0000-000000000001'
								DELETE FROM Permission where PrincipalID = @Principal_AllBasicRightsId
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Table.*.*')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'Action.*')
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @Principal_AllBasicRightsId, 'ViewCost.*')
							")
							 // Note this step is incorrect at this point; when a method is put into UpgradeSteps to build the permissions and roles
							// based on the security extensions found in the schema, the permissions will be set properly. For now, simply allow all basic
							// users access to everything until the upgrade step is built.
						 ),
						#endregion
						#region 1.0.4.20 - 1.0.4.29
						new UpgradeStepSequence( // 1.0.4.20 Remove the CompanyInformation table schema no longer required.
							new RemoveTableUpgradeStep(GetOriginalSchema, "CompanyInformation")
						),
						new UpgradeStepSequence( // 1.0.4.21 Remove chaff from the UserPermission view
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserPermission"),
							new AddTableUpgradeStep("UserPermission")
						),
						new UpgradeStepSequence( // 1.0.4.22 Make Roles outright deletable.
							// First delete any Hidden roles already present, along with any child Permission and UserRole records.
							new SqlUpgradeStep(@"
								delete from Permission
									from Permission
									join Role on Permission.PrincipalID = Role.PrincipalID
									where Role.Hidden is not null
								delete from UserRole
									from UserRole
									join Role on UserRole.RoleID = Role.ID
									where Role.Hidden is not null
								-- delete the roles themselves by deleting the base Principal; cascade deletes will remove the Role records.
								delete from Principal
									from Principal
									join Role on Role.PrincipalID = Principal.ID
									where Role.Hidden is not null
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Role.Hidden")
						),
						new UpgradeStepSequence( // 1.0.4.23 AccountingTransactionDerivations
							new AddTableUpgradeStep("AccountingTransactionDerivations")
						),
						new UpgradeStepSequence( // 1.0.4.24 Adding Class, Desc, and Comment to Role
							new AddColumnUpgradeStep("Role.Class"),
							new AddColumnUpgradeStep("Role.Desc"),
							new AddColumnUpgradeStep("Role.Comment"),
							new SqlUpgradeStep(@"
								DECLARE @Role_AdministratorId uniqueidentifier
								UPDATE Role SET Class = 0, [Desc] = '', Comment = ''
								SET @Role_AdministratorId= '4D61696E-426F-7373-0001-000000000000'
								UPDate Role SET Role.Code = 'All' , [Desc] ='Permissions to use all parts of the program', 
									Comment = 'This Role should not normally be granted to users of MainBoss. Each user should be granted roles for only the parts of the program that they should be using.'
									WHERE Role.ID = @Role_AdministratorId
							"),
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema,"Role.Code"),
							new SetColumnRequiredUpgradeStep("Role.Class"),
							new AddUniqueConstraintUpgradeStep("Role.Code")
						),
						new UpgradeStepSequence( // 1.0.4.25 Remove Desc/Comment from Role mistakenly added; move values to the base record
							new SqlUpgradeStep(@"
								UPDATE Principal set [DESC] = COALESCE(Principal.[DESC],[Role].[Desc]), Comment = COALESCE(Principal.Comment,[Role].Comment)
									from Principal join [Role] on [Role].PrincipalID = Principal.ID
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Role.Desc"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Role.Comment")
						),
						new UpgradeStepSequence( // 1.0.4.26 Add IsBuiltInRole column to identify builtin roles
							new AddColumnUpgradeStep("Role.IsBuiltInRole"),
							new AddExtensionObjectUpgradeStep("mbfn_IsBuiltInRole"),
							new ChangeToComputedColumnUpgradeStep("Role.IsBuiltInRole", "[dbo].[mbfn_IsBuiltInRole]([ID])")
						),
						new UpgradeStepSequence( // 1.0.4.27 Add WorkOrder/Purchase Order Assignee linkages
							new AddTableUpgradeStep("WorkOrderAssignee"),
							new AddTableUpgradeStep("WorkOrderAssignment"),
							new AddTableUpgradeStep("WorkOrderAssigneeStatistics"),
							new AddColumnUpgradeStep("WorkOrderAssignee.WorkOrderAssigneeStatisticsID"),
							new AddTableUpgradeStep("PurchaseOrderAssignee"),
							new AddTableUpgradeStep("PurchaseOrderAssignment"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeStatistics"),
							new AddColumnUpgradeStep("PurchaseOrderAssignee.PurchaseOrderAssigneeStatisticsID"),
							new ChangeToComputedColumnUpgradeStep("WorkOrderAssignee.WorkOrderAssigneeStatisticsID", "[ID]"),
							new ChangeToComputedColumnUpgradeStep("PurchaseOrderAssignee.PurchaseOrderAssigneeStatisticsID", "[ID]"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"AttentionStatus"),
							new AddTableUpgradeStep("AttentionStatus")
						),
						new UpgradeStepSequence( //  1.0.4.28 Correct AttentionStatus to provide counts for WorkOrder/PurchaseOrder
							 new RemoveTableUpgradeStep(GetOriginalSchema,"AttentionStatus"),
							 new AddTableUpgradeStep("AttentionStatus")
						),
						new UpgradeStepSequence( //  1.0.4.29 Enabled hyperlink feature in reports
							new RemoveTableUpgradeStep(GetOriginalSchema,"PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("ItemReceivingReport"),
							new AddTableUpgradeStep("ItemOnOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						#endregion
						#region 1.0.4.30 - 1.0.4.39
						new UpgradeStepSequence( //  1.0.4.30 Fix Chargeback Report's IsCorrection logic
							new RemoveTableUpgradeStep(GetOriginalSchema,"ChargebackFormReport"),
							new AddTableUpgradeStep("ChargebackFormReport")
						),
						new UpgradeStepSequence( // 1.0.4.31 Add Transition.WorkOrder.xxx rights to WorkOrder State Transition table
							new AddColumnUpgradeStep("WorkOrderStateTransition.RightName"),
							new SqlUpgradeStep(@"
								UPDATE WorkOrderStateTransition SET RightName = 'Transition.WorkOrder.Void'
									from WorkOrderStateTransition as ST
										join WorkOrderState as FS on ST.FromState = FS.ID
										join WorkOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Draft' and TS.Code = 'Voided'
								UPDATE WorkOrderStateTransition SET RightName = 'Transition.WorkOrder.Draft'
									from WorkOrderStateTransition as ST
										join WorkOrderState as FS on ST.FromState = FS.ID
										join WorkOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Voided' and TS.Code = 'Draft'
								UPDATE WorkOrderStateTransition SET RightName = 'Transition.WorkOrder.Open'
									from WorkOrderStateTransition as ST
										join WorkOrderState as FS on ST.FromState = FS.ID
										join WorkOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Draft' and TS.Code = 'Open'
								UPDATE WorkOrderStateTransition SET RightName = 'Transition.WorkOrder.Suspend'
									from WorkOrderStateTransition as ST
										join WorkOrderState as FS on ST.FromState = FS.ID
										join WorkOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Open' and TS.Code = 'Draft'
								UPDATE WorkOrderStateTransition SET RightName = 'Transition.WorkOrder.Close'
									from WorkOrderStateTransition as ST
										join WorkOrderState as FS on ST.FromState = FS.ID
										join WorkOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Open' and TS.Code = 'Closed'
								UPDATE WorkOrderStateTransition SET RightName = 'Transition.WorkOrder.Reopen'
									from WorkOrderStateTransition as ST
										join WorkOrderState as FS on ST.FromState = FS.ID
										join WorkOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Closed' and TS.Code = 'Open'
							")
						),
						new UpgradeStepSequence( // 1.0.4.32 Add Transition.PurchaseOrder.xxx and Request.xxx rights to State Transition table
							new AddColumnUpgradeStep("PurchaseOrderStateTransition.RightName"),
							new SqlUpgradeStep(@"
								UPDATE PurchaseOrderStateTransition SET RightName = 'Transition.PurchaseOrder.Void'
									from PurchaseOrderStateTransition as ST
										join PurchaseOrderState as FS on ST.FromState = FS.ID
										join PurchaseOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Draft' and TS.Code = 'Voided'
								UPDATE PurchaseOrderStateTransition SET RightName = 'Transition.PurchaseOrder.Draft'
									from PurchaseOrderStateTransition as ST
										join PurchaseOrderState as FS on ST.FromState = FS.ID
										join PurchaseOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Voided' and TS.Code = 'Draft'
								UPDATE PurchaseOrderStateTransition SET RightName = 'Transition.PurchaseOrder.Issue'
									from PurchaseOrderStateTransition as ST
										join PurchaseOrderState as FS on ST.FromState = FS.ID
										join PurchaseOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Draft' and TS.Code = 'Issued'
								UPDATE PurchaseOrderStateTransition SET RightName = 'Transition.PurchaseOrder.Withdraw'
									from PurchaseOrderStateTransition as ST
										join PurchaseOrderState as FS on ST.FromState = FS.ID
										join PurchaseOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Issued' and TS.Code = 'Draft'
								UPDATE PurchaseOrderStateTransition SET RightName = 'Transition.PurchaseOrder.Close'
									from PurchaseOrderStateTransition as ST
										join PurchaseOrderState as FS on ST.FromState = FS.ID
										join PurchaseOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Issued' and TS.Code = 'Closed'
								UPDATE PurchaseOrderStateTransition SET RightName = 'Transition.PurchaseOrder.ReActivate'
									from PurchaseOrderStateTransition as ST
										join PurchaseOrderState as FS on ST.FromState = FS.ID
										join PurchaseOrderState as TS on ST.ToState = TS.ID
										where FS.Code = 'Closed' and TS.Code = 'Issued'
							"),
							new AddColumnUpgradeStep("RequestStateTransition.RightName"),
							new SqlUpgradeStep(@"
								UPDATE RequestStateTransition SET RightName = 'Transition.Request.Void'
									from RequestStateTransition as ST
										join RequestState as FS on ST.FromState = FS.ID
										join RequestState as TS on ST.ToState = TS.ID
										where FS.Code = 'New' and TS.Code = 'Closed'
								UPDATE RequestStateTransition SET RightName = 'Transition.Request.Reopen'
									from RequestStateTransition as ST
										join RequestState as FS on ST.FromState = FS.ID
										join RequestState as TS on ST.ToState = TS.ID
										where FS.Code = 'Closed' and TS.Code = 'In Progress'
								UPDATE RequestStateTransition SET RightName = 'Transition.Request.InProgress'
									from RequestStateTransition as ST
										join RequestState as FS on ST.FromState = FS.ID
										join RequestState as TS on ST.ToState = TS.ID
										where FS.Code = 'New' and TS.Code = 'In Progress'
								UPDATE RequestStateTransition SET RightName = 'Transition.Request.Close'
									from RequestStateTransition as ST
										join RequestState as FS on ST.FromState = FS.ID
										join RequestState as TS on ST.ToState = TS.ID
										where FS.Code = 'In Progress' and TS.Code = 'Closed'
							")
						), // 1.0.4.33 Add Transition.*.* to All permission group (note change to new names for known IDs)
						new SqlUpgradeStep(@"
								DECLARE @PrincipalId_All uniqueidentifier
								DECLARE @RoleId_All uniqueidentifier
								SET @PrincipalId_All = '4D61696E-426F-7373-0000-000000000000'
								SET @RoleId_All= '4D61696E-426F-7373-0001-000000000000'
								INSERT INTO Permission (ID, PrincipalID, PermissionPathPattern) VALUES (NEWID(), @PrincipalId_All, 'Transition.*.*')
						"),
						new CreateAllColumnIndexesUpgradeStep(),	// 1.0.4.34 Add indexes to all columns with foreign key constraints where possible
						new UpgradeStepSequence( // 1.0.4.35 Speed up mbfn_Ordered_Location_Code
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestockingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PermanentItemLocationReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Ordered_Location_Code"),
							new AddExtensionObjectUpgradeStep("mbfn_Ordered_Location_Code"),
							new AddTableUpgradeStep("PermanentItemLocationReport"),
							new AddTableUpgradeStep("ItemLocationReport"),
							new AddTableUpgradeStep("ItemRestockingReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.4.36 Add variables for @Requests' SMTP authentication
							new AddVariableUpgradeStep("ATRSMTPUseSSL"),
							new AddVariableUpgradeStep("ATRSMTPCredentialType"),
							new AddVariableUpgradeStep("ATRSMTPUserDomain"),
							new AddVariableUpgradeStep("ATRSMTPUserName"),
							new AddVariableUpgradeStep("ATRSMTPEncryptedPassword"),
							new SqlUpgradeStep(
							@"
								exec dbo._vsetATRSMTPUseSSL 0;
								exec dbo._vsetATRSMTPCredentialType 0;
							")
						),
						new UpgradeStepSequence( // 1.0.4.37 Change to new role permissions as defined in rightset.xml
							new PermissionUpgradeStep()
						),
						new UpgradeStepSequence( // 1.0.4.38 Update ContactRoles with Work Order/Purchase Order Assignee (forgotten in earlier step)
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRoles"),
							new AddTableUpgradeStep("ContactRoles")
						),
						new UpgradeStepSequence( // 1.0.4.39 Add WorkOrderAssigneeProspect view for picker assistance
							new AddTableUpgradeStep("WorkOrderAssigneeProspect")
						),
						#endregion
						#region 1.0.4.40 - 1.0.4.49
						new UpgradeStepSequence( // 1.0.4.40 Update OtherWorkOutside et.al for consistency wrt Trade and other report fields
							new AddColumnUpgradeStep("OtherWorkOutside.TradeID"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "OtherWorkInsideReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "OtherWorkOutsideReport"),
							new AddTableUpgradeStep("OtherWorkInsideReport"),
							new AddTableUpgradeStep("OtherWorkOutsideReport")
						),
						new UpgradeStepSequence( // 1.0.4.41 Add CanTransitionWithoutUI columns to state transition tables for existing transitions
							new AddColumnUpgradeStep("RequestStateTransition.CanTransitionWithoutUI"),
							new AddColumnUpgradeStep("WorkOrderStateTransition.CanTransitionWithoutUI"),
							new AddColumnUpgradeStep("PurchaseOrderStateTransition.CanTransitionWithoutUI"),
							new SqlUpgradeStep(@"
									UPDATE RequestStateTransition SET CanTransitionWithoutUI = 0
									UPDATE WorkOrderStateTransition SET CanTransitionWithoutUI = 0
									UPDATE PurchaseOrderStateTransition SET CanTransitionWithoutUI = 0
							"),
							new SetColumnRequiredUpgradeStep("RequestStateTransition.CanTransitionWithoutUI"),
							new SetColumnRequiredUpgradeStep("WorkOrderStateTransition.CanTransitionWithoutUI"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderStateTransition.CanTransitionWithoutUI")
						),
						new UpgradeStepSequence( // 1.0.4.42 Add Request/WorkOrder/PurchaseOrder AssigneeReport views
							new AddTableUpgradeStep("RequestAssigneeReport"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeReport"),
							new AddTableUpgradeStep("WorkOrderAssigneeReport")
						),
						new UpgradeStepSequence( // 1.0.4.43 Add RoleAndUserReport view for reporting on Roles & Users
							new AddTableUpgradeStep("RoleAndUserReport")
						),
						new UpgradeStepSequence( // 1.0.4.44 Update RoleAndUserReport view for reporting on Roles & Users
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleAndUserReport"),
							new AddTableUpgradeStep("RoleAndUserReport")
						),
						new UpgradeStepSequence( // 1.0.4.45 Add DemandActualCalculationInitValue fields to all Demand records
							new AddColumnUpgradeStep("Demand.DemandActualCalculationInitValue"),
							new AddColumnUpgradeStep("DemandTemplate.DemandActualCalculationInitValue"),
							new SqlUpgradeStep(@"
								UPDATE Demand SET DemandActualCalculationInitValue = 0
								UPDATE DemandTemplate SET DemandActualCalculationInitValue = 0
							"),
							new SetColumnRequiredUpgradeStep("Demand.DemandActualCalculationInitValue"),
							new SetColumnRequiredUpgradeStep("DemandTemplate.DemandActualCalculationInitValue")
							),
						new UpgradeStepSequence( // 1.0.4.46 Enhanse WorkOrderAssigneeProspect view for picker assistance
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("WorkOrderAssigneeProspect")
						),
						new UpgradeStepSequence( // 1.0.4.47 Split RoleAndUserReport into two views
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleAndUserReport"),
							new AddTableUpgradeStep("UserReport"),
							new AddTableUpgradeStep("RoleReport")
						),
						new UpgradeStepSequence( // 1.0.4.48 Set deletemethod to 'hide' for UserReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserReport"),
							new AddTableUpgradeStep("UserReport"),
							new AddTableUpgradeStep("RoleReport")
						),
						new UpgradeStepSequence( // 1.0.4.49 Change ContactRoles to ContactFunctions
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRoles"),
							new AddTableUpgradeStep("ContactFunctions")
						),
						#endregion
						#region 1.0.4.50 - 1.0.4.59
						new UpgradeStepSequence( // 1.0.4.50 Update triggers to use caseless compare on Code fields
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_SetUpdatedRelativeLocationContainment"),
							new AddExtensionObjectUpgradeStep("mbtg_SetUpdatedRelativeLocationContainment"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_SetUpdatedPostalAddressCodes"),
							new AddExtensionObjectUpgradeStep("mbtg_SetUpdatedPostalAddressCodes"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_SetUpdatedWorkOrderCodes"),
							new AddExtensionObjectUpgradeStep("mbtg_SetUpdatedWorkOrderCodes")
						),
						new UpgradeStepSequence( // 1.0.4.51 Add WorkOrderState CanModifyDefinition/OperationalField; delete CanModifyMainFields/WorkInterval columns
							new AddColumnUpgradeStep("WorkOrderState.CanModifyDefinitionFields"),
							new AddColumnUpgradeStep("WorkOrderState.CanModifyOperationalFields"),
							new SqlUpgradeStep(@"
								UPDATE WorkOrderState SET CanModifyDefinitionFields = 1, CanModifyOperationalFields = 1
									where Code = 'Draft'
								UPDATE WorkOrderState SET CanModifyDefinitionFields = 0, CanModifyOperationalFields = 1
									where Code = 'Open'
								UPDATE WorkOrderState SET CanModifyDefinitionFields = 0, CanModifyOperationalFields = 0
									where Code = 'Closed'
								UPDATE WorkOrderState SET CanModifyDefinitionFields = 0, CanModifyOperationalFields = 0
									where Code = 'Voided'
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderState.CanModifyMainFields"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderState.CanModifyWorkInterval"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderState.Archive"), // remove unused field until such time as it will be needed
							new SetColumnRequiredUpgradeStep("WorkOrderState.CanModifyDefinitionFields"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.CanModifyOperationalFields")
						),
						new UpgradeStepSequence( // 1.0.4.52 Add RequestAssigneeProspects AnyAssigneeNotOnNewOrInProgress
							new AddTableUpgradeStep("RequestAssigneeProspect")
						),
						new UpgradeStepSequence( // 1.0.4.53 Add Close xx no comment transitions to state transition tables. 
							new SqlUpgradeStep(@"
								update RequestStateTransition SET [RANK] = [RANK] + 1  where [RANK] > 2
								insert into RequestStateTransition (ID, Operation, OperationHint, FromState, ToState, [Rank], RightName, CanTransitionWithoutUI)
									select NEWID(), 'Close WR (No Comment)', 'Change an In Progress request to the Closed state with no comment required.', FromState, ToState, 3, RightName, 1
									from RequestStateTransition where [Rank] = 2

								update WorkOrderStateTransition SET [RANK] = [RANK] + 1  where [RANK] > 2
								insert into WorkOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, [Rank], RightName, CanTransitionWithoutUI)
									select NEWID(), 'Close WO (No Comment)', 'Close a Work Order with no comment required.', FromState, ToState, 3, RightName, 1
									from WorkOrderStateTransition where [Rank] = 2

								update PurchaseOrderStateTransition SET [RANK] = [RANK] + 1  where [RANK] > 2
								insert into PurchaseOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, [Rank], RightName, CanTransitionWithoutUI)
									select NEWID(), 'Close PO (No Comment)', 'Close a Purchase Order with no comment required.', FromState, ToState, 3, RightName, 1
									from PurchaseOrderStateTransition where [Rank] = 2 
							")
						),
						new UpgradeStepSequence( // 1.0.4.54 Update mbfn_Contact_UserID to ignore HIDDEN records
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Contact_UserID"),
							new AddExtensionObjectUpgradeStep("mbfn_Contact_UserID"),
							new SqlUpgradeStep(@"
								UPDATE Contact SET UserID = NULL from [Contact] as C
									join [User] as U on C.UserID = U.ID where U.Hidden is not null
							")
						),
						new UpgradeStepSequence( // 1.0.4.55 Update ContactFunctions to include User's as a function
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactFunctions"),
							new AddTableUpgradeStep("ContactFunctions")
						),
						new UpgradeStepSequence( // 1.0.4.56 Replace 'union' with 'union all' for speed
							new RemoveTableUpgradeStep(GetOriginalSchema, "PeriodicityView"),
							new AddTableUpgradeStep("PeriodicityView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndContainers"),
							new AddTableUpgradeStep("PMGenerationDetailAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeProspect"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("RequestAssigneeProspect"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside"),
							new AddTableUpgradeStep("WorkOrderInside"),
							new AddTableUpgradeStep("WorkOrderInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItemsTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems"),
							new AddTableUpgradeStep("WorkOrderItems"),
							new AddTableUpgradeStep("WorkOrderItemsTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderMiscellaneousTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderMiscellaneousTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOutside"),
							new AddTableUpgradeStep("WorkOrderOutside"),
							new AddTableUpgradeStep("WorkOrderOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderLinkedPurchaseOrdersTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderLinkedWorkOrdersTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderPurchaseOrderView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderPurchaseOrderLinkage"),
							new AddTableUpgradeStep("WorkOrderPurchaseOrderLinkage"),
							new AddTableUpgradeStep("WorkOrderPurchaseOrderView"),
							new AddTableUpgradeStep("PurchaseOrderLinkedWorkOrdersTreeview"),
							new AddTableUpgradeStep("WorkOrderLinkedPurchaseOrdersTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateInside"),
							new AddTableUpgradeStep("WorkOrderTemplateInside"),
							new AddTableUpgradeStep("WorkOrderTemplateInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateMiscellaneousTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderTemplateMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderTemplateMiscellaneousTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateOutside"),
							new AddTableUpgradeStep("WorkOrderTemplateOutside"),
							new AddTableUpgradeStep("WorkOrderTemplateOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateLinkedPurchaseOrderTemplatesTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateLinkedWorkOrderTemplatesTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplatePurchaseOrderTemplateView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplatePurchaseOrderTemplateLinkage"),
							new AddTableUpgradeStep("WorkOrderTemplatePurchaseOrderTemplateLinkage"),
							new AddTableUpgradeStep("WorkOrderTemplatePurchaseOrderTemplateView"),
							new AddTableUpgradeStep("PurchaseOrderTemplateLinkedWorkOrderTemplatesTreeview"),
							new AddTableUpgradeStep("WorkOrderTemplateLinkedPurchaseOrderTemplatesTreeview"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserPermission"),
							new AddTableUpgradeStep("UserPermission")
						),
						new UpgradeStepSequence( // 1.0.4.57 Add Task info to WorkOrder-related reports
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport")
						),
						new UpgradeStepSequence( // 1.0.4.58 Add PurchaseOrderAssigneeProspect
							new AddTableUpgradeStep("PurchaseOrderAssigneeProspect")
						),
						new UpgradeStepSequence( // 1.0.4.59 Add Billable Requests to WorkOrderAssigneeProspect
							new RemoveTableUpgradeStep(GetOriginalSchema,"RequestAssigneeProspect"),				// view refers WorkOrderAssigneeProspect
							new RemoveTableUpgradeStep(GetOriginalSchema,"PurchaseOrderAssigneeProspect"),			// view refers WorkOrderAssigneeProspect
							new RemoveTableUpgradeStep(GetOriginalSchema,"WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("RequestAssigneeProspect"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeProspect")
						),
						#endregion
						#region 1.0.4.60 - 1.0.4.69
						new UpgradeStepSequence( // 1.0.4.60 new work order items views
							new AddTableUpgradeStep("WorkOrderItems2"),
							new AddTableUpgradeStep("WorkOrderItems2TreeView")
						),
						new UpgradeStepSequence( // 1.0.4.61 new work order Inside views
							new AddTableUpgradeStep("WorkOrderInside2"),
							new AddTableUpgradeStep("WorkOrderInside2TreeView")
						),
						new UpgradeStepSequence( // 1.0.4.62 fix WorkOrderInside2 to use UNION to remove duplicate rows caused by left join to Actuals
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside2TreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside2"),
							new AddTableUpgradeStep("WorkOrderInside2"),
							new AddTableUpgradeStep("WorkOrderInside2TreeView")
						),
						new UpgradeStepSequence( // 1.0.4.63 fix WorkOrderItems2 to only return DemandItem records, not all Demands, and to use 'union all' rather than 'union'.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems2TreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems2"),
							new AddTableUpgradeStep("WorkOrderItems2"),
							new AddTableUpgradeStep("WorkOrderItems2TreeView")
						),
						new UpgradeStepSequence( // 1.0.4.64 fix WorkOrderInside2 to have the correct types of records (more or less)
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside2TreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside2"),
							new AddTableUpgradeStep("WorkOrderInside2"),
							new AddTableUpgradeStep("WorkOrderInside2TreeView")
						),
						new UpgradeStepSequence( // 1.0.4.65 add a new View for wo temp storage and assignments
							new AddTableUpgradeStep("WorkOrderTemporaryStorage"),
							new AddTableUpgradeStep("WorkOrderTemporaryStorageTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.66 add a new View so Actuals are correctable within the Demand editor
							new AddTableUpgradeStep("ActualItemAndCorrections")
						),
						new UpgradeStepSequence( // 1.0.4.67 add a new View to allow location of the WorkOrderExpenseModelEntryID from just the derived Demand ID
							new AddTableUpgradeStep("DemandWorkOrderExpenseModelEntry")
						),
						new UpgradeStepSequence( // 1.0.4.68 Eliminate the AccountingTransactionDerivations view
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionDerivations")
						),
						new UpgradeStepSequence( // 1.0.4.69 Eliminate the ActualItemAndCorrections view
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActualItemAndCorrections")
						),
						#endregion
						#region 1.0.4.70 - 1.0.4.79
						new UpgradeStepSequence( // 1.0.4.70 - Remove the '2' from the new WO resource views. Make WorkOrderInside be trade-centric
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside2TreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderInside2"),
							new AddTableUpgradeStep("WorkOrderInside"),
							new AddTableUpgradeStep("WorkOrderInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItemsTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems2TreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderItems2"),
							new AddTableUpgradeStep("WorkOrderItems"),
							new AddTableUpgradeStep("WorkOrderItemsTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.71 Replace Work Order Miscellaneous browser
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderMiscellaneousTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderMiscellaneous"),
							new AddTableUpgradeStep("WorkOrderMiscellaneousTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.72 Replace Work Order Outside browser
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOutside"),
							new AddTableUpgradeStep("WorkOrderOutside"),
							new AddTableUpgradeStep("WorkOrderOutsideTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.73 Add DemandLabor/OtherWorkOutsideActivity views
							new AddTableUpgradeStep("DemandLaborOutsideActivity"),
							new AddTableUpgradeStep("DemandOtherWorkOutsideActivity")
						),
						new UpgradeStepSequence( // 1.0.4.74 Correct ItemActivity view to give distinct ID's for TransferTo and TransferFrom
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivity"),
							new AddTableUpgradeStep("ItemActivity")
						),
						new UpgradeStepSequence( // 1.0.4.75 - Update to new WorkOrderTemplateInside views
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateInsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateInside"),
							new AddTableUpgradeStep("WorkOrderTemplateInside"),
							new AddTableUpgradeStep("WorkOrderTemplateInsideTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.76 add a new View to allow location of the WorkOrderExpenseModelEntryID from just the derived DemandTemplate ID
							new AddTableUpgradeStep("DemandTemplateWorkOrderExpenseModelEntry")
						),
						new UpgradeStepSequence( // 1.0.4.77 Replace Work Order Template Outside browser
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateOutside"),
							new AddTableUpgradeStep("WorkOrderTemplateOutside"),
							new AddTableUpgradeStep("WorkOrderTemplateOutsideTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.78 Replace Work Order Template Items browser views
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateItemsTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateItems"),
							new AddTableUpgradeStep("WorkOrderTemplateItems"),
							new AddTableUpgradeStep("WorkOrderTemplateItemsTreeView"),
							new AddTableUpgradeStep("WorkOrderTemplateStorage"),
							new AddTableUpgradeStep("WorkOrderTemplateStorageTreeView")
						),
						new UpgradeStepSequence( // 1.0.4.79 Add BackupFileName table for backup/restore; note this is part of the dbversion set of tables
							new AddTableUpgradeStep("BackupFileName")
						),
						#endregion
						#region 1.0.4.80 - 1.0.4.82
						new UpgradeStepSequence( // 1.0.4.80 Update DatabaseStatus view to include backup data from BackupFileName table
							new RemoveTableUpgradeStep(GetOriginalSchema, "DatabaseStatus"),
							new AddTableUpgradeStep("DatabaseStatus")
						),
						new UpgradeStepSequence( // 1.0.4.81 - Add a WorkOrderCreationStateID override field to PMGenerationBatch to control state of generated workorders
							new AddColumnUpgradeStep("PMGenerationBatch.WorkOrderCreationStateID")
						),
						new UpgradeStepSequence( // 1.0.4.82 - Change from Close (No comment) to Close (With Comment) and add similar for Open transitions
							new SqlUpgradeStep(@"
								update RequestStateTransition SET [RANK] = [RANK] + 1  where [RANK] > 2
								update RequestStateTransition SET [CanTransitionWithoutUI] = 1 where [RANK] <= 2
								update RequestStateTransition SET [CanTransitionWithoutUI] = 0, Operation = 'Close WR (With Comment)', OperationHint = 'Change an In Progress request to the Closed state and allow a comment.' where [RANK] = 4
								insert into RequestStateTransition (ID, Operation, OperationHint, FromState, ToState, [Rank], RightName, CanTransitionWithoutUI)
									select NEWID(), 'InProgress (With Comment)', 'Change a request to the In Progress state and allow a comment.', FromState, ToState, 3, RightName, 0
									from RequestStateTransition where [Rank] = 1

								update WorkOrderStateTransition SET [RANK] = [RANK] + 1  where [RANK] > 2
								update WorkOrderStateTransition SET [CanTransitionWithoutUI] = 1 where [RANK] <= 2
								update WorkOrderStateTransition SET [CanTransitionWithoutUI] = 0, Operation = 'Close WO (With Comment)', OperationHint = 'Close a Work Order and allow a comment' where [RANK] = 4
								insert into WorkOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, [Rank], RightName, CanTransitionWithoutUI)
									select NEWID(), 'Open WO (With Comment)', 'Change a Draft Work Order to Open and allow a comment', FromState, ToState, 3, RightName, 0
									from WorkOrderStateTransition where [Rank] = 1

								update PurchaseOrderStateTransition SET [RANK] = [RANK] + 1  where [RANK] > 2
								update PurchaseOrderStateTransition SET [CanTransitionWithoutUI] = 1 where [RANK] <= 2
								update PurchaseOrderStateTransition SET [CanTransitionWithoutUI] = 0, Operation = 'Close PO (With Comment)', OperationHint = 'Close a Purchase Order and allow a comment' where [RANK] = 4
								insert into PurchaseOrderStateTransition (ID, Operation, OperationHint, FromState, ToState, [Rank], RightName, CanTransitionWithoutUI)
									select NEWID(), 'Issue (With Comment)', 'Issue a Purchase Order and allow a comment', FromState, ToState, 3, RightName, 0
									from PurchaseOrderStateTransition where [Rank] = 1
							")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.0.5.x Reserved for steps included in released versions of MB3.2
						#region 1.0.5.0 - 1.0.5.9
						// MB 3.2.0 uses db version 1.0.5.0
						new UpgradeStepSequence( //1.0.5.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.0.10'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.0.10'")
						),
						new UpgradeStepSequence( //1.0.5.1  ( to force the security to recreated )
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.0.15'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.0.15'")
						),
						new UpgradeStepSequence( //1.0.5.2  ( to force the security to recreated )
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.0.16'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.0.16'")
						),
						new UpgradeStepSequence( //1.0.5.3  ( to force the security to recreated )
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.0.17'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.0.17'")
						),
						new UpgradeStepSequence( // 1.0.5.4 Update 1 - fix UserID/ContactID linkages
							new SqlUpgradeStep(@"UPDATE Contact Set UserID = dbo.mbfn_Contact_Userid(Id)")
						),
						new UpgradeStepSequence( //1.0.5.5 Fix ItemOnOrderReport to filter out received items and items on non draft or issued P/Os.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new AddTableUpgradeStep("ItemOnOrderReport")
						),
						new UpgradeStepSequence( //1.0.5.6 Add UserID reference fields to statehistory records
							new AddColumnUpgradeStep("RequestStateHistory.UserID"),
							new AddColumnUpgradeStep("WorkOrderStateHistory.UserID"),
							new AddColumnUpgradeStep("PurchaseOrderStateHistory.UserID")
						),
						new UpgradeStepSequence( //1.0.5.7 Add WR close trigger, add site-option flag
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddVariableUpgradeStep("ManageRequestStates"),
							new SqlUpgradeStep(@"exec dbo._vsetManageRequestStates 0"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_Sets_RequestState")
						),
						new UpgradeStepSequence( //1.0.5.8 Add variables for user-settable report fonts
							new AddVariableUpgradeStep("ReportFont"),
							new AddVariableUpgradeStep("ReportFontFixedWidth"),
							new AddVariableUpgradeStep("ReportFontSize"),
							new SqlUpgradeStep(@"
								exec dbo._vsetReportFont 'Verdana'
								exec dbo._vsetReportFontFixedWidth 'Courier New'
								exec dbo._vsetReportFontSize 8
							")
						),
						new UpgradeStepSequence( // 1.0.5.9 - fix WorkOrderTemplate (Task) linkage in WorkOrder reports & Add User information to state-history reports
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport")
						),
						#endregion
						#region 1.0.5.10 - 1.0.5.14
						new UpgradeStepSequence( // 1.0.5.10 - Expand special location filtering to items, contacts and forecast reports.
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestockingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("EmployeeReport"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeReport"),
							new AddTableUpgradeStep("RequestAssigneeReport"),
							new AddTableUpgradeStep("RequestorReport"),
							new AddTableUpgradeStep("BillableRequestorReport"),
							new AddTableUpgradeStep("WorkOrderAssigneeReport"),
							new AddTableUpgradeStep("ItemLocationReport"),
							new AddTableUpgradeStep("ItemRestockingReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( //1.0.5.11
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.1.1'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.1.1'")
						),
						new UpgradeStepSequence( // 1.0.5.12 - Set demands to default to making the actuals use current costing
							new SqlUpgradeStep(@"update _DDemand set DemandActualCalculationInitValue = 1"), // DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue
							new SqlUpgradeStep(@"update _DDemandTemplate set DemandActualCalculationInitValue = 1") // DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue
						),
						new UpgradeStepSequence( //1.0.5.13
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.1.3'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.1.3'"),
							new AddVariableUpgradeStep("MinMBRemoteAppVersion"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.3.0.1'")
						),
						new UpgradeStepSequence( //1.0.5.14
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.1.4'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.1.4'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.2.1.4'")
						),
						// Following added for Update 2 and beyond
						new UpgradeStepSequence( // 1.0.5.15 - Fix ManageRequestState triggers to copy the originating UserID from the WSH to the RSH
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_RequestedWorkOrder_Sets_RequestState"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new SqlUpgradeStep(@"
									UPDATE RequestStateHistory set UserID = WSH.UserID from WorkOrderStateHistory as WSH
													JOIN RequestStateHistory as RSH on WSH.EffectiveDate = RSH.EffectiveDate
													WHERE RSH.UserID is null
							"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_Sets_RequestState")
						),
						// Uncomment when official release of Update 2 is ready to go, and change versions below as required
						new UpgradeStepSequence( //1.0.5.16
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.2.1'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.2.1'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.2.2.1'")
						),
						// Uncomment when official release of Update 2 is ready to go, and change versions below as required
						new UpgradeStepSequence( //1.0.5.17
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.2.2'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.2.2'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.2.2.2'")
						),
						new UpgradeStepSequence( // 1.0.5.18 - clear out _DDemandTemplate WorkOrderExpenseCategoryID fields
							new SqlUpgradeStep(@"update _DDemandTemplate SET WorkOrderExpenseCategoryID = null"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.2.3'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.2.3'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.2.2.3'")
						),
						new UpgradeStepSequence( // 1.0.5.19 - release of Update 2
							new SqlUpgradeStep(@"update _DDemandTemplate SET WorkOrderExpenseCategoryID = null"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.2.2.4'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.2.2.4'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.2.2.4'")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.0.6.x Reserved for development steps for MB3.3
						#region 1.0.6.0 - 1.0.6.9
						new UpgradeStepSequence( //1.0.6.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.3.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.3.0.0'")
						),
						new UpgradeStepSequence( //1.0.6.1  ( to force the security to recreated )
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.3.0.1'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.3.0.1'")
						),
						new UpgradeStepSequence( //1.0.6.2 Add trigger to automatically close requests when corresponding W/O closes or voided
							new Version(1,0,6,0), // preceded by trigger in 1.0.5.7, superceded in 1.0.6.10
							// This trigger may have been provided to customers using MB 3.2 to insert into their database; so we have to remove it first if it exists.
							new SqlUpgradeStep(@"
								if object_id('mbtg_CloseRequests', 'TR') is not null
									drop trigger mbtg_CloseRequests;
								"),
							new AddExtensionObjectUpgradeStep("mbtg_CloseRequests")
						),
						new UpgradeStepSequence( //1.0.6.3 Set the EffectiveDate of closed-RequestStateHistory to that of the WorkOrderStateHistory's.
							new Version(1,0,6,0),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_CloseRequests"),
							new AddExtensionObjectUpgradeStep("mbtg_CloseRequests")
						),
						new UpgradeStepSequence( //1.0.6.4  ( to force the security to recreated )
						    new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.3.0.1'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.3.0.1'")
						),
						new UpgradeStepSequence( //1.0.6.5 Check effectivedate and remove loop in the trigger
							new Version(1,0,6,0),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_CloseRequests"),
							new AddExtensionObjectUpgradeStep("mbtg_CloseRequests")
						),
						new UpgradeStepSequence( //1.0.6.6 Use the _DClosesValue function to get the proper date format in the trigger
							new Version(1,0,6,5), new Version(1,0,6,6),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_CloseRequests"),
							new AddExtensionObjectUpgradeStep("mbtg_CloseRequests")
						),
						new UpgradeStepSequence( //1.0.6.7 Fix ItemOnOrderReport to filter out received items and items on non draft or issued P/Os.
							new Version(1,0,6,0),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new AddTableUpgradeStep("ItemOnOrderReport")
						),
						new UpgradeStepSequence( //1.0.6.8 Add variables for user-settable report fonts
							new Version(1,0,6,0),
							new AddVariableUpgradeStep("ReportFont"),
							new AddVariableUpgradeStep("ReportFontFixedWidth"),
							new AddVariableUpgradeStep("ReportFontSize"),
							new SqlUpgradeStep(@"
								exec dbo._vsetReportFont 'Verdana'
								exec dbo._vsetReportFontFixedWidth 'Courier New'
								exec dbo._vsetReportFontSize 8
							")
						),
						new UpgradeStepSequence( //1.0.6.9 Add UserID reference fields to statehistory records
							new Version(1,0,6,0),	// Already upgraded in the 1.0.5.x stream as 1.0.5.6
							new AddColumnUpgradeStep("RequestStateHistory.UserID"),
							new AddColumnUpgradeStep("WorkOrderStateHistory.UserID"),
							new AddColumnUpgradeStep("PurchaseOrderStateHistory.UserID")
						),
						#endregion
						#region 1.0.6.10 - 1.0.6.19
						new UpgradeStepSequence( //1.0.6.10 Move WR close to different trigger, add site-option flag; already upgraded in 1.0.5.x stream as 1.0.5.7
							new Version(1,0,6,0), // only need this if we are upgrading from step 1.0.6.3 to 1.0.6.10
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_CloseRequests"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddVariableUpgradeStep("ManageRequestStates"),
							new SqlUpgradeStep(@"exec dbo._vsetManageRequestStates 0"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_Sets_RequestState")
						),
						new UpgradeStepSequence( // 1.0.6.11 - fix UserID/ContactID linkages
							new Version(1,0,6,0),
							new SqlUpgradeStep(@"UPDATE Contact Set UserID = dbo.mbfn_Contact_Userid(Id)")
						),
						new UpgradeStepSequence( // 1.0.6.12 - fix WorkOrderTemplate (Task) linkage in WorkOrder reports
							new Version(1,0,6,0),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport")
						),
						new UpgradeStepSequence( // 1.0.6.13 - Add User infomation to state-history reports
							new Version(1,0,6,0),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport")
						),
						new UpgradeStepSequence( // 1.0.6.14 - Expand special location filtering to items, contacts and forecast reports.
							new Version(1,0,6,0),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestockingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("EmployeeReport"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeReport"),
							new AddTableUpgradeStep("RequestAssigneeReport"),
							new AddTableUpgradeStep("RequestorReport"),
							new AddTableUpgradeStep("BillableRequestorReport"),
							new AddTableUpgradeStep("WorkOrderAssigneeReport"),
							new AddTableUpgradeStep("ItemLocationReport"),
							new AddTableUpgradeStep("ItemRestockingReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.6.15 - Set demands to default to making the actuals use current costing
							new SqlUpgradeStep(@"update _DDemand set DemandActualCalculationInitValue = 1"), // DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue
							new SqlUpgradeStep(@"update _DDemandTemplate set DemandActualCalculationInitValue = 1") // DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue
						),
						new UpgradeStepSequence( // 1.0.6.16 - Add MinMBRemoteAppVersion variable to control MainBoss Remote versioning
							new Version(1,0,6,0),
							new AddVariableUpgradeStep("MinMBRemoteAppVersion"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.3.0.1'")
						),
						new UpgradeStepSequence( // 1.0.6.17 - Add CommentToRequestor comment variables
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_RequestedWorkOrder_Sets_RequestState"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddVariableUpgradeStep("CommentToRequestorWhenNewRequestedWorkOrder"),
							new AddVariableUpgradeStep("CommentToRequestorWhenWorkOrderCloses"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_Sets_RequestState")
						),
						new UpgradeStepSequence( // 1.0.6.18 - Clear out any non-null default value in WorkOrderTemplate.F.ContainingWorkOrderTemplateID
							// The code up until this point incorrectly gave the user a chance to set a value here but such setting is always ineffectual.
							new SqlUpgradeStep(@"update _DWorkOrderTemplate set ContainingWorkOrderTemplateID = null")
						),
						new UpgradeStepSequence( // 1.0.6.19 - Fix ManageRequestState triggers to copy the originating UserID from the WSH to the RSH
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_RequestedWorkOrder_Sets_RequestState"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new SqlUpgradeStep(@"
									UPDATE RequestStateHistory set UserID = WSH.UserID from WorkOrderStateHistory as WSH
													JOIN RequestStateHistory as RSH on WSH.EffectiveDate = RSH.EffectiveDate
													WHERE RSH.UserID is null
							"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_Sets_RequestState")
							),
						#endregion
						#region 1.0.6.20 - 1.0.6.29
						new UpgradeStepSequence( // 1.0.6.20 - Add Notification support for RequestAssignment, WorkOrderAssignment and PurchaseOrderAssignment
							new AddColumnUpgradeStep("RequestAssignment.LastNotificationDate"),
							new AddColumnUpgradeStep("WorkOrderAssignment.LastNotificationDate"),
							new AddColumnUpgradeStep("PurchaseOrderAssignment.LastNotificationDate"),
							new AddColumnUpgradeStep("RequestAssignee.ReceiveNotification"),
							new AddColumnUpgradeStep("WorkOrderAssignee.ReceiveNotification"),
							new AddColumnUpgradeStep("PurchaseOrderAssignee.ReceiveNotification"),
							// Set all existing Assignee records to receive notifications, and set the default value as well.
							// To guard against a flurry of initial notifications from current records in the database, set the LastNotificationDate on all current assignments
							// to be the Currentstatehistory's Entry date (the last state of that record)
							new SqlUpgradeStep(@"
								UPDATE RequestAssignee set ReceiveNotification = 1
								UPDATE _DRequestAssignee set ReceiveNotification = 1
								UPDATE WorkOrderAssignee set ReceiveNotification = 1
								UPDATE _DWorkOrderAssignee set ReceiveNotification = 1
								UPDATE PurchaseOrderAssignee set ReceiveNotification = 1
								UPDATE _DPurchaseOrderAssignee set ReceiveNotification = 1
								UPDATE RequestAssignment set LastNotificationDate = RSH.EntryDate from RequestAssignment as RA
									join Request as R on R.ID = RA.RequestID
									join RequestStateHistory as RSH on RSH.ID = R.CurrentRequestStateHistoryID
									join RequestState on RequestState.ID = RSH.RequestStateID
								UPDATE WorkOrderAssignment set LastNotificationDate = WOSH.EntryDate from WorkOrderAssignment as WOA
									join WorkOrder as WO on WO.ID = WOA.WorkOrderID
									join WorkOrderStateHistory as WOSH on WOSH.ID = WO.CurrentWorkOrderStateHistoryID
									join WorkOrderState on WorkOrderState.ID = WOSH.WorkOrderStateID
								UPDATE PurchaseOrderAssignment set LastNotificationDate = POSH.EntryDate from PurchaseOrderAssignment as POA
									join PurchaseOrder as PO on PO.ID = POA.PurchaseOrderID
									join PurchaseOrderStateHistory as POSH on POSH.ID = PO.CurrentPurchaseOrderStateHistoryID
									join PurchaseOrderState on PurchaseOrderState.ID = POSH.PurchaseOrderStateID
							"),
							new SetColumnRequiredUpgradeStep("RequestAssignee.ReceiveNotification"),
							new SetColumnRequiredUpgradeStep("WorkOrderAssignee.ReceiveNotification"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderAssignee.ReceiveNotification"),
							new AddTableUpgradeStep("AssignmentNotification")
						),
						new UpgradeStepSequence( // 1.0.6.21 - Update AssignmentNotification view to something a bit more sane
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignmentNotification"),
							new AddTableUpgradeStep("AssignmentNotification")
						),
						new UpgradeStepSequence( // 1.0.6.22 - Add new configuration variables for mainboss service
							new AddVariableUpgradeStep("MBSNotificationInterval"),
							new AddVariableUpgradeStep("MBSReturnEmailAddress"),
							new AddVariableUpgradeStep("MBSReturnEmailDisplayName"),
							new AddVariableUpgradeStep("MBSHtmlEmailNotification"),
							new AddVariableUpgradeStep("MBSNotificationSubjectPrefix"),
							new AddVariableUpgradeStep("MBSNotificationIntroduction"),
							new SqlUpgradeStep(@"
							declare @copystr as nvarchar(max)
							declare @x datetime
							set @copystr = dbo._vgetATRAdminEmailAddress()
							exec dbo._vsetMBSReturnEmailAddress @copystr
							set @copystr = dbo._vgetATRAcknowledgmentTitle()
							exec dbo._vsetMBSReturnEmailDisplayName @copystr
							SET @x = dbo._INew(0,0,10,0,0)
							exec dbo._vsetMBSNotificationInterval @x
							exec dbo._vsetMBSHtmlEmailNotification 1
							"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "ATRAdminEmailAddress"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "ATRAcknowledgmentTitle")
						),
						new UpgradeStepSequence( // 1.0.6.23 - Change configuration variables for mainboss service
							new AddVariableUpgradeStep("MBSAssignmentNotificationSubjectPrefix"),
							new AddVariableUpgradeStep("MBSAssignmentNotificationIntroduction"),
							new AddVariableUpgradeStep("MBSRequestorNotificationSubjectPrefix"),
							new AddVariableUpgradeStep("MBSRequestorNotificationIntroduction"),
							new SqlUpgradeStep(@"
							declare @copystr as nvarchar(max)
							set @copystr = dbo._vgetATRAcknowledgmentFirstLine()
							exec dbo._vsetMBSRequestorNotificationIntroduction @copystr
							set @copystr = dbo._vgetATRAcknowledgmentSubjectPrefix()
							exec dbo._vsetMBSRequestorNotificationSubjectPrefix @copystr
							set @copystr = dbo._vgetMBSNotificationIntroduction()
							exec dbo._vsetMBSAssignmentNotificationIntroduction @copystr
							set @copystr = dbo._vgetMBSNotificationSubjectPrefix()
							exec dbo._vsetMBSAssignmentNotificationSubjectPrefix @copystr
							"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "ATRAcknowledgmentFirstLine"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "ATRAcknowledgmentSubjectPrefix"),
							new RemoveVariableUpgradeStep(GetOriginalSchema ,"MBSNotificationIntroduction"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "MBSNotificationSubjectPrefix")
						),
						new UpgradeStepSequence( // 1.0.6.24 - clear out _DDemandTemplate WorkOrderExpenseCategoryID fields (for existing 6.0 + databases - step was done in 1.0.5.18)
							new SqlUpgradeStep(@"update _DDemandTemplate SET WorkOrderExpenseCategoryID = null")
						),
						new UpgradeStepSequence( // 1.0.6.25 Add "MBSMainBossRemoteURL" variable for mainboss service use to create HTML links in emails to MainBossRemote web site
							new AddVariableUpgradeStep("MBSMainBossRemoteURL")
						),
						new UpgradeStepSequence( // 1.0.6.26 - Add EmailRequestID computed column to Request table
							new AddColumnUpgradeStep("Request.EmailRequestID"),
							new AddExtensionObjectUpgradeStep("mbfn_Request_EmailRequest"),
							new ChangeToComputedColumnUpgradeStep("Request.EmailRequestID", "[dbo].[mbfn_Request_EmailRequest]([ID])")
						),
						new UpgradeStepSequence( // 1.0.6.27 - Add scheduled-wo report with resource listings
							new AddTableUpgradeStep("ScheduledWorkOrderDetailedReport")
						),
						new UpgradeStepSequence( // 1.0.6.28 - Add WorkOrderAssignmentAll view to include implicit labor demands as workorderassignments
							new AddTableUpgradeStep("WorkOrderAssignmentAll")
						),
						new UpgradeStepSequence( // 1.0.6.29 - Speed up WorkOrderFormReport view.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						#endregion
						#region 1.0.6.30 - 1.0.6.39
						new UpgradeStepSequence( // 1.0.6.30 - Update WorkOrderAssignee statistics to account for implied assignment throught labor demands
							new RemoveTableUpgradeStep(GetOriginalSchema, "AttentionStatus"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderAssignee.WorkOrderAssigneeStatisticsID"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeStatistics"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentAll"),
							new AddTableUpgradeStep("WorkOrderAssignmentAll"),
							new AddTableUpgradeStep("WorkOrderAssigneeStatistics"),
							new AddTableUpgradeStep("WorkOrderAssigneeReport"),
							new AddColumnUpgradeStep("WorkOrderAssignee.WorkOrderAssigneeStatisticsID"),
							new ChangeToComputedColumnUpgradeStep("WorkOrderAssignee.WorkOrderAssigneeStatisticsID", "[ID]"),
							new AddTableUpgradeStep("AttentionStatus")
						),
						new UpgradeStepSequence( // 1.0.6.31 - Add WorkOrderAssignmentNotification table for mainboss service usage.
							new AddTableUpgradeStep("WorkOrderAssignmentNotification"),
							new SqlUpgradeStep(@"
								INSERT INTO WorkOrderAssignmentNotification (ID, WorkOrderAssigneeID, WorkOrderID, LastNotificationDate)
									select NEWID(), WorkOrderAssigneeID, WorkOrderID, LastNotificationDate from WorkOrderAssignment
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderAssignment.LastNotificationDate"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignmentNotification"),
							new AddTableUpgradeStep("AssignmentNotification")
						),
						new UpgradeStepSequence( // 1.0.6.32 - Clean up PurchaseOrderFormReport view.
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.6.33 - Add Maintenance Forecast report
							new AddTableUpgradeStep("MaintenanceForecastReport")
						),
						new UpgradeStepSequence( // 1.0.6.34 - Add xxxStateHistoryStatus tables and related fields in the xxxStateHistory tables
							new AddTableUpgradeStep("RequestStateHistoryStatus"),
							new AddColumnUpgradeStep("RequestStateHistory.RequestStateHistoryStatusID"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryStatus"),
							new AddColumnUpgradeStep("PurchaseOrderStateHistory.PurchaseOrderStateHistoryStatusID"),
							new AddTableUpgradeStep("WorkOrderStateHistoryStatus"),
							new AddColumnUpgradeStep("WorkOrderStateHistory.WorkOrderStateHistoryStatusID")
						),
						new UpgradeStepSequence( // 1.0.6.35 - Added separated address fields to UnitReport and all other views that depend on it for exporting purposes.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("UnitSparePartsReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("UnitReplacementScheduleReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderDetailedReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("ChargebackFormReport")
						),
						new UpgradeStepSequence( // 1.0.6.36 -- Add billable-requestor's address to chargeback reports.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new AddTableUpgradeStep("ChargebackFormReport")
						),
						new UpgradeStepSequence( // 1.0.6.37 - Add xxxStateHistoryStatus to reports.
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("ItemOnOrderReport"),
							new AddTableUpgradeStep("ItemReceivingReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.6.38 - Add xxxStateHistoryStatus to reports.
							new AddVariableUpgradeStep("ActiveFilterInterval"),
							new AddVariableUpgradeStep("ActiveFilterSinceDate"),
							new SqlUpgradeStep(@"
											declare @x datetime
											SET @x = dbo._INew(500,0,0,0,0)
											exec dbo._vsetActiveFilterInterval @x
							")
						),
						new UpgradeStepSequence( // 1.0.6.39 - Correct type and initial values for trigger-calculated fields ActualItemLocation.EffectiveMinimum and EffectiveMaximum; add Shortage field.
							new SqlUpgradeStep(@"
								UPDATE _DActualItemLocation SET EffectiveMinimum = 0, EffectiveMaximum = 0
							"),
							// Correct values in the actual records where child records have never been created
							new SqlUpgradeStep(@"
								UPDATE ActualItemLocation SET EffectiveMinimum = 0 WHERE EffectiveMinimum IS NULL
								UPDATE ActualItemLocation SET EffectiveMaximum = 0 WHERE EffectiveMaximum IS NULL
							"),
							// Set all the columns non-null.
							new SetColumnRequiredUpgradeStep("ActualItemLocation.EffectiveMinimum"),
							new SetColumnRequiredUpgradeStep("ActualItemLocation.EffectiveMaximum"),
							// Add the SHortage column to the schema
							new AddColumnUpgradeStep("ActualItemLocation.Shortage"),
							// Correct it in SQL to a calculated column
							new ChangeToComputedColumnUpgradeStep("ActualItemLocation.Shortage", "case when EffectiveMinimum > (OnHand + OnOrder - OnReserve) then EffectiveMinimum - (OnHand + OnOrder - OnReserve) else 0 end")
						),
						#endregion
						#region 1.0.6.40 - 1.0.6.49
						new UpgradeStepSequence( // 1.0.6.40 - Add GIS infomation to location
							new AddColumnUpgradeStep("Location.GISLocation"),
							new AddColumnUpgradeStep("Location.GISZoom")
						),
						new UpgradeStepSequence( // 1.0.6.41 - Add ItemRestocking view
							new AddTableUpgradeStep("ItemRestocking")
						),
						new UpgradeStepSequence( // 1.0.6.42 - Remove old RelativeLocationCodes left over from imports prior to 3.3
							new SqlUpgradeStep(@"
								if object_id('RelativeLocationCodes', 'U') is not null
									drop table RelativeLocationCodes;
								")
						),
						new UpgradeStepSequence( // 1.0.6.43 - Add the OrganizationName variable and initialize it to the current CompanyLocationID Code
							new AddVariableUpgradeStep("OrganizationName"),
							new SqlUpgradeStep(@"
									DECLARE @name NVARCHAR(max)
									DECLARE @LocationID uniqueidentifier
									SET @LocationID = dbo._vgetCompanyLocationID()
									select @name=PA.Code from PostalAddress as PA where PA.LocationID = @LocationID
									if @name is not null
										exec dbo._vsetOrganizationName @name
							")
						),
						new UpgradeStepSequence( // 1.0.6.44 - Allow ActualItemLocation.CostCenterID to change only if TotalCost is zero
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualItemLocation_Updates_Item"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualItemLocation_Updates_Item")
						),
						new UpgradeStepSequence( // 1.0.6.45 - Change WOXM Category Defaults to be stored as links to their corresponding WOXME records
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultItemExpenseModelEntryID"),
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultHourlyInsideExpenseModelEntryID"),
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultHourlyOutsideExpenseModelEntryID"),
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultPerJobInsideExpenseModelEntryID"),
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultPerJobOutsideExpenseModelEntryID"),
							new AddColumnUpgradeStep("WorkOrderExpenseModel.DefaultMiscellaneousExpenseModelEntryID"),
							new SqlUpgradeStep(@"
								update WorkOrderExpenseModel
									set DefaultItemExpenseModelEntryID = IE.ID,
										DefaultHourlyInsideExpenseModelEntryID = HIE.ID,
										DefaultHourlyOutsideExpenseModelEntryID = HOE.ID,
										DefaultPerJobInsideExpenseModelEntryID = PJIE.ID,
										DefaultPerJobOutsideExpenseModelEntryID = PJOE.ID,
										DefaultMiscellaneousExpenseModelEntryID = ME.ID
									from WorkOrderExpenseModel as WOXM
									left join WorkOrderExpenseModelEntry as IE on IE.WorkOrderExpenseModelID = WOXM.ID and IE.WorkOrderExpenseCategoryID = WOXM.DefaultItemExpenseCategoryID
									left join WorkOrderExpenseModelEntry as HIE on IE.WorkOrderExpenseModelID = WOXM.ID and HIE.WorkOrderExpenseCategoryID = WOXM.DefaultHourlyInsideExpenseCategoryID
									left join WorkOrderExpenseModelEntry as HOE on IE.WorkOrderExpenseModelID = WOXM.ID and HOE.WorkOrderExpenseCategoryID = WOXM.DefaultHourlyOutsideExpenseCategoryID
									left join WorkOrderExpenseModelEntry as PJIE on IE.WorkOrderExpenseModelID = WOXM.ID and PJIE.WorkOrderExpenseCategoryID = WOXM.DefaultPerJobInsideExpenseCategoryID
									left join WorkOrderExpenseModelEntry as PJOE on IE.WorkOrderExpenseModelID = WOXM.ID and PJOE.WorkOrderExpenseCategoryID = WOXM.DefaultPerJobOutsideExpenseCategoryID
									left join WorkOrderExpenseModelEntry as ME on IE.WorkOrderExpenseModelID = WOXM.ID and ME.WorkOrderExpenseCategoryID = WOXM.DefaultMiscellaneousExpenseCategoryID
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderExpenseModel.DefaultItemExpenseCategoryID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderExpenseModel.DefaultHourlyInsideExpenseCategoryID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderExpenseModel.DefaultHourlyOutsideExpenseCategoryID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderExpenseModel.DefaultPerJobInsideExpenseCategoryID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderExpenseModel.DefaultPerJobOutsideExpenseCategoryID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderExpenseModel.DefaultMiscellaneousExpenseCategoryID")
						),
						new UpgradeStepSequence( // 1.0.6.46 - Add Status Statistics reports
							new AddTableUpgradeStep("WorkOrderStatusStatisticsReport"),
 							new AddTableUpgradeStep("RequestStatusStatisticsReport"),
							new AddTableUpgradeStep("PurchaseOrderStatusStatisticsReport")
						),
						new UpgradeStepSequence( // 1.0.6.47 - Add PurchaseOrderStateHistory Report
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport")
						),
						new UpgradeStepSequence( // 1.0.6.48 - Add Accounting transaction report
							new AddTableUpgradeStep("AccountingTransactionReport")
						),
						new UpgradeStepSequence( // 1.0.6.49 - Add filtering fields to categorize WorkOrderExpenseCategory records
							new AddColumnUpgradeStep("WorkOrderExpenseCategory.FilterAsItem"),
							new AddColumnUpgradeStep("WorkOrderExpenseCategory.FilterAsLabor"),
							new AddColumnUpgradeStep("WorkOrderExpenseCategory.FilterAsMiscellaneous"),
							// All existing categories are set
							new SqlUpgradeStep(@"
									UPDATE WorkOrderExpenseCategory SET FilterAsItem = 1, FilterAsLabor = 1, FilterAsMiscellaneous = 1
									UPDATE _DWorkOrderExpenseCategory SET FilterAsItem = 0, FilterAsLabor = 0, FilterAsMiscellaneous = 0
							"),
							new SetColumnRequiredUpgradeStep("WorkOrderExpenseCategory.FilterAsItem"),
							new SetColumnRequiredUpgradeStep("WorkOrderExpenseCategory.FilterAsLabor"),
							new SetColumnRequiredUpgradeStep("WorkOrderExpenseCategory.FilterAsMiscellaneous")
						),
						#endregion
						#region 1.0.6.50 - 1.0.6.59
						new UpgradeStepSequence( // 1.0.6.50 - Add GenerateLeadTime to WorkOrderTemplate
							new AddColumnUpgradeStep("WorkOrderTemplate.GenerateLeadTime"),
							new AddColumnUpgradeStep("WorkOrderTemplate.MaxGenerateLeadTime"),
							// All existing categories are set
							new SqlUpgradeStep(@"
									declare @zerointerval datetime
									set @zerointerval = dbo._INew(0,0,0,0,0)
									UPDATE WorkOrderTemplate SET GenerateLeadTime = @zerointerval
									UPDATE _DWorkOrderTemplate SET GenerateLeadTime = @zerointerval
							"),
							new SetColumnRequiredUpgradeStep("WorkOrderTemplate.GenerateLeadTime"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrderTemplate_MaxGenerateLeadTime"),
							new ChangeToComputedColumnUpgradeStep("WorkOrderTemplate.MaxGenerateLeadTime", "[dbo].[mbfn_WorkOrderTemplate_MaxGenerateLeadTime]([ID])")
						),
						new UpgradeStepSequence( // 1.0.6.51 - Fix AccountingTransactionReport for case when ToCostCenter = FromCostCenter
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionReport"),
							new AddTableUpgradeStep("AccountingTransactionReport")
						),
						new UpgradeStepSequence( // 1.0.6.52 - Change AccountingTransactionReport references for items and stop cancelling out costs when splitting transaction record.
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionReport"),
							new AddTableUpgradeStep("AccountingTransactionReport")
						),
						new UpgradeStepSequence( // 1.0.6.53 - Show PO Lines in Outside Labor browsette in work orders
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOutsideTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOutside"),
							new AddTableUpgradeStep("WorkOrderOutside"),
							new AddTableUpgradeStep("WorkOrderOutsideTreeView")
						),
						new UpgradeStepSequence( // 1.0.6.54 - Break out the flags in WorkOrderState
							new AddColumnUpgradeStep("WorkOrderState.CanModifyActuals"),
							new AddColumnUpgradeStep("WorkOrderState.CanModifyPOLines"),
							new AddColumnUpgradeStep("WorkOrderState.CanModifyChargebackLines"),
							new SqlUpgradeStep(@"
								update WorkOrderState
									set CanModifyActuals = CanModifyNonDemands,
										CanModifyPOLines = CanModifyNonDemands,
										CanModifyChargebackLines = CanModifyChargebacks
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderState.CanModifyNonDemands"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.CanModifyActuals"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.CanModifyPOLines"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.CanModifyChargebackLines")
						),
						new UpgradeStepSequence( // 1.0.6.55 - Forbid Actualization and Chargeback Line editing in Draft mode
							new SqlUpgradeStep(@"
								update WorkOrderState
									set CanModifyActuals = 0, CanModifyChargebackLines = 0
									where Code = 'Draft'
							")
						),
						new UpgradeStepSequence( // 1.0.6.56 - Add variables to customize EACH of the 3 types of Assignment Notifications
							new RemoveVariableUpgradeStep(GetOriginalSchema, "MBSAssignmentNotificationIntroduction"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "MBSAssignmentNotificationSubjectPrefix"),
							new AddVariableUpgradeStep("MBSANRequestIntroduction"),
							new AddVariableUpgradeStep("MBSANRequestSubjectPrefix"),
							new AddVariableUpgradeStep("MBSANWorkOrderIntroduction"),
							new AddVariableUpgradeStep("MBSANWorkOrderSubjectPrefix"),
							new AddVariableUpgradeStep("MBSANPurchaseOrderIntroduction"),
							new AddVariableUpgradeStep("MBSANPurchaseOrderSubjectPrefix")
						),
						new UpgradeStepSequence( //1.0.6.57 - Fix OnReserve triggers in WorkOrderStateHistory/WorkOrderState, and update all existing OnReserve values in ActualItemLocation records
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderState_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderState_Updates_ActualItemLocation"),
							// Execute the code with the trigger disabled
							// then do what the trigger would have done using inlined versions of the functions in the trigger for speed
							new SqlUpgradeStep(@"
									DISABLE TRIGGER dbo.mbtg_ActualItemLocation_Updates_Item on dbo.ActualItemLocation;
									UPDATE ActualItemLocation set OnReserve = dbo.mbfn_ActualItemLocation_OnReserve(ActualItemLocation.ID);
									ENABLE TRIGGER dbo.mbtg_ActualItemLocation_Updates_Item on dbo.ActualItemLocation;
									update Item
										  set TotalCost =
			  								(select coalesce(SUM(ActualItemLocation.TotalCost), 0)
											   from ActualItemLocation
											   join ItemLocation on ItemLocation.ID = ActualItemLocation.ItemLocationID
											   where ItemLocation.ItemID = Item.Id)
												,
											  OnHand = 
											  (select coalesce(SUM(ActualItemLocation.OnHand), 0)
											   from ActualItemLocation
											   join ItemLocation on ItemLocation.ID = ActualItemLocation.ItemLocationID
											   where ItemLocation.ItemID = Item.Id)
											  ,
											  OnReserve = 
											  (select coalesce(SUM(ActualItemLocation.OnReserve), 0)
											   from ActualItemLocation
											   join ItemLocation on ItemLocation.ID = ActualItemLocation.ItemLocationID
											   where ItemLocation.ItemID = Item.Id)
											  ,
											  OnOrder = 
											  (select coalesce(SUM(ActualItemLocation.OnOrder), 0)
											   from ActualItemLocation
											   join ItemLocation on ItemLocation.ID = ActualItemLocation.ItemLocationID
											   where ItemLocation.ItemID = Item.Id)
											  ,
											  Available = (select coalesce(SUM(ActualItemLocation.Available), 0)
											   from ActualItemLocation
											   join ItemLocation on ItemLocation.ID = ActualItemLocation.ItemLocationID
											   where ItemLocation.ItemID = Item.Id)
									from Item
							")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.0.7.x Reserved for steps included in released versions of MB3.3
						#region 1.0.7.0 - 
						// MB 3.3.0 uses db version 1.0.7.0
						new UpgradeStepSequence( //1.0.7.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.3.0.6'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.3.0.6'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.3.0.6'")
						),
						// 1.0.7.1 grant the MainBoss role create function and create procedure permissions so non-db_owner users can execute upgrade steps that
						// create DB variables
						new SqlUpgradeStep(@"
							grant create function, create procedure to [MainBoss]
						"),
						#endregion
					},
					new UpgradeStep[] { // 1.0.8.x Reserved for development steps for MB3.4
						#region 1.0.8.0 - 1.0.8.9
						new UpgradeStepSequence( //1.0.8.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.0'")
						),
						new UpgradeStepSequence( //1.0.8.1 Add regular ID field and change XxxxID to be a linkage field in WO/PO/Req reports
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStatusStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStatusStatisticsReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderStatusStatisticsReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("ItemReceivingReport"),
							new AddTableUpgradeStep("ItemOnOrderReport")
						),
						new UpgradeStepSequence( //1.0.8.2 Add the DBServerVersion variable; we initially set it to 0.0.0.0 since we will determine later during DBServerVersion checking what it should be
							new AddVariableUpgradeStep("DBServerVersion"),
							new SqlUpgradeStep(@"exec dbo._vsetDBServerVersion '0.0.0.0'")
						),
						new UpgradeStepSequence( // 1.0.8.3 Change the From/To State field names in the Transition tables to be consistent with naming convention 
							new AddColumnUpgradeStep("RequestStateTransition.ToStateID"),
							new AddColumnUpgradeStep("RequestStateTransition.FromStateID"),
							new AddColumnUpgradeStep("WorkOrderStateTransition.ToStateID"),
							new AddColumnUpgradeStep("WorkOrderStateTransition.FromStateID"),
							new AddColumnUpgradeStep("PurchaseOrderStateTransition.ToStateID"),
							new AddColumnUpgradeStep("PurchaseOrderStateTransition.FromStateID"),
							new SqlUpgradeStep(@"
								UPDATE RequestStateTransition SET ToStateID = ToState from RequestStateTransition
								UPDATE RequestStateTransition SET FromStateID = FromState from RequestStateTransition
								UPDATE WorkOrderStateTransition SET ToStateID = ToState from WorkOrderStateTransition
								UPDATE WorkOrderStateTransition SET FromStateID = FromState from WorkOrderStateTransition
								UPDATE PurchaseOrderStateTransition SET ToStateID = ToState from PurchaseOrderStateTransition
								UPDATE PurchaseOrderStateTransition SET FromStateID = FromState from PurchaseOrderStateTransition
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "RequestStateTransition.ToState"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "RequestStateTransition.FromState"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderStateTransition.ToState"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderStateTransition.FromState"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderStateTransition.ToState"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderStateTransition.FromState")
						),
						new UpgradeStepSequence( // 1.0.8.4 Add ExternalTag to PermanentItemLocation for identifying storage locations
							new AddColumnUpgradeStep("PermanentItemLocation.ExternalTag"),
							new AddUniqueConstraintUpgradeStep("PermanentItemLocation.ExternalTag")
						),
						new UpgradeStepSequence( // 1.0.8.5 Add ExternalTag to RelativeLocation for identifying units (only)
							new AddColumnUpgradeStep("RelativeLocation.ExternalTag"),
							new AddUniqueConstraintUpgradeStep("RelativeLocation.ExternalTag")
						),
						new UpgradeStepSequence( // 1.0.8.6 Add ExternalTag View to fetch all records that we consider to be ExternalTag'd
							new AddTableUpgradeStep("ExternalTag")
						),
						new UpgradeStepSequence( // 1.0.8.7 Set Nullability on EntryDate column for Receipt (code cleanup)
							new RemoveUniqueConstraintUpgradeStep(() => { return GetPreviousSchema("1_0_8_6"); }, "Receipt.EntryDate"), // remove XID so column reference is removed for alter column
							new SetColumnRequiredUpgradeStep("Receipt.EntryDate"), // change nullability
							new AddUniqueConstraintUpgradeStep("Receipt.EntryDate") // re add XID
						),
						// 1.0.8.8 grant the MainBoss role create function and create procedure permissions so non-db_owner users can execute upgrade steps that
						// create DB variables
						new SqlUpgradeStep(@"
							grant create function, create procedure to [MainBoss]
						"),
						new UpgradeStepSequence( // 1.0.8.9 Add Relationship, et al. tables (then removed for renaming to better names)
						),
						#endregion
						#region 1.0.8.10 - 1.0.8.19
						new UpgradeStepSequence( // 1.0.8.10 Remove old relationship tables, introduce with new names
							new SqlUpgradeStep(@"
								if object_id('UnitRelationshipsContainment', 'V') is not null
									DROP VIEW UnitRelationshipsContainment;
								if object_id('UnitRelationships', 'V') is not null
									DROP VIEW UnitRelationships;
								if object_id('_DUnitContactRelationship', 'U') is not null
									DROP TABLE _DUnitContactRelationship;
								if object_id('UnitContactRelationship', 'U') is not null
									DROP TABLE UnitContactRelationship;
								if object_id('_DUnitUnitRelationship', 'U') is not null
									DROP TABLE _DUnitUnitRelationship;
								if object_id('UnitUnitRelationship', 'U') is not null
									DROP TABLE UnitUnitRelationship;
								if object_id('_DRelationship', 'U') is not null
									DROP TABLE _DRelationship;
								if object_id('Relationship', 'U') is not null
									DROP TABLE Relationship;
								if object_id('_DRelationshipRole', 'U') is not null
									DROP TABLE _DRelationshipRole;
								if object_id('RelationshipRole', 'U') is not null
									DROP TABLE RelationshipRole;
							"),
							new AddTableUpgradeStep("Relationship"),
							new AddTableUpgradeStep("RelatedRecord"),
							new AddTableUpgradeStep("UnitRelatedUnit"),
							new AddTableUpgradeStep("UnitRelatedContact"),
							new AddTableUpgradeStep("UnitRelatedRecords"),
							new AddTableUpgradeStep("UnitRelatedRecordsContainment")
						),
						new UpgradeStepSequence( // 1.0.8.11 Add in ContactRelatedRecord support
							new AddTableUpgradeStep("ContactRelatedRecords"),
							new AddTableUpgradeStep("ContactRelatedRecordsContainment")
						),
						new UpgradeStepSequence( // 1.0.8.12 Add PaymentTerm to Vendor record for default purchasing
							new RemoveTableUpgradeStep(GetOriginalSchema, "VendorReport"),
							new AddColumnUpgradeStep("Vendor.PaymentTermID"),
							new AddTableUpgradeStep("VendorReport")
						),
						new UpgradeStepSequence( // 1.0.8.13 Remove ItemReceivingReport view, Add ReceivingReport view
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReceivingReport"),
							new AddTableUpgradeStep("ReceivingReport")
						),
						new UpgradeStepSequence( // 1.0.8.14 Fix ContactRelatedRecord support
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRelatedRecordsContainment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRelatedRecords"),
							new AddTableUpgradeStep("ContactRelatedRecords"),
							new AddTableUpgradeStep("ContactRelatedRecordsContainment")
						),
						new UpgradeStepSequence( // 1.0.8.15 Add Desc and Comment field to Attachment
							new AddColumnUpgradeStep("Attachment.Desc"),
							new AddColumnUpgradeStep("Attachment.Comment"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Attachment_Summary"),
							new AddExtensionObjectUpgradeStep("mbfn_Attachment_Summary")
						),
						new UpgradeStepSequence( //1.0.8.16 Add regular WorkOrderExpenseModel to WorkOrderReport et al.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStatusStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("ChargeBackFormReport")
						),
						new UpgradeStepSequence( // 1.0.8.17 - update WorkOrderTemplate report to have the LeadTime field information
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateReport"),
							new AddTableUpgradeStep("WorkOrderTemplateReport"),
							new AddTableUpgradeStep("WorkOrderTemplateDetailedReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderDetailedReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport")
						),
						new UpgradeStepSequence( // 1.0.8.18 - add ExternalTagBarCode field to WorkOrderFormReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						new UpgradeStepSequence( //1.0.8.19 Add UnitExternalTag and BarCode to UnitReport et al.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("UnitReplacementScheduleReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStatusStatisticsReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("UnitSparePartsReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderDetailedReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStatusStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("ChargeBackFormReport")
						),
						#endregion
						#region 1.0.8.20 - 1.0.8.29
						new UpgradeStepSequence( // 1.0.8.20 - add ExternalTagBarCode field to WorkOrderFormReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "PermanentItemLocationReport"),
							new AddTableUpgradeStep("PermanentItemLocationReport")
						),
						new UpgradeStepSequence( // 1.0.8.21 - Move ContactID information to Relationship records and remove ContactID from Unit records (et al).
							// First create a Relationship of Unit to Contact IF any ContactID records have been set in the database.
							// Note that as of code compiled after Jan 13/2012, when upgrading from before 1.0.8.10, the Relationship table will be created using
							// a rolled-back schema based on information in dummies.xafdb. Due to simplification of the coding of the
							// "old" Relationship table in dummies.xafdb, our schema at this point may have XID="Hidden" xunique="true"
							// so we will only be able to put one record in the Relationship table, but that's all we *want* to do so this is not a problem.
							new SqlUpgradeStep(@"
								select cast(null as uniqueidentifier) as RelatedRecordID, RL.LocationID as UnitLocationID, Unit.ContactID as ContactID
									INTO NewUnitRelatedContact
									from Unit
										join RelativeLocation as RL on RL.Id = Unit.RelativeLocationID
										where Unit.ContactID is not null

								if EXISTS(select * from NewUnitRelatedContact) 
								begin
									DECLARE @RID AS UNIQUEIDENTIFIER
									SET @RID = NEWID()

									INSERT INTO [Relationship] (Id, Code, [Desc], BAsRelatedToAPhrase, AAsRelatedToBPhrase, AType, BType, ReverseID)
										VALUES (@RID, 'Unit Contact', 'Unit Contact Upgrade', 'is the Unit Contact for', 'has Unit Contact', 0, 1, NEWID())

									UPDATE NewUnitRelatedContact set RelatedRecordID = NEWID();
	
									INSERT INTO RelatedRecord (Id, RelationshipID ) select RelatedRecordID, @RID from NewUnitRelatedContact
	
									INSERT INTO UnitRelatedContact (Id, RelatedRecordID, UnitLocationID, ContactID) 
										select NEWID(), RelatedRecordID, UnitLocationID, ContactID from NewUnitRelatedContact
								end
								DROP table NewUnitRelatedContact
							"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Unit.ContactID"),
							new AddExtensionObjectUpgradeStep("mbfn_Relationship_Summary"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("UnitReplacementScheduleReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStatusStatisticsReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("UnitSparePartsReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderDetailedReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStatusStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("ChargeBackFormReport")
						),
						new UpgradeStepSequence( // 1.0.8.22 -- Update mbfn_Relationship_Summary for better formatting
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Relationship_Summary"),
							new AddExtensionObjectUpgradeStep("mbfn_Relationship_Summary")
						),
						new UpgradeStepSequence( // 1.0.8.23 -- Update ContactReport to include Relationship information
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new AddExtensionObjectUpgradeStep("mbfn_ContactRelationship_Summary"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("EmployeeReport"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeReport"),
							new AddTableUpgradeStep("RequestAssigneeReport"),
							new AddTableUpgradeStep("RequestorReport"),
							new AddTableUpgradeStep("BillableRequestorReport"),
							new AddTableUpgradeStep("WorkOrderAssigneeReport")
						),
						new UpgradeStepSequence( // 1.0.8.24 -- Fix xxxAssigneeProspect views to account for movement of ContactID from Unit to a Relationship
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeProspect"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeProspect"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("WorkOrderAssigneeProspect"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeProspect"),
							new AddTableUpgradeStep("RequestAssigneeProspect")
						),
						new UpgradeStepSequence( // 1.0.8.25 -- Add WorkOrderEndDateHistogram view to drive a test chart on the Status page.
							new AddTableUpgradeStep("WorkOrderEndDateHistogram")
						),
						new UpgradeStepSequence( // 1.0.8.26 -- Add ActiveRequestAgeHistogram view to drive a second chart on the Status page.
							new AddTableUpgradeStep("ActiveRequestAgeHistogram")
						),
						new UpgradeStepSequence( // 1.0.8.27 -- Correct ActiveRequestAgeHistogram view to exclude closed Requests from the counts.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("ActiveRequestAgeHistogram")
						),
						new UpgradeStepSequence( // 1.0.8.28 -- Correct date rounding, include zero-counts for today and tomorrow, add more views for charts
							new AddExtensionObjectUpgradeStep("mbfn_DateFromDateTime"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("ActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("WorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("AssignedWorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("AssignedActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("WorkOrderCountsByStatus"),
							new AddTableUpgradeStep("WorkOrderCountsByPriority"),
							new AddTableUpgradeStep("RequestCountsByStatus"),
							new AddTableUpgradeStep("RequestCountsByPriority")
						),
						new UpgradeStepSequence( // 1.0.8.29 -- Move from variable to table configuration for mainboss service
							new AddTableUpgradeStep("ServiceConfiguration"),
							new SqlUpgradeStep(@"
								UPDATE _DServiceConfiguration SET
									WakeUpInterval = dbo._INew(0,0,30,0,0),
									SMTPPort = 25,
									SMTPUseSSL = 0,
									SMTPCredentialType = 0, -- (sbyte)DatabaseEnums.SMTPCredentialType.ANONYMOUS,
									MailServerType = 0,  --(sbyte)DatabaseEnums.MailServerType.POP3
									MailPort = 110,	
									AutomaticallyCreateRequestors = 1,
									ReturnEmailDisplayName = 'MainBoss Service',
									RequestorNotificationSubjectPrefix = '@Re:',
									RequestorNotificationIntroduction = 'Current status of your work request',
									HtmlEmailNotification = 1,
									NotificationInterval = dbo._INew(0,0,10,0,0),
									ProcessNotificationEmail = 1,
									ProcessRequestorIncomingEmail = 1
							"),
							 // Always try to copy old service values into a new record, even if never used to preserve whatever a user may have set before
							 // in most cases, the upgrade step will be happening with the mainboss service uninstalled per our instructions and we want to preserve any configuration
							 // values they had set before.
							 // However, the new configuration record has some required fields, so we can't just assume we can create a record.
							new SqlUpgradeStep(@"
								IF      dbo._vgetATRMailServer() IS NOT NULL
									AND dbo._vgetATRSMTPServer() IS NOT NULL
								BEGIN
								INSERT INTO ServiceConfiguration (
									[ID], [Code], [Desc], ProcessNotificationEmail, ProcessRequestorIncomingEmail,
									MainBossRemoteURL,
									ANRequestIntroduction,
									ANRequestSubjectPrefix,
									ANWorkOrderIntroduction,
									ANWorkOrderSubjectPrefix,
									ANPurchaseOrderIntroduction,
									ANPurchaseOrderSubjectPrefix,
									HtmlEmailNotification,
									NotificationInterval,
									ReturnEmailDisplayName,
									ReturnEmailAddress,
									RequestorNotificationIntroduction,
									RequestorNotificationSubjectPrefix,
									AutomaticallyCreateRequestors,
									WakeUpInterval,
									MailServerType,
									MailServer,
									MailPort,
									MailUserName,
									MailboxName,
									MailEncryptedPassword,
									SMTPServer,
									SMTPPort,
									SMTPUseSSL,
									SMTPCredentialType,
									SMTPUserDomain,
									SMTPUserName,
									SMTPEncryptedPassword,
									ServiceMachineName,
									ServiceName,
									ServiceVersion
								)
								VALUES
								(
									NEWID(), 'MainBossService', 'Previous MainBoss Service Configuration', 1, 1, 
									dbo._vgetMBSMainBossRemoteURL(),
									dbo._vgetMBSANRequestIntroduction(),
									dbo._vgetMBSANRequestSubjectPrefix(),
									dbo._vgetMBSANWorkOrderIntroduction(),
									dbo._vgetMBSANWorkOrderSubjectPrefix(),
									dbo._vgetMBSANPurchaseOrderIntroduction(),
									dbo._vgetMBSANPurchaseOrderSubjectPrefix(),
									dbo._vgetMBSHtmlEmailNotification(),
									dbo._vgetMBSNotificationInterval(),
									dbo._vgetMBSReturnEmailDisplayName(),
									dbo._vgetMBSReturnEmailAddress(),
									dbo._vgetMBSRequestorNotificationIntroduction(),
									dbo._vgetMBSRequestorNotificationSubjectPrefix(),
									dbo._vgetATRAutomaticallyCreateRequestors(),
									dbo._vgetATRWakeUpInterval(),
									dbo._vgetATRMailServerType(),
									dbo._vgetATRMailServer(),
									dbo._vgetATRMailPort(),
									dbo._vgetATRMailUserName(),
									dbo._vgetATRMailboxName(),
									dbo._vgetATRMailEncryptedPassword(),
									dbo._vgetATRSMTPServer(),
									dbo._vgetATRSMTPPort(),
									dbo._vgetATRSMTPUseSSL(),
									dbo._vgetATRSMTPCredentialType(),
									dbo._vgetATRSMTPUserDomain(),
									dbo._vgetATRSMTPUserName(),
									dbo._vgetATRSMTPEncryptedPassword(),
									dbo._vgetATRServiceMachineName(),
									dbo._vgetATRServiceName(),
									dbo._vgetATRServiceVersion()
								)
								END
							")
						 ),
						#endregion
 						#region 1.0.8.30 - 1.0.8.39
						 new UpgradeStepSequence( // 1.0.8.30 -- Synchronize ItemActivityReport with ItemActivity
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivityReport"),
							new AddTableUpgradeStep("ItemActivityReport")
						),
						new UpgradeStepSequence( // 1.0.8.31 -- Cleanup up trigger referencing soon to be obsolete variable
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_NewRequestAcknowledgementStatus"),
							new AddExtensionObjectUpgradeStep("mbfn_ServiceRequestorNotificationsEnabled"),
							new AddExtensionObjectUpgradeStep("mbtg_NewRequestAcknowledgementStatus")
						),
						new UpgradeStepSequence( // 1.0.8.32 -- remove service variables no longer used
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSMainBossRemoteURL"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSANRequestIntroduction"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSANRequestSubjectPrefix"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSANWorkOrderIntroduction"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSANWorkOrderSubjectPrefix"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSANPurchaseOrderIntroduction"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSANPurchaseOrderSubjectPrefix"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSHtmlEmailNotification"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSNotificationInterval"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSReturnEmailDisplayName"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSReturnEmailAddress"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSRequestorNotificationIntroduction"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "MBSRequestorNotificationSubjectPrefix"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRAutomaticallyCreateRequestors"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRWakeUpInterval"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRMailServerType"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRMailServer"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRMailPort"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRMailUserName"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRMailboxName"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRMailEncryptedPassword"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPServer"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPPort"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPUseSSL"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPCredentialType"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPUserDomain"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPUserName"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRSMTPEncryptedPassword"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRServiceMachineName"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRServiceName"),
							new RemoveVariableUpgradeStep( GetOriginalSchema, "ATRServiceVersion")
						),
						new UpgradeStepSequence( // 1.0.8.33 -- Add organization wide default bar code symbology to use as default
							new AddVariableUpgradeStep("BarCodeSymbology"),
							new SqlUpgradeStep(@"
								exec dbo._vsetBarCodeSymbology 0
							")
						),
						new UpgradeStepSequence( // 1.0.8.34 -- replace ItemRestocking view with one that uses BINARY(32) row ids to avoid duplicate ids
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestocking"),
							new AddTableUpgradeStep("ItemRestocking")
						),
						new UpgradeStepSequence( // 1.0.8.35 -- Add WorkOrderID to TemporaryItemLocation to have editable XID 
							new AddColumnUpgradeStep("TemporaryItemLocation.WorkOrderID"),
							new SqlUpgradeStep(@"
								UPDATE TemporaryItemLocation SET WorkOrderID = WO.ID
									from TemporaryItemLocation as TIL
									join ActualItemLocation as AIL on AIL.Id = TIL.ActualItemLocationID
									join ItemLocation as IL on IL.Id = AIL.ItemLocationID
									join TemporaryStorage as TS on TS.LocationID = IL.LocationID
									join WorkOrder as WO on WO.Id = TS.WorkOrderID
							"),
							new SetColumnRequiredUpgradeStep("TemporaryItemLocation.WorkOrderID")
						),
						new UpgradeStepSequence( //1.0.8.36 -- Add AssignedWorkOrderCountsByStatus and Priority
							new AddTableUpgradeStep("AssignedWorkOrderCountsByStatus"),
							new AddTableUpgradeStep("AssignedWorkOrderCountsByPriority")
						),
						new UpgradeStepSequence( //1.0.8.37 -- Add AssignedRequestCountsByStatus and Priority
							new AddTableUpgradeStep("AssignedRequestCountsByStatus"),
							new AddTableUpgradeStep("AssignedRequestCountsByPriority")
						),
						new UpgradeStepSequence( // 1.0.8.38 -- Fix InProgress (With Comment) Operation Key in database to match Key in resource files
							new SqlUpgradeStep(@"
								UPDATE RequestStateTransition SET Operation = 'In Progress (With Comment)' where Operation = 'InProgress (With Comment)'
							")
						),
						new UpgradeStepSequence( // 1.0.8.39 -- Make Relationship records outright deletable
							//new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "Relationship.Hidden"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Relationship.Hidden"),
							new AddUniqueConstraintUpgradeStep("Relationship.Code")
						),
						#endregion
 						#region 1.0.8.40 - 1.0.8.44
						new UpgradeStepSequence( // 1.0.8.40 -- Add field to RequestAcknowledgementStatus for error recording
							new AddColumnUpgradeStep("RequestAcknowledgementStatus.LastAcknowledgementError"),
							// Update the AcknowledgementStatus trigger to new functionality
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_NewRequestAcknowledgementStatus"),
							new AddExtensionObjectUpgradeStep("mbtg_Request_Updates_RequestAcknowledgementStatus")
						),
						new UpgradeStepSequence( //1.0.8.41  -- Update database application requirements
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.6'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.6'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.6'")
						),
						new CreateAllColumnIndexesUpgradeStep(),	// 1.0.8.42 -- Add indices for linkage columns that were in a multi-column unique constraint
						new UpgradeStepSequence( //1.0.8.43  -- Update database application requirements
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.8'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.8'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.8'")
						),
						new UpgradeStepSequence( // 1.0.8.44 -- Roll RelatedRecord table into its derived tables
							// Drop all the views
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitRelatedRecordsContainment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitRelatedRecords"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRelatedRecordsContainment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactRelatedRecords"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ContactRelationship_Summary"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Relationship_Summary"),
							// Actually do the upgrade
							new AddColumnUpgradeStep("UnitRelatedUnit.RelationshipID"),
							new AddColumnUpgradeStep("UnitRelatedContact.RelationshipID"),
							new SqlUpgradeStep(@"
								update UnitRelatedUnit
									set RelationshipID = RR.RelationshipID
									from
										UnitRelatedUnit
										join RelatedRecord as RR on RR.ID = UnitRelatedUnit.RelatedRecordID
								update _DUnitRelatedUnit
									set RelationshipID = RR.RelationshipID
									from
										_DUnitRelatedUnit
										join _DRelatedRecord as RR on RR.ID = _DUnitRelatedUnit.RelatedRecordID
								update UnitRelatedContact
									set RelationshipID = RR.RelationshipID
									from
										UnitRelatedContact
										join RelatedRecord as RR on RR.ID = UnitRelatedContact.RelatedRecordID
								update _DUnitRelatedContact
									set RelationshipID = RR.RelationshipID
									from
										_DUnitRelatedContact
										join _DRelatedRecord as RR on RR.ID = _DUnitRelatedContact.RelatedRecordID
							"),
							new SetColumnRequiredUpgradeStep("UnitRelatedUnit.RelationshipID"),
							new SetColumnRequiredUpgradeStep("UnitRelatedContact.RelationshipID"),
							new AddUniqueConstraintUpgradeStep("UnitRelatedUnit.RelationshipID"),
							new AddUniqueConstraintUpgradeStep("UnitRelatedContact.RelationshipID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "UnitRelatedUnit.RelatedRecordID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "UnitRelatedContact.RelatedRecordID"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RelatedRecord"),
#if LATER_InlineOldSchemas
							new RemoveColumnUpgradeStep(OldSchema(@"
										<table name='RelatedRecord'>
											<field name='ReverseID' type='link(nonnull)' link='RelatedRecord'>
										</table>"), "Relationship.ReverseID"),
#else
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Relationship.ReverseID"),
#endif
							// Add all the views again
							new AddExtensionObjectUpgradeStep("mbfn_Relationship_Summary"),
							new AddExtensionObjectUpgradeStep("mbfn_ContactRelationship_Summary"),
							new AddTableUpgradeStep("ContactRelatedRecords"),
							new AddTableUpgradeStep("ContactRelatedRecordsContainment"),
							new AddTableUpgradeStep("UnitRelatedRecords"),
							new AddTableUpgradeStep("UnitRelatedRecordsContainment"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStatusStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStatisticsReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderOverdueReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("LaborOutsideHistoryReport"),
							new AddTableUpgradeStep("LaborInsideHistoryReport"),
							new AddTableUpgradeStep("ItemHistoryReport"),
							new AddTableUpgradeStep("UnitReplacementScheduleReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestStatusStatisticsReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderDetailedReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("UnitSparePartsReport"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("WorkOrderAssigneeReport"),
							new AddTableUpgradeStep("BillableRequestorReport"),
							new AddTableUpgradeStep("RequestorReport"),
							new AddTableUpgradeStep("RequestAssigneeReport"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeReport"),
							new AddTableUpgradeStep("EmployeeReport")
						)
					},
						#endregion
					new UpgradeStep[] { // 1.0.9.x Reserved for steps included in released versions of MB3.4
						#region 1.0.9.0 - 1.0.9.9
						// MB 3.4.0.10 uses db version 1.0.9.0
						new UpgradeStepSequence( //1.0.9.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.10'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.10'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.10'")
						),
						new UpgradeStepSequence( // 1.0.9.1 Restructuring of ItemLocation queries to improve performance
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMGenerationDetailAndScheduledWorkOrderAndLocation"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemporaryStorageTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemporaryStorage"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateStorage"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateStorageTreeView"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationAndContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationContainment"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationDerivationsAndItemLocations"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationDerivations"),
							new AddTableUpgradeStep("LocationDerivations"),
							new AddTableUpgradeStep("LocationDerivationsAndItemLocations"),
							new AddTableUpgradeStep("ItemLocationContainment"),
							new AddTableUpgradeStep("ItemLocationAndContainers"),
							new AddTableUpgradeStep("LocationAndContainers"),
							new AddTableUpgradeStep("LocationReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderTemplateStorage"),
							new AddTableUpgradeStep("WorkOrderTemplateStorageTreeView"),
							new AddTableUpgradeStep("WorkOrderTemporaryStorage"),
							new AddTableUpgradeStep("WorkOrderTemporaryStorageTreeView"),
							new AddTableUpgradeStep("PMGenerationDetailAndScheduledWorkOrderAndLocation"),
							new AddTableUpgradeStep("PMGenerationDetailAndContainers")
						),
						new UpgradeStepSequence( // 1.0.9.2 Fix ServiceConfiguration to track username for service and update DB with a trigger
							new AddColumnUpgradeStep( "ServiceConfiguration.ServiceAccountName"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ServiceVersion"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						// MB 3.4.0.13 uses db version 1.0.9.3
						new UpgradeStepSequence( //1.0.9.3
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.13'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.13'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.13'")
						),
						new UpgradeStepSequence( // 1.0.9.4 Update MaintainSecurity triggers and procedure to handle multiple updates.
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						// MB 3.4.0.14 uses db version 1.0.9.5
						new UpgradeStepSequence( //1.0.9.5
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.14'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.14'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.14'")
						),
						new UpgradeStepSequence( // 1.0.9.6 Update MaintainSecurity again! to limit to specific SIDs matched against UserNames
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						// MB 3.4.0.15 uses db version 1.0.9.7
						new UpgradeStepSequence( //1.0.9.7
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.15'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.15'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.15'")
						),
						new UpgradeStepSequence( // 1.0.9.8 Update MaintainSecurity again! to use the db user name in the add-role-membership call
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						new UpgradeStepSequence( // 1.0.9.9 Update maintenance forecast reports to properly merge PMGenerationDetail records that will result in a "shared work order"
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport")
						),
						#endregion
						#region 1.0.9.10 -
						// MB 3.4.0.16 uses db version 1.0.9.10
						new UpgradeStepSequence( //1.0.9.10
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.4.0.16'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.4.0.16'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.4.0.16'")
						),
						new UpgradeStepSequence( // 1.0.9.11 Redefine trigger on ItemCountValueVoid to not update every PermanentItemLocation in the database.
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl")
						),
						new UpgradeStepSequence( // 1.0.9.12 A dummy, not required upgrade step to allow rightset.xml to be processed for (vault version) 12009 changes to be enacted
							// This empty step will allow a user to use the database upgrade step to force the database table rights to be reprocessed,
							// but we didn't change the MBMinMBAppVersion values, nor the MBMinDBVersion value in the app to force this upgrade.
							// The user can be directed to do the upgrade if they observe the problem fixed by 12009 (W20120132: Costs still appear when in WorkOrder Role (but no Accounting Role))
							),
						new UpgradeStepSequence( // 1.0.9.13 -- replace ItemRestocking view with one that does not offer ILs where OnHand < 0 as possible sources for transfers.
							// Note that the OriginalSchema in the dummies file does not have the proper type for the ID field, but no upgrade steps care.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestocking"),
							new AddTableUpgradeStep("ItemRestocking")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.0.10.x Reserved for development for MB 3.5
						#region 1.0.10.0 - 1.0.10.9
						// MB 3.5.0 with db version 1.0.10.0
						new UpgradeStepSequence( //1.0.10.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '3.5.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '3.5.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '3.5.0.0'")
						),
						new UpgradeStepSequence( // 1.0.10.1 replace PurchaseOrderFormReport view with a simpler one not dependent on PurchaseOrderReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.10.2 replace PurchaseOrderFormReport view with a simpler one not dependent on PurchaseOrderReport (again)
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport")
						),
						new UpgradeStepSequence( // 1.0.10.3 Change types used in License table to translationkey
							new ChangeColumnUpgradeStep(() => {return dsLicense_1_0_10_3.Schema;}, "License.ExpiryModelName"),
							new ChangeColumnUpgradeStep(() => {return dsLicense_1_0_10_3.Schema;}, "License.LicenseModelName"),
							new SqlUpgradeStep(@"
								update License SET ExpiryModelName  = 'Thinkage.Libraries§' + ExpiryModelName from License where LEFT(ExpiryModelName,19) <> 'Thinkage.Libraries§'
								update License SET LicenseModelName  = 'Thinkage.Libraries§' + LicenseModelName from License where LEFT(LicenseModelName,19) <> 'Thinkage.Libraries§'
							")
						),
						new UpgradeStepSequence( // 1.0.10.4 Change types used in StateTransition tables to translationkey
							new ChangeColumnUpgradeStep(GetOriginalSchema, "WorkOrderStateTransition.Operation"),
							new ChangeColumnUpgradeStep(GetOriginalSchema, "WorkOrderStateTransition.OperationHint"),
							new ChangeColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderStateTransition.Operation"),
							new ChangeColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderStateTransition.OperationHint"),
							new ChangeColumnUpgradeStep(GetOriginalSchema, "RequestStateTransition.Operation"),
							new ChangeColumnUpgradeStep(GetOriginalSchema, "RequestStateTransition.OperationHint"),
							new SqlUpgradeStep(@"
								update WorkOrderStateTransition SET Operation  = 'Thinkage.MainBoss.Database§' + Operation from WorkOrderStateTransition where LEFT(Operation,27) <> 'Thinkage.MainBoss.Database§'
								update PurchaseOrderStateTransition SET Operation  = 'Thinkage.MainBoss.Database§' + Operation from PurchaseOrderStateTransition where LEFT(Operation,27) <> 'Thinkage.MainBoss.Database§'
								update RequestStateTransition SET Operation  = 'Thinkage.MainBoss.Database§' + Operation from RequestStateTransition where LEFT(Operation,27) <> 'Thinkage.MainBoss.Database§'
								update WorkOrderStateTransition SET OperationHint  = 'Thinkage.MainBoss.Database§' + OperationHint from WorkOrderStateTransition where LEFT(OperationHint,27) <> 'Thinkage.MainBoss.Database§'
								update PurchaseOrderStateTransition SET OperationHint  = 'Thinkage.MainBoss.Database§' + OperationHint from PurchaseOrderStateTransition where LEFT(OperationHint,27) <> 'Thinkage.MainBoss.Database§'
								update RequestStateTransition SET OperationHint  = 'Thinkage.MainBoss.Database§' + OperationHint from RequestStateTransition where LEFT(OperationHint,27) <> 'Thinkage.MainBoss.Database§'
							")
						 ),
						new UpgradeStepSequence( // 1.0.10.5 Remove CultureInfo field from Contact, Add PreferredLanguage field
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Contact.CultureInfo"),
							new AddColumnUpgradeStep("Contact.PreferredLanguage")
						),
						new UpgradeStepSequence( // 1.0.10.6 Add UserMessageKey and UserMessageTranslation tables (contents to come later)
							new AddTableUpgradeStep("UserMessageKey"),
							new AddTableUpgradeStep("UserMessageTranslation")
						),
						new UpgradeStepSequence( // 1.0.10.7 UserMessageKeyID is now a RequiredGUIDREF
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "UserMessageTranslation.UserMessageKeyID"),
							new SetColumnRequiredUpgradeStep("UserMessageTranslation.UserMessageKeyID"),
							new AddUniqueConstraintUpgradeStep("UserMessageTranslation.UserMessageKeyID")
						),
						new UpgradeStepSequence( // 1.0.10.8 Add ActiveRequestor for counting non-hidden requestor/Contact combinations for licensing & the EmailRequest PreferredLanguage
							new AddTableUpgradeStep("ActiveRequestor"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactWithRequestor"),
							new AddColumnUpgradeStep("EmailRequest.PreferredLanguage")
						),
						new UpgradeStepSequence( // 1.0.10.9 Convert EmailRequest contents to EmailRequest and EmailPart format
							new AddTableUpgradeStep("EmailPart"),
							new AddColumnUpgradeStep("EmailRequest.RequestorEmailAddress"),
							new AddColumnUpgradeStep("EmailRequest.RequestorEmailDisplayName"),
							new AddColumnUpgradeStep("EmailRequest.MailHeader"),
							new EmailMessageToPartsUpgradeStep(),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "EmailRequest.FromEmailAddress"),
							new SetColumnRequiredUpgradeStep("EmailRequest.RequestorEmailAddress"),
							new SetColumnRequiredUpgradeStep("EmailRequest.MailHeader")
						),
						#endregion
						#region 1.0.10.10 - 1.0.10.19
						new UpgradeStepSequence( // 1.0.10.10 Build the MainBoss Service UserMessageKey and translations
							// Add all the UserMessageTranslation strings and keys for the MainBoss Service
							GetServiceMessageStep("RequestorNotificationIntroduction"),
							GetServiceMessageStep("RequestorNotificationSubjectPrefix"),
							GetServiceMessageStep("ANRequestIntroduction"),
							GetServiceMessageStep("ANRequestSubjectPrefix"),
							GetServiceMessageStep("ANWorkOrderIntroduction"),
							GetServiceMessageStep("ANWorkOrderSubjectPrefix"),
							GetServiceMessageStep("ANPurchaseOrderIntroduction"),
							GetServiceMessageStep("ANPurchaseOrderSubjectPrefix"),
							new SqlUpgradeStep(@"
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'EstimatedCompletionDate', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'EstimatedCompletionDate_Comment', 'MainBossService')
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'ReferenceWorkRequest', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'ReferenceWorkRequest_Comment', 'MainBossService')
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'RequestAddCommentPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'RequestAddCommentPreamble_Comment', 'MainBossService')
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'OriginalRequestPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'OriginalRequestPreamble_Comment', 'MainBossService')
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'RequestURLPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'RequestURLPreamble_Comment', 'MainBossService')
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'WorkOrderURLPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'WorkOrderURLPreamble_Comment', 'MainBossService')
INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
	VALUES (NEWID(), 'PurchaseOrderURLPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'PurchaseOrderURLPreamble_Comment', 'MainBossService')
")
						),
						new UpgradeStepSequence( // 1.0.10.11 Add the French and Spanish language MainBoss Service UserMessageKey and translations
							new SqlUpgradeStep(@"
	INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
		VALUES (NEWID(), 'RequestDeniedPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'RequestDeniedPreamble_Comment', 'MainBossService')
	INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
		VALUES (NEWID(), 'RequestorInitialCommentPreamble', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'RequestorInitialCommentPreamble_Comment', 'MainBossService')
	"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ANRequestIntroduction"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ANRequestSubjectPrefix"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ANWorkOrderIntroduction"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ANWorkOrderSubjectPrefix"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ANPurchaseOrderIntroduction"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.ANPurchaseOrderSubjectPrefix"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.RequestorNotificationIntroduction"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.RequestorNotificationSubjectPrefix")
						),
						new UpgradeStepSequence( // 1.0.10.12 Change nullability of UserMessageTranslation.LanguageLCID field to required
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "UserMessageTranslation.UserMessageKeyID"),
							new SqlUpgradeStep(@"UPDATE UserMessageTranslation SET LanguageLCID = 127 where LanguageLCID IS NULL"),
							new SetColumnRequiredUpgradeStep("UserMessageTranslation.LanguageLCID"),
							new AddUniqueConstraintUpgradeStep("UserMessageTranslation.UserMessageKeyID")
						),
						new UpgradeStepSequence( // 1.0.10.13 Add the HiddenFeatures variable to save customization of forms
							new AddVariableUpgradeStep("HiddenFeatures")
						),
						new UpgradeStepSequence( // 1.0.10.14 adds a separate font size for fixed-width report fonts
							new AddVariableUpgradeStep("ReportFontSizeFixedWidth"),
							new SqlUpgradeStep(@"declare @junk int
												set @junk = dbo._vgetReportFontSize()
												exec dbo._vsetReportFontSizeFixedWidth @junk ")
						),
						new UpgradeStepSequence( // 1.0.10.15 add support for translatable CommentToRequestor for auto closing requests
							new AddExtensionObjectUpgradeStep("mbfn_UserMessageTranslate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_RequestedWorkOrder_Sets_RequestState"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_Sets_RequestState"),
							new SqlUpgradeStep(@"
					INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
						VALUES (NEWID(), 'CommentToRequestorWhenNewRequestedWorkOrder', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'CommentToRequestorWhenNewRequestedWorkOrder_Comment', 'RequestClosePreference')
					INSERT INTO UserMessageTranslation (Id, [UserMessageKeyID], [LanguageLCID], [Translation])
						SELECT TOP 1 NEWID(), UMK.ID, 127, dbo._vgetCommentToRequestorWhenNewRequestedWorkOrder()
									FROM UserMessageKey as UMK
									WHERE UMK.[KEY] = 'CommentToRequestorWhenNewRequestedWorkOrder'

					INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
						VALUES (NEWID(), 'CommentToRequestorWhenWorkOrderCloses', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'CommentToRequestorWhenWorkOrderCloses_Comment', 'RequestClosePreference')
					INSERT INTO UserMessageTranslation (Id, [UserMessageKeyID], [LanguageLCID], [Translation])
						SELECT TOP 1 NEWID(), UMK.ID, 127, dbo._vgetCommentToRequestorWhenWorkOrderCloses()
									FROM UserMessageKey as UMK
									WHERE UMK.[KEY] = 'CommentToRequestorWhenWorkOrderCloses'
						"),
						new RemoveVariableUpgradeStep(GetOriginalSchema, "CommentToRequestorWhenNewRequestedWorkOrder"),
						new RemoveVariableUpgradeStep(GetOriginalSchema, "CommentToRequestorWhenWorkOrderCloses")
						),
						new UpgradeStepSequence( // 1.0.10.16 add translation keys to Role table 
							new AddColumnUpgradeStep("Role.RoleName"),
							new AddColumnUpgradeStep("Role.RoleDesc"),
							new AddColumnUpgradeStep("Role.RoleComment"),
							new SqlUpgradeStep(@"
								UPDATE [ROLE] SET RoleName = 'Thinkage.MainBoss.Database' + NCHAR(167) + R.Code + '_Name',
												RoleDesc = 'Thinkage.MainBoss.Database' + NCHAR(167) + R.Code + '_Desc',
												RoleComment = 'Thinkage.MainBoss.Database' + NCHAR(167) + R.Code + '_Comment'
									FROM [ROLE] AS R where R.RoleName IS NULL
							"),
							new SetColumnRequiredUpgradeStep("Role.RoleName"),
							new SetColumnRequiredUpgradeStep("Role.RoleDesc"),
							new SetColumnRequiredUpgradeStep("Role.RoleComment"),
							new AddColumnUpgradeStep( "User.Desc" ),
							new AddColumnUpgradeStep( "User.Comment"),
							new SqlUpgradeStep(@"
								DISABLE TRIGGER dbo.mbtg_PreventDeletionOfLastUser on dbo.[User];
								UPDATE [USER] SET [DESC] = P.[DESC], [COMMENT] = P.[COMMENT] FROM [PRINCIPAL] AS P JOIN [USER] AS U on [U].PRINCIPALID = P.ID;
								ENABLE TRIGGER dbo.mbtg_PreventDeletionOfLastUser on dbo.[User];
							"),
							new RemoveColumnUpgradeStep(() => { return dsPermission_1_0_4_14_To_1_0_10_15.Schema; }, "Principal.Desc"),
							new RemoveColumnUpgradeStep(() => { return dsPermission_1_0_4_14_To_1_0_10_15.Schema;}, "Principal.Comment")
						),
						new UpgradeStepSequence( // 1.0.10.17 Fix UserReport views et al for changes in Role/User tables
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserReport"),
							new AddTableUpgradeStep("UserReport"),
							new AddTableUpgradeStep("RoleReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("RequestStatusStatisticsReport"),
							new AddTableUpgradeStep("AccountingTransactionReport")
						),
						new UpgradeStepSequence( // 1.0.10.18 Transition from nvarchar(max) to nvarchar(512) for translationkey types stored
							// RoleReport needs redefining with new type of translationkey
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new AddTableUpgradeStep("RoleReport"),
							new SqlUpgradeStep(@"
alter table PurchaseOrderStateTransition alter column [Operation] nvarchar(512) not null
alter table PurchaseOrderStateTransition alter column [OperationHint] nvarchar(512) not null
alter table RequestStateTransition alter column [Operation] nvarchar(512) not null
alter table RequestStateTransition alter column [OperationHint] nvarchar(512) not null
alter table Role alter column [RoleName] nvarchar(512) not null
alter table Role alter column [RoleDesc] nvarchar(512) not null
alter table Role alter column [RoleComment] nvarchar(512) not null
alter table _DRole alter column [RoleName] nvarchar(512) 
alter table _DRole alter column [RoleDesc] nvarchar(512) 
alter table _DRole alter column [RoleComment] nvarchar(512)
alter table UserMessageKey alter column [Comment] nvarchar(512)
alter table WorkOrderStateTransition alter column [Operation] nvarchar(512) not null
alter table WorkOrderStateTransition alter column [OperationHint] nvarchar(512) not null
alter table License alter column [ExpiryModelName] nvarchar(512) not null
alter table License alter column [LicenseModelName] nvarchar(512) not null
						")
						),
						new UpgradeStepSequence( // 1.0.10.19 Transition to translationkey for xxxState Code/Desc fields
								new ChangeColumnUpgradeStep(GetOriginalSchema, "WorkOrderState.Code"),
								new ChangeColumnUpgradeStep(GetOriginalSchema, "WorkOrderState.Desc"),
								new ChangeColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderState.Code"),
								new ChangeColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderState.Desc"),
								new ChangeColumnUpgradeStep(GetOriginalSchema, "RequestState.Code"),
								new ChangeColumnUpgradeStep(GetOriginalSchema, "RequestState.Desc"),
								new SqlUpgradeStep(@"
									update WorkOrderState SET Code  = 'Thinkage.MainBoss.Database.StateCode§' + Code from WorkOrderState where LEFT(Code,37) <> 'Thinkage.MainBoss.Database.StateCode§'
									update PurchaseOrderState SET Code  = 'Thinkage.MainBoss.Database.StateCode§' + Code from PurchaseOrderState where LEFT(Code,37) <> 'Thinkage.MainBoss.Database.StateCode§'
									update RequestState SET Code  = 'Thinkage.MainBoss.Database.StateCode§' + Code from RequestState where LEFT(Code,37) <> 'Thinkage.MainBoss.Database.StateCode§'
									update WorkOrderState SET [Desc]  = 'Thinkage.MainBoss.Database.StateCode§' + [Desc] from WorkOrderState where LEFT([Desc],37) <> 'Thinkage.MainBoss.Database.StateCode§'
									update PurchaseOrderState SET [Desc]  = 'Thinkage.MainBoss.Database.StateCode§' + [Desc] from PurchaseOrderState where LEFT([Desc],37) <> 'Thinkage.MainBoss.Database.StateCode§'
									update RequestState SET [Desc]  = 'Thinkage.MainBoss.Database.StateCode§' + [Desc] from RequestState where LEFT([Desc],37) <> 'Thinkage.MainBoss.Database.StateCode§'
								")
						),
						#endregion
						#region 1.0.10.20 - 1.0.10.29
						new UpgradeStepSequence( // 1.0.10.20 Allow empty strings to be in UserMessageTranslation table (as opposed to null)
							// Because the ChangeColumnUpgradeStep will try to set the nonnull attribute on the column, we need to do the upgrade step to set all current NULL values to empty strings first
							// At some point, the SetColumnRequiredUpgradeStep and ChangeColumnUpgradeStep will coordinate the nullability of the schema on rollback/rollforward consistently and these steps
							// would appear in a logical order.
							new SqlUpgradeStep(@"
								UPDATE UserMessageTranslation SET Translation = '' WHERE Translation IS NULL
							"),
							new ChangeColumnUpgradeStep(GetOriginalSchema, "UserMessageTranslation.Translation"),
							new SetColumnRequiredUpgradeStep("UserMessageTranslation.Translation")
						),
						new UpgradeStepSequence( // 1.0.10.21 Remove trigger that prevents deletion of last user
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_PreventDeletionOfLastUser")
						),
						new UpgradeStepSequence( // 1.0.10.22 -- replace ItemRestocking view with one that does not offer ILs where OnHand < 0 as possible sources for transfers.
							// Note that the OriginalSchema in the dummies file does not have the proper type for the ID field, but no upgrade steps care.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestocking"),
							new AddTableUpgradeStep("ItemRestocking")
						),
						new UpgradeStepSequence( // 1.0.10.23 -- Set all the XxxxState.FilterAsYyyy to be required fields.
							new SetColumnRequiredUpgradeStep("RequestState.FilterAsNew"),
							new SetColumnRequiredUpgradeStep("RequestState.FilterAsInProgress"),
							new SetColumnRequiredUpgradeStep("RequestState.FilterAsClosed"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.FilterAsDraft"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.FilterAsIssued"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.FilterAsClosed"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderState.FilterAsVoid"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.FilterAsOpen"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.FilterAsClosed"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.FilterAsVoid"),
							new SetColumnRequiredUpgradeStep("WorkOrderState.FilterAsDraft")
						),
						new UpgradeStepSequence( // 1.0.10.24 -- Add a Status to the BackupFileName table
							new AddColumnUpgradeStep("BackupFileName.Message"),
							new AddColumnUpgradeStep("BackupFileName.DatabaseVersion"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "DatabaseStatus"),
							new AddTableUpgradeStep("DatabaseStatus")
						),
						new UpgradeStepSequence( // 1.0.10.25 -- Add RequestAssignmentAssistant view
							new AddTableUpgradeStep("RequestAssignmentAssistant")
						),
						new UpgradeStepSequence( // 1.0.10.26 -- Add RequestAssignmentAssistant view
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentAssistant"),
							new AddTableUpgradeStep("RequestAssignmentByAssignee")
						),
						new UpgradeStepSequence( // 1.0.10.27 -- Remove unused fields in RequestState
							new RemoveColumnUpgradeStep(GetOriginalSchema, "RequestState.UnitRequired"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "RequestState.WorkOrderAllowed")
						),
						new UpgradeStepSequence( // 1.0.10.28 -- Add the new ServiceLog table to store logging messages.
							new AddTableUpgradeStep( "ServiceLog" )
						),
						new UpgradeStepSequence( // 1.0.10.29 -- Update ServiceLog with EntryType and EntryVersion columns
							new AddColumnUpgradeStep( "ServiceLog.EntryType" ),
							// Add values to ServiceLog Entry type j.i.c. there are rows already present
							// The EntryVersion is a special typed column treated as a server hosted value; we need to add it with a Sql Step (for now)
							new SqlUpgradeStep(@"UPDATE ServiceLog SET EntryType = 1"),
							new SetColumnRequiredUpgradeStep("ServiceLog.EntryType"),
							new AddColumnUpgradeStep( "ServiceLog.EntryVersion" ),
							new SetServerHostedColumnDefinitionUpgradeStep("ServiceLog.EntryVersion", "rowversion")
						),
						#endregion
						#region 1.0.10.30 - 1.0.10.39
						new UpgradeStepSequence( // 1.0.10.30 -- Add a CustomRole table for user defined security roles
							new AddTableUpgradeStep("CustomRole")
							// TODO: Subsequent Upgrade step needs to migrate any hand coded User custom roles from Role table to CustomRole table (use same ID values..)
						),
						new UpgradeStepSequence( // 1.0.10.31 -- Change UserRole to reference PrincipalID not RoleID
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "UserRole.UserID"),
							new AddColumnUpgradeStep( "UserRole.PrincipalID" ),
							new SqlUpgradeStep("UPDATE UserRole SET PrincipalID = [Role].PrincipalID from UserRole join [Role] on [Role].ID = UserRole.RoleID"),
							new SetColumnRequiredUpgradeStep("UserRole.PrincipalID"),
							// The following 3 steps deal with changing the Unique constraint from "UserID RoleID" to "UserID PrincipalID"
							// and are necessary because the RemoveColumnUpgradeStep surreptitiously affects the Unique constraints as defined in the XID/XUNIQUE specifications
							new AddUniqueConstraintUpgradeStep("UserRole.RoleID"),
							new RemoveColumnUpgradeStep( GetOriginalSchema, "UserRole.RoleID"),
							new AddUniqueConstraintUpgradeStep("UserRole.UserID"),
							// UPdate the remaining views affected by UserRole change
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new AddTableUpgradeStep("RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserPermission"),
							new AddTableUpgradeStep("UserPermission")
						),
						new UpgradeStepSequence( // 1.0.10.32 -- Remove IsBuiltin and other fields from old Role table. Move possible custom roles to CustomRole table
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new SqlUpgradeStep(@"
								INSERT INTO CustomRole (Id, Code, PrincipalID) select NEWID(), [Role].Code, [Role].PrincipalID from [Role] where IsBuiltInRole = 0
								DELETE from [Role] where IsBuiltInRole = 0
								"),
								 // The next 3 steps are required because Class was part of the original XID and removing it removes the UniqueConstraint associated with it and Code
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Role.Class"),
							new AddUniqueConstraintUpgradeStep("Role.Code"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Role.IsBuiltInRole"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_IsBuiltInRole"),
							new AddTableUpgradeStep("RoleReport")
						),
						new UpgradeStepSequence( // 1.0.10.33 -- change the xxxState Id fields to 'knownIds'
							// we do this by removing the foreign key constraints, updating all the referencing fields to the new ids, then put the foreign constraints back
							new RepairDefaultTableForeignConstraintsUpgradeStep(),	// but first, ensure the foreign constraints from defaults tables are present.
							new SqlUpgradeStep(@"
-- Add the extra columns we need to do our work.
alter table RequestState add [newid] uniqueidentifier
alter table WorkOrderState add [newid] uniqueidentifier
alter table PurchaseOrderState add [newid] uniqueidentifier
							"),
							new SqlUpgradeStep(@"
declare @Drop nvarchar(max)
declare @DeleteOriginalForeignConstraints nvarchar(max)
declare @CreateCascadeForeignConstraints nvarchar(max)
declare @DeleteCascadeForeignConstraints nvarchar(max)
declare @ReplaceOriginalForeignConstraints nvarchar(max)

-- Assign the new State Id's
update RequestState set [newid] = '4D61696E-426F-7373-0003-000000000000' where RequestState.FilterAsNew = 1
update RequestState set [newid] =  '4D61696E-426F-7373-0003-000000000001' where RequestState.FilterAsInProgress = 1
update RequestState set [newid] =  '4D61696E-426F-7373-0003-000000000002' where RequestState.FilterAsClosed = 1

update WorkOrderState set [newid] = '4D61696E-426F-7373-0004-000000000000' where WorkOrderState.FilterAsDraft = 1
update WorkOrderState set [newid] = '4D61696E-426F-7373-0004-000000000001' where WorkOrderState.FilterAsOpen = 1
update WorkOrderState set [newid] = '4D61696E-426F-7373-0004-000000000002' where WorkOrderState.FilterAsClosed = 1
update WorkOrderState set [newid] = '4D61696E-426F-7373-0004-000000000003' where WorkOrderState.FilterAsVoid = 1

update PurchaseOrderState set [newid] = '4D61696E-426F-7373-0005-000000000000' where PurchaseOrderState.FilterAsDraft = 1
update PurchaseOrderState set [newid] = '4D61696E-426F-7373-0005-000000000001' where PurchaseOrderState.FilterAsIssued = 1
update PurchaseOrderState set [newid] = '4D61696E-426F-7373-0005-000000000002' where PurchaseOrderState.FilterAsClosed = 1
update PurchaseOrderState set [newid] = '4D61696E-426F-7373-0005-000000000003' where PurchaseOrderState.FilterAsVoid = 1

-- Build to command to restore all the Foreign constraints to their original state while XxxxStateTransition.ToStateID still have their constraints
SET  @ReplaceOriginalForeignConstraints =
  (SELECT 'ALTER TABLE [dbo].[' + C.TABLE_NAME + ']  WITH CHECK ADD FOREIGN KEY([' + KCU.COLUMN_NAME + ']) REFERENCES [dbo].[' + C2.Table_Name + '] ([Id]) on UPDATE NO ACTION ON DELETE ' + RC.DELETE_RULE + ';'
		FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS C 
			   INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU 
				 ON C.CONSTRAINT_SCHEMA = KCU.CONSTRAINT_SCHEMA 
					AND C.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME 
			   INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC 
				 ON C.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
					AND C.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 
			   INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C2 
				 ON RC.UNIQUE_CONSTRAINT_SCHEMA = C2.CONSTRAINT_SCHEMA 
					AND RC.UNIQUE_CONSTRAINT_NAME = C2.CONSTRAINT_NAME 
			   INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 
				 ON C2.CONSTRAINT_SCHEMA = KCU2.CONSTRAINT_SCHEMA 
					AND C2.CONSTRAINT_NAME = KCU2.CONSTRAINT_NAME 
					AND KCU.ORDINAL_POSITION = KCU2.ORDINAL_POSITION 
		WHERE  C.CONSTRAINT_TYPE = 'FOREIGN KEY'
				and (C2.Table_Name = 'RequestState' or C2.Table_Name = 'WorkOrderState' or C2.Table_Name = 'PurchaseOrderState')
				and KCU2.COLUMN_NAME = 'Id'
		for xml path(''), TYPE).value('.','varchar(max)')

-- Break the Foreign Key constraint from XxxxStateTransition.ToStateID so we can manually update those fields.
set @Drop =  (SELECT 'ALTER TABLE [dbo].['+ccu.Table_Name+'] DROP CONSTRAINT [' + CCU.CONSTRAINT_NAME + '];' 
	from information_schema.constraint_column_usage as ccu	-- ccu for the column being dropped
	join information_schema.referential_constraints as rc	-- referencing foreign constraint
		on ccu.constraint_catalog = rc.constraint_catalog
			and ccu.constraint_schema = rc.constraint_schema
			and ccu.constraint_name = rc.constraint_name
WHERE   (ccu.Table_Name = 'RequestStateTransition' or ccu.Table_Name = 'WorkOrderStateTransition' or ccu.Table_Name = 'PurchaseOrderStateTransition')
		and ccu.COLUMN_NAME = 'ToStateID'
for xml path(''), TYPE).value('.','varchar(max)')
exec sp_executesql @Drop

-- Update XxxStateTransition.ToStateID
update RequestStateTransition
  set ToStateID = [Newid]
  from
	RequestStateTransition
	join
	RequestState on ToStateID = RequestState.ID
update WorkOrderStateTransition
  set ToStateID = [Newid]
  from
	WorkOrderStateTransition
	join
	WorkOrderState on ToStateID = WorkOrderState.ID
update PurchaseOrderStateTransition
  set ToStateID = [Newid]
  from
	PurchaseOrderStateTransition
	join
	PurchaseOrderState on ToStateID = PurchaseOrderState.ID
	
-- Build a command to make new Foreign Constraints with ON UPDATE CASCADE to replace all existing ones. (which will not include the one already removed on XxxStateTransition.ToStateID)
SET  @CreateCascadeForeignConstraints =
  (SELECT 'ALTER TABLE [dbo].[' + C.TABLE_NAME + ']  WITH CHECK ADD FOREIGN KEY([' + KCU.COLUMN_NAME + ']) REFERENCES [dbo].[' + C2.Table_Name + '] ([Id]) on UPDATE CASCADE ON DELETE ' + RC.DELETE_RULE + ';'
		FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS C 
			   INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU 
				 ON C.CONSTRAINT_SCHEMA = KCU.CONSTRAINT_SCHEMA 
					AND C.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME 
			   INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC 
				 ON C.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
					AND C.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 
			   INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C2 
				 ON RC.UNIQUE_CONSTRAINT_SCHEMA = C2.CONSTRAINT_SCHEMA 
					AND RC.UNIQUE_CONSTRAINT_NAME = C2.CONSTRAINT_NAME 
			   INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 
				 ON C2.CONSTRAINT_SCHEMA = KCU2.CONSTRAINT_SCHEMA 
					AND C2.CONSTRAINT_NAME = KCU2.CONSTRAINT_NAME 
					AND KCU.ORDINAL_POSITION = KCU2.ORDINAL_POSITION 
		WHERE  C.CONSTRAINT_TYPE = 'FOREIGN KEY'
				and (C2.Table_Name = 'RequestState' or C2.Table_Name = 'WorkOrderState' or C2.Table_Name = 'PurchaseOrderState')
				and KCU2.COLUMN_NAME = 'Id'
		for xml path(''), TYPE).value('.','varchar(max)')

-- Remove the existing Foreign Constraints
set @DeleteOriginalForeignConstraints =  (SELECT 'ALTER TABLE [dbo].[' + C.TABLE_NAME + '] DROP CONSTRAINT [' + C.CONSTRAINT_NAME + '];' 
FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS C 
       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU 
         ON C.CONSTRAINT_SCHEMA = KCU.CONSTRAINT_SCHEMA 
            AND C.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME 
       INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC 
         ON C.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
            AND C.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 
       INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C2 
         ON RC.UNIQUE_CONSTRAINT_SCHEMA = C2.CONSTRAINT_SCHEMA 
            AND RC.UNIQUE_CONSTRAINT_NAME = C2.CONSTRAINT_NAME 
       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 
         ON C2.CONSTRAINT_SCHEMA = KCU2.CONSTRAINT_SCHEMA 
            AND C2.CONSTRAINT_NAME = KCU2.CONSTRAINT_NAME 
            AND KCU.ORDINAL_POSITION = KCU2.ORDINAL_POSITION 
WHERE  C.CONSTRAINT_TYPE = 'FOREIGN KEY'
		and (C2.Table_Name = 'RequestState' or C2.Table_Name = 'WorkOrderState' or C2.Table_Name = 'PurchaseOrderState')
		and KCU2.COLUMN_NAME = 'Id'
for xml path(''), TYPE).value('.','varchar(max)')
exec sp_executesql @DeleteOriginalForeignConstraints

-- Put in the cascade Foreign Constraints.
exec sp_executesql @CreateCascadeForeignConstraints

-- Drop certain INDEXES that really impede the CASCADE update process (such that large databases cannot be upgraded due to timeout)
-- They are automatically recreated in upgrade step 1.0.10.41 (just by chance this was done after the INDEX problem was detected here)
DROP INDEX [RequestStateID] ON [dbo].[RequestStateHistory]
DROP INDEX [WorkOrderStateID] ON [dbo].[WorkOrderStateHistory]
DROP INDEX [PurchaseOrderStateID] ON [dbo].[PurchaseOrderStateHistory]

-- Change the ID field on all the state records to the new value, the cascade will fix all the references.
update RequestState
  set Id = [newid]
update WorkOrderState
  set Id = [newid]
update PurchaseOrderState
  set Id = [newid]
  
-- Remove the Cascade constraints we created
-- We could also have created the constraints with known names and created this command at that time.
set @DeleteCascadeForeignConstraints =  (SELECT 'ALTER TABLE [dbo].[' + C.TABLE_NAME + '] DROP CONSTRAINT [' + C.CONSTRAINT_NAME + '];' 
FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS C 
       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU 
         ON C.CONSTRAINT_SCHEMA = KCU.CONSTRAINT_SCHEMA 
            AND C.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME 
       INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC 
         ON C.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
            AND C.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 
       INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C2 
         ON RC.UNIQUE_CONSTRAINT_SCHEMA = C2.CONSTRAINT_SCHEMA 
            AND RC.UNIQUE_CONSTRAINT_NAME = C2.CONSTRAINT_NAME 
       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 
         ON C2.CONSTRAINT_SCHEMA = KCU2.CONSTRAINT_SCHEMA 
            AND C2.CONSTRAINT_NAME = KCU2.CONSTRAINT_NAME 
            AND KCU.ORDINAL_POSITION = KCU2.ORDINAL_POSITION 
WHERE  C.CONSTRAINT_TYPE = 'FOREIGN KEY'
		and (C2.Table_Name = 'RequestState' or C2.Table_Name = 'WorkOrderState' or C2.Table_Name = 'PurchaseOrderState')
		and KCU2.COLUMN_NAME = 'Id'
for xml path(''), TYPE).value('.','varchar(max)')
exec sp_executesql @DeleteCascadeForeignConstraints

-- Recreate the original constraints. This include the constraints on the ToStateID columns of the transition tables.
exec sp_executesql @ReplaceOriginalForeignConstraints

-- Delete the work fields we created
alter table RequestState drop column [Newid]
alter table WorkOrderState drop column [Newid]
alter table PurchaseOrderState drop column [Newid]

-- Rebuild the stats.

update statistics dbo.WorkOrderState
update statistics dbo.PurchaseOrderState
update statistics dbo.RequestState
							")
						),
						new UpgradeStepSequence( // 1.0.10.34  -- Create and populate the ManageRequestTransition table
							new AddTableUpgradeStep("ManageRequestTransition"),
							new SqlUpgradeStep(@"
INSERT INTO ManageRequestTransition (ID, RequestStateID, WorkOrderStateID, ChangeToRequestStateID,CommentToRequestor) Values (NEWID(), '4D61696E-426F-7373-0003-000000000000', '4D61696E-426F-7373-0004-000000000001', '4D61696E-426F-7373-0003-000000000001', 'RequestClosePreference' + NCHAR(167) + 'CommentToRequestorWhenNewRequestedWorkOrder')
INSERT INTO ManageRequestTransition (ID, RequestStateID, WorkOrderStateID, ChangeToRequestStateID,CommentToRequestor) Values (NEWID(), '4D61696E-426F-7373-0003-000000000000', '4D61696E-426F-7373-0004-000000000003', '4D61696E-426F-7373-0003-000000000002', 'RequestClosePreference' + NCHAR(167) + 'CommentToRequestorWhenWorkOrderCloses')
INSERT INTO ManageRequestTransition (ID, RequestStateID, WorkOrderStateID, ChangeToRequestStateID,CommentToRequestor) Values (NEWID(), '4D61696E-426F-7373-0003-000000000001', '4D61696E-426F-7373-0004-000000000003', '4D61696E-426F-7373-0003-000000000002', 'RequestClosePreference' + NCHAR(167) + 'CommentToRequestorWhenWorkOrderCloses')
INSERT INTO ManageRequestTransition (ID, RequestStateID, WorkOrderStateID, ChangeToRequestStateID,CommentToRequestor) Values (NEWID(), '4D61696E-426F-7373-0003-000000000000', '4D61696E-426F-7373-0004-000000000002', '4D61696E-426F-7373-0003-000000000001', 'RequestClosePreference' + NCHAR(167) + 'CommentToRequestorWhenNewRequestedWorkOrder')
INSERT INTO ManageRequestTransition (ID, RequestStateID, WorkOrderStateID, ChangeToRequestStateID,CommentToRequestor) Values (NEWID(), '4D61696E-426F-7373-0003-000000000001', '4D61696E-426F-7373-0004-000000000002', '4D61696E-426F-7373-0003-000000000002', 'RequestClosePreference' + NCHAR(167) + 'CommentToRequestorWhenWorkOrderCloses')
INSERT INTO ManageRequestTransition (ID, RequestStateID, WorkOrderStateID, ChangeToRequestStateID,CommentToRequestor) Values (NEWID(), '4D61696E-426F-7373-0003-000000000000', '4D61696E-426F-7373-0004-000000000000', '4D61696E-426F-7373-0003-000000000001', 'RequestClosePreference' + NCHAR(167) + 'CommentToRequestorWhenNewRequestedWorkOrder')
							"),
							new AddExtensionObjectUpgradeStep("mbfn_UserMessageTranslateFromKey"),
							new RemoveExtensionObjectUpgradeStep( GetOriginalSchema, "mbtg_RequestedWorkOrder_Sets_RequestState"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_ManageRequest"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl")
						),
						new UpgradeStepSequence( // 1.0.10.35 -- Add a cycle detection trigger to prevent cycles in ManageRequestTransition table
							new AddExtensionObjectUpgradeStep("mbtg_ManageRequestTransitionCycleDetection")
						),
						new UpgradeStepSequence( // 1.0.10.36 -- Change from a translationkey type to a link to the UserMessageKey table for ManageRequestTransition
							new AddColumnUpgradeStep("ManageRequestTransition.CommentToRequestorUserMessageKeyID"),
							new SqlUpgradeStep(@"
								UPDATE ManageRequestTransition SET CommentToRequestorUserMessageKeyID = UMK.ID from UserMessageKey as UMK
									join ManageRequestTransition MRT on UMK.Context + NCHAR(167) + UMK.[key] = MRT.CommentToRequestor
							"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ManageRequestTransition.CommentToRequestor"),
							new AddExtensionObjectUpgradeStep("mbfn_UserMessageTranslateFromID"),
							new RemoveExtensionObjectUpgradeStep( GetOriginalSchema, "mbtg_RequestedWorkOrder_ManageRequest"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_ManageRequest"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl")

						),
						new UpgradeStepSequence( // 1.0.10.37 -- Remove ATRServicexxx variables possibly still present in database due to schema errors in earlier versions of 3.5
							new SqlUpgradeStep(@"
								if exists (select Name from __Variables where Name = 'ATRServiceMachineName')
								begin
									DELETE FROM __Variables where Name = 'ATRServiceMachineName'
									DROP function _vgetATRServiceMachineName
									DROP procedure _vsetATRServiceMachineName
									DROP procedure _vupdateATRServiceMachineName
								end
								if exists (select Name from __Variables where Name = 'ATRServiceName')
								begin
									DELETE FROM __Variables where Name = 'ATRServiceName'
									DROP function _vgetATRServiceName
									DROP procedure _vsetATRServiceName
									DROP procedure _vupdateATRServiceName
								end"
								)
						),
						new UpgradeStepSequence( // 1.0.10.38 -- Remove leftover MB29 import conversion function fn_weekdays if present in database
							new SqlUpgradeStep(@"
							if object_id('fn_weekdays', 'FN') is not null
								drop function dbo.fn_weekdays
							")
						),
						new UpgradeStepSequence( // 1.0.10.39 -- Fix _DXXXState table columns missed in change to translationkey types and ChangeColumnUpgradeStep DIDN'T alter the _DXXX table if present
							new SqlUpgradeStep(@"
alter table _DPurchaseOrderState alter column [Code] nvarchar(512) null
alter table _DPurchaseOrderState alter column [Desc] nvarchar(512) null
alter table _DWorkOrderState alter column [Code] nvarchar(512) null
alter table _DWorkOrderState alter column [Desc] nvarchar(512) null
alter table _DRequestState alter column [Code] nvarchar(512) null
alter table _DRequestState alter column [Desc] nvarchar(512) null
							")
							 // No data to fix since we have never allowed data to be put into these tables at this point in time.
						),
						#endregion
						#region 1.0.10.40 - 1.0.10.49
						new UpgradeStepSequence( // 1.0.10.40 -- Fix column types column type to match what the schema is now
							// The following column types SQL types may be different due to change in xaf type implementation or change in the integral min/max for a field (e.g. an enum type field) where
							// the range of values was small enough to warrant an SBYTE or TINYINT instead of an INT. In some cases, the .XAFDB type was changed from (for example) integer(unsigned 16) to integer(min 0, max 2) and no upgrade
							// step was put in to possibly correct the SQL type.
							new ChangeColumnUpgradeStep(GetOriginalSchema, "ScheduledWorkOrder.RescheduleBasisAlgorithm"),
							// A column changed in the EmailRequestReport but no upgrade step had been done because the report views were 'changing'. This step is here to at least make the View match the schema so when schema
							// comparisons are done, this different no longer shows up. 
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmailRequestReport"),
							new AddTableUpgradeStep("EmailRequestReport"),
							// A change long ago from integer(unsigned 16,nonull) to RequiredCount on CountOfLinkedWorkOrders may have left some older databases with a _DRequest.CountOfLinkedWorkOrders with a TINYINT type.
							new SqlUpgradeStep("alter table _DRequest alter column [CountOfLinkedWorkOrders] int null")
						),
						new UpgradeStepSequence( // 1.0.10.41 -- Repair all past transgressions with ForeignConstraints on Default tables, and other minor observations on development databases upgraded over time
							new SetColumnRequiredUpgradeStep("MeterReading.EffectiveReading"),
							new RepairDefaultTableForeignConstraintsUpgradeStep(),
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "RelativeLocation.ExternalTag"),
							new RemoveUniqueConstraintUpgradeStep(GetOriginalSchema, "PermanentItemLocation.ExternalTag"),
							new AddUniqueConstraintUpgradeStep("PermanentItemLocation.ExternalTag"),
							new AddUniqueConstraintUpgradeStep("RelativeLocation.ExternalTag"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_SetNewLocationContainment"),
							new AddExtensionObjectUpgradeStep("mbtg_SetNewLocationContainment"),
							new CreateAllColumnIndexesUpgradeStep()	// Fix any possible missing indexes (AGAIN) since some were found missing in 3.4 upgrade steps (UnitRelatedUnit.RelationshipID for example)
						),
						new UpgradeStepSequence( // 1.0.10.42 Simplify Report tbls
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderOverdueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborOutsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborInsideHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReplacementScheduleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitSparePartsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderDetailedReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ServiceContractReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmailRequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemAdjustmentCodeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MeterClassReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "EmployeeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "TemporaryStorageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "VendorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "OtherWorkOutsideReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "OtherWorkInsideReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivityReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ReceivingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UserReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "BillableRequestorReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStatusStatisticsReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "SpecificationFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackLineCategoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MiscellaneousWorkOrderCostReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemIssueReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemIssueCodeReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PermanentStorageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PermanentItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "TemporaryItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "TemplateItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestockingReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MiscellaneousReport"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrder_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrder_History_As_String"),
							new AddExtensionObjectUpgradeStep("mbfn_Request_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_History_As_String"),
							new AddExtensionObjectUpgradeStep("mbfn_Permissions_As_String"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("ItemUsageReport"),
							new AddTableUpgradeStep("ItemLocationReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("ChargebackFormReport"),
							new AddTableUpgradeStep("WorkOrderTemplateReport"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("ItemActivityReport"),
							new AddTableUpgradeStep("AccountingTransactionReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderTemplateReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("ReceivingReport"),
							new AddTableUpgradeStep("ItemOnOrderReport"),
							new AddTableUpgradeStep("RoleReport")
						),
						new UpgradeStepSequence( // 1.0.10.43 Correct report views to remove inappropriate fanout and canonicalize some reports to SQL2005-compatible definitions
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("UnitMetersReport"),
							new AddTableUpgradeStep("ItemLocationReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("RoleReport")
						),
						new UpgradeStepSequence( // 1.0.10.44 Use the new .L. in Paths to simplify report views.
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemUsageReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitMetersReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargeBackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("ChargeBackFormReport"),
							new AddTableUpgradeStep("ItemLocationReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderTemplateReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("RoleReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("RequestReport")
						),
						new UpgradeStepSequence( // 1.0.10.45 Fix spelling mistake in view parameter
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.10.46 Update the Initial Data for PM generation to include MeterID
							new RemoveTableUpgradeStep(GetOriginalSchema, "PMInitializingGenerationDetail"),
							new AddTableUpgradeStep("PMInitializingGenerationDetail")
						),
						new UpgradeStepSequence( // 1.0.10.47 Remove PeriodicityView
							new RemoveTableUpgradeStep(GetOriginalSchema, "PeriodicityView")
						),
						new UpgradeStepSequence( // 1.0.10.48 Add labor/credit/debit
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionReport"),
							new AddTableUpgradeStep("AccountingTransactionReport")
						),
						new UpgradeStepSequence( // 1.0.10.49 number resource records
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport")
						),
						#endregion
						#region 1.0.10.50 - 1.0.10.59
						new UpgradeStepSequence( // 1.0.10.50 remove assumption that a workorder had to be opened before it was closed
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.10.51 removed IsOverdue and replaced by EndVariance
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport")
						),
						new UpgradeStepSequence( // 1.0.10.52 Add tables for saved settings
							new AddTableUpgradeStep("SettingsName"),
							new AddTableUpgradeStep("Settings"),
							new AddTableUpgradeStep("DefaultSettings")
						),
						new UpgradeStepSequence( // 1.0.10.53 Adjust AttentionStatus et. all to show UnAssigned request counts
							new RemoveColumnUpgradeStep(GetOriginalSchema, "RequestAssignee.RequestAssigneeStatisticsID"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AttentionStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeStatistics"),
							new AddTableUpgradeStep("RequestAssigneeStatistics"),
							new AddTableUpgradeStep("AttentionStatus")
						),
						new UpgradeStepSequence( // 1.0.10.54 Remove unneeded columns; use the .L. syntax now to retrieve the statistic values
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderAssignee.WorkOrderAssigneeStatisticsID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignee.PurchaseOrderAssigneeStatisticsID")
						),
						new UpgradeStepSequence( // 1.0.10.55 Update statistics for WorkOrders and PurchaseOrders in same vein as Requests
							new RemoveTableUpgradeStep(GetOriginalSchema,"DatabaseStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"AttentionStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"WorkOrderAssigneeStatistics"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"PurchaseOrderAssigneeStatistics"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentByAssignee"),
							new AddTableUpgradeStep("WorkOrderAssignmentByAssignee"),
							new AddTableUpgradeStep("WorkOrderAssigneeStatistics"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeStatistics"),
							new AddTableUpgradeStep("AttentionStatus"),
							new AddTableUpgradeStep("DatabaseStatus")
						),
						new UpgradeStepSequence( // 1.0.10.56 Cleanup RequestAcknowledgementStatus usage
							new RemoveExtensionObjectUpgradeStep( GetOriginalSchema, "mbtg_Request_Updates_RequestAcknowledgementStatus"),
							new AddColumnUpgradeStep("Request.LastRequestorAcknowledgementDate"),
							new AddColumnUpgradeStep("Request.LastRequestorAcknowledgementError"),
							new SqlUpgradeStep(@"
								UPDATE Request SET LastRequestorAcknowledgementDate  = RAS.LastAcknowledgementDate, LastRequestorAcknowledgementError  = RAS.LastAcknowledgementError 
										from Request as R join RequestAcknowledgementStatus as RAS on R.Id = RAS.RequestID
							"),
							new AddExtensionObjectUpgradeStep("mbtg_Request_Updates_RequestorAcknowledgement"),
							new RemoveTableUpgradeStep( GetOriginalSchema, "RequestAcknowledgement"),
							new RemoveTableUpgradeStep( GetOriginalSchema, "RequestAcknowledgementStatus"),
							new AddTableUpgradeStep( "RequestAcknowledgement")
						),
						new UpgradeStepSequence( // 1.0.10.57 Add report to allow printing by Assignee
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrder_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrder_Assignee_List"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport"),
							new AddTableUpgradeStep("RequestAssignmentReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport")
						),
						new UpgradeStepSequence( // 1.0.10.58 Add SettingsAdministration view
							new AddTableUpgradeStep("SettingsAdministration")
						),
						new UpgradeStepSequence( // 1.0.10.59 Add Charts for requests
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestAssignmentReport")
						),
						#endregion
						#region 1.0.10.60 - 1.0.10.69
						new UpgradeStepSequence( // 1.0.10.60 Remove the UnitLocationID from the RequestReport, instead finding this through the linkage in the request.
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestAssignmentReport")
						),
						new UpgradeStepSequence( // 1.0.10.61 Add Charts for requests
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ReceivingReport"),
							new AddTableUpgradeStep("ReceivingReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport"),
							new AddTableUpgradeStep("ItemOnOrderReport")
						),
						new UpgradeStepSequence( // 1.0.10.62 Limit range of "additional line" counts
							// Two variables where changed from type Quantity (integer(32)) to integer(min 0, max 40), and as a result they now contain
							// a Byte converted to binary rather than an Int converted to binary and so should only contain binary(1). We use substring to strip off
							// the extra bytes. If the user entered some huge number here previously it will be reduced modulo 256.
							new SqlUpgradeStep(@"
								update __Variables set value = substring(value, 4, 1)
									where name = 'WOFormAdditionalBlankLines' or name = 'POFormAdditionalBlankLines'
							")
						),
						new UpgradeStepSequence( // 1.0.10.63 Show state comment on WorkOrder/PurchaseOrder history
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrder_History_As_String"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_History_As_String"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationReport"),
							new AddTableUpgradeStep("LocationReport"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_History_As_String"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrder_History_As_String")
						),
						new UpgradeStepSequence( // 1.0.10.64 Replace ContactReport with much simplified version
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new AddTableUpgradeStep("ContactReport")
						),
						new UpgradeStepSequence( // 1.0.10.65 Factor out the views for AccountingTransaction reporting
							new AddTableUpgradeStep("AccountingTransactionVariants"),
							new AddTableUpgradeStep("AccountingTransactionsAndReversals"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionReport")
						),
						new UpgradeStepSequence( // 1.0.10.66 Correct and streamline totalling methods for inventory
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveItemCountValue"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivityReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemPO_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemNonPO_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemTransfer_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemIssue_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemAdjustment_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualItem_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_OnHand"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_TotalCost"),

							new AddExtensionObjectUpgradeStep("mbfn_ItemLocationLastTotallingBasis"),
							new AddExtensionObjectUpgradeStep("mbfn_ItemLocationStatusAtEternity"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualItemLocation_TotalCostAtEternity"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualItemLocation_TotalCostOnDate"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualItemLocation_OnHandAtEternity"),
							new AddExtensionObjectUpgradeStep("mbfn_ActualItemLocation_OnHandOnDate"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualItem_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemAdjustment_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemIssue_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemTransfer_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemNonPO_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemPO_Updates_ActualItemLocation"),
							new AddTableUpgradeStep("ItemActivityReport")
						),
						new UpgradeStepSequence( // 1.0.10.67 Add another view for reporting
							new AddTableUpgradeStep("PurchaseOrdersWithAssignments")
						),
						new UpgradeStepSequence( // 1.0.10.68 Add more views for reporting
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestReportX")
						),
						new UpgradeStepSequence( // 1.0.10.69 Add more views for reporting
							new AddTableUpgradeStep("RequestsWithAssignments"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemOnOrderReport")
						),
						#endregion
						#region 1.0.10.70 -
						new UpgradeStepSequence( // 1.0.10.70 Replace ReceivingReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "ReceivingReport"),
							new AddTableUpgradeStep("ReceivingReport")
						),
						new UpgradeStepSequence( // 1.0.10.71 Change StateId references to Required fields
							new SetColumnRequiredUpgradeStep("RequestStateTransition.ToStateID"),
							new SetColumnRequiredUpgradeStep("RequestStateTransition.FromStateID"),
							new SetColumnRequiredUpgradeStep("WorkOrderStateTransition.ToStateID"),
							new SetColumnRequiredUpgradeStep("WorkOrderStateTransition.FromStateID"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderStateTransition.ToStateID"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderStateTransition.FromStateID")
						),
						new UpgradeStepSequence( // 1.0.10.72 Add functions to classify datetimes into larger intervals for reporting
							new AddExtensionObjectUpgradeStep("ClassifyDateByMonth"),
							new AddExtensionObjectUpgradeStep("ClassifyDateByTime")
						),
						new UpgradeStepSequence( // 1.0.10.73 Fix (again) MB permissions management on the server
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbfn_User_Sql_Credentials"),
							new AddExtensionObjectUpgradeStep("mbfn_Service_Sql_Credentials"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						new UpgradeStepSequence( // 1.0.10.74 Remove WorkOrderAssignmentReport.WorkOrderReportID
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport")
						),
						new UpgradeStepSequence( // 1.0.10.75 Add mbsp_AddDataBaseUser to allow creation of a user for service.
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_AddDataBaseUser"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						new UpgradeStepSequence( // 1.0.10.75 Use the domain of the sql server rather than the current user as the default domain
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						)	// Next is 1.1.x.x. Do not add any more steps here.
						#endregion
					}
				},
				#endregion 
				#region 1.1.x.x
				new UpgradeStep[][] {	// 1.1.x.x
					new UpgradeStep[] {	// 1.1.0.x Reserved for development for MB 4.0
						#region 1.1.0.0 - 1.1.0.9
						// Start of a new era; MainBoss 4.0 development
						new UpgradeStepSequence( // Establish base version for 4.0 development
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '4.0.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '4.0.0.0'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '4.0.0.0'")
						),
						new UpgradeStepSequence( // 1.1.0.1 Correct overly-precise dates
							// DatabaseHistory.EntryDate was given excess precision by 2.9 import code and by step 1.0.2.9
							// ItemLocation.Hidden was given excess precision by original code for step 1.0.2.54 for TemplateItemLocation derivations but these were all subsequently deleted
							new SqlUpgradeStep("Update DatabaseHistory set EntryDate = dbo._DClosestValue(EntryDate, 2, 100)"),
							// Correct all the report views that return GetDate in columns without restricting its result precision somehow.
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReportX"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestReportX"),
							new AddTableUpgradeStep("RequestAssignmentReport"),

							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),

							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),

							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),

							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),

							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_History_As_String"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_Assignee_List"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_History_As_String"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport")
						),
						new UpgradeStepSequence( // 1.1.0.2 Correct some incorrect date rounding code and codify the meaning of the epsilon argument to _DClosestValue.
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("AssignedActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("ActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("WorkOrderEndDateHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedWorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("AssignedWorkOrderEndDateHistogram"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_DateFromDateTime"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "ClassifyDateByTime"),
							new AddExtensionObjectUpgradeStep("ClassifyDateByTime"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "ClassifyDateByMonth"),
							new AddExtensionObjectUpgradeStep("ClassifyDateByMonth")
							// This also changes a comment in _DClosestValue but the upgrade step does not change the existing definition.
						),
						new UpgradeStepSequence( // 1.1.0.3 Correct integer overflow in UnitReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport")
						),
						new UpgradeStepSequence( // 1.1.0.4 Correct type of PurchaseOrderTemplateReport.Labor
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateReport"),
							new AddTableUpgradeStep("PurchaseOrderTemplateReport")
						),
						new UpgradeStepSequence( // 1.1.0.5 fixing security to work with domain of sql srv or service account
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_AddDataBaseUser"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Service_Sql_Credentials"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_User_Sql_Credentials"),
							new AddExtensionObjectUpgradeStep("mbsp_GetDefaultWindowsDomain"),
							new AddExtensionObjectUpgradeStep("mbsp_AddDataBaseUser"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbfn_User_Has_MainBoss_Access")
						),
						new UpgradeStepSequence( // 1.1.0.6 redo all SQL that concatinate strings
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_History_As_String"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrder_History_As_String"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ContactReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_Assignee_List"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrder_Assignee_List"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Request_Assignee_List"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ContactRelationship_Summary"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Permissions_As_String"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_GetDefaultWindowsDomain"),
							new AddExtensionObjectUpgradeStep("mbsp_GetDefaultWindowsDomain"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbfn_Permissions_As_String"),
							new AddExtensionObjectUpgradeStep("mbfn_ContactRelationship_Summary"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbfn_Request_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrder_Assignee_List"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_Assignee_List"),
							new AddTableUpgradeStep("ContactReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderReport"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrder_History_As_String"),
							new AddExtensionObjectUpgradeStep("mbfn_PurchaseOrder_History_As_String"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("LaborForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("RequestAssignmentReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("RoleReport")
						),
						new UpgradeStepSequence(		// 1.1.0.7 Redefine the RequestAssignmentReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new AddTableUpgradeStep("RequestAssignmentReport")
						),
						new UpgradeStepSequence(		// 1.1.0.8 Redefine the PurchaseOrderFormReport, PurchaseOrderAssignmentReport, WorkOrderAssignmentReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport")
						),
						new UpgradeStepSequence(		// 1.1.0.9 changes WorkOrderFormReport to provide linkages to Demand and AccountingTransaction and remove fields found through these records.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport")
						),
						#endregion
						#region 1.1.0.10 - 1.1.0.19
						new UpgradeStepSequence( // 1.1.0.10 Clean up views for WO assignee etc
							// AttentionStatus has the counts of unassigned WO/PO/Req removed as they were not used
							// DatabaseStatus changed to callculate these counts itself
							// XxxxAssigneeStatistics changed to remove second case for counts of unassigned records, which made the Id field null and other bad things.
							//		They also no longer use XxxxxAssignmentByAssignee so the latter aare now purely views to drive tree-structured browsers.
							new RemoveTableUpgradeStep(GetOriginalSchema, "AttentionStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "DatabaseStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssigneeStatistics"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssigneeStatistics"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssigneeStatistics"),
							new AddTableUpgradeStep("RequestAssigneeStatistics"),
							new AddTableUpgradeStep("PurchaseOrderAssigneeStatistics"),
							new AddTableUpgradeStep("WorkOrderAssigneeStatistics"),
							new AddTableUpgradeStep("DatabaseStatus"),
							new AddTableUpgradeStep("AttentionStatus"),
							// Add a new view for the Work Order By Assignee (summary) report.
							new AddTableUpgradeStep("WorkOrderAssignmentAndUnassignedWorkOrder")
						),
						new UpgradeStepSequence( // 1.1.0.11 Add a ServerVersion column to ServerConfiguration
							new AddColumnUpgradeStep("ServiceConfiguration.InstalledServiceVersion"),
							new SqlUpgradeStep(@"
								delete from ServiceConfiguration where Id not in (select top 1 Id from ServiceConfiguration) ;-- make sure there is only one record
								update ServiceConfiguration set Code = ServiceName, Servicename = null where ServiceName is not null;
							")
						),
						new UpgradeStepSequence( // 1.1.0.12 Simplify the WorkOrderStateHistoryReport, PurchaseOrderStateHistoryReport, and RequestStateHistoryReport views
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport")
						),
						new UpgradeStepSequence( // 1.1.0.13 Simplify the WorkOrderFormReport view and move some of its fields to WorkOrderReport because they are invariant for each WO.
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new AddTableUpgradeStep("MaterialForecastReport"),
							new AddTableUpgradeStep("LaborForecastReport")
						),
						new UpgradeStepSequence( // 1.1.0.14 Check security using SID's rather that userid string, 
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_User_Has_MainBoss_Access"),
							new AddExtensionObjectUpgradeStep("mbfn_User_Has_MainBoss_Access")
						),
						new UpgradeStepSequence( // 1.1.0.15 Check security using SID's rather that userid string, 
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_ServiceConfiguration_MaintainSecurity"),
							new AddExtensionObjectUpgradeStep("mbtg_User_MaintainSecurity")
						),
						new RemoveDuplicateExternalTagsUpgradeStep( // 1.1.0.16 Remove any possible ExternalTags that may be duplicated from EARLIER SQL server versions that did not have the constraint
						),
						new UpgradeStepSequence( // 1.1.0.17 Fix the XXXAssignementByAssignee views to include only open/draft (or their Request equivalents)
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentByAssignee"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentByAssignee"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentByAssignee"),
							new AddTableUpgradeStep("WorkOrderAssignmentByAssignee"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentByAssignee"),
							new AddTableUpgradeStep("RequestAssignmentByAssignee")
						),
						new UpgradeStepSequence( // 1.1.0.18 Change the ItemRestocking view to treat Deleted IL's as being never understocked.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemRestocking"),
							new AddTableUpgradeStep("ItemRestocking")
						),
						new UpgradeStepSequence( // 1.1.0.19 Change the PurchaseOrderTemplateReport view so PO Templates with no line items still show up in the report.
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderTemplateReport"),
							new AddTableUpgradeStep("PurchaseOrderTemplateReport")
						),
						#endregion
						#region 1.1.0.20 - 1.1.0.29
						new UpgradeStepSequence( // 1.1.0.20 Add the ExistingAndForecastResources view to eventually replace LaborForecastReport et al.
							new AddTableUpgradeStep("ExistingAndForecastResources")
						),
						new UpgradeStepSequence( // 1.1.0.21 Add the xxx view to eventually replace LaborForecastReport et al.
							new RemoveTableUpgradeStep(GetOriginalSchema, "ExistingAndForecastResources"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LaborForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaterialForecastReport"),
							new AddTableUpgradeStep("ResolvedWorkOrderTemplate"),
							new AddTableUpgradeStep("ExistingAndForecastResources")
						),
						new UpgradeStepSequence( // 1.1.0.22 Update UnitReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport")
						),
						new UpgradeStepSequence( // 1.1.0.23 Eliminate DemandTemplateWorkOrderExpenseModelEntry which is not used
							new RemoveTableUpgradeStep(GetOriginalSchema, "DemandTemplateWorkOrderExpenseModelEntry")
						),
						new UpgradeStepSequence( // 1.1.0.24 Update WorkOrderFormReport and ChargebackFormReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "ChargebackFormReport"),
							new AddTableUpgradeStep("ChargebackFormReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderFormReport"),
							new AddTableUpgradeStep("WorkOrderAssignmentReport")
						),
						new UpgradeStepSequence( // 1.1.0.25 Update XxxxStateHistoryReport to use "Previous" linkage rather than "Next"
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestStateHistoryReport"),
							new AddTableUpgradeStep("RequestStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderStateHistoryReport"),
							new AddTableUpgradeStep("WorkOrderStateHistoryReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderStateHistoryReport"),
							new AddTableUpgradeStep("PurchaseOrderStateHistoryReport")
						),
						new UpgradeStepSequence( // 1.1.0.26 Update RequestReportX
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReportX"),
							new AddTableUpgradeStep("RequestReportX")
						),
						new UpgradeStepSequence( // 1.1.0.27 Update PurchaseOrderReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new AddTableUpgradeStep("PurchaseOrderReport")
						),
						new UpgradeStepSequence( // 1.1.0.28  SecurityRoleAndUserRoleReport et al.
							new AddExtensionObjectUpgradeStep("mbfn_Permissions_As_Text"),
							new AddTableUpgradeStep("PrincipalExtraInformation"),
							new AddTableUpgradeStep("SecurityRoleAndUserRoleReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RoleReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Permissions_As_String")
						),
						new UpgradeStepSequence( // 1.1.0.29  Remove unused service PurchaseOrder message keys
							new SqlUpgradeStep(@"
								DELETE UserMessageKey WHERE [Key] = 'PurchaseOrderURLPreamble'
							")
						),
						#endregion
						#region 1.1.0.30 - 11.1.0.39
						new UpgradeStepSequence( // 1.1.0.30  Auto requestor created include/exclude patterns
							new AddColumnUpgradeStep("ServiceConfiguration.AcceptAutoCreateEmailPattern"),
							new AddColumnUpgradeStep("ServiceConfiguration.RejectAutoCreateEmailPattern")
						),
						new UpgradeStepSequence( // 1.1.0.31  LDAP Requestor creation
							new AddColumnUpgradeStep("ServiceConfiguration.AutomaticallyCreateRequestorsFromLDAP"),
							new AddColumnUpgradeStep("Contact.LDAPPath"),
							new SqlUpgradeStep(@"UPDATE ServiceConfiguration SET AutomaticallyCreateRequestorsFromLDAP = 0")
						),
						new UpgradeStepSequence( // 1.1.0.32  Alternate Email addresses
							new AddColumnUpgradeStep("Contact.AlternateEmail")
						),
						new UpgradeStepSequence( // 1.1.0.33 Correct WorkOrderTemplateReport to not duplicate PO Template lines once for each regular resource demand
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateReport"),
							new AddTableUpgradeStep("WorkOrderTemplateReport")
						),
						new UpgradeStepSequence( // 1.1.0.34  Requestor create and Manual processing allowance.
							new AddColumnUpgradeStep("ServiceConfiguration.AutomaticallyCreateRequestorsFromEmail"),
							new AddColumnUpgradeStep("ServiceConfiguration.ManualProcessingTimeAllowance"),
							new AddColumnUpgradeStep("EmailRequest.ProcessedDate"),
							new SqlUpgradeStep(@"UPDATE ServiceConfiguration SET AutomaticallyCreateRequestorsFromEmail = AutomaticallyCreateRequestors "),
							new SqlUpgradeStep(@"INSERT INTO UserMessageKey (Id, [Key], Comment, [Context])
										VALUES (NEWID(), 'RequestorStatusPending', 'Thinkage.MainBoss.Database' + NCHAR(167) + 'RequestStatusPending_Comment', 'MainBossService')
							")
						),
						new UpgradeStepSequence( // 1.1.0.35  ReceiptReport added
							new AddTableUpgradeStep("ReceiptReport")
						),
						new UpgradeStepSequence( // 1.1.0.36 Add Cascade delete to EmailRequest
							new RemoveForeignConstraintUpgradeStep(GetOriginalSchema, "EmailPart.EmailRequestID"),
							new AddForeignConstraintUpgradeStep( "EmailPart.EmailRequestID")
						),
						new UpgradeStepSequence( // 1.1.0.37 Add MaintenanceTimingReport
							new AddTableUpgradeStep("MaintenanceTimingReport")
						),
						new UpgradeStepSequence( // 1.1.0.38 Add a view for the ScheduledWorkOrderReport (the Print on Maintenance Plans)
							// This also fixes WorkOrderTemplateContainment to behave if the trigger somehow does not prevent looped structure in the tasks.
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderTemplate.TotalDuration"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrderTemplate_TotalDuration"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderTemplate.MaxGenerateLeadTime"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrderTemplate_MaxGenerateLeadTime"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateWithContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ExistingAndForecastResources"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderTemplateLoopCheck"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateContainment"),
							new AddTableUpgradeStep("NonLoopedWorkOrderTemplates"),
							new AddTableUpgradeStep("WorkOrderTemplateContainment"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderTemplateLoopCheck"),
							new AddTableUpgradeStep("ExistingAndForecastResources"),
							new AddTableUpgradeStep("WorkOrderTemplateWithContainers"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrderTemplate_MaxGenerateLeadTime"),
							new AddColumnUpgradeStep("WorkOrderTemplate.MaxGenerateLeadTime"),
							new ChangeToComputedColumnUpgradeStep("WorkOrderTemplate.MaxGenerateLeadTime", "[dbo].[mbfn_WorkOrderTemplate_MaxGenerateLeadTime]([ID])"),
							new AddExtensionObjectUpgradeStep("mbfn_WorkOrderTemplate_TotalDuration"),
							new AddColumnUpgradeStep("WorkOrderTemplate.TotalDuration"),
							new ChangeToComputedColumnUpgradeStep("WorkOrderTemplate.TotalDuration", "[dbo].[mbfn_WorkOrderTemplate_TotalDuration]([ID])"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport")
						),
						new UpgradeStepSequence( // 1.1.0.39 Add uniqueness to ItemCountValueVoid.VoidedItemCountValueID, add field to ResolvedWorkOrderTemplate to add label context to paths
							new RemoveTableUpgradeStep(GetOriginalSchema, "ExistingAndForecastResources"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ResolvedWorkOrderTemplate"),
							new AddTableUpgradeStep("ResolvedWorkOrderTemplate"),
							new AddTableUpgradeStep("ExistingAndForecastResources"),
							new AddUniqueConstraintUpgradeStep("ItemCountValueVoid.VoidedItemCountValueID")
						),
						#endregion
						#region 1.1.0.40 - 
						new UpgradeStepSequence( // 1.1.0.40 correct previous AddColumn nullability missing
							// Add ColumnRequiredUpgradeSteps missed in other ServiceConfiguration column adds
							new SetColumnRequiredUpgradeStep("ServiceConfiguration.AutomaticallyCreateRequestorsFromLDAP"),
							new SetColumnRequiredUpgradeStep("ServiceConfiguration.AutomaticallyCreateRequestorsFromEmail")
						),
						new UpgradeStepSequence( // 1.1.0.41 Add the LDAP Guid to contact, add count of email message needing manual processing.
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Contact.LDAPPath"),
							new AddColumnUpgradeStep("Contact.LDAPGuid"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "DatabaseStatus"),
							new AddTableUpgradeStep("DatabaseStatus"),
							new SetColumnRequiredUpgradeStep("ServiceConfiguration.MailServer"),
							new SetColumnRequiredUpgradeStep("ServiceConfiguration.SMTPServer")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.1.1.x  Released Versions of MB4.0
						#region 1.1.1.0 - 1.1.1.9
						// MB 4.0.0 uses db version 1.1.1.0
						new UpgradeStepSequence( //1.1.1.0
							new SqlUpgradeStep(@"exec dbo._vsetMinMBAppVersion '4.0.0.25'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinAReqAppVersion '4.0.0.25'"),
							new SqlUpgradeStep(@"exec dbo._vsetMinMBRemoteAppVersion '4.0.0.25'")
						),
						new UpgradeStepSequence( //1.1.1.1  Add EmailRequest.Subject
							new AddColumnUpgradeStep("EmailRequest.Subject"), 
							//
							// several steps in upgrade, This code will only work properly on validly email messages
							// 1) Set the subject field to Header field removing all information before 'Subject:'
							// 2) Convert '\r\n ' to ' ', only one should be necessary since  there is at least 66 char in a MailMessage header
							// 3) Take only the Character before the \r
							// 4) Remove the 'Subject:' at the start of the field
							new SqlUpgradeStep(@"UPDATE EmailRequest SET Subject = Left(Stuff( MailHeader, 1, charindex( Char(13)+Char(10)+'Subject:',MailHeader)-1+2,''), 1000) where CHARINDEX( Char(13)+Char(10)+'Subject:', MailHeader) > 0 ;
												 UPDATE EmailRequest SET Subject = Replace(Subject, Char(13)+Char(10)+' ',' ');
												 UPDATE EmailRequest SET Subject = Left(Subject, CharIndex(Char(13), Subject))  where CharIndex(char(13), Subject ) > 0; 
												 UPDATE EmailRequest SET Subject = Ltrim(Rtrim(Right(Subject, Len(Subject)-8))) where Len(subject) > 8
								")
						),
						new UpgradeStepSequence( // 1.1.1.2 Add ItemRestockingReport
							new AddTableUpgradeStep("ItemRestockingReport")
						),
						new UpgradeStepSequence( // 1.1.1.3 Add EmailPart.ContentEncoding
							new SetColumnRequiredUpgradeStep("ServiceConfiguration.MailPort"),
							new AddColumnUpgradeStep("ServiceConfiguration.Encryption"),
							new AddColumnUpgradeStep("ServiceConfiguration.MaxMailSize"),
							new AddColumnUpgradeStep("EmailPart.ContentEncoding"),
							new SqlUpgradeStep(@"Update ServiceConfiguration Set MailPort = null, Encryption = 0, MaxMailSize = null, MailServerType = 0"),
							new SqlUpgradeStep(@"UPDATE EmailPart SET ContentEncoding = Left(Stuff( Header, 1, charindex('charset=""',Header)-1+9,''), 100) where CHARINDEX('charset=""', Header) > 0 ;
												UPDATE EmailPart SET ContentEncoding = Left(ContentEncoding, CharIndex('""', ContentEncoding)-1)
							"),
							new SetColumnRequiredUpgradeStep("EmailRequest.RequestorEmailAddress"),
							new AddColumnUpgradeStep("EmailPart.ContentTypeDispostion"),
							new SqlUpgradeStep(@"UPDATE EmailPart SET ContentTypeDispostion =  null")
						),
						new UpgradeStepSequence( // 1.1.1.4 Modify setting of ServiceConfiguration Encryption
							new SqlUpgradeStep(@"Update ServiceConfiguration Set MailPort = null, Encryption = 1, MaxMailSize = null, MailServerType = 0"),
							new SetColumnRequiredUpgradeStep("ServiceConfiguration.Encryption")
						),
						new UpgradeStepSequence( // 1.1.1.5 fix spelling error
							new AddColumnUpgradeStep("EmailPart.ContentTypeDisposition"),
							new SqlUpgradeStep(@"Update EmailPart Set ContentTypeDisposition = ContentTypeDispostion"),
							new RemoveColumnUpgradeStep(GetOriginalSchema,"EmailPart.ContentTypeDispostion")
						),
						new UpgradeStepSequence( // 1.1.1.6 add EmailPart.FileName
							new AddColumnUpgradeStep("EmailPart.FileName"),
							new SqlUpgradeStep(@"UPDATE EmailPart SET FileName = Left(Stuff( Header, 1, charindex('filename=""',Header)-1+10,''), 100) where CHARINDEX('filename=""', Header) > 0 ;
												UPDATE EmailPart SET FileName = Left(FileName, CharIndex('""', FileName)-1)
							")
						),
						new UpgradeStepSequence( // 1.1.1.7 Simplify WorkOrderReport
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderReport"),
							new AddTableUpgradeStep("MaintenanceForecastReport")
						),
						// MB 4.0.0 uses db version 1.1.1.8
						new UpgradeStepSequence( //1.1.1.8
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBAppVersion, new Version(4,0,0,31)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinAReqAppVersion, new Version(4,0,0,31)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBRemoteAppVersion, new Version(4,0,0,31))
						),
						#endregion
					},
					new UpgradeStep[] { // 1.1.2.x  Development Versions of MB4.1
						#region 1.1.2.0 - 1.1.2.6
						new UpgradeStepSequence( //1.1.2.0
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestAssignmentReport")
						),
						new UpgradeStepSequence( // 1.1.2.1 Cleanup expensive calculated fields in WorkOrderTemplate
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderTemplate.MaxGenerateLeadTime"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "WorkOrderTemplate.TotalDuration"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrderTemplate_TotalDuration"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrderTemplate_MaxGenerateLeadTime")
						),
						new UpgradeStepSequence( //1.1.2.2
							// All steps have been removed; They were adding/deleting dependencies and the upgrader no longer handles dependencies.
						),
						new UpgradeStepSequence( //1.1.2.3
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivityReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemAdjustment_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemIssue_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemTransfer_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemPO_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemNonPO_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualItem_Updates_ActualItemLocation"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemIssue_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemTransfer_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemPO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemNonPO_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualItem_Updates_Corrected"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_TotalCostOnDate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_OnHandOnDate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_TotalCostAtEternity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ActualItemLocation_OnHandAtEternity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ItemLocationTotallingBasisBeforeDate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ItemLocationTotallingBasisOnOrBeforeDate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ItemLocationLastTotallingBasis"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ItemLocationStatusOnDate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ItemLocationStatusBeforeDate"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_ItemLocationStatusAtEternity"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RationalizedInventoryActivityDeltas"),
							new AddColumnUpgradeStep("ItemAdjustment.TotalCost"),
							new AddColumnUpgradeStep("ItemAdjustment.TotalQuantity"),
							new AddColumnUpgradeStep("ItemIssue.TotalCost"),
							new AddColumnUpgradeStep("ItemIssue.TotalQuantity"),
							new AddColumnUpgradeStep("ItemTransfer.ToTotalCost"),
							new AddColumnUpgradeStep("ItemTransfer.FromTotalCost"),
							new AddColumnUpgradeStep("ItemTransfer.ToTotalQuantity"),
							new AddColumnUpgradeStep("ItemTransfer.FromTotalQuantity"),
							new AddColumnUpgradeStep("ReceiveItemPO.TotalCost"),
							new AddColumnUpgradeStep("ReceiveItemPO.TotalQuantity"),
							new AddColumnUpgradeStep("ReceiveItemNonPO.TotalCost"),
							new AddColumnUpgradeStep("ReceiveItemNonPO.TotalQuantity"),
							new AddColumnUpgradeStep("ActualItem.TotalCost"),
							new AddColumnUpgradeStep("ActualItem.TotalQuantity"),
							new AddTableUpgradeStep("RationalizedInventoryActivityDeltas"),
							new AddExtensionObjectUpgradeStep("mbsp_RetotalNormalInventoryTransactions"),
							new AddExtensionObjectUpgradeStep("mbsp_RetotalInventory"),
							new AddExtensionObjectUpgradeStep("mbsp_AddInventoryDeltas"),
							// As of the addition of the 1.1.3.1 upgrade step, at this point we would have dummy versions of things like mbsp_RetotalInventory.
							// Since 1.1.3.1 recalculates all the totals, we replace our calls to mbsp_RetotalInventory with code that just sets all the new total
							// fields to zero so we can set the (non)nullability.
							new SqlUpgradeStep(@"
								update ItemAdjustment set TotalCost = 0, TotalQuantity = 0
								update ItemIssue set TotalCost = 0, TotalQuantity = 0
								update ItemTransfer set ToTotalCost = 0, ToTotalQuantity = 0, FromTotalCost = 0, FromTotalQuantity = 0
								update ReceiveItemPO set TotalCost = 0, TotalQuantity = 0
								update ReceiveItemNonPO set TotalCost = 0, TotalQuantity = 0
								update ActualItem set TotalCost = 0, TotalQuantity = 0
							"),
							// Provide default values of zero for the new columns
							new SqlUpgradeStep(@"
								update _DItemAdjustment set TotalCost = 0, TotalQuantity = 0
								update _DItemIssue set TotalCost = 0, TotalQuantity = 0
								update _DItemTransfer set ToTotalCost = 0, ToTotalQuantity = 0, FromTotalCost = 0, FromTotalQuantity = 0
								update _DReceiveItemPO set TotalCost = 0, TotalQuantity = 0
								update _DReceiveItemNonPO set TotalCost = 0, TotalQuantity = 0
								update _DActualItem set TotalCost = 0, TotalQuantity = 0
							"),
							new SetColumnRequiredUpgradeStep("ItemAdjustment.TotalCost"),
							new SetColumnRequiredUpgradeStep("ItemAdjustment.TotalQuantity"),
							new SetColumnRequiredUpgradeStep("ItemIssue.TotalCost"),
							new SetColumnRequiredUpgradeStep("ItemIssue.TotalQuantity"),
							new SetColumnRequiredUpgradeStep("ItemTransfer.FromTotalCost"),
							new SetColumnRequiredUpgradeStep("ItemTransfer.ToTotalCost"),
							new SetColumnRequiredUpgradeStep("ItemTransfer.FromTotalQuantity"),
							new SetColumnRequiredUpgradeStep("ItemTransfer.ToTotalQuantity"),
							new SetColumnRequiredUpgradeStep("ReceiveItemPO.TotalCost"),
							new SetColumnRequiredUpgradeStep("ReceiveItemPO.TotalQuantity"),
							new SetColumnRequiredUpgradeStep("ReceiveItemNonPO.TotalCost"),
							new SetColumnRequiredUpgradeStep("ReceiveItemNonPO.TotalQuantity"),
							new SetColumnRequiredUpgradeStep("ActualItem.TotalCost"),
							new SetColumnRequiredUpgradeStep("ActualItem.TotalQuantity"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemIssue_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemTransfer_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemPO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemNonPO_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualItem_Updates_Corrected"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemAdjustment_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemIssue_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemTransfer_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemPO_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemNonPO_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualItem_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),
							new AddTableUpgradeStep("ItemActivityReport")
						),
						new UpgradeStepSequence( // 1.1.2.4 clean up unneeded stuff
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemLocationReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Ordered_Location_Code"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PermanentItemLocation_CurrentItemCountValue"),
							new AddExtensionObjectUpgradeStep("mbfn_PermanentItemLocation_CurrentItemCountValue"),
							// Steps have been removed; They were adding/deleting dependencies and the upgrader no longer handles dependencies.
							new AddTableUpgradeStep("ItemLocationReport")
						),
						new UpgradeStepSequence( // 1.1.2.5 replace WorkOrderTemplateContainment view with a stored table
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderTemplateLoopCheck"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateWithContainers"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ScheduledWorkOrderReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ExistingAndForecastResources"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderTemplateContainment"),
							new AddTableUpgradeStep("WorkOrderTemplateContainment"),
							new AddTableUpgradeStep("ExistingAndForecastResources"),
							new AddTableUpgradeStep("ScheduledWorkOrderReport"),
							new AddTableUpgradeStep("WorkOrderTemplateWithContainers"),
							new SqlUpgradeStep(@"
							  With PartialContainment (ContainedID, ContainingID, depth)
								as
								(
								  select Id, Id, 0
									from NonLoopedWorkOrderTemplates
								union all
								  select WOT.Id, PC.ContainingID, PC.depth + 1
									from
									  PartialContainment as PC
									join
									  WorkOrderTemplate as WOT
										on WOT.ContainingWorkOrderTemplateID = PC.ContainedID
								)
							  insert into WorkOrderTemplateContainment(Id, ContainedWorkOrderTemplateID, ContainingWorkOrderTemplateID, Depth)
								  select cast(ContainedID as binary(16))+cast(ContainingID as binary(16)), ContainedID, ContainingID, Depth
									from PartialContainment
							"),
							new AddExtensionObjectUpgradeStep("mbtg_SetNewWorkOrderTemplateContainment")
						),
						new UpgradeStepSequence( // 1.1.2.6 replace PurchaseOrderFormReport to add CostCenters for POLines
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderFormReport"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.1.3.x  Release Versions of MB4.1
						#region 1.1.3.0 -
						new UpgradeStepSequence( //1.1.3.0 Set the minimum app version to 4.1
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBAppVersion, new Version(4,1,0,0)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinAReqAppVersion, new Version(4,1,0,0)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBRemoteAppVersion, new Version(4,1,0,0))
						),
						new UpgradeStepSequence( //1.1.3.1 Correct the triggers for physical count and its void, correct resulting bad totals
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemAdjustment_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemAdjustment_Updates_ActualItemLocation"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValue_Updates_ActualItemLocationEtAl"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemCountValueVoid_Updates_ItemCountValueEtAl"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemIssue_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemIssue_Updates_ActualItemLocation"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ItemTransfer_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ItemTransfer_Updates_ActualItemLocation"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemNonPO_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemNonPO_Updates_ActualItemLocation"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ReceiveItemPO_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ReceiveItemPO_Updates_ActualItemLocation"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ActualItem_Updates_ActualItemLocation"),
							new AddExtensionObjectUpgradeStep("mbtg_ActualItem_Updates_ActualItemLocation"),

							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_RetotalInventory"),
							new AddExtensionObjectUpgradeStep("mbsp_RetotalInventory"),
							new SqlUpgradeStep(@"
								-- Remove the CHECK constraint on ActualItemLocation which prevents negative on-hand or total value
								declare @cmd nvarchar(max)
								set @cmd = (select 'alter table ActualItemLocation drop constraint '+CC.CONSTRAINT_NAME
									from INFORMATION_SCHEMA.CHECK_CONSTRAINTS as CC
									join INFORMATION_SCHEMA.CONSTRAINT_TABLE_USAGE as CTU
										on CC.CONSTRAINT_CATALOG = CTU.CONSTRAINT_CATALOG
										and CC.CONSTRAINT_SCHEMA = CTU.CONSTRAINT_SCHEMA
										and CC.CONSTRAINT_NAME = CTU.CONSTRAINT_NAME
									where CTU.TABLE_SCHEMA = 'dbo' and CTU.TABLE_NAME = 'ActualItemLocation')
  
								exec sp_executesql @cmd

								-- Retotal the inventory, possibly generating negative on-hand or total value
								declare @ILID uniqueidentifier
								declare AILs cursor local fast_forward
									for
										select ItemLocationID from ActualItemLocation

								open AILs
								fetch next from AILs into @ILID
								while @@FETCH_STATUS = 0
								begin
									exec mbsp_RetotalInventory @ILID, null
									fetch next from AILs into @ILID
								end

								-- Find and fix AILs with negative on-hand or total value, and note them in the DB history.
								if exists(select * from ActualItemLocation where OnHand < 0 or TotalCost < 0 or (OnHand = 0 and TotalCost != 0)) begin
									-- Determine when the database arrived at 1.1.3.0
									declare @Upgraded datetime
									set @Upgraded = (select EntryDate from DatabaseHistory
																		where Subject = 'Database version 1.1.3.0 created'
																			or Subject like 'Database upgraded from version % to 1.1.3.0')

									-- List all the storage assignments that will be affected.
									declare @dtxmsg nvarchar(max)
									set @dtxmsg = 'Some inventory transactions on the following storage assignments have been cancelled because they result in negative on-hand or total value:' + CHAR(13) + CHAR(10)
									select @dtxmsg = @dtxmsg + 'Item '''+I.Code+''' in location '''+L.Code + '''' + CHAR(13) + CHAR(10)
														from ItemLocation as IL
															join Item as I on I.Id = IL.ItemID
															join Location as L on L.Id = IL.LocationID
															join ActualItemLocation as AIL on AIL.ItemLocationID = IL.Id
															join PermanentItemLocation as PIL on PIL.ActualItemLocationID = AIL.Id
															where AIL.OnHand < 0 or AIL.TotalCost < 0 or (AIL.OnHand = 0 and AIL.TotalCost != 0)

									select @dtxmsg = @dtxmsg + 'Item '''+I.Code+''' in temporary storage for Work Order '''+WO.Number+''' in location '''+CL.Code + '''' + CHAR(13) + CHAR(10)
														from ItemLocation as IL
															join Item as I on I.Id = IL.ItemID
															join Location as L on L.Id = IL.LocationID
															join TemporaryStorage as TS on TS.LocationID = L.Id
															join Location as CL on CL.Id = TS.ContainingLocationID
															join WorkOrder as WO on WO.Id = TS.WorkOrderID
															join ActualItemLocation as AIL on AIL.ItemLocationID = IL.Id
															where AIL.OnHand < 0 or AIL.TotalCost < 0 or (AIL.OnHand = 0 and AIL.TotalCost != 0)
									insert into DatabaseHistory (id, EntryDate, Subject, Description)
											values (newid(), dbo._DClosestValue(getdate(),2,100), '***Cancelled inventory transactions to permit upgrade', @dtxmsg);

									-- Find all the ItemIssue, ItemAdjustment, or ActualItem transactions that result in negative conditions; get IDs for the correction transactions
									select R.*, NEWID() as NewTXID into #toCancel
										from RationalizedInventoryActivityDeltas as R
										left join ItemIssue as II on II.AccountingTransactionID = R.AccountingTransactionID
										left join ItemAdjustment as IA on IA.AccountingTransactionID = R.AccountingTransactionID
										left join ActualItem as AI on AI.AccountingTransactionID = R.AccountingTransactionID
										where EntryDate >= @Upgraded
											and (R.TotalQuantity < 0 or R.TotalCost < 0 or (R.TotalQuantity = 0 and R.TotalCost != 0))
											and (II.Id is not null or IA.Id is not null or AI.Id is not null)

									-- Create the AccountingTransactions for the transactions to cancel
									insert into AccountingTransaction (Id, EntryDate, EffectiveDate, UserID, Cost, FromCostCenterID, ToCostCenterID, AccountingSystemTransactionID)
										select NewTXID, dbo._DClosestValue(GETDATE(), 2, 100), TX.EffectiveDate, UserID, -TX.Cost, TX.FromCostCenterID, TX.ToCostCenterID, null
											from #toCancel as R
											join AccountingTransaction as TX on TX.Id = R.AccountingTransactionID

									-- Create the derived ItemIssue corrections
									insert into ItemIssue (Id, AccountingTransactionID, ItemLocationID, ItemIssueCodeID, EmployeeID, Quantity, CorrectionID, CorrectedQuantity, CorrectedCost, TotalCost, TotalQuantity)
									select NEWID(), NewTXID, II.ItemLocationID, II.ItemIssueCodeID, II.EmployeeID, -II.Quantity, II.CorrectionID, 0, 0, 0, 0
										from #toCancel as R
										join ItemIssue as II on II.AccountingTransactionID = R.AccountingTransactionID

									-- Create the derived ItemAdjustment corrections
									insert into ItemAdjustment (Id, AccountingTransactionID, ItemLocationID, ItemAdjustmentCodeID, Quantity, TotalCost, TotalQuantity)
									select NEWID(), NewTXID, IA.ItemLocationID, IA.ItemAdjustmentCodeID, -IA.Quantity, 0, 0
										from #toCancel as R
										join ItemAdjustment as IA on IA.AccountingTransactionID = R.AccountingTransactionID

									-- Create the derived ActualItem corrections
									insert into ActualItem (Id, AccountingTransactionID, DemandItemID, Quantity, CorrectionID, CorrectedQuantity, CorrectedCost, TotalCost, TotalQuantity)
									select NEWID(), NewTXID, AI.DemandItemID, -AI.Quantity, AI.CorrectionID, 0, 0, 0, 0
										from #toCancel as R
										join ActualItem as AI on AI.AccountingTransactionID = R.AccountingTransactionID
								end

								-- Recreate the CHECK constraint
								alter table ActualItemLocation add check ((OnHand > 0 and TotalCost >= 0) or (OnHand = 0 and TotalCost = 0))

								-- Note any AILs that are deleted with stuff on hand (even if not related to 1.1.3.0 upgrade)
								if exists(select *
												from ActualItemLocation as AIL
												join ItemLocation as IL on IL.Id = AIL.ItemLocationID
												where Hidden is not null
													and AIL.OnHand != 0) begin
									declare @dilmsg nvarchar(max)
									set @dilmsg = 'The following Storage Assignments are deleted but contain items:' + CHAR(13) + CHAR(10)
									select @dilmsg = @dilmsg + 'Item '''+I.Code+''' in location '''+L.Code + '''' + CHAR(13) + CHAR(10)
														from ItemLocation as IL
															join Item as I on I.Id = IL.ItemID
															join Location as L on L.Id = IL.LocationID
															join ActualItemLocation as AIL on AIL.ItemLocationID = IL.Id
															join PermanentItemLocation as PIL on PIL.ActualItemLocationID = AIL.Id
															where AIL.OnHand != 0 and IL.Hidden is not null
									select @dilmsg = @dilmsg + 'Item '''+I.Code+''' in temporary storage for Work Order '''+WO.Number+''' in location '''+CL.Code + '''' + CHAR(13) + CHAR(10)
														from ItemLocation as IL
															join Item as I on I.Id = IL.ItemID
															join Location as L on L.Id = IL.LocationID
															join TemporaryStorage as TS on TS.LocationID = L.Id
															join Location as CL on CL.Id = TS.ContainingLocationID
															join WorkOrder as WO on WO.Id = TS.WorkOrderID
															join ActualItemLocation as AIL on AIL.ItemLocationID = IL.Id
															where AIL.OnHand != 0 and IL.Hidden is not null
									insert into DatabaseHistory (id, EntryDate, Subject, Description)
											values (newid(), dbo._DClosestValue(getdate(),2,100), '***Deleted non-empty Storage Assignments', @dilmsg);
								end

								-- Note any work orders which are closed but have non-empty temporary storage (even if not related to 1.1.3.0 upgrade)
								if exists(select *
													from WorkOrder
													join WorkOrderStateHistory as WOSH on WOSH.Id = workorder.CurrentWorkOrderStateHistoryID
													join WorkOrderState as WOS on WOS.Id = WOSH.WorkOrderStateID
													where WOS.TemporaryStorageActive = 0 and WorkOrder.TemporaryStorageEmpty = 0) begin
									declare @womsg nvarchar(max)
									set @womsg = 'The following Work Orders are closed but have items in Temporary Storage:' + CHAR(13) + CHAR(10)
									select @womsg = @womsg + WorkOrder.Number + CHAR(13) + CHAR(10)
														from WorkOrder
														join WorkOrderStateHistory as WOSH on WOSH.Id = workorder.CurrentWorkOrderStateHistoryID
														join WorkOrderState as WOS on WOS.Id = WOSH.WorkOrderStateID
														where WOS.TemporaryStorageActive = 0 and WorkOrder.TemporaryStorageEmpty = 0
									insert into DatabaseHistory (id, EntryDate, Subject, Description)
											values (newid(), dbo._DClosestValue(getdate(),2,100), '***Closed Work Orders with non-empty Temporary Storage', @womsg);
								end
							")
						),
						new UpgradeStepSequence( //1.1.3.2 Put back function to create ordered location codes
							new AddExtensionObjectUpgradeStep("mbfn_OrderByRank"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationReport"),
							new AddTableUpgradeStep("LocationReport")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.1.4.x  Development Versions of MB4.2
						#region 1.1.4.0 - 1.1.4.9
						new UpgradeStepSequence( //1.1.4.0 Set the minimum app version
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBAppVersion, new Version(4,2,0,0)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinAReqAppVersion, new Version(4,2,0,0)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBRemoteAppVersion, new Version(4,2,0,0))
						),
						new UpgradeStepSequence( //1.1.4.1 Remove the UserID field and related crap from Contact table
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_Updates_Contact"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Contact_UserID"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Contact.UserID")
						),
						new UpgradeStepSequence( //1.1.4.2 Transition the User table from UserName/ScopeName to AuthenticationCredential (SYSTEM_USER)
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_User_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_AddDataBaseUser"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_GetDefaultWindowsDomain"),
							new RemoveVariableUpgradeStep(GetOriginalSchema, "ManageServerSecurity"),
							new RemoveTableUpgradeStep(GetOriginalSchema,"AttentionStatus"),
							new AddTableUpgradeStep("AttentionStatus"),
							new AddExtensionObjectUpgradeStep("mbsp_AddDataBaseUser"),
							new AddExtensionObjectUpgradeStep("mbsp_DropDataBaseUser"),
							new AddExtensionObjectUpgradeStep("mbsp_MaintainSecurity"),
							new AddColumnUpgradeStep("User.AuthenticationCredential"),
							new SqlUpgradeStep(@"
								declare @domain nvarchar(128)
								declare @ServerVariant int
								DECLARE @key varchar(128)

								SELECT @ServerVariant = CONVERT(INT, SERVERPROPERTY('EngineEdition'))
								-- EngineEdition less than or equal to 4 are SQL servers with traditional login and user model
								if @ServerVariant <= 4
								BEGIN
									-- DEFAULT_DOMAIN returns the work group when the sql server is on a machine that is not a member of a domain
									-- The registry read give the DNS domain name if the sql server is a member of the domain or null otherwise.
									SET @key = 'SYSTEM\ControlSet001\Services\Tcpip\Parameters\'
									BEGIN TRY
									EXEC master..xp_regread @rootkey='HKEY_LOCAL_MACHINE', @key=@key,@value_name='Domain',@value=@Domain OUTPUT
									END TRY
									BEGIN CATCH
										RAISERROR ('You do not have permissions to use the xp_regread function to determine the server domain name, or the function no longer exists' , 16, 1)
									END CATCH
									SET @Domain = Case when @Domain is null then Convert(nvarchar(128),SERVERPROPERTY('MachineName')) else DEFAULT_DOMAIN() end

									UPDATE [User] SET AuthenticationCredential = SUSER_SNAME(SUSER_SID(ScopeName + '\' + UserName, 0)) from [USER] where ScopeName IS NOT NULL
									UPDATE [User] SET AuthenticationCredential = ScopeName + '\' + UserName from [USER] where ScopeName IS NOT NULL AND AuthenticationCredential IS NULL
									UPDATE [User] SET AuthenticationCredential = SUSER_SNAME(SUSER_SID(@domain + '\' + UserName, 0)) from [USER] where ScopeName IS NULL
									UPDATE [User] SET AuthenticationCredential = UPPER(@domain) + '\' + UserName from [USER] where ScopeName IS NULL AND AuthenticationCredential IS NULL
								END
								ELSE IF @ServerVariant = 5  -- SQL Database (Azure) Only way this exists is it was created with a SQL Userid name (UserName only)
								BEGIN
									UPDATE [User] SET AuthenticationCredential = UserName from [USER] where AuthenticationCredential IS NULL
								END
							"
							),
							new SetColumnRequiredUpgradeStep("User.AuthenticationCredential"),
							new AddUniqueConstraintUpgradeStep("User.AuthenticationCredential"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "User.UserName"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "User.ScopeName"),
							new RemoveColumnUpgradeStep(() => {return dsLicense_1_0_10_3.Schema;}, "License.ExpiryModelName"),
							new RemoveColumnUpgradeStep(() => {return dsLicense_1_0_10_3.Schema;}, "License.LicenseModelName"),
							new RemoveColumnUpgradeStep(() => {return dsLicense_1_0_10_3.Schema;}, "License.ApplicationName")
						),
						new UpgradeStepSequence( // 1.1.4.3 Fix DropUser to work on Azure
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_DropDataBaseUser"),
							new AddExtensionObjectUpgradeStep("mbsp_DropDataBaseUser"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_User_Has_MainBoss_Access"),
							new AddExtensionObjectUpgradeStep("mbfn_User_Has_MainBoss_Access")
						),
						new UpgradeStepSequence( // 1.1.4.4 Cleanup DatabaseStatus view
							new RemoveTableUpgradeStep(GetOriginalSchema, "DatabaseStatus"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "OverdueWorkOrder"),
							new AddTableUpgradeStep("DatabaseStatus")
						),
						new UpgradeStepSequence( //1.1.4.5 Put back function to create ordered location codes (merged from 4.1, Update 3)
							new Version(1,1,4,0), 	// Already upgraded in the 1.1.3.x stream as 1.1.3.2, only needed if we start >= 1.1.4.x
							new AddExtensionObjectUpgradeStep("mbfn_OrderByRank"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "LocationReport"),
							new AddTableUpgradeStep("LocationReport")
						),
						new UpgradeStepSequence( //1.1.4.6 Expand StateTransition tables to control copying of Status and to include "add comment"
							new AddColumnUpgradeStep("WorkOrderStateTransition.CopyStatusFromPrevious"),
							new AddColumnUpgradeStep("PurchaseOrderStateTransition.CopyStatusFromPrevious"),
							new AddColumnUpgradeStep("RequestStateTransition.CopyStatusFromPrevious"),
							new SqlUpgradeStep(@"
								update WorkOrderStateTransition set CopyStatusFromPrevious = 0
								update PurchaseOrderStateTransition set CopyStatusFromPrevious = 0
								update RequestStateTransition set CopyStatusFromPrevious = 0
							"),
							new AddAddCommentStateTransitionRecordsUpgradeStep(),
							new SetColumnRequiredUpgradeStep("WorkOrderStateTransition.CopyStatusFromPrevious"),
							new SetColumnRequiredUpgradeStep("PurchaseOrderStateTransition.CopyStatusFromPrevious"),
							new SetColumnRequiredUpgradeStep("RequestStateTransition.CopyStatusFromPrevious")
						),
						new UpgradeStepSequence( //1.1.4.7 Make TableEnum return type match the schema definition to not confuse LINQ/SQL fetching
							new RemoveTableUpgradeStep(GetOriginalSchema, "ItemActivity"),
							new AddTableUpgradeStep("ItemActivity")
						),
						new UpgradeStepSequence( //1.1.4.8 Change Cycle detection trigger in ManageRequestTransition to permit transistions from one state to SAME state
							new AddVariableUpgradeStep("CopyWSHCommentToRSH"),
							new SqlUpgradeStep(
							@"
								exec dbo._vsetCopyWSHCommentToRSH 0;
							"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ManageRequestTransitionCycleDetection"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_RequestedWorkOrder_ManageRequest"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_ManageRequest"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_ManageRequestTransitionCycleDetection")
						),
						new UpgradeStepSequence( //1.1.4.9 Saving ServiceConfiguration no longer tries to give permission to service user
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_ServiceConfiguration_MaintainSecurity")
						),
						#endregion
						#region 1.1.4.10 - 1.1.4.15
						new UpgradeStepSequence(// 1.1.4.10 Add PurchaseOrderCategory/Project references to Purchasing
							new AddTableUpgradeStep("PurchaseOrderCategory"),
							new AddColumnUpgradeStep("PurchaseOrder.ProjectID"),
							new AddColumnUpgradeStep("PurchaseOrder.PurchaseOrderCategoryID"),
							new AddColumnUpgradeStep("PurchaseOrderTemplate.ProjectID"),
							new AddColumnUpgradeStep("PurchaseOrderTemplate.PurchaseOrderCategoryID")
						),
						new UpgradeStepSequence(// 1.1.4.11 Add SqlUserid and remove ServiceName from service configuration
							new RemoveColumnUpgradeStep(GetOriginalSchema,"ServiceConfiguration.ServiceName"),
							new AddColumnUpgradeStep("ServiceConfiguration.SqlUserid")
						),
						new UpgradeStepSequence(// 1.1.4.12 Add the NewsURL variable
							new AddVariableUpgradeStep("NewsURL")
						),
						new UpgradeStepSequence(// 1.1.4.13 Cleanup EmailRequestID linkages)
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Request.EmailRequestID"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Request_EmailRequest"),
							new AddUniqueConstraintUpgradeStep("EmailRequest.RequestID")
						),
						new UpgradeStepSequence(// 1.1.4.14 Remove mbsp_MaintainSecurity/mbfn_User_Has_MainBoss_Access no longer used
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbsp_MaintainSecurity"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_User_Has_MainBoss_Access")
						),
						new UpgradeStepSequence(// 1.1.4.15 Add WorkOrder WorkDueDate stuff
							new AddColumnUpgradeStep("WorkOrder.WorkDueDate"),
							// So that the Overdue status of old WOs more or less mimics old behaviour, we set the WorkDueDate on existing WOs to be the EndDateEstimate
							new SqlUpgradeStep("Update WorkOrder set WorkDueDate = EndDateEstimate"),
							new AddColumnUpgradeStep("ScheduledWorkOrder.SlackDays"),
							// So newly-generated work orders have due dates, we initialize SlackDays to zero.
							new SqlUpgradeStep("Update ScheduledWorkOrder set SlackDays = 0")
						),
						#endregion
					},
					new UpgradeStep[] { // 1.1.5.x  Release Versions of MB4.2
						#region 1.1.5.0 - 1.1.5.9
						new UpgradeStepSequence( //1.1.5.0 Set the minimum app version to 4.2
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBAppVersion, new Version(4,2,0,4)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinAReqAppVersion, new Version(4,2,0,4)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBRemoteAppVersion, new Version(4,2,0,4))
						),
						new UpgradeStepSequence( //1.1.5.1 Clean up WorkOrderReport
							new RemoveColumnUpgradeStep(GetOriginalSchema, "Request.CountOfLinkedWorkOrders"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrder_History_As_String"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_WorkOrder_Assignee_List"),
							new AddTableUpgradeStep("WorkOrderExtras"),
							new AddTableUpgradeStep("MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReportX"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Request_CountOfLinkedWorkOrders"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_Request_Assignee_List"),
							new AddTableUpgradeStep("RequestExtras"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderReport"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbfn_PurchaseOrder_Assignee_List"),
							new AddTableUpgradeStep("PurchaseOrderExtras"),
							new AddTableUpgradeStep("PurchaseOrderAssignmentReport")
						),
						new UpgradeStepSequence( //1.1.5.2 Set the minimum app version to 4.2
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBAppVersion, new Version(4,2,0,7)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinAReqAppVersion, new Version(4,2,0,7)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBRemoteAppVersion, new Version(4,2,0,7))
						),
						// Databases upgraded with Head code had other steps numbered 1.1.5.3, 1.1.5.4, or 1.1.5.5.
						// These did not make the cut for 4.2 and so have been combined into a single step 1.1.6.0.
						// However, to ensure that DB's upgraded with the Head code in the interem properly get the remaining 1.1.5.x steps
						// these three dummy steps have been added to synchronize the numbering.
						// A side effect of this is that sites running 4.2 may re-execute up to three of the steps now numbered 1.1.5.6-1.1.5.10
						// (formerly 1.1.5.3-1.1.5.8 in 4.2) but these steps are all idempotent: delete a view or function and re-create it.
						new UpgradeStepSequence(// Dummy 1.1.5.3
						),
						new UpgradeStepSequence(// Dummy 1.1.5.4
						),
						new UpgradeStepSequence(// Dummy 1.1.5.5
						),
						new UpgradeStepSequence(// 1.1.5.6 (originally 1.1.5.3) Replace date/interval functions with more effecient ones
							new BuiltinFunctionUpdateUpgradeStep("_DMinValue"),
							new BuiltinFunctionUpdateUpgradeStep("_DClosestValue"),
							new BuiltinFunctionUpdateUpgradeStep("_IIToSum"),
							new BuiltinFunctionUpdateUpgradeStep("_IIToMilliseconds"),
							new BuiltinFunctionUpdateUpgradeStep("_ISumToI"),
							new BuiltinFunctionUpdateUpgradeStep("_IAdd"),
							new BuiltinFunctionUpdateUpgradeStep("_ISubtract"),
							new BuiltinFunctionUpdateUpgradeStep("_ISum"),
							new BuiltinFunctionUpdateUpgradeStep("_IDiff"),
							new BuiltinFunctionUpdateUpgradeStep("_INegate"),
							new BuiltinFunctionUpdateUpgradeStep("_IScale"),
							new BuiltinFunctionUpdateUpgradeStep("_IRatio"),
							new BuiltinFunctionUpdateUpgradeStep("_INew"),
							new BuiltinFunctionUpdateUpgradeStep("_IDateDiff")
						),
						new UpgradeStepSequence(// 1.1.5.7 (originally 1.1.5.4) Replace WorkOrderExtras view to properly calculate EarliestEndDate
							new RemoveTableUpgradeStep(GetOriginalSchema, "MaintenanceForecastReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderExtras"),
							new AddTableUpgradeStep("WorkOrderExtras"),
							new AddTableUpgradeStep("MaintenanceForecastReport")
						),
						new UpgradeStepSequence(// 1.1.5.8 (originally 1.1.5.5) Add DemandID and POLineID to AccountingTransactionDerivations view
							new RemoveTableUpgradeStep(GetOriginalSchema, "AccountingTransactionVariants"),
							new AddTableUpgradeStep("AccountingTransactionVariants")
						),
						new UpgradeStepSequence(// 1.1.5.9 (originally 1.1.5.6) Correct ticks per day and _IRatio definition
							new BuiltinFunctionUpdateUpgradeStep("_IRatio"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("ActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("AssignedActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("WorkOrderEndDateHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedWorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("AssignedWorkOrderEndDateHistogram")
						),
						#endregion
						#region 1.1.5.10 -
						new UpgradeStepSequence(// 1.1.5.10 (originally 1.1.5.7) Correct _DClosestValue for dates on or before 1/jan/1900
							new BuiltinFunctionUpdateUpgradeStep("_DClosestValue")
						),
						new UpgradeStepSequence(	// 1.1.5.11 Add schemabinding to date/time math functions and new fns
							// Most of this is also in 1.1.6.20 for databases that were already in the Head chain whan this step was added.
							new BuiltinFunctionCreateUpgradeStep("_DClosestDivisions"),
							new BuiltinFunctionCreateUpgradeStep("_DClosestMonths"),
							new BuiltinFunctionCreateUpgradeStep("_DClosestTicks"),
							new BuiltinFunctionUpdateUpgradeStep("_DMinValue"),
							new BuiltinFunctionUpdateUpgradeStep("_DClosestValue"),
							new BuiltinFunctionUpdateUpgradeStep("_IIToSum"),
							new BuiltinFunctionUpdateUpgradeStep("_IIToMilliseconds"),
							new BuiltinFunctionUpdateUpgradeStep("_ISumToI"),
							new BuiltinFunctionUpdateUpgradeStep("_IAdd"),
							new BuiltinFunctionUpdateUpgradeStep("_ISubtract"),
							new BuiltinFunctionUpdateUpgradeStep("_ISum"),
							new BuiltinFunctionUpdateUpgradeStep("_IDiff"),
							new BuiltinFunctionUpdateUpgradeStep("_INegate"),
							new BuiltinFunctionUpdateUpgradeStep("_IScale"),
							new BuiltinFunctionUpdateUpgradeStep("_IRatio"),
							new BuiltinFunctionUpdateUpgradeStep("_INew"),
							new BuiltinFunctionUpdateUpgradeStep("_IDateDiff")
						),
						new UpgradeStepSequence(	// 1.1.5.12 Update views to use new _DClosestXxxx functions
							// This is a duplicate of 1.1.6.22
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("AssignedActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedWorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("AssignedWorkOrderEndDateHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "PurchaseOrderExtras"),
							new AddTableUpgradeStep("PurchaseOrderExtras"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_RequestedWorkOrder_ManageRequest"),
							new AddExtensionObjectUpgradeStep("mbtg_RequestedWorkOrder_ManageRequest"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestExtras"),
							new AddTableUpgradeStep("RequestExtras"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "RequestReport"),
							new AddTableUpgradeStep("RequestReport"),
							new AddTableUpgradeStep("RequestAssignmentReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("WorkOrderEndDateHistogram"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl")
						),
						new UpgradeStepSequence(	// 1.1.5.13 Update views to always return Sql DATETIME type for date columns
							// This is a duplicate of 1.1.6.23
							new RemoveTableUpgradeStep(GetOriginalSchema, "ActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("ActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedActiveRequestAgeHistogram"),
							new AddTableUpgradeStep("AssignedActiveRequestAgeHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "AssignedWorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("AssignedWorkOrderEndDateHistogram"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "UnitReport"),
							new AddTableUpgradeStep("UnitReport"),
							new RemoveTableUpgradeStep(GetOriginalSchema, "WorkOrderEndDateHistogram"),
							new AddTableUpgradeStep("WorkOrderEndDateHistogram"),
							new RemoveExtensionObjectUpgradeStep(GetOriginalSchema, "mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl"),
							new AddExtensionObjectUpgradeStep("mbtg_WorkOrderStateHistory_Updates_WorkOrderEtAl")
						),
						new UpgradeStepSequence(	// 1.1.5.14 Widen encrypted password fields in service config record
							// This is a duplicate of 1.1.6.24
							// We don't preserve the values from the _D table because we have never let anyone put non-null default passwords in
							new SqlUpgradeStep("alter table ServiceConfiguration add TempPassword varbinary(max)"),
							new SqlUpgradeStep("update ServiceConfiguration set TempPassword = SMTPEncryptedPassword"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.SMTPEncryptedPassword"),
							new AddColumnUpgradeStep("ServiceConfiguration.SMTPEncryptedPassword"),
							new SqlUpgradeStep("update ServiceConfiguration set SMTPEncryptedPassword = TempPassword, TempPassword = MailEncryptedPassword"),
							new RemoveColumnUpgradeStep(GetOriginalSchema, "ServiceConfiguration.MailEncryptedPassword"),
							new AddColumnUpgradeStep("ServiceConfiguration.MailEncryptedPassword"),
							new SqlUpgradeStep(@"
								update ServiceConfiguration set MailEncryptedPassword = TempPassword
                                alter table ServiceConfiguration drop column TempPassword"),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinMBAppVersion, new Version(4,2,4,9)),
							new SqlMinApplicationVersionUpgradeStep(dsMB.Schema.V.MinAReqAppVersion, new Version(4,2,4,9))
						)
						#endregion
					}
				}
				#endregion
			}
		},
		// The DEBUG check schema has CODE; update when Schema has changed and upgrade steps have been added
		0Xc6fd6d2c208e955UL, dsMB.Schema);
		#endregion
	}
}
