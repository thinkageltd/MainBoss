using Thinkage.Libraries.XAF.Database.Service.MSSql;

namespace WOResourceEntities {
	/// <summary>
	/// Interface with common Demand base record
	/// </summary>
	public interface IDemandBase {
		/// <summary>
		/// The Base Demand record
		/// </summary>
		WOResourceEntities.Demand BaseDemand {
			get;
		}
	}
	/// <summary>
	/// The demand Entity has a quantity of type T; we use an alternative name beginning with MB because MainBoss stores interval types in
	/// a DateTime field and the Linq generated classes use DateTime as the type of the Quantity field. Our MBQuantity field knows how to 'convert'
	/// the stored DateTime field to the TimeSpan interval used within MainBoss.
	/// Also provide the common Id field in MB records as part of the interface
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDemandQuantity<T> where T : struct {
		System.Guid Id {
			get;
		}
		T? MBQuantity {
			get;
		}
		T MBActualQuantity {
			get;
		}
	}
	public interface IDerivedDemand {
		System.Guid DemandID {
			get;
		}
	}
	#region Partial DemandXXX classes implementing IDemandBase&IDemandQuantity<T>
	/// <summary>
	/// Wrapper sto enclose implement the different Quantity Types
	/// </summary>
	public partial class DemandItem : IDemandBase, IDemandQuantity<long>, IDerivedDemand {
		public long? MBQuantity {
			get {
				return Quantity;
			}
		}
		public long MBActualQuantity {
			get {
				return ActualQuantity;
			}
		}
	}
	public partial class DemandLaborInside : IDemandBase, IDemandQuantity<System.TimeSpan>, IDerivedDemand {
		public System.TimeSpan? MBQuantity {
			get {
				return Quantity - SqlClient.SqlServer.SqlDateEpoch;
			}
		}
		public System.TimeSpan MBActualQuantity {
			get {
				return ActualQuantity - SqlClient.SqlServer.SqlDateEpoch;
			}
		}
	}
	public partial class DemandMiscellaneousWorkOrderCost : IDemandBase, IDemandQuantity<decimal>, IDerivedDemand {
		public decimal? MBQuantity {
			get {
				return BaseDemand.CostEstimate;
			}
		}
		public decimal MBActualQuantity {
			get {
				return BaseDemand.ActualCost;
			}
		}
	}
	public partial class DemandOtherWorkInside : IDemandBase, IDemandQuantity<long>, IDerivedDemand {
		public long? MBQuantity {
			get {
				return Quantity;
			}
		}
		public long MBActualQuantity {
			get {
				return ActualQuantity;
			}
		}
	}
	#endregion
}