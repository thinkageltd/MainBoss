$builddir = $args[0]
$version = $args[1]
$package = $args[2]

Add-PSSnapin Pscx
$output = join-path -path $builddir -childpath ("$package"+"."+$version+".zip")
push-location (join-path -path $builddir -childpath $package)
write-zip -quiet -OutputPath $output -include .
pop-location
