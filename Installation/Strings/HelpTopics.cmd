@echo on
if '%1' neq '' goto check2
echo Need location of HelpTopics.xml
goto end
:check2
set CONTEXTSTRINGS=%SRC%a\output\ContextStrings.xml
set convertoutput=%1

"%TOOLS%\xsltutil" /iuri="%CONTEXTSTRINGS%" /muri=HelpTopic.xsl /output=%convertoutput%.NEW
:end
