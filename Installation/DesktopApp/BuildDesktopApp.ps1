#It expects to use the MSIXPackagingTool that is installed
#and you have started this script as Administrator
. (resolve-path '..\SignProcedure.ps1')
. (resolve-path '..\..\SolutionSettings.ps1')
$DESKTOPAPP_BASE = (get-location)
rd -force installLoc
md installLoc
pushd ..\Install.MainBoss
copy .\bin\Desktop\en-US\Install.MainBoss-en-US.$SolutionVersion.msi (join-path -path $DESKTOPAPP_BASE -childpath INSTALLFILE.msi)
popd
msiexec /uninstall INSTALLFILE.msi /quiet | out-null
$VERSION = $SolutionVersion -replace "\..$",""
# The PackageName and Publisher are the values from the Microsoft Store identity found in AppManagement->App identity
$Publisher = "CN=Thinkage Ltd., O=Thinkage Ltd., L=Kitchener, S=Ontario, C=CA"
$appIdentity = "MainBoss"
$PublisherStore = "CN=7D069FE8-6815-4B7C-88A9-87E6294070E8"
$appIdentityStore = "ThinkageLtd.MainBoss"
#The following was from using the MakeSelfSignedCertificateForStore procedure
$StoreCertificateThumbprint = "AB655D498DF6F06B655EDE858989666FBFD4E3A9"
$StoreCertificate = "7D069FE8-6815-4B7C-88A9-87E6294070E8"
$template = (Get-Content .\MainBoss_template.xml).replace('#PACKAGENAME#',$appIdentity).replace('#VERSION#',$VERSION).replace('#DESKTOPAPP_BASE#',$DESKTOPAPP_BASE).replace('#APPFILENAME#',$appIdentity).replace('#PUBLISHERNAME#',$Publisher) | set-content .\the_template.xml
MSIXPackagingTool.exe create-package --template .\the_template.xml
ThinkageCodeSign "$DESKTOPAPP_BASE\$appIdentity.msix"
#
# Now do the Microsoft App store version
#
msiexec /uninstall INSTALLFILE.msi /quiet | out-null
$template = (Get-Content .\MainBoss_template.xml).replace('#PACKAGENAME#',$appIdentityStore).replace('#VERSION#',$VERSION).replace('#DESKTOPAPP_BASE#',$DESKTOPAPP_BASE).replace('#APPFILENAME#',$appIdentityStore).replace('#PUBLISHERNAME#',$PublisherStore) | set-content .\the_template.xml
MSIXPackagingTool.exe create-package --template .\the_template.xml
&$signtool sign /fd SHA256 /a /sm /s "My" /n "$StoreCertificate" "$DESKTOPAPP_BASE\$appIdentityStore.msix"
#Cleanup
msiexec /uninstall INSTALLFILE.msi /quiet | out-null
rd -force installLoc
rm -force the_template.xml
rm -force INSTALLFILE.msi
