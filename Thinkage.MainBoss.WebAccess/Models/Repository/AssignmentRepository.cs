using System;
using System.Linq;

namespace Thinkage.MainBoss.WebAccess.Models {
	public class AssignmentRepository : BaseRepository, Thinkage.MainBoss.WebAccess.Models.interfaces.IAssignmentRepository {
		#region Constructor and Base Support
		public AssignmentRepository()
			: base(governingTableRight:"AttentionStatus") {
		}
		public override void InitializeDataContext() {
			DataContext = new AssignmentDataContext(Connection.ConnectionString);
		}
		public AssignmentDataContext DataContext {
			get;
			private set;
		}
		#endregion
		public Models.Assignment GetAssignment(Guid UserId) {
			var m = new Assignment();
			m.AttentionStatus = DataContext.AttentionStatus.Single<AssignmentEntities.AttentionStatus>(d => d.UserID == UserId);
			m.DatabaseStatus = DataContext.DatabaseStatus.Single<AssignmentEntities.DatabaseStatus>();
			return m;
		}
	}
}
