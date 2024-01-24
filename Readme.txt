This web application project exists solely for 'building' the MainBossSolo clickonce application the accompanying web site.
The only way to get any MSBUILD automation is to pretend to be an ASP.NET web application (we are not), and then modify the
Web project file to undo the ASP.NET 'stuff', and do our 'stuff' using powershell and command files.

To this end, this project targets a dummy dll called Install.X.dll to satisfy the ASP.NET build sequence. We subsequently
have ExcludeFromPackageFiles definitions to exclude the resulting output from our package.

Similarily the actual project is built in a directory called 'BuildDir' with the powershell script.
To get this BuildDir as part of the MSBUILD publish procedure, we had to add the following MSBUILD XML
to the project file to collect all the files and include them in the publish list.
  <!-- Add a BuildDirCollectFiles to the CopyAllFilesToSingleFolderForPackageDependsOn dependent property so we can add our custom files to the package -->
  <PropertyGroup>
    <CopyAllFilesToSingleFolderForPackageDependsOn>
    BuildDirCollectFiles;
    $(CopyAllFilesToSingleFolderForPackageDependsOn);
  </CopyAllFilesToSingleFolderForPackageDependsOn>
  </PropertyGroup>
    <Target Name="BuildDirCollectFiles">
    <ItemGroup>
      <_BuildDirFiles Include="BuildDir\**\*" />
      <FilesForPackagingFromProject Include="%(_BuildDirFiles.Identity)">
        <DestinationRelativePath>%(RecursiveDir)%(Filename)%(Extension)</DestinationRelativePath>
      </FilesForPackagingFromProject>
    </ItemGroup>
  </Target>

Included in the project is the Install.MainBossSolo.Publish.xml profile to publish to the internal web.thinkage.ca/MainBossSolo web
pages. With this profile you can use the Publish: ToWebThinkage button in the tool bar to build, and install the entire package to
web.thinkage.ca/MainBossSolo.
REMEMBER YOU MUST RESET THE PERMISSIONS ON THE WEB SERVER. I do this with an external tool (configured as a menu
item in Visual Studio) that runs "c:\program files (x86)\putty\plink.exe web /home/jagardner/bin/setperms" (needs Putty installed and setup with
appropriate encryption keys set on web.thinkage.ca and your system)

Furthermore, you will have to RECONFIGURE Powershell.exe so that it can load .NET 4 assemblies for inspection
by adding a 'powershell.exe.config' file containing:
<?xml version="1.0"?> 
<configuration> 
    <startup useLegacyV2RuntimeActivationPolicy="true"> 
        <supportedRuntime version="v4.0.30319"/> 
        <supportedRuntime version="v2.0.50727"/> 
    </startup> 
</configuration>
in the following locations (first one if you are using x64)
C:\Windows\SysWOW64\WindowsPowerShell\v1.0\ 
C:\Windows\System32\WindowsPowerShell\v1.0\

If you fail to do this you will get an error during the powershell build process about an assembly that cannot be loaded
because it is newer than the version powershell was built with.

The SAMPLEInstall.MainBossSolo.Publish.xml contains the configuration for publishing to web.thinkage.ca;
As MSBUILD insists on writing to it after each Publish (to save settings ?), it must exist as a writeable file 
on your system. Copy SAMPLEInstall.MainBossSolo.Publish.xml to Install.MainBossSolo.Publish.xml in the same directory
to use it and REMEMBER to make it writable ('attrib -r') it as well.