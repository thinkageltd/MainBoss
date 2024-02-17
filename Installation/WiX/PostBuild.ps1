param([string]$SolutionDir, [string]$ProjectName, [string]$TargetDir, [string]$TargetName, [string]$TargetExt, [string]$Configuration)
# Following should be same as $(var.BuildDir) defined in Common.wxi
$ScriptRoot = Split-path $MyInvocation.MyCommand.Path
pushd $ScriptRoot
. (resolve-path '..\SignProcedure.ps1')
. (resolve-path (join-path -path $SolutionDir -childpath "SolutionSettings.ps1"))
if( $Configuration -eq "Release") {
	$cultures = $WixCulturesToBuildRelease
	$codesign = $true
}
elseif( $Configuration -eq "Desktop") {
	$cultures = $WixCulturesToBuildDesktop
	$codesign = $true
}
else {
	$cultures = ""
	$codesign = $false
}
$extension=$TargetExt
$name=$TargetName
$fullname=$name + $extension
pushd $TargetDir
# Burn packages (.exe) do not have multiple cultures and YOU CANNOT change its filename after it's built (it knows what it is called to bootstrap itself)
if ($extension -eq ".exe") {
		if( $codesign) {
			ThinkageCodeSign($fullname)
		}
}
else { # .msi packages do have multiple cultures
	foreach ($culture in $cultures) {
		pushd $culture
		if( $codesign) {
			ThinkageCodeSign($fullname)
		}
		copy $fullname "$name-$culture.$SolutionVersion$extension" -force
		del $fullname
		popd
	}
}
popd
