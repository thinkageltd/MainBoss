using System;
using System.Linq;

namespace Thinkage.MainBoss.WebAccess.Models.interfaces
{
	internal interface IAuthenticationRepository : IBaseRepository
	{
		IQueryable<AuthenticationEntities.User> Users();
		AuthenticationEntities.Contact GetContactForUser(Guid userID);
		AuthenticationEntities.Requestor GetRequestorForEmailAddress(string emailAddress);
	}
}
