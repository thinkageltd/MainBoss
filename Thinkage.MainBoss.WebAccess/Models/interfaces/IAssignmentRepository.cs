using System;

namespace Thinkage.MainBoss.WebAccess.Models.interfaces
{
	interface IAssignmentRepository : IBaseRepository
	{
		Models.Assignment GetAssignment(Guid UserId);
	}
}
