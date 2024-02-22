#It expects to find a base image (not part of vault because they are so large) beside this script and the Desktop converter program installed
#and you have started the Desktop converted command window as Administrator from which you run this script
. (resolve-path '..\SignProcedure.ps1')
. (resolve-path '..\..\SolutionSettings.ps1')
pushd ..\Install.MainBoss
#
$appFolder = join-path -path (get-location) -childpath "appFolder"
rd -r -force $appfolder | out-null
md $appFolder 
DesktopAppConverter.exe -Installer .\bin\Desktop\en-US\Install.MainBoss-en-US.$SolutionVersion.msi -destination $appFolder -PackageName "MainBoss" -Publisher "CN=Thinkage Ltd., O=Thinkage Ltd., L=Kitchener, S=Ontario, C=CA" -Version $DesktopAppVersion -PackageArch x86 -MakeAppx
&$signtool sign /fd SHA256 /t http://tsa.starfieldtech.com /n "Thinkage Ltd." /i "Go Daddy Secure Certificate Authority - G2" $appFolder\Mainboss\mainboss.appx
popd