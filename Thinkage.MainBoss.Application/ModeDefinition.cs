using System;
using Thinkage.Libraries.DBILibrary;
using Thinkage.Libraries.Licensing;
using Thinkage.Libraries.Presentation;
using Thinkage.Libraries.Translation;

namespace Thinkage.MainBoss.Application
{
	/// <summary>
	/// Application ModeDefinition for define tables of application modes where required.
	/// </summary>
	public class ModeDefinition
	{
		public ModeDefinition([Invariant] string fullName, int appMode, Key hint, DBI_Variable minAppVersionVariable, Version minDBVersion, LicenseRequirement[] mainKeys, params LicenseEnabledFeatureGroups[] featureGroupLicensing)
			: this(fullName, appMode, hint, minAppVersionVariable, minDBVersion, new[] { mainKeys }, featureGroupLicensing) {
		}
		public ModeDefinition([Invariant] string fullName, int appMode, Key hint, DBI_Variable minAppVersionVariable, Version minDBVersion, LicenseRequirement[][] mainKeys, params LicenseEnabledFeatureGroups[] featureGroupLicensing) {
			FullName = fullName;
			Hint = hint;
			AppMode = appMode;
			SessionModeID = appMode;
			MinAppVersionVariable = minAppVersionVariable;
			MinDBVersion = minDBVersion;
			MainKeys = mainKeys;
			FeatureGroupLicensing = featureGroupLicensing;
		}
		public readonly string FullName;				// for display
		public readonly int AppMode;
		public readonly LicenseRequirement[][] MainKeys;
		public readonly bool ConsumeMainLicense = true;
		public readonly int SessionModeID;
		public readonly LicenseEnabledFeatureGroups[] FeatureGroupLicensing;
		public readonly DBI_Variable MinAppVersionVariable;
		public readonly Version MinDBVersion;
		public readonly Key Hint;
	}
}