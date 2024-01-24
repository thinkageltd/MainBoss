$env:TOOLS="$env:ProgramFiles (x86)\Thinkage\ToolKit\1.1"

& "$env:TOOLS\xsltutil.exe" /iuri=..\MainBoss.xafdb /muri=BuildHtml.xsl  /output=MainBoss.html
