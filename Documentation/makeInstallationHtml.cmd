@echo on
set WINWORD=%ProgramFiles(x86)%\Microsoft Office\root\Office16\WINWORD.EXE
rd /s /q install
md install
md install\0000
copy manual\Resources\AboutLogo.jpg install\0000
copy manual\Resources\mbmanual.css install\0000
copy manual\install.docx print0000install.docx
"%WINWORD%" /mConvertInstallToHTMLCoverClose print0000install.docx
"%WINWORD%" /mBreakUpHTMLInstallCover html0000install.docx
"%WINWORD%" /mSaveWebPictures print0000install.docx
copy print0000install_files\*.png install\0000
del print0000install.htm
rd /s /q print0000install_files



