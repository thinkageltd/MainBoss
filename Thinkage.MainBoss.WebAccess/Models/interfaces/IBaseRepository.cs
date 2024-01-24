using Thinkage.Libraries.MVC.Models;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebAccess.Models.interfaces
{
	public interface IBaseRepository {
		DatabaseConnection Connection {
			get;
		}
		MainBossPermissionsManager PermissionsManager {
			get;
		}
		void CheckPermission(params Thinkage.Libraries.Permissions.Right[] rList);
	}
	/// <summary>
	/// BaseRepository with DataContext
	/// </summary>
	/// <typeparam name="DC"></typeparam>
	public interface IBaseRepository<DC> : IRepository, IBaseRepository where DC : System.Data.Linq.DataContext
	{
		DC DataContext {
			get;
		}
	}
}
