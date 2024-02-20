using System;
using Thinkage.Libraries.Presentation;
using Thinkage.MainBoss.Database;


namespace Thinkage.MainBoss.Controls {
	#region DetailsTabNode
	public static class DetailsTabNode {
		/// <summary>
		/// This is used to create a TabNode labeled Details that can be merged with columns outside the TabNode
		/// </summary>
		/// <param name="columns"></param>
		/// <returns></returns>
		public static TblTabNode New(params TblLayoutNode[] columns) {
			return TblTabNode.NewMergeable(KB.K("Details"), KB.K("Display the general properties for this record"), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, columns);
		}
	}
	#endregion
	#region BrowsetteTabNode
	/// <summary>
	/// A helper class to provide consistent labels and tips for common browsette arrangements where the browsette is simply a display of records tied to the master record of the enclosing Tbl
	/// Labels and tips are composed of the Tbl.TblIdentification of the master and target tbl
	/// </summary>
	public static class BrowsetteTabNode {
		/// <summary>
		/// Common browsettes target other Tbl's, and the label is the collective form of the target, with a tip that refers to the target collectively related to the master tbl
		/// </summary>
		/// <param name="master"></param>
		/// <param name="target"></param>
		/// <param name="browsette"></param>
		/// <returns></returns>
		public static TblTabNode New(Tbl.TblIdentification target, Tbl.TblIdentification master, TblLayoutNode browsette) {
			return TblTabNode.New(KB.TOBrowsetteLabel(master, target), KB.TOBrowsetteTip(master, target), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, browsette);
		}
		public static TblTabNode New(Tbl.TblIdentification target, Tbl.TblIdentification master, TblLayoutNode.ICtorArg[] attrs, params TblLayoutNode[] columns) {
			return TblTabNode.New(KB.TOBrowsetteLabel(master, target), KB.TOBrowsetteTip(master, target), new TblLayoutNode.ICtorArg[] { DCol.Normal, ECol.Normal }, columns);
		}
	}
	#endregion
	#region Pre-built TblLayoutNode attributes: PermissionToView or PermissionToEdit applied using particular permissions
	public static class CommonListColumnAttrs {
		public static BTbl.ListColumnArg.IAttr PermissionToViewAccounting = new PermissionToView(Root.Rights.Action.ViewAccounting);
	}
	public static class CommonNodeAttrs {
		public static TblLayoutNode.ICtorArg PermissionToViewAccounting = new PermissionToView(Root.Rights.Action.ViewAccounting);
		public static TblLayoutNode.ICtorArg PermissionToEditAccounting = new PermissionToEdit(Root.Rights.Action.EditAccounting);
		public static TblLayoutNode.ICtorArg PermissionToEditGlobalSettings = new PermissionToEdit(Root.Rights.Action.Customize);   // TODO: For now. Do we want a separate permission for this?
		public static TblLayoutNode.ICtorArg PermissionToMergeContacts = new PermissionToEdit(Root.Rights.Action.MergeContacts);
		public static TblLayoutNode.ICtorArg ViewUnitValueCosts = new PermissionToView(Root.Rights.ViewCost.UnitValue);
		public static TblLayoutNode.ICtorArg[] ViewTotalWorkOrderCosts = new TblLayoutNode.ICtorArg[] {
			new PermissionToView(Root.Rights.ViewCost.WorkOrderInside),
			new PermissionToView(Root.Rights.ViewCost.WorkOrderItem),
			new PermissionToView(Root.Rights.ViewCost.WorkOrderMiscellaneous),
			new PermissionToView(Root.Rights.ViewCost.WorkOrderOutside)
		};
		public static TblLayoutNode.ICtorArg[] ViewTotalPurchaseOrderCosts = new TblLayoutNode.ICtorArg[] {
			new PermissionToView(Root.Rights.ViewCost.PurchaseOrderLabor),
			new PermissionToView(Root.Rights.ViewCost.PurchaseOrderItem),
			new PermissionToView(Root.Rights.ViewCost.PurchaseOrderMiscellaneous)
		};
		public static TblLayoutNode.ICtorArg ViewUnitSparePartCosts = new PermissionToView(Root.Rights.ViewCost.UnitSparePart);
	}
	#endregion
}
