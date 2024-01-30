@echo off
set FORMATQUALIFIERS=%SRC%a\output\FormatQualifiers
echo FRENCH (fr) IS NOT CURRENTLY BEING ANALYZED
for %%i in (en,es) do call MergeResX.cmd /fq:%FORMATQUALIFIERS%.%%i.xml /config:%SRC%FormatQualifierConfiguration.%%i.xml
