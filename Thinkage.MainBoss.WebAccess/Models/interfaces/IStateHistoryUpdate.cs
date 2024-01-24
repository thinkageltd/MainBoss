using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thinkage.Libraries.DBAccess;
using Thinkage.Libraries.MVC;
using Thinkage.MainBoss.Database;

namespace Thinkage.MainBoss.WebAccess.Models
{
	[Obsolete()]
	interface IStateHistoryUpdate<T>
	{
		void PrepareToClose(T model);
		void Close(T originalModel, T updatedModel);
	}

	interface IStateHistoryRepository {
		List<Thinkage.MainBoss.WebAccess.Models.StateHistoryRepository.Transition> Transitions {
			get;
		}
	}
	interface IStateHistoryRepository<T> : IStateHistoryRepository {
		void Prepare(T model, IEnumerable<Guid> allowedStates, IEnumerable<StateHistoryRepository.CustomInstructions> customPreparationInstructions, params Thinkage.Libraries.Permissions.Right[] rights);
		Guid Update(T originalModel, T updatedModel, Guid? changeToState, IEnumerable<Guid> allowedStates, IEnumerable<StateHistoryRepository.CustomInstructions> customInstructions);
	}
}
