# Set the $SolutionVersion and $SolutionProduct values for use by the Solution Build process
# Note that if you want a new build to install quietly over an old one you must at least increase the "update" number (third Version value). Just updating the Build number will not work.
# After changing this file you must do a Rebuild of the solution, since there is no way of telling Studio which projects depend on this file.
# After the first rebuild, you then must close and reopen the solution because one of projects 'Import' statements will refer to a changed version in the SolutionSettings.targets
# file and those values are only set at time solution is opened; you then must rebuild again. sigh
$SolutionVersion = new-object System.Version(4,2,4,16)
$DesktopAppVersion = new-object System.Version(4,2,4,0) #Revision must be 0 for Desktop App
$SolutionProduct = "MainBoss"
#The following drives the localized WiX installation builds. It is a list for iteration purposes
$WixCulturesToBuildRelease = "en-US","es-ES", "fr-FR"
$WixCulturesToBuildDesktop = "en-US"
