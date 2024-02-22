using System.Collections.Generic;
using Thinkage.Libraries.Translation;
namespace Thinkage.MainBoss.Database {
	public static partial class Licensing {
		#region LicenseModuleIdProvider
		private static Libraries.EnumValueTextRepresentations InitializeLicenseModuleProvider() {
			List<Key> moduleName = new List<Key>();
			List<object> moduleIds = new List<object>();
			foreach (int i in (int[])System.Enum.GetValues(typeof(ApplicationID))) {
				switch ((ApplicationID)i) {
				case ApplicationID.NoApplication:
				case ApplicationID.MaxValue:
				case ApplicationID.RequestNotifier:
				case ApplicationID.GeneralNotifier:
					break; // don't add the Unknown ApplicationID variants to the choice list.
				default:
					moduleName.Add(KB.T(ApplicationName((ApplicationID)i)));
					moduleIds.Add(i);
					break;
				}
			}
			return new Libraries.EnumValueTextRepresentations(moduleName.ToArray(), null, moduleIds.ToArray());
		}
		public readonly static Libraries.EnumValueTextRepresentations LicenseModuleIdProvider = InitializeLicenseModuleProvider();
		#endregion
	}
}
