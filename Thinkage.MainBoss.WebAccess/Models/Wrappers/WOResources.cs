using System;
using System.Collections.Generic;
using Thinkage.Libraries;
using Thinkage.Libraries.TypeInfo;
using Thinkage.MainBoss.Database;
using WOResourceEntities;

namespace Thinkage.MainBoss.WebAccess.Models {
	#region Interfaces & Descriptors
	[Serializable]
	public struct ResourceDescription {
		public string Description;
		public bool HiddenDescription;
		public string Location;
		public bool HiddenLocation;
	}
	interface IWOResource {
		string Quantity {
			get;
			set;
		}
		string ActualQuantity {
			get;
			set;
		}
		ResourceDescription Description {
			get;
		}
		bool CanBeActualized {
			get;
		}
		Guid Id {
			get;
			set;
		}
		IDictionary<string, object> InputAttributes {
			get;
			set;
		}
	}
	#endregion
	#region WOResource
	/// <summary>
	/// The basic WOResource for displaying information to the user
	/// </summary>
	[Serializable]
	public class WOResource : IWOResource {
		public Guid Id {
			get;
			set;
		}
		public ResourceDescription Description {
			get;
			set;
		}
		public string Quantity {
			get;
			set;
		}
		public string ActualQuantity {
			get;
			set;
		}
		public bool CanBeActualized {
			get;
			set;
		}
		public string RemainingToActualize {
			get;
			set;
		}
		public IDictionary<string, object> InputAttributes {
			get;
			set;
		}
	}
	#endregion
	#region WOResourceInfo
	public interface IWOResourceInfo {
		Guid Id {
			get;
			set;
		}
		void Actualize(dsMB ds);
		WOResource MakeWOResource();
		void SetQuantityToActualize(string v);
	}
	#region WOResourceInfo<T>
	/// <summary>
	/// A representation of a specific typed quantity resource used to build WOResource entities.
	/// </summary>
	abstract public class WOResourceInfo<T> : IWOResourceInfo
		where T : struct {
		public WOResourceInfo(WOResourceEntities.Demand demand, TypeInfo type) {
			DemandBase = demand;
			if (type != null) // type is null for MiscellaneousWorkOrderCost
				QuantityType = type.IntersectCompatible(ObjectTypeInfo.NonNullUniverse);
		}
		/// <summary>
		/// Produce a simple serializable representation of our values
		/// </summary>
		/// <returns></returns>
		public WOResource MakeWOResource() {
			return new WOResource() {
				Id = this.Id,
				RemainingToActualize = this.RemainingToActualize,
				CanBeActualized = this.CanBeActualized,
				ActualQuantity = this.ActualQuantity,
				Description = this.Description,
				Quantity = this.Quantity,
				InputAttributes = Thinkage.Web.Mvc.Html.ThinkageExtensions.HtmlAttributes(Id.ToString(), QuantityType, null)
			};
		}
		public Guid Id {
			get;
			set;
		}
		protected WOResourceEntities.Demand DemandBase;
		public readonly TypeInfo QuantityType;
		abstract public string Quantity {
			get;
			set;
		}
		abstract public string ActualQuantity {
			get;
			set;
		}
		abstract public ResourceDescription Description {
			get;
		}
		/// <summary>
		/// Actual value to be used to actualize; init
		/// </summary>
		protected T? QuantityToActualize;
		public void SetQuantityToActualize(string qty) {
			if (String.IsNullOrEmpty(qty))
				QuantityToActualize = null;
			else {
				object v = QuantityType.GetTypeEditTextHandler(System.Globalization.CultureInfo.CurrentUICulture).ParseEditText(qty);
				QuantityToActualize = (T)QuantityType.ClosestValueTo(v);
			}
			System.Exception check = QuantityType.CheckMembership(QuantityToActualize);
			if (check != null)
				throw check;
		}
		virtual public string RemainingToActualize {
			get {
				return FormatQuantity(RemainingToActualizeValue);
			}
		}
		public abstract T RemainingToActualizeValue {
			get;
		}
		/// <summary>
		/// Create the appropriate Actual record in the dataset
		/// </summary>
		/// <param name="ds"></param>
		abstract public void Actualize(dsMB ds);
		/// <summary>
		/// Determine if this Demand has sufficient cost information and a suitable default cost setting that allows the record to be actualized
		/// without requiring the user to input cost information, only quantity.
		/// </summary>
		public bool CanBeActualized {
			get {
				decimal totalCost;
				T? quantity;
				return GetCostBasis(out totalCost, out quantity);
			}
		}
		abstract public bool GetDemandEstimateCostBasis(out decimal TotalCost, out T? quantity);
		abstract public bool GetCurrentSourceInformationCostBasis(out decimal TotalCost, out T? quantity);
		public virtual bool GetManualCostBasis(out decimal TotalCost, out T? quantity) {
			TotalCost = 0;
			quantity = null;
			return false;
		}
		public bool GetCostBasis(out decimal totalCost, out T? quantity) {
			totalCost = 0;
			quantity = null;
			switch ((DatabaseEnums.DemandActualCalculationInitValues)DemandBase.DemandActualCalculationInitValue) {
				case DatabaseEnums.DemandActualCalculationInitValues.ManualEntry:
					return GetManualCostBasis(out totalCost, out quantity);
				case DatabaseEnums.DemandActualCalculationInitValues.UseCurrentSourceInformationValue:
					return GetCurrentSourceInformationCostBasis(out totalCost, out quantity);
				case DatabaseEnums.DemandActualCalculationInitValues.UseDemandEstimateValue:
					return GetDemandEstimateCostBasis(out totalCost, out quantity);
				default:
					return false;
			}
		}
		protected string FormatQuantity(object v) {
			if (v == null)
				return "";
			return QuantityType.GetTypeFormatter(System.Globalization.CultureInfo.CurrentUICulture).Format(v);
		}
	}
	#endregion
	#region WOResourceInfo<E,T>
	abstract public class WOResourceInfo<E, T> : WOResourceInfo<T>
		where E : IDemandBase, IDemandQuantity<T>
		where T : struct, IComparable<T> {
		public WOResourceInfo(E d, Guid toCostCenter, TypeInfo type)
			: base(d.BaseDemand, type) {
			DerivedEntity = d;
			Id = DerivedEntity.Id;
			ToCostCenterID = toCostCenter;
		}
		[NonSerialized]
		protected E DerivedEntity;

		public override string Quantity {
			get {
				if (pQuantity == null) {
					if (!DerivedEntity.MBQuantity.HasValue)
						pQuantity = "";
					else
						pQuantity = FormatQuantity(DerivedEntity.MBQuantity.Value);
				}
				return pQuantity;
			}
			set {
				pQuantity = value;
			}
		}
		protected string pQuantity;

		public override string ActualQuantity {
			get {
				if (pActualQuantity == null)
					pActualQuantity = FormatQuantity(DerivedEntity.MBActualQuantity);
				return pActualQuantity;
			}
			set {
				pActualQuantity = value;
			}
		}
		protected string pActualQuantity;
		public override T RemainingToActualizeValue {
			get {
				return Compute.Remaining<T>(DerivedEntity.MBQuantity, DerivedEntity.MBActualQuantity);
			}
		}
		public override bool GetDemandEstimateCostBasis(out decimal totalCost, out T? quantity) {
			totalCost = 0;
			quantity = null;

			if (!DemandBase.CostEstimate.HasValue || !DerivedEntity.MBQuantity.HasValue)
				return false;
			totalCost = DemandBase.CostEstimate.Value;
			quantity = DerivedEntity.MBQuantity.Value;
			return true;
		}

		abstract protected Guid FromCostCenterID {
			get;
		}
		protected Guid ToCostCenterID {
			get;
			private set;
		}
		// abstract protected Guid ToCostCenterID;
		protected void SetAccountingTransactionInformation(dsMB.AccountingTransactionRow arow) {
			DateTime now = DateTime.Now;
			arow.F.EffectiveDate = now;
			arow.F.UserID = ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).UserID.Value;
			arow.F.FromCostCenterID = FromCostCenterID;
			arow.F.ToCostCenterID = ToCostCenterID;
			arow.F.Cost = ComputeCost();
		}
		protected virtual decimal ComputeCost() {
			decimal totalCost;
			T? quantity;
			if (!GetCostBasis(out totalCost, out quantity))
				throw new GeneralException(KB.K("No Cost Basis for calculating total cost"));
			var result = Compute.TotalFromQuantityAndBasisCost<T>(QuantityToActualize, quantity, totalCost);
			if (!result.HasValue)
				throw new GeneralException(KB.K("Error calculating total cost"));
			return result.Value;
		}
	}
	#endregion
	#region ItemWOResourceInfo
	public class ItemWOResourceInfo : WOResourceInfo<WOResourceEntities.DemandItem, long> {
		public ItemWOResourceInfo(WOResourceEntities.DemandItem r, Guid toCostCenter)
			: base(r, toCostCenter, dsMB.Schema.T.DemandItem.F.Quantity.EffectiveType) {
		}
		public override ResourceDescription Description {
			get {
				return new ResourceDescription() {
					Description = DerivedEntity.ItemLocation.Item.Code,
					HiddenDescription = DerivedEntity.ItemLocation.Item.Hidden != null,
					Location = DerivedEntity.ItemLocation.Location.Code,
					HiddenLocation = DerivedEntity.ItemLocation.Hidden != null
				};
			}
		}
		public override bool GetCurrentSourceInformationCostBasis(out decimal cost, out long? quantity) {
			cost = 0;
			quantity = null;
			if (DerivedEntity.ItemLocation.DerivedActualItemLocation.OnHand == 0)
				return false;
			cost = DerivedEntity.ItemLocation.DerivedActualItemLocation.TotalCost;
			quantity = DerivedEntity.ItemLocation.DerivedActualItemLocation.OnHand;
			return true;
		}
		public override void Actualize(dsMB ds) {
			long onHand = DerivedEntity.ItemLocation.DerivedActualItemLocation.OnHand;
			decimal valueOnHand = DerivedEntity.ItemLocation.DerivedActualItemLocation.TotalCost;

			if (QuantityToActualize > onHand)
				throw new GeneralException(KB.K("Quantity must not exceed quantity on hand"));

			decimal cost = ComputeCost();
			if (cost > valueOnHand)
				throw new GeneralException(KB.K("Cost must not exceed value on hand"));
			if (QuantityToActualize == onHand && cost != valueOnHand)
				throw new GeneralException(KB.K("This would set quantity on hand to zero but leave non-zero value"));

			// now safe to create the records
			var r = (dsMB.ActualItemRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.ActualItem);
			r.F.DemandItemID = DerivedEntity.Id;
			r.F.CorrectionID = r.F.Id;
			r.F.Quantity = (int)QuantityType.GenericAsNativeType(QuantityToActualize, typeof(int));
			SetAccountingTransactionInformation(r.AccountingTransactionIDParentRow);
		}
		protected override Guid FromCostCenterID {
			get {
				return DerivedEntity.ItemLocation.DerivedActualItemLocation.CostCenterID;
			}
		}
	}
	#endregion
	#region HourlyWOResourceInfo
	public class HourlyWOResourceInfo : WOResourceInfo<WOResourceEntities.DemandLaborInside, TimeSpan> {
		public HourlyWOResourceInfo(WOResourceEntities.DemandLaborInside r, Guid toCostCenter)
			: base(r, toCostCenter, dsMB.Schema.T.DemandLaborInside.F.Quantity.EffectiveType) {
		}
		public override ResourceDescription Description {
			get {
				return new ResourceDescription() {
					Description = DerivedEntity.LaborInside.Trade == null ? KB.K("Unspecified Trade").Translate() : DerivedEntity.LaborInside.Trade.Code,
					HiddenDescription = DerivedEntity.LaborInside.Trade != null && DerivedEntity.LaborInside.Trade.Hidden != null,
					Location = DerivedEntity.LaborInside.Employee == null ? KB.K("Unspecified Employee").Translate() : DerivedEntity.LaborInside.Employee.Contact.Code,
					HiddenLocation = DerivedEntity.LaborInside.Employee != null && DerivedEntity.LaborInside.Employee.Contact.Hidden != null
				};
			}
		}
		public override string Quantity {
			get {
				if (pQuantity == null) {
					if (!DerivedEntity.MBQuantity.HasValue)
						pQuantity = "";
					else
						pQuantity = FormatQuantity(DerivedEntity.MBQuantity.Value);
				}
				return pQuantity;
			}
		}
		public override string ActualQuantity {
			get {
				if (pActualQuantity == null)
					pActualQuantity = FormatQuantity(DerivedEntity.MBActualQuantity);
				return pActualQuantity;
			}
		}
		public override TimeSpan RemainingToActualizeValue {
			get {
				return Compute.Remaining<TimeSpan>(DerivedEntity.MBQuantity, DerivedEntity.MBActualQuantity);
			}
		}
		public override string RemainingToActualize {
			get {
				return FormatQuantity(RemainingToActualizeValue);
			}
		}
		public override void Actualize(dsMB ds) {
			var r = (dsMB.ActualLaborInsideRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.ActualLaborInside);
			r.F.DemandLaborInsideID = DerivedEntity.Id;
			r.F.CorrectionID = r.F.Id;
			r.F.Quantity = (TimeSpan)QuantityType.GenericAsNativeType(QuantityToActualize, typeof(TimeSpan));
			SetAccountingTransactionInformation(r.AccountingTransactionIDParentRow);
		}
		public override bool GetCurrentSourceInformationCostBasis(out decimal cost, out TimeSpan? quantity) {
			cost = 0;
			quantity = null;
			if (!DerivedEntity.LaborInside.Cost.HasValue)
				return false;
			cost = DerivedEntity.LaborInside.Cost.Value;
			quantity = new TimeSpan(1, 0, 0); // per hour
			return true;
		}
		protected override Guid FromCostCenterID {
			get {
				return DerivedEntity.LaborInside.CostCenterID;
			}
		}
	}
	#endregion
	#region PerJobWOResourceInfo
	public class PerJobWOResourceInfo : WOResourceInfo<WOResourceEntities.DemandOtherWorkInside, long> {
		public PerJobWOResourceInfo(WOResourceEntities.DemandOtherWorkInside r, Guid toCostCenter)
			: base(r, toCostCenter, dsMB.Schema.T.DemandOtherWorkInside.F.Quantity.EffectiveType) {
		}
		public override ResourceDescription Description {
			get {
				return new ResourceDescription() {
					Description = DerivedEntity.OtherWorkInside.Trade == null ? KB.K("Unspecified Trade").Translate() : DerivedEntity.OtherWorkInside.Trade.Code,
					HiddenDescription = DerivedEntity.OtherWorkInside.Trade != null && DerivedEntity.OtherWorkInside.Trade.Hidden != null,
					Location = DerivedEntity.OtherWorkInside.Employee == null ? KB.K("Unspecified Employee").Translate() : DerivedEntity.OtherWorkInside.Employee.Contact.Code,
					HiddenLocation = DerivedEntity.OtherWorkInside.Employee != null && DerivedEntity.OtherWorkInside.Employee.Contact.Hidden != null
				};
			}
		}
		public override void Actualize(dsMB ds) {
			var r = (dsMB.ActualOtherWorkInsideRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.ActualOtherWorkInside);
			r.F.DemandOtherWorkInsideID = DerivedEntity.Id;
			r.F.CorrectionID = r.F.Id;
			r.F.Quantity = (int)QuantityType.GenericAsNativeType(QuantityToActualize, typeof(int));
			SetAccountingTransactionInformation(r.AccountingTransactionIDParentRow);
		}
		public override bool GetCurrentSourceInformationCostBasis(out decimal cost, out long? quantity) {
			cost = 0;
			quantity = null;

			if (!DerivedEntity.OtherWorkInside.Cost.HasValue)
				return false;
			cost = DerivedEntity.OtherWorkInside.Cost.Value;
			quantity = 1;
			return true;
		}
		protected override Guid FromCostCenterID {
			get {
				return DerivedEntity.OtherWorkInside.CostCenterID;
			}
		}
	}
	#endregion
	#region MiscellaneousWOResourceInfo
	public class MiscellaneousWOResourceInfo : WOResourceInfo<WOResourceEntities.DemandMiscellaneousWorkOrderCost, decimal> {
		public MiscellaneousWOResourceInfo(WOResourceEntities.DemandMiscellaneousWorkOrderCost r, Guid toCostCenter)
			: base(r, toCostCenter, dsMB.Path.T.DemandMiscellaneousWorkOrderCost.F.DemandID.F.CostEstimate.ReferencedColumn.EffectiveType) {
		}
		public override ResourceDescription Description {
			get {
				return new ResourceDescription() {
					Description = DerivedEntity.MiscellaneousWorkOrderCost.Code,
					HiddenDescription = DerivedEntity.MiscellaneousWorkOrderCost.Hidden != null,
					Location = "",
					HiddenLocation = false
				};
			}
		}
		public override void Actualize(dsMB ds) {
			var r = (dsMB.ActualMiscellaneousWorkOrderCostRow)ds.DB.AddNewRowAndBases(ds, dsMB.Schema.T.ActualMiscellaneousWorkOrderCost);
			r.F.DemandMiscellaneousWorkOrderCostID = DerivedEntity.Id;
			r.F.CorrectionID = r.F.Id;
			SetAccountingTransactionInformation(r.AccountingTransactionIDParentRow);
		}
		protected override decimal ComputeCost() {
			if (!QuantityToActualize.HasValue || QuantityToActualize.Value == 0)
				throw new GeneralException(KB.K("Cost cannot be zero"));
			return QuantityToActualize.Value; // whatever the user entered is the actual cost
		}
		public override bool GetCurrentSourceInformationCostBasis(out decimal cost, out decimal? quantity) {
			if (DerivedEntity.MiscellaneousWorkOrderCost.Cost.HasValue)
				cost = DerivedEntity.MiscellaneousWorkOrderCost.Cost.Value;
			else
				cost = 0;
			quantity = 1;
			return true;
		}
		protected override Guid FromCostCenterID {
			get {
				return DerivedEntity.MiscellaneousWorkOrderCost.CostCenterID;
			}
		}
		public override bool GetManualCostBasis(out decimal TotalCost, out decimal? quantity) {
			// Whatever the user enters in the form is what is recorded as the cost regardless of this setting; We just make sure if the default setting is manual entry
			// that the user can enter a value to be saved. We provide a default value of 0 in this case.
			quantity = 1;
			TotalCost = 0;
			return true;
		}
		public override decimal RemainingToActualizeValue {
			get {
				// For miscellaneous costs, we will use the SourceInformationCost if the DemandEstimate is empty as our remaining value basis.
				return Compute.Remaining<decimal>(DerivedEntity.MBQuantity ?? DerivedEntity.MiscellaneousWorkOrderCost.Cost, DerivedEntity.MBActualQuantity);
			}
		}
	}
	#endregion
	#endregion
}
