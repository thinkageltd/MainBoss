@echo on
set WINWORD=%ProgramFiles(x86)%\Microsoft Office\root\Office16\WINWORD.EXE
rd /s /q html\0000
md html
md html\0000
copy manual\Resources\*.* html\0000
copy manual\online42.docx print0000man.docx
"%WINWORD%" /mConvertMBDocToHTMLCoverClose print0000man.docx
"%WINWORD%" /mConvertMBDocToHTMLCoverClose2 html0000man.docx
"%WINWORD%" /mBreakUpHTMLFile30Cover html0000man.docx
"%WINWORD%" /mSaveWebPictures print0000man.docx
copy print0000man_files\*.png html\0000
del print0000man.htm
rd /s /q print0000man_files
rem COPY files that are dialogs for other windows in mainboss
pushd html\0000
copy "View.Purchase Order States.htm" "Select.Purchase Order State.htm"
copy "View.Purchase Order States.htm" "Browse.Purchase Order State.htm"
copy "View.Work Order States.htm" "Select.Work Order State.htm"
copy "View.Work Order States.htm" "Browse.Work Order State.htm"
copy "View.Work Order States.htm" "Work Order States.htm"
copy "Browse.Culture Information.htm" "Select.Culture Information.htm"
copy "Report.Request State History.htm" "Request State History.htm"
copy "Report.Request Summary by Assignee.htm" "Report.Request History by Assignee.htm"
copy "Edit.Postal Address.htm" "Select.Postal Address.htm"
copy "Edit.Postal Address.htm" "Browse.Postal Address.htm"
copy "Edit.Postal Address.htm" "View.Postal Addresses.htm"
copy "Edit.Postal Address.htm" "Browse.Postal Addresses.htm"
copy "Edit.Item Pricing.htm" "Select.Item Pricing.htm"
copy "Edit.Item Pricing.htm" "Browse.Item Pricing.htm"
copy "Edit.Physical Count.htm" "Browse.Physical Count.htm"
copy "Edit.Physical Count.htm" "Browse.Void Physical Count.htm"
copy "Edit.Expense Mapping.htm" "Browse.Expense Mapping.htm"
copy "Edit.Temporary Storage.htm" "Select.multiple Temporary Storage.htm"
copy "Edit.Temporary Storage.htm" "Browse.multiple Temporary Storage.htm"
copy "Edit.Task Temporary Storage.htm" "Select.multiple Template Temporary Storage.htm"
copy "Edit.Temporary Storage Assignment.htm" "Select.multiple Storeroom or Temporary Storage Assignment.htm"
copy "Edit.Temporary Storage Assignment.htm" "Select.Storeroom or Temporary Storage Assignment.htm"
copy "Edit.MainBoss-defined Security Role.htm" "Edit.User Security Role.htm"
copy "Browse.Storeroom Assignment.htm" "Browse.Temporary Storage.htm"
copy "Browse.Storeroom Assignment.htm" "Browse.Template Temporary Storage.htm"
copy "Browse.Storeroom Assignment.htm" "Browse.Storeroom or Temporary Storage Assignment.htm"
copy "Browse.Storeroom Assignment.htm" "Browse.Task Storeroom or Temporary Storage Assignment.htm"
copy "Edit.Task Temporary Storage Assignment.htm" "Select.multiple Task Storeroom or Temporary Storage Assignment.htm"
copy "Edit.Purchase Item.htm" "Browse.Purchase Item.htm"
copy "Edit.Purchase Item.htm" "Select.Purchase Item.htm"
copy "Browse.Location.htm" "Browse.Allowed Ship To Location.htm"
copy "Report.Time in Work Order Status.htm" "Time in Work Order Status.htm"
copy "Report.Item Adjustment Code.htm" "Item Adjustments.htm"
popd

