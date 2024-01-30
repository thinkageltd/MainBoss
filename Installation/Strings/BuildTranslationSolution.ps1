#return just the directories of the current location
function Directories {get-childitem $args[0] |  where {$_.gettype().name -eq "DirectoryInfo"}}
function Files {get-childitem $args[0] |  where {$_.gettype().name -eq "FileInfo"}}

$DESTINATION = (resolve-path ".\Translators")
push-location "..\.."

$ProjectList = new-object System.Collections.ArrayList

foreach ($d in Directories(".") )
{
	write-host "Looking at $d"
	$TranslationResources = join-path $d "TranslationResources"
	$projectFile = join-path $d ($d.name + ".csproj")
	if ((test-path -pathType "Container" -path $TranslationResources) -and (test-path -pathType "Leaf" -path $projectFile))
	{
		[void]$ProjectList.Add($d)
		[void]( new-item (join-path $DESTINATION $d) -Type 'directory' -Force)
		[void]( new-item (join-path $DESTINATION $TranslationResources) -Type 'directory' -Force)
		foreach ($m in Files($TranslationResources))
		{
			$pathToCopy = join-path $TranslationResources $m
			copy-item  $pathToCopy (join-path $DESTINATION $pathToCopy) -recurse
			(Get-childItem (join-path $DESTINATION $pathToCopy)).Set_IsReadOnly($false)
		}
		
		[xml] $projectFileContents = get-content -path $projectFile -verbose
		# remove all project content references EXCEPT for the TranslationResource ones
		$xToRemove = new-object System.Collections.ArrayList
		#make all projects libraries
		
		$propertyGroup = $projectFileContents.Project.PropertyGroup[0]
		$propertyGroup.Item("OutputType").set_InnerText("Library")
		$x = $propertyGroup.Item("ApplicationIcon")
		if ($x -ne $null) {[void]	$propertyGroup.RemoveChild($x) }
		$x = $propertyGroup.Item("ApplicationManifest")
		if ($x -ne $null) { [void] $propertyGroup.RemoveChild($x) }
		
		#remove remnants of SourceControl
		foreach( $c in $projectFileContents.Project.PropertyGroup[0].get_ChildNodes() )
		{
			if ($c.get_Name().StartsWith("Scc")) {[void] $xToRemove.Add($c) }
			if ($c.get_Name().StartsWith("RunPostBuildEvent")) {[void] $xToRemove.Add($c) }
		}
		foreach( $x in $xToRemove) { [void] $projectFileContents.Project.PropertyGroup[0].RemoveChild($x) }
		#remove all items except translation resources
		$itemGroupToRemove = new-object System.Collections.ArrayList
		foreach( $itemgroup in $projectFileContents.Project.ItemGroup )
		{
			if (($itemgroup.get_HasChildNodes()) )
			{
				$xToRemove = new-object System.Collections.ArrayList
				foreach( $n in $itemgroup.get_ChildNodes() )
				{
					if ($n.get_Name() -ne "EmbeddedResource" -or -not($n.Include.StartsWith("TranslationResources")))
					{
						[void] $xToRemove.Add($n)
					}
				}
				foreach($x in $xToRemove ){[void] $itemgroup.RemoveChild($x) }
				remove-item variable:xToRemove

				if (-not($itemgroup.get_HasChildNodes()))
				{
					[void] $itemGroupToRemove.Add($itemgroup)
				}
			}
		}
		foreach($x in $itemGroupToRemove){[void] $projectFileContents.Project.RemoveChild($x)}
# scan all the property groups for PostBuildEvent and remove it
		foreach($propertyGroup in $projectFileContents.Project.PropertyGroup)
		{
			$xToRemove = new-object System.Collections.ArrayList
			foreach ($c in $propertyGroup.get_ChildNodes())
			{
				if ($c.get_Name().StartsWith("PostBuildEvent")) {[void] $xToRemove.Add($c) }
			}
			foreach( $x in $xToRemove) { [void] $propertyGroup.RemoveChild($x) }
		}
		$projectFileContents.save((join-path $DESTINATION $projectFile))
	}
}
# Now make the solution file reflect the above
$solution = get-content "MainBoss.sln"
$skipTo = $null
for($lineno = 0; $lineno -lt $solution.length; $lineno += 1)
{
	if ($skipTo -ne $null)
	{
		if ($skipTo.match($solution[$lineno]).Success)
		{
			$skipTo = $null
		}
		$solution[$lineno] = ""
	}
	$m = ([regex] "^.*GlobalSection.SourceCodeControl").match($solution[$lineno])
	if ($m.Success)
	{
		$solution[$lineno] = ""
		$skipTo = [regex]"^.*EndGlobalSection"
	}
	$m = ([regex] "(Debug\|Any CPU)|(Installation\|Any CPU)").match($solution[$lineno])
	if ($m.Success)
	{
		$solution[$lineno] = ""
	}
	# check Project names against ones we copied.
	$m = ([regex] "^Project").match($solution[$lineno])
	if ($m.Success)
	{
		$keep = $false
		foreach( $project in $ProjectList )
		{
			if ($solution[$lineno].Contains($project.name + ".csproj"))
			{
				$keep = $true
				break
			}
		}
		if (-not $keep)
		{
			$solution[$lineno] = ""
			$skipTo = [regex] "^EndProject$"
		}
	}
}
out-file -filePath (join-path $DESTINATION "MainBoss.sln") -inputObject $solution 

pop-location

