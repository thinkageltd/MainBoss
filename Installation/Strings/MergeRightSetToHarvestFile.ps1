param (
[string]$HARVESTFILE = "You forgot the HARVESTFILE argument"
)
$RIGHTSET = (resolve-path ".\a\output\RightSetTranslations.xml")
$OUTPUT = $HARVESTFILE #join-path -path (resolve-path ".") -childpath "a\output\CRAPForTesting.xml"
$nt = new-object System.Xml.NameTable

[System.Xml.XmlDocument] $xr = new-object System.Xml.XmlDocument
[System.Xml.XmlDocument] $xc = new-object System.Xml.XmlDocument

$xr.load($RIGHTSET)
$xc.load($HARVESTFILE)

[System.Xml.XmlNode] $DatabaseAssembly = $xc.SelectSingleNode("/root")
[System.Xml.XmlNode] $mergefrom = $xr.SelectSingleNode("/root")

foreach($child in $mergefrom.get_ChildNodes())
{
	# we expect the root node (assembly) to have no namespace and the
	# childnodes (string) to be in namespace "http://www.thinkage.ca/XmlNamespaces/TranslatableStrings" using any prefix.
	# we alter the prefix to match what ContextStrings.xml declares for the same
	#namespace
	$imported = $xc.ImportNode($child, $true)
	$imported.set_Prefix("")
	$DatabaseAssembly.AppendChild($imported) | out-null
}
$s = new-object System.Xml.XmlWriterSettings
$s.Indent = $true
$s.IndentChars = " "
$s.NewLineHandling = [System.Xml.NewLineHandling]::Entitize
$s.CloseOutput = $true
$s.CheckCharacters = $false

$w = [System.Xml.XmlWriter]::Create($OUTPUT, $s)
$xc.WriteTo($w)
$w.Close()



