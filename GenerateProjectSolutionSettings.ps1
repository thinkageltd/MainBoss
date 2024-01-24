# invoked from PreBuild of a VS Solution Project with 3 arguments
# Will write a suitable 'SolutionSettingsProjectInclusion.?' file in the invoking projects directory prior to compilation
# so that version information is available at compile time.

param([string]$ProjectDir, [string]$ProjectTypesList, [string]$ProjectName)

$SolutionSettingsFileName = "SolutionSettings"

$ScriptRoot = Split-path $MyInvocation.MyCommand.Path
# Multiple project types can be specified separated by a '/' character.
$ProjectTypes = $ProjectTypesList.Split([char]'/')

pushd $ScriptRoot
. (join-path -path $ScriptRoot -childpath "$SolutionSettingsFileName.ps1")
foreach($ProjectType in $ProjectTypes ){
	write-host "ProjectType $ProjectType"
	switch ($ProjectType) {
	"msbuild"
		{
			$SolutionSettingsPath =  (join-path -path $ProjectDir -childpath "$SolutionSettingsFileName.targets")
			$Version = [System.String]::Format("{0}.{1}", $SolutionVersion.Major, $SolutionVersion.Minor)
			$Update = ($SolutionVersion.Build)
			$Build = ($SolutionVersion.Revision)
			set-itemproperty -path "$SolutionSettingsPath" -name IsReadonly -value $false
			$content = @"
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	<PropertyGroup>
	<SupportUrl>http://www.mainboss.com/info/support.shtml%3fversion=$version</SupportUrl>
	<ApplicationRevision>$Build</ApplicationRevision>
	<ApplicationVersion>$Version.$Update.$Build</ApplicationVersion>
    <MinimumRequiredVersion>$Version.$Update.$Build</MinimumRequiredVersion>
	</PropertyGroup>
</Project>
"@
			[System.IO.File]::WriteAllText($SolutionSettingsPath,$content)
		}
	"wix"
		 {
			$SolutionSettingsPath =  (join-path -path $ProjectDir -childpath "$SolutionSettingsFileName.wxi")
			$Version = [System.String]::Format("{0}.{1}", $SolutionVersion.Major, $SolutionVersion.Minor)
			$Update = ($SolutionVersion.Build)
			$Build = ($SolutionVersion.Revision)
			$content = @"
<?xml version="1.0" encoding="utf-8"?>
<Include>
	<?define PRODUCTNAME="$SolutionProduct"?>
	<?define VERSION = "$Version"?>
	<?define UPDATE = "$Update"?>
	<?define BUILD = "$Build"?>
</Include>
"@
			[System.IO.File]::WriteAllText($SolutionSettingsPath,$content)
		 }

	"C#"
		{
			$SolutionSettingsPath =  (join-path -path $ProjectDir -childpath "$SolutionSettingsFileName.cs")
			$content = @"
using System.Reflection;
[assembly: AssemblyTitle("$ProjectName in $SolutionProduct $SolutionVersion")]
[assembly: AssemblyVersion("$SolutionVersion")]
"@
		[System.IO.File]::WriteAllText($SolutionSettingsPath,$content)
		}

	"C++"
		{
			$SolutionSettingsPath =  (join-path -path $ProjectDir -childpath "$SolutionSettingsFileName.h")
			$s = [System.String]::Format("{0},{1},{2},{3}", $SolutionVersion.Major,$SolutionVersion.Minor,$SolutionVersion.Build,$SolutionVersion.Revision)
			$content = @"
#define SOLUTION_VERSION $s
#define SOLUTION_VERSION_STRING "$SolutionVersion"
#define SOLUTION_NAME "$SolutionProduct"

"@
			[System.IO.File]::WriteAllText($SolutionSettingsPath,$content)
		}
	}
}


