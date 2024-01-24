// Note this is a shared SOURCE file between the key generator in Thinkage Toolkit and the MainBoss application

using Thinkage.Libraries.Translation;
/// <summary>
/// Define the actual licensing values for MainBoss
/// </summary>
namespace Thinkage.MainBoss.Database {
	public static partial class Licensing {
		/// <summary>
		/// Number of days to permit warnings on license expiry.
		/// </summary>
		public const int ExpiryWarningDays = 30;
		// The Application ID's that appear in license keys. Note that the Key Generator program does (or should) have knowledge of
		// these values and their names.
		public enum ApplicationID {
			NoApplication = 0,
			NamedUsers, // formerly MainBoss
			MainBossService,
			RequestNotifier,
			Requests,
			GeneralNotifier,
			Inventory,
			ScheduledMaintenance,
			Purchasing,
			Accounting,
			WebAccess,
			WebRequests,
			WorkOrderLabor,
			MaxValue
		}
		/// <summary>
		/// Return name (if in use) of Application associated with ApplicationID; null if reserved or not relevant at present.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[return: Invariant]
		public static string ApplicationName(ApplicationID id) {
			switch (id) {
			case ApplicationID.NamedUsers:
				return "Named Users";
			case ApplicationID.MainBossService:
				return "MainBoss Service";
			case ApplicationID.RequestNotifier:
				return null;// "Request Notifier";
			case ApplicationID.Requests:
				return "Requests";
			case ApplicationID.GeneralNotifier:
				return null;// "General Notifier";
			case ApplicationID.Inventory:
				return "Inventory";
			case ApplicationID.ScheduledMaintenance:
				return "Planned Maintenance";
			case ApplicationID.Purchasing:
				return "Purchasing";
			case ApplicationID.Accounting:
				return "Accounting";
			case ApplicationID.WebAccess:
				return "Web Access";
			case ApplicationID.WebRequests:
				return "Web Requests";
			case ApplicationID.WorkOrderLabor:
				return "Work Order Labor";
			default:
				return Thinkage.Libraries.Strings.IFormat("Unknown ApplicationID {0}", ((int)id).ToString()); // identify the bad id as an integer
			}
		}
		/// <summary>
		/// For Version expiry of this product, this is the deemed release date for all licensed objects in MainBoss 4.2
		/// </summary>
		public static readonly System.DateTime DeemedReleaseDate = new System.DateTime(2017, 1, 1);
	}
}
