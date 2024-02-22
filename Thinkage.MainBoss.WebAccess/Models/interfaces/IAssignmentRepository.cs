using System;

namespace Thinkage.MainBoss.WebAccess.Models.interfaces
{
	internal interface IAssignmentRepository : IBaseRepository
	{
		Models.Assignment GetAssignment(Guid UserId);
	}
}
