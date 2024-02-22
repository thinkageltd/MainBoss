using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Database.Service {
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "<Pending>")]
	public class MainBossServiceWorkerException : Thinkage.Libraries.GeneralException {
		public MainBossServiceWorkerException(Key msg)
			: base(msg) {
		}
		public MainBossServiceWorkerException(System.Exception inner, Key msg)
			: base(inner, msg) {
		}
	}
}

