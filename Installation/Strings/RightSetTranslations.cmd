@echo on
set CONTEXTSTRINGS=%SRC%..\..\Thinkage.MainBoss.Database\Schema\Security\rightset.xml

"%TOOLS%\xsltutil" /iuri="%CONTEXTSTRINGS%" /muri=RightSetTranslations.xsl /output=a/output/rightsettranslations.xml
:end
