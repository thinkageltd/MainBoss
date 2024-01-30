@echo off
if '%1' neq ''  goto check1
echo Need assembly name pattern for matching within ContextString input.
goto end
:check1
REM if second argument is given, use that as the output directory, otherwise construct one from argument 1
set convertoutput=%2
if '%2' neq '' goto check2
set convertoutput="..\..\%1"
:check2
set CONTEXTSTRINGS=%SRC%a\output\ContextStrings.xml
set assemblyName=%1
set PHRASECOMPOSITION=%SRC%a\output\PhraseCompositions.xml
set FORMATQUALIFIERS=%SRC%a\output\FormatQualifiers


if not exist %convertoutput%\TranslationResources\UPDATES md %convertoutput%\TranslationResources\UPDATES
@echo on
"%TOOLS%\xsltutil" /param=assemblyName:%assemblyName% /iuri="%CONTEXTSTRINGS%" /muri=ResxAssemblyConversion.xsl /output=%convertoutput%\TranslationResources\UPDATES\harvested.resx
@echo off
for %%i in (en,fr,es) do call MergeResX.cmd /original:%convertoutput%\TranslationResources\messages.%%i.resx /update:%convertoutput%\TranslationResources\UPDATES\harvested.resx /mo:%convertoutput%\TranslationResources\UPDATES\merged.%%i.resx /do:%convertoutput%\TranslationResources\UPDATES\deleted.%%i.resx /qi:%assemblyName% /fq:%FORMATQUALIFIERS%.%%i.xml /pc:%PHRASECOMPOSITION%
:end
