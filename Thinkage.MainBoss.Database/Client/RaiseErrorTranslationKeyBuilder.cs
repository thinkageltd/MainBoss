using Thinkage.Libraries;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database {
	// TODO: This should probably not be done this way. XAFDB should be extended so that custom messages can be declared by providing their format string.
	// The exception mapping would automatically be done by the ISession object (?)
	// Because the format would be available, the unformatting operation could be done (as it is for stock SQL messages) to extract the insertions.
	// The format string would be the key for the message (context from same or another XAFDB declaration?). Type information for the insertions
	// could also tell if they are e.g. column or table names, for which the labelkey should be fetched.
	public class RaiseErrorTranslationKeyBuilder : GroupedInterface<IApplicationInterfaceGroup>, IRaiseErrorTranslationKeyBuilder {
		public RaiseErrorTranslationKeyBuilder(GroupedInterface<IApplicationInterfaceGroup> attachTo)
			: base(attachTo) {
			RegisterService<IRaiseErrorTranslationKeyBuilder>(this);
		}
		#region IRaiseErrorTranslationKeyBuilder Members

		public Key MakeKey(string raiseErrorExceptionMessage) {
			return RaiseErrorsTranslation.FindKey(raiseErrorExceptionMessage);
		}
		#endregion
	}
	static internal class RaiseErrorsTranslation {
		static internal Key FindKey(string message) {
			if (message.StartsWith(KB.I("EffectiveDate"))) {
				if (message.Contains(KB.I("Request"))) {
					if (message.EndsWith(KB.I("altered")))
						return KB.K("EffectiveDate-order of records in table 'RequestStateHistory' cannot be altered");
					else
						return KB.K("EffectiveDate-order of new records in table 'RequestStateHistory' must be later than those of existing records for the same Request");
				}
				if (message.Contains(KB.I("WorkOrder")))
					if (message.EndsWith(KB.I("altered")))
						return KB.K("EffectiveDate-order of records in table 'WorkOrderStateHistory' cannot be altered");
					else
						return KB.K("EffectiveDate-order of new records in table 'WorkOrderStateHistory' must be later than those of existing records for the same Work Order");
				if (message.Contains(KB.I("PurchaseOrder")))
					if (message.EndsWith(KB.I("altered")))
						return KB.K("EffectiveDate-order of records in table 'PurchaseOrderStateHistory' cannot be altered");
					else
						return KB.K("EffectiveDate-order of new records in table 'PurchaseOrderStateHistory' must be later than those of existing records for the same Purchase Order");
			}
			if( message.Equals(KB.I("Cannot change Cost Center for Storage Assignment when Total Cost is not zero")))
				return KB.K("Cannot change Cost Center for Storage Assignment when Total Cost is not zero");
			if( message.Equals(KB.I("Adding a transition would result in an infinite cycle of state transitions")))
				return KB.K("Adding a transition would result in an infinite cycle of state transitions");
			if (message.Equals(KB.I("Meter readings must not decrease with increasing Effective Date")))
				return KB.K("Meter readings must not decrease with increasing Effective Date");
			if (message.Equals(KB.I("A record in table 'WorkOrderTemplate' cannot contain itself directly or indirectly")))
				return KB.K("The Containing Task cannot contain itself directly or indirectly");
			if (message.EndsWith(KB.I("predates the latest Physical Count"))) {
				// Message format is 'RecordType' record predates
				// extract the 'RecordType and build a translated message
				int firstQuote = message.IndexOf('\'');
				int lastQuote = message.LastIndexOf('\'');
				string activity = message.Substring(firstQuote + 1, lastQuote - firstQuote-1);
				// It would be nice if the raiserror arguments used identical activity labels to the existing translation TId that they correspond to to avoid the next set of mapping.
				if( activity.Equals(KB.I("ItemCountValue")))
					activity = KB.K("Physical Count").Translate();
				else if( activity.Equals(KB.I("ItemCountValueVoid")))
					activity = KB.K("Void Physical Count").Translate();
				else if( activity.Equals(KB.I("ItemAdjustment")))
					activity = KB.K("Item Adjustment").Translate();
				else if( activity.Equals(KB.I("ItemIssue")))
					activity =  KB.K("Item Issue").Translate();
				else if( activity.Equals(KB.I("ItemTransfer")))
					activity = KB.K("Item Transfer").Translate();
				else if( activity.Equals(KB.I("ReceiveItemNonPO")))
					activity = KB.K("Receive Item (no PO)").Translate();
				else if( activity.Equals(KB.I("ReceiveItemPO")))
					activity = KB.K("Receive Item (with PO)").Translate();
				else if( activity.Equals(KB.I("ActualItem")))
					activity = KB.K("Actual Item").Translate();
				return KB.T(Strings.Format(KB.K("The '{0}' activity predates the latest Physical Count"), activity));
			}
			return KB.T(message);
		}
	}
}
