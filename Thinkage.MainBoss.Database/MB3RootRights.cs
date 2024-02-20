using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Translation;
using Thinkage.Libraries;
using System.Linq;

namespace Thinkage.MainBoss.Database
{
	#region MB3RootRights - The Rights structure that defines all the controlled operations in the app.
	public class MB3RootRights : NamedRightsGroup
	{
		#region Constructor
		public MB3RootRights()
			: base()
		{
		}
		#region ViewCostGroupClass
		public class ViewCostGroupClass : NamedRightsGroup
		{
			public ViewCostGroupClass(RightsGroup parent, [Invariant]string groupName)
				: base(parent, groupName)
			{
			}
			// Initialization of any permission here is done in base class using name of member as the permission
			// These correspond to the enum values for the costright_type type in SecuritySchema.xsd
			public readonly Right WorkOrderItem;
			public readonly Right WorkOrderInside;
			public readonly Right WorkOrderOutside;
			public readonly Right WorkOrderMiscellaneous;
			public readonly Right UnitValue;
			public readonly Right LaborInside;
			public readonly Right LaborOutside;
			public readonly Right InventoryActivity;
			public readonly Right Chargeback;
			public readonly Right UnitSparePart;
			public readonly Right PurchaseOrderItem;
			public readonly Right PurchaseOrderLabor;
			public readonly Right PurchaseOrderMiscellaneous;
			public readonly Right ServiceContract;
		}
		#endregion
		#region ActionGroupClass
		public class ActionGroupClass : NamedRightsGroup
		{
			public ActionGroupClass(RightsGroup parent, [Invariant]string groupName)
				: base(parent, groupName)
			{
			}
			// Initialization of any permission here is done in base class using name of member as the permission
			// These correspond to the enum values for the actionright_type type in SecuritySchema.xsd
			public readonly Right ViewAccounting;								// Permission to view accounting-related information
			public readonly Right EditAccounting;								// Permission to edit accounting-related information
			public readonly Right UpgradeDatabase;								// Permission to upgraded this database
			public readonly Right Customize;                                    // Permission to Customize the UI
			public readonly Right MergeContacts;									// Permission to Merge Contacts
		}
		#endregion
		#region TransitionGroupClass
		public class TransitionGroupClass : NamedRightsGroup
		{
			public class WorkOrderTransitionGroupClass : NamedRightsGroup
			{
				public WorkOrderTransitionGroupClass(RightsGroup parent, [Invariant]string groupName)
					: base(parent, groupName)
				{
				}
				public Right Draft;
				public Right Open;
				public Right Reopen;
				public Right Close;
				public Right Void;
				public Right Suspend;
			}
			public class PurchaseOrderTransitionGroupClass : NamedRightsGroup
			{
				public PurchaseOrderTransitionGroupClass(RightsGroup parent, [Invariant]string groupName)
					: base(parent, groupName)
				{
				}
				public Right Draft;
				public Right Issue;
				public Right ReActivate;
				public Right Close;
				public Right Void;
				public Right Withdraw;
			}
			public class RequestTransitionGroupClass : NamedRightsGroup
			{
				public RequestTransitionGroupClass(RightsGroup parent, [Invariant]string groupName)
					: base(parent, groupName)
				{
				}
				public Right InProgress;
				public Right Reopen;
				public Right Close;
				public Right Void;
			}
			public TransitionGroupClass(RightsGroup parent, [Invariant]string groupName)
				: base(parent, groupName)
			{
			}
			public WorkOrderTransitionGroupClass WorkOrder;
			public PurchaseOrderTransitionGroupClass PurchaseOrder;
			public RequestTransitionGroupClass Request;
		}
		#endregion
		public readonly TableRightsGroup Table;
		public readonly ActionGroupClass Action;
		public readonly ViewCostGroupClass ViewCost;
		public readonly TransitionGroupClass Transition;
		#endregion
		/// <summary>
		/// A helper function to validate permissions that a user may be trying to create. We return null if the pattern provided matches a permitted permission.
		/// Otherwise we provide our best suggestion as to what the pattern best matches
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		private static readonly char[] SplitOnDot = new char[] { '.' };
		public GeneralException ValidatePermissionPattern([Invariant]string permissionPattern) {
			string[] parts = permissionPattern.Split(SplitOnDot);
			switch (parts.Length) {
				case 0:
					break;
				case 1:
				case 2:
				case 3: {
						RightsGroupItem item = this[parts[0]];
						if (item == null)
							return new GeneralException(KB.K("Looking for an identifier from set of {0}"), GetChoices(this));
						RightsGroup subGroup = item as RightsGroup;
						if (parts.Length < 2 && subGroup != null) // expecting another level of items and we don't have anything to lookup; get list of choices
							return new GeneralException(KB.K("{0} group is looking for an identifier from set of *, {1}"), subGroup.Name, GetChoices(subGroup));

						if (subGroup == null)
							return new GeneralException(KB.K("{0} is not a group expecting any more identifiers"), item.Name);
						bool wildcard = Strings.Equals(parts[1], "*");
						if (wildcard) {
							// need one of the direct children as a proxy for checking validity to get next level of choices if we
							foreach (RightsGroupItem rg in subGroup) {
								parts[1] = rg.Name; // use the first one
								break;	 // and stop
							}
						}
						item = subGroup[parts[1]];
						if( item == null )
							return new GeneralException(KB.K("{0} group is looking for an identifier from set of *, {1}"), subGroup.Name, GetChoices(subGroup));
						subGroup = item as RightsGroup;
						if (parts.Length < 3 ) {
							if (subGroup != null && wildcard) { // Get list of choices at this level for
								return new GeneralException(KB.K("{0} group is looking for an identifier from set of *, {1}"), subGroup.Name, GetChoices(subGroup));
							}
							return null; // everybody happy at present
						}
						if(subGroup == null)
							return new GeneralException(KB.K("{0} is not a group expecting any more identifiers"), wildcard ? "*" : item.Name);
						wildcard = Strings.Equals(parts[2], "*");
						if (wildcard) {
							// need one of the direct children as a proxy to get next level of choices if we only have '*' to work with
							foreach (RightsGroupItem rg in subGroup) {
								parts[2] = rg.Name; // use the first one
								break;	 // and stop
							}
						}
						item = subGroup[parts[2]];
						if (item == null)
							return new GeneralException(KB.K("{0} group is looking for an identifier from set of *, {1}"), subGroup.Name, GetChoices(subGroup));
						break;
					}
				default:
					return new GeneralException(KB.K("MainBoss permissions have the form [{0}].[* | identifier].[* | identifier]"), GetChoices(this, "|"));
			}
			return null;

		}
		private string GetChoices(RightsGroup g, [Invariant] string separator = ", ") {
			// Didn't find in our RightsGroup, return the set that is allowed
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int counter = 0;
			foreach (var rg in (from n in g
								orderby n.Name ascending
								select n)) {
				if (counter++ > 0) {
					if ((counter % 5) == 0)
						sb.Append(System.Environment.NewLine);
					else
						sb.Append(separator);
				}
				sb.Append(rg.Name);
			}
			return sb.ToString();
		}
	}
	#endregion
}