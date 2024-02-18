#It expects to find a base image (not part of vault because they are so large) beside this script and the Desktop converter program installed
#and you have started the Desktop converted command window as Administrator from which you run this script
. (resolve-path '..\SignProcedure.ps1')
. (resolve-path '..\..\SolutionSettings.ps1')
pushd ..\Install.MainBoss
#
$appFolder = join-path -path (get-location) -childpath "appFolder"
rd -r -force $appfolder | out-null
md $appFolder
# The PackageName and Publisher are the values from the Microsoft Store identity found in AppManagement->App identity
$Publisher = "CN=Thinkage Ltd., O=Thinkage Ltd., L=Kitchener, S=Ontario, C=CA"
$appIdentity = "MainBoss"
$PublisherStore = "CN=7D069FE8-6815-4B7C-88A9-87E6294070E8"
$appIdentityStore = "ThinkageLtd.MainBoss"
#The following was from using the MakeSelfSignedCertificateForStore procedure
$StoreCertificateThumbprint = "2B2F9F03DC2B0AD60A4BCE316FC262BA58C23BD4"
$StoreCertificate = "7D069FE8-6815-4B7C-88A9-87E6294070E8"
DesktopAppConverter.exe -Installer .\bin\Desktop\en-US\Install.MainBoss-en-US.$SolutionVersion.msi -destination $appFolder -PackagePublisherDisplayName "Thinkage Ltd." -PackageDisplayName "MainBoss" -AppDisplayName "MainBoss" -PackageName "$appIdentity" -Publisher "$Publisher" -Version $DesktopAppVersion -PackageArch x86 -MakeAppx
&$signtool sign /fd SHA256 /t http://tsa.starfieldtech.com /n "Thinkage Ltd." /i "Go Daddy Secure Certificate Authority - G2" "$appFolder\$appIdentity\$appIdentity.appx"
DesktopAppConverter.exe -Installer .\bin\Desktop\en-US\Install.MainBoss-en-US.$SolutionVersion.msi -destination $appFolder -PackagePublisherDisplayName "Thinkage Ltd." -PackageDisplayName "MainBoss" -AppDisplayName "MainBoss" -PackageName "$appIdentityStore" -Publisher "$PublisherStore" -Version $DesktopAppVersion -PackageArch x86 -MakeAppx
&$signtool sign /debug /fd SHA256 /a /sm /s "My" /n "$StoreCertificate" "$appFolder\$appIdentityStore\$appIdentityStore.appx"
popd