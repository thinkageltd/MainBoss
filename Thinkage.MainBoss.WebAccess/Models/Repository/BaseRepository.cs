using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.MVC.Models;
using Thinkage.Libraries.Permissions;
using Thinkage.Libraries.Translation;
using Thinkage.MainBoss.Database;
using Thinkage.MainBoss.WebAccess.Models.interfaces;

namespace Thinkage.MainBoss.WebAccess.Models {
	#region NoPermissionException
	[System.Serializable]
	public class NoPermissionException : System.Exception {
		public NoPermissionException([Invariant]string roleRequired)
			: base(roleRequired) {
		}
	}
	#endregion
	/// <summary>
	/// Common implementation of BaseRespository wrapper classes
	/// </summary>
	public abstract partial class BaseRepository : Thinkage.Libraries.MVC.Models.Repository, IBaseRepository {
		protected static System.Guid UnassignedGuid = new System.Guid("A0000000-0000-0000-0000-00000000000A");
		public BaseRepository([Invariant] string governingTableRight)
			: this(governingTableRight, null) {
		}
		public BaseRepository([Invariant] string governingTableRight, FormMap formDefinition)
			: base(formDefinition) {
			GoverningTableRight = GetTableRightsGroup(governingTableRight);
		}
		private readonly TableOperationRightsGroup GoverningTableRight;
		public DatabaseConnection Connection {
			get {
				return ((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).Connection;
			}
		}
		public MainBossPermissionsManager PermissionsManager {
			get {
				if (pPermissionsManager == null)
					pPermissionsManager = (MainBossPermissionsManager)((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).GetInterface<IPermissionsFeature>().PermissionsManager;
				return pPermissionsManager;
			}
		}
		private MainBossPermissionsManager pPermissionsManager;

		public void CheckPermission(params Right[] rList) {
			MainBossPermissionDisabler pd;
			foreach (Right r in rList) {
				pd = (MainBossPermissionDisabler)PermissionsManager.GetPermission(r);
				if (!pd.Enabled)
					throw new NoPermissionException(pd.DisablerTip);
			}
		}
		protected Thinkage.MainBoss.Database.MB3Client MB3DB {
			get {
				return (Thinkage.MainBoss.Database.MB3Client)((Thinkage.MainBoss.WebAccess.MainBossWebAccessApplication)Thinkage.Libraries.Application.Instance).GetInterface<ISessionDatabaseFeature>().Session;
			}
		}
		#region Accounting/Cost Right Support
		public bool HasViewCostRight([Invariant]string rightName, out string disablerReason) {
			Right right = Root.Rights.ViewCost.FindRightByName(rightName);
			var pd = (MainBossPermissionDisabler)PermissionsManager.GetPermission(right);
			disablerReason = pd.DisablerTip;
			return pd.Enabled;
		}
		public bool HasActionRight([Invariant]string rightName, out string disablerReason) {
			Right right = Root.Rights.Action.FindRightByName(rightName);
			var pd = (MainBossPermissionDisabler)PermissionsManager.GetPermission(right);
			disablerReason = pd.DisablerTip;
			return pd.Enabled;
		}
		#endregion
		#region Table Right support
		protected TableOperationRightsGroup GetTableRightsGroup([Invariant]string tableName) {
			return (TableOperationRightsGroup)Root.Rights.Table.FindDirectChild(tableName);
		}
		public Right ViewRight {
			get {
				return GoverningTableRight.GetTableOperationRight(TableOperationRightsGroup.TableOperation.View);
			}
		}
		public Right BrowseRight {
			get {
				return GoverningTableRight.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Browse);
			}
		}
		public Right CreateRight {
			get {
				return GoverningTableRight.GetTableOperationRight(TableOperationRightsGroup.TableOperation.Create);
			}
		}
		#endregion
	}
}
