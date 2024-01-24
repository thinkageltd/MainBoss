using System;
using Thinkage.Libraries.Service;

namespace Thinkage.MainBoss.Database.Service {
	/// <summary>
	/// Provides the base class for processing emails within the database
	/// </summary>
	public class EmailProcessor : IDisposable {
		#region Declarations
		protected readonly MainBossServiceConfiguration ServiceConfiguration;
		protected dsMB dsmb;
		protected MB3Client DB;
		protected IServiceLogging Logger;
		#endregion

		#region Constructors, Destructors

		public EmailProcessor(IServiceLogging logger, MB3Client dbClient)
			: base() {
			this.Logger = logger;
			DB = dbClient;
			ServiceConfiguration = MainBossServiceConfiguration.GetConfiguration(DB.ConnectionInfo);
			dsmb = new dsMB(DB);
		}
		#endregion
		#region IDisposable Members
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (dsmb != null) {
					dsmb.Dispose();
					dsmb = null;
				}
			}
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
