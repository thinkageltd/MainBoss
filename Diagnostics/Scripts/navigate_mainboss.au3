#include <sqlite.au3>
#include <sqlite.dll.au3>
#include <Date.au3>

Global $hDB
Global $_Forms[2][2]
Global $_timer			= TimerInit()	
Global $_logFile
Global $_MainBoss		= "MainBoss Maintenance Manager"
Global $_MainBoss_Help	= "MainBoss Advanced 3.0 Reference Manual"
Global $_MainBoss_Error	= "MainBoss Error"

_Main()	;; Entry Point



#cs----------------------------------------------------------------------------
	Function		: _Main()
	Date			: September 19, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This is the entry point for an artificial 'F1' or form
						help test.  Originally it's purpose was to open each
						form to test for the presence of help files.  It can
						be changed at the appropriate points to enable other
						tests on MainBoss 3.0.  The location mapping is done
						by SQLite in this version.
	Parameter List	: None.
	Return Value	: None.
	Revisions		: October 10, 2007
						- Added All functions being called by _Main to the 
							same file so the file may be placed in the source
							tree of MainBoss.
						- Updated Comments.
						- Added _LogEvent() for event tracking.
					  October 11, 2007
						- Changed file naming conventions to include variable
							log names.  This change includes the current date.
#ce----------------------------------------------------------------------------
Func _Main()
	Opt("WinTitleMatchMode", 1)
	
	Local $_logFileName
	Local $Query, $aRow, $test
	Local $titles[1]
	$titles[0] = 0

	For $a = 1 To 100
		If NOT FileExists("logs\Log" & $a & "-" & StringReplace(_NowCalcDate(), "/", "-") & ".txt") Then 
			$_logFileName = "logs\Log" & $a & "-" & StringReplace(_NowCalcDate(), "/", "-") & ".txt"
			ExitLoop
		EndIf	
	Next

	$_logFile = FileOpen($_logFileName, 10)
	
	;; Start SQLite and open the database.  Then query for the possible locations we can visit to test.
	_SQLite_Startup ()
	$hDB = _SQLite_Open ("MainBoss.db3")
	
	_SQlite_Query ($hDB, "SELECT name, direction FROM navcontrol ORDER BY name;", $Query)
	
	;; Now Process the query list.  If a location fails to give us F1, we record it into a table that we 
	;; can print out later.
	$test = True
	
	While _SQLite_FetchData ($Query, $aRow) = $SQLITE_OK
		_LogEvent("Visiting " & $aRow[0] & "; In direction: " & $aRow[1])
		_NavControl( $aRow[0], $aRow[1], $hDB )
		
		If $aRow[1] == "rev" Then 
			ContinueLoop
		Else	
			_LogEvent("Detecting controls for " & $aRow[0])
			$array = _DetectControls($_MainBoss)
			
			_LogEvent("Processing controls for " & $aRow[0])
			_ProcessButtons($array)
			
			_LogEvent("Testing help on form: " & $aRow[0])
			
			;; Start F1 Test.  Artificial sleep added at critical times to allow MB to catch up to its messagequeue.
			;; Start of test_code block.  this is where we throw in other tests.
;~ 			If $test Then 
;~ 				WinActivate($_MainBoss)
;~ 				If NOT WInWaitActive($_MainBoss, "", 1) Then MsgBox(0, "Please Activate MainBoss", "MainBoss must be active during the next operation.  Please activate")
;~ 				
;~ 				$test = False
;~ 				Send("{F1}")
;~ 				
;~ 				If WinWait($_MainBoss_Help, "", 5) Then 
;~ 					_LogEvent("Help appeared for " & $aRow[0])
;~ 					Sleep(500)
;~ 					_killIEHelp()
;~ 				Else
;~ 					_LogEvent("No help was opened for " & $aRow[0])
;~ 					
;~ 					$szArray = UBound($titles, 1)
;~ 					ReDim $titles[$szArray + 1]
;~ 					$titles[$szArray] = $aRow[0]
;~ 					$titles[0] += 1
;~ 	;~ 				_SQLite_Exec( $hDB, "INSERT INTO missinghelp (formTitle) values('" & $aRow[0] & "');")
;~ 				EndIf
;~ 				
;~ 			Else
;~ 				$test = True
;~ 			EndIf
			
			;; END F1 Help test
			;; End of test_code block.
			
			_LogEvent("Sleeping for 2 milliseconds")
			Sleep(200)
		EndIf
	WEnd
	
	_SQLite_Close($hDB)
	_SQLite_Shutdown()
	
	_LogEvent("Test Complete.  Terminating")
	FileClose($_logFile)
	Exit
EndFunc			;- End _Main()



#cs----------------------------------------------------------------------------
	Function		: _LogEvent($event_message)
	Date			: October 10, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: Function that writes events to a log file.
	Parameter List	: $event_message is any string that is to be written to the 
						log.
	Return Value	: None.
	Revisions		: None.
#ce----------------------------------------------------------------------------
Func _LogEvent(const $event_message)
	Local $_step = Round(TimerDiff($_timer)/1000, 3)
	FileWrite($_logFile, $_step & " (sec) into execution ... Event: " & $event_message & @CRLF)
EndFunc			;- End _LogEvent()



#cs----------------------------------------------------------------------------
	Function		: _DetectControls()
	Date			: September 19, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function takes a given form title and gets a class
						list from the form.  It then processes the class list
						copying out only the appropriate controls to be 
						tested.
	Parameter List	: $_windowTitle which is the string found in the titlebar
						of the form.
	Return Value	: $Locations which is an array of valid controls (buttons
						in this test) to process.
	Revisions		: October 10, 2007
						- Added to help_test.au3 source file to enable easier
							compiling and execution.
						- Esleif blocks commented out because the ini-write
							is not required in this revision.
						- Commented out the iniwrite() operation as this is 
							not needed.
					 October 4, 2007
						- Added _IsVisible to the If statement that is deter-
							mining if the button should be processed or not.
#ce----------------------------------------------------------------------------
Func _DetectControls(const $_windowTitle)
	
	Local $b = 0
;~ 	Local $s = 0
;~ 	Local $m = 0
;~ 	Local $h = 0
;~ 	Local $t = 0
;~ 	Local $e = 0
	Local $text2 = StringSplit( StringTrimRight( WinGetClassList( $_windowTitle, "" ), 1 ), @CRLF )
	Local $Locations[2][2]
	
;; Commented out as we don't care if the ini file exists in this test.
;~ 	If FileExists($_windowTitle & ".ini") Then 
;~ 		Return 1
;~ 	EndIf

	Local $_outFile =  $_windowTitle & ".ini"
		
	For $a=0 To Ubound($text2)-1 Step 1
		If StringInStr($text2[$a], "BUTTON", 2) AND _IsVisible(ControlGetHandle($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $b+1 & "]")) Then
			If $b == 0 Then 
;~ 				iniwrite($_outFile, "Form", "button"&$b+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $b+1 & "]"))
				
				$Locations[0][1] = $_windowTitle
				$Locations[$b + 1][0] = $text2[$a]
				$locations[$b + 1][1] = $b + 1
			Else
;~ 				iniwrite($_outFile, "Form", "button"&$b+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $b+1 & "]"))
				
				Local $Size1 = UBound($Locations, 1)
				Local $Size2 = UBound($Locations, 2)
				ReDim $Locations[$size1 + 1][$size2]
				
				$Locations[$b + 1][0] = $text2[$a]
				$Locations[$b + 1][1] = $b + 1
			EndIf
			$b += 1

;; This block was commented out in this revision because the rest of the controls do not matter.
;~ 		ElseIf StringInStr($text2[$a], "STATIC", 2) Then
;~ 			If $s == 0 Then
;~ 				iniwrite($_outFile, "Form", "sttext"&$s+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $s+1 & "]"))
;~ 			Else
;~ 				iniwrite($_outFile, "Form", "sttext"&$s+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $s+1 & "]"))
;~ 			EndIf
;~ 			$s += 1
;~ 		ElseIf StringInStr($text2[$a], "HEADER", 2) Then
;~ 			If $h == 0 Then
;~ 				iniwrite($_outFile, "Form", "header"&$h+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $h+1 & "]"))
;~ 			Else
;~ 				iniwrite($_outFile, "Form", "header"&$h+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $h+1 & "]"))
;~ 			EndIf
;~ 			$h += 1
;~ 		ElseIf StringInStr($text2[$a], "TABCONTROL", 2) Then
;~ 			If $t == 0 Then
;~ 				iniwrite($_outFile, "Form", "tab"&$t+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $t+1 & "]"))
;~ 			Else
;~ 				iniwrite($_outFile, "Form", "tab"&$t+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $t+1 & "]"))
;~ 			EndIf
;~ 			$t += 1
;~ 		ElseIf StringInStr($text2[$a], "EDIT", 2) Then
;~ 			If $e == 0 Then
;~ 				iniwrite($_outFile, "Form", "edit"&$e+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $e+1 & "]"))
;~ 			Else
;~ 				iniwrite($_outFile, "Form", "edit"&$e+1, ControlGetText($_windowTitle, "", "[CLASS:" & $text2[$a] & "; INSTANCE:" & $e+1 & "]"))
;~ 			EndIf
;~ 			$e += 1

		EndIf
			
	Next
	
	$Locations[0][0] = UBound($Locations, 1) - 1
	Return $Locations	
EndFunc		;- End _DetectControls()



#cs----------------------------------------------------------------------------
	Function		: _ProcessButtons()
	Date			: August 16, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function is where all the controls detected are
						'processed'.  It sends a click event to each control
						then tests for new form creation (and added to the 
						MainBoss PID).
					 This function was originally written to be part of a 
						mutually exclusive pair.  However because of
						undetermined problems with local variables being
						trashed, I opted to duplicate code.  Thereby getting
						expected functionality, without more time spent 
						debugging.
	Parameter List	: $Buttons which is an array of valid controls (buttons 
						in this test) to be processed.  It is expected this
						array will be generated by _DetectControls()
	Return Value	: None.
	Revisions		: Ovtober 11, 2007
						- Moved (removed) the "MainBoss Error" messages to 
							_IdentifyChildren().
					  October 10, 2007
						- Added to help_test.au3 source file to enable easier
							compiling.
						- removed all DEBUG tags that allowed variables to be
							seen from a debug pane.
						- tried to clean the code up a little bit.
#ce----------------------------------------------------------------------------
Func _ProcessButtons(const ByRef $Buttons)
	Opt("MouseClickDelay", 500)
	
	;;; This is used to pass into IdentifyChildren() as an additional filter.
	;;; Element 0 is uesd to manage the number in the array.
	;;; Here I am setting the default value to the window that we started from.
	Local $titles[2]
	$titles[1] = $Buttons[0][1]
	$titles[0] = 1
	
	Local $ChildInst1 = 1
	Local $ChildInst2 = 16
	Local $MiscInst1 = 31
	Local $MiscInst2 = 46
	
	For $a = 1 To $Buttons[0][0]

		;; Push the button and test for a new form under the main process.
		ControlClick($Buttons[0][1], "", "[CLASS:" & $Buttons[$a][0] & "; INSTANCE:" & $Buttons[$a][1] & "]")
		Sleep(500)
		Local $_Child = _IdentifyChildren($titles)
			
		If $_Child <> $Buttons[0][1] Then 
			$titles = _AddTitle($titles, $_Child)
;~ 			If $_Child <> 1 Then _AddFindForm($_Child, ControlGetText($Buttons[0][1], "", "[CLASS:" & $Buttons[$a][0] & "; INSTANCE:" & $Buttons[$a][1] & "]"), $Buttons[0][1])
				
			Local $new_array = _DetectControls($_Child)
			If $new_array == 1 Then 
				$titles = _DelTitle($titles, $_Child)
				If WinExists($_Child) Then WinClose($_Child)
				ContinueLoop
			EndIf

			For $b = 1 To $new_array[0][0]

				;; Push the button and test for a new form under our main process.
				ControlClick($new_array[0][1], "", "[CLASS:" & $new_array[$b][0] & "; INSTANCE:" & $new_array[$b][1] & "]")
				Sleep(500)
				Local $_Child2 = _IdentifyChildren($titles)


				;;If IdentifyChildren returned a 1 then continue the next instance of the loop.
				If $_Child2 == 1 Then ContinueLoop
																
				
				;; If the child returned is not the same as any of the forms we have open, then add this form title to the titles list,
				;; increment the index number at element 0 and assign the form title to the last element of the titles array.
				If $_Child2 <> $new_array[0][1] AND $_Child2 <> $Buttons[0][1] Then
					$titles = _AddTitle($titles, $_Child2)
;~ 					If $_Child2 <> 1 Then _AddFindForm($_Child2, ControlGetText($new_array[0][1], "", "[CLASS:" & $new_array[$b][0] & "; INSTANCE:" & $new_array[$b][1] & "]"), $new_array[0][1])
						
					;;; Detect the controls of our child.  Copy the returned array into $new_array2
					Local $new_array2 = _DetectControls($_Child2)
					If $new_array2 == 1 Then
						$titles = _DelTitle($titles, $_Child2)
						If WinExists($_Child2) Then WinClose($_Child2)
						ContinueLoop
					EndIf
						
					For $c = 1 To $new_Array2[0][0]
						
						;;Push the button and test for a new form under our main process.
						ControlClick($new_array2[0][1], "", "[CLASS:" & $new_array2[$c][0] & "; INSTANCE:" & $new_array2[$c][1] & "]")
						Sleep(500)
						Local $_Child3 = _IdentifyChildren($titles)
						
						;; If IdentifyChildren returned a 1 then the window exists in our titles list.  Continue the next instance of the loop.
						If $_Child3 == 1 Then ContinueLoop
						
						;; If the child returned is not the same as any of the forms we have open, then add this form title to the titles list,
						;; increment the index number at element 0, and assign the form title to the last element of the titles array.
						If $_Child3 <> $new_array2[0][1] AND $_Child3 <> $new_array[0][1] AND $_Child3 <> $Buttons[0][1] Then
							$titles = _AddTitle($titles, $_Child3)
;~ 							If $_Child3 <> 1 Then _AddFindForm($_Child3, ControlGetText($new_array2[0][1], "", "[CLASS:" & $new_array2[$c][0] & "; INSTANCE:" & $new_array2[$c][1] & "]"), $new_array2[0][1])
							
							
							;; Detect the controls on the new form.
							Local $new_array3 = _DetectControls($_Child3)
							If $new_array3 == 1 Then
								$titles = _DelTitle($titles, $_Child3)
								If WinExists($_Child3) Then WinClose($_Child3)
								ContinueLoop
							EndIf
							
							For $d = 1 To $new_array3[0][0]
								
								ControlClick($new_array3[0][1], "", "[CLASS:" & $new_array3[$d][0] & "; INSTANCE:" & $new_array3[$d][1] & "]")
								Sleep(500)
								Local $_Child4 = _IdentifyChildren($titles)
								
								;; If IdentifyChildren retuened 1 then continue the next instance of this loop.
								If $_Child4 == 1 Then ContinueLoop
								
								;; If the child returned is not the same as any of the forms we have open, then add this form title to the titles list,
								;; increment the index number at element 0, and assign the form title to the last element of the titles array.
								If $_Child4 <> $new_array3[0][1] AND $_Child4 <> $new_array2[0][1] AND $_Child4 <> $new_array[0][1] AND $_Child4 <> $Buttons[0][1] Then
									$titles = _AddTitle($titles, $_Child4)									
;~ 									If $_Child4 <> 1 then _AddFindForm($_Child4, ControlGetText($new_array3[0][1], "", "[CLASS:" & $new_array3[$d][0] & "; INSTANCE:" & $new_array3[$d][1] & "]"), $new_array3[0][1])
									
									
									;; Detect the controls of the new form.
									Local $new_array4 = _DetectControls($_Child4)
									If $new_array4 == 1 Then 
										$titles = _DelTitle($titles, $_Child4)
										If WinExists($_Child4) Then WinClose($_Child4)
										ContinueLoop
									EndIf
										
									For $e= 1 To $new_array4[0][0]
										
										ControlClick($new_array4[0][1], "", "[CLASS:" & $new_array4[$e][0] & "; INSTANCE:" & $new_array4[$e][1] & "]")
										Sleep(500)
										Local $_Child5 = _IdentifyChildren($titles)
										
										;; If IdentifyChildren returned 1 then continue the next iteration of this loop.
										If $_Child5 == 1 Then ContinueLoop
											
										;; If the child returned is not the same as any of the forms we have open, then add this form title to the titles list,
										;; increment the index number at element 0, and assign the form title to the last element of the titles array.
										If $_Child5 <> $new_array4[0][1] AND $_Child5 <> $new_array3[0][1] AND $_Child5 <> $new_array2[0][1] AND $_Child5 <> $new_array[0][1] AND $_Child5 <> $Buttons[0][1] Then
											$titles = _AddTitle($titles, $_Child5)
;~ 											If $_Child5 <> 1 Then _AddFindForm($_Child5, ControlGetText($new_array4[0][1], "", "[CLASS:" & $new_array4[$e][0] & "; INSTANCE:" & $new_array4[$e][1] & "]"), $new_array4[0][1])											
											
											Local $new_array5 = _DetectControls($_Child5)
											If $new_array5 == 1 Then
												$titles = _DelTitle($titles, $_Child5)
												If WinExists($_Child5) Then WinClose($_Child5)
												ContinueLoop
											EndIf
												
											For $f = 1 To $new_array5[0][0]
												
												ControlClick($new_array5[0][1], "", "[CLASS:" & $new_array5[$f][0] & "; INSTANCE:" & $new_array5[$f][1] & "]")
												Sleep(500)
												Local $_Child6 = _IdentifyChildren($titles)

												;; If IdentifyChildren returns 1 then continue with the next iteration of the loop.
												If $_Child6 == 1 Then ContinueLoop
																
												;; If the child returned is not the same as any of the forms we have open, then add this form title to the titles list,
												;; increment the index number at element 0, and assign the form title to the last element of the titles array.
												If $_Child6 <> $new_array5[0][1] AND $_Child6 <> $new_array4[0][1] AND $_Child6 <> $new_array3[0][1] AND $_Child6 <> $new_array2[0][1] AND $_Child6 <> $new_array[0][1] AND $_Child6 <> $Buttons[0][1] Then
													$titles = _AddTitle($titles, $_Child6)
;~ 													If $_Child6 <> 1 Then _AddFindForm($_Child6, ControlGetText($new_array5[0][1], "", "[CLASS:" & $new_array5[$f][0] & "; INSTANCE:" & $new_array5[$f][1] & "]"), $new_array5[0][1])
													
													Local $new_array6 = _DetectControls($_Child6)
													If $new_array6 == 1 Then
														$titles = _DelTitle($titles, $_Child6)
														If WinExists($_Child6) Then WinClose($_Child6)
														ContinueLoop
													EndIf
														
													For $g = 1 To $new_array6[0][0]
														
														ControlClick($new_array6[0][1], "", "[CLASS:" & $new_array6[$g][0] & "; INSTANCE:" & $new_array6[$g][1] & "]")
														Sleep(500)
														Local $_Child7 = _IdentifyChildren($titles)
														
														If $_Child7 == 1 Then ContinueLoop
														
														If $_Child7 <> $new_array6[0][1] AND $_Child7 <> $new_array5[0][1] AND $_Child7 <> $new_array4[0][1] AND $_Child7 <> $new_array3[0][1] AND $_Child7 <> $new_array2[0][1] AND $_Child7 <> $new_array[0][1] AND $_Child7 <> $Buttons[0][1] Then
															$titles = _AddTitle($titles, $_Child7)
;~ 															If $_Child7 <> 1 Then _AddFindForm($_Child7, ControlGetText($new_array6[0][1], "", "[CLASS:" & $new_array6[$g][0] & "; INSTANCE:" & $new_array6[$g][1] & "]"), $new_array6[0][1])
															
															
															Local $new_array7 = _DetectControls($_Child7)
															If $new_array7 == 1 Then
																$titles = _DelTitle($titles, $_Child7)
																If WinExists($_Child7) Then WinClose($_Child7)
																ContinueLoop
															EndIf
															
															For $h = 1 To $new_array7[0][0]
																
																ControlClick($new_array7[0][1], "", "[CLASS:" & $new_array7[$h][0] & "; INSTANCE:" & $new_array7[$h][1] & "]")
																Sleep(500)
																Local $_Child8 = _IdentifyChildren($titles)
																	
																If $_Child8 == 1 then ContinueLoop
																	
																If $_Child8 <> $new_array7[0][1] AND $_Child8 <> $new_array6[0][1] AND $_Child8 <> $new_array5[0][1] AND $_Child8 <> $new_array4[0][1] AND $_Child8 <> $new_array3[0][1] AND $_Child8 <> $new_array2[0][1] AND $_Child8 <> $new_array[0][1] AND $_Child8 <> $Buttons[0][1] Then
																	$titles = _AddTitle($titles, $_Child8)
;~ 																	If $_Child8 <> 1 Then _AddFindForm($_Child8, ControlGetText($new_array7[0][1], "", "[CLASS:" & $new_array7[$h][0] & "; INSTANCE:" & $new_array7[$h][1] & "]"), $new_array7[0][1])
																		
																	Local $new_array8 = _DetectControls($_Child8)
																	If $new_array8 == 1 Then
																		$titles = _DelTitle($titles, $_Child8)
																		If WinExists($_Child8) Then WinClose($_Child8)
																		ContinueLoop
																	EndIf
																	
																	For $i = 1 To $new_array8[0][0]

																		ControlClick($new_array8[0][1], "", "[CLASS:" & $new_array8[$i][0] & "; INSTANCE:" & $new_array8[$i][1] & "]")
																		Sleep(500)
																		Local $_Child9 = _IdentifyChildren($titles)
																		
																		If $_Child9 == 1 Then ContinueLoop
																		
																		If $_Child9 <> $new_array8[0][1] AND $_Child9 <> $new_array7 AND $_Child9 <> $new_array6[0][1] AND $_Child9 <> $new_array5[0][1] AND $_Child9 <> $new_array4[0][1] AND $_Child9 <> $new_array3[0][1] AND $_Child9 <> $new_array2[0][1] AND $_Child9 <> $new_array[0][1] AND $_Child9 <> $Buttons[0][1] Then
																			$titles = _AddTitle($titles, $_Child9)
;~ 																			If $_Child9 <> 1 Then _AddFindForm($_Child9, ControlGetText($new_array8[0][1], "", "[CLASS:" & $new_array8[$i][0] & "; INSTANCE:" & $new_array8[$i][1] & "]"), $new_array8[0][1])
																			
																			Local $new_array9 = _DetectControls($_Child9)
																			If $new_array9 == 1 Then
																				$titles = _DelTitle($titles, $_Child9)
																				If WinExists($_Child9) Then WinClose($_Child9)
																				ContinueLoop
																			EndIf
																			
																			For $j = 1 To $new_array9[0][0]
																				
																				ControlClick($new_array9[0][1], "", "[CLASS:" & $new_array9[$j][0] & "; INSTANCE:" & $new_array9[$j][1] & "]")
																				Sleep(500)
																				Local $_Child10 = _IdentifyChildren($titles)

																				If $_Child10 == 1 then ContinueLoop
																					
																				If $_Child10 <> $new_array9[0][1] AND $_Child10 <> $new_array8[0][1] AND $_Child10 <> $new_array7[0][1] AND $_Child10 <> $new_array6[0][1] AND $_Child10 <> $new_array5[0][1] AND $_Child10 <> $new_array4[0][1] AND $_Child10 <> $new_array3[0][1] AND $_Child10 <> $new_array2[0][1] AND $_Child10 <> $new_array[0][1] AND $_Child10 <> $Buttons[0][1] Then
																					$titles = _AddTitle($titles, $_Child10)
;~ 																					If $_Child10 <> 1 Then _AddFindForm($_Child10, ControlGetText($new_array9[0][1], "", "[CLASS:" & $new_array9[$j][0] & "; INSTANCE:" & $new_array9[$j][1] & "]"), $new_array9[0][1])
																					
																					Local $new_array10 = _DetectControls($_Child10)
																					If $new_array10 == 1 Then
																						$titles = _DelTitle($titles, $_Child10)
																						If WinExists($_Child10) Then WinClose($_Child10)
																						ContinueLoop
																					EndIf
																						
																					For $k = 1 To $new_array10[0][0]
																						
																						ControlClick($new_array10[0][1], "", "[CLASS:" & $new_array10[$k][0] & "; INSTANCE:" & $new_array10[$k][1] & "]")
																						Sleep(500)
																						Local $_Child11 = _IdentifyChildren($titles)
																							
																						If $_Child11 == 1 Then ContinueLoop
																								
																						If $_Child11 <> $new_array10[0][1] AND $_Child11 <> $new_array9[0][1] AND $_Child11 <> $new_array8[0][1] AND $_Child11 <> $new_array7 AND $_Child11 <> $new_array6[0][1] AND $_Child11 <> $new_array5[0][1] AND $_Child11 <> $new_array4[0][1] AND $_Child11 <> $new_array3[0][1] AND $_Child11 <> $new_array2[0][1] AND $_Child11 <> $new_array[0][1] AND $_Child11 <> $Buttons[0][1] Then
																							$titles = _AddTitle($titles, $_Child11)
;~ 																							If $_Child11 <> 1 Then _AddFindForm($_Child11, ControlGetText($new_array10[0][1], "", "[CLASS:" & $new_array10[$k][0] & "; INSTANCE:" & $new_array10[$k][1] & "]"), $new_array10[0][1])
																								
																							Local $new_array11 = _DetectControls($_Child11)
																							If $new_array11 == 1 Then
																								$titles = _DelTitle($titles, $_Child11)
																								If WinExists($_Child11) Then WinClose($_Child11)
																								ContinueLoop
																							EndIf
																							
																							For $l = 1 To $new_array11[0][0]
																								
																								ControlClick($new_array11[0][1], "", "[CLASS:" & $new_array11[$l][0] & "; INSTANCE:" & $new_array11[$l][1] & "]")
																								Sleep(500)
																								Local $_Child12 = _IdentifyChildren($titles)
																									
																								If $_Child12 == 1 then ContinueLoop
																																																
																								If $_Child12 <> $new_array11[0][1] AND $_Child12 <> $new_array10[0][1] AND $_Child12 <> $new_array9[0][1] AND $_Child12 <> $new_array8[0][1] AND $_Child12 <> $new_array7[0][1] AND $_Child12 <> $new_array6[0][1] AND $_Child12 <> $new_array5[0][1] AND $_Child12 <> $new_array4[0][1] AND $_Child12 <> $new_array3[0][1] AND $_Child12 <> $new_array2[0][1] AND $_Child12 <> $new_array[0][1] AND $_Child12 <> $Buttons[0][1] Then
																									$titles = _AddTitle($titles, $_Child12)
;~ 																									If $_Child12 <> 1 Then _AddFindForm($_Child12, ControlGetText($new_array11[0][1], "", "[CLASS:" & $new_array11[$l][0] & "; INSTANCE:" & $new_array11[$l][1] & "]"), $new_array11[0][1])
																										
																									Local $new_array12 = _DetectControls($_Child12)
																									If $new_array12 == 1 Then
																										$titles = _DelTitle($titles, $_Child12)
																										If WinExists($_Child12) Then WinClose($_Child12)																										
																										ContinueLoop
																									EndIf
																									
																									For $m = 1 To $new_array12[0][0]
																										
																										ControlClick($new_array12[0][1], "", "[CLASS:" & $new_array12[$m][0] & "; INSTANCE:" & $new_array12[$m][1] & "]")
																										Sleep(500)
																										Local $_Child13 = _IdentifyChildren($titles)
																											
																										If $_Child13 == 1 Then ContinueLoop
																											
																										If $_Child13 <> $new_array12 AND $_Child13 <> $new_array11 AND $_Child13 <> $new_array10[0][1] AND $_Child13 <> $new_array9[0][1] AND $_Child13 <> $new_array8[0][1] AND $_Child13 <> $new_array7 AND $_Child13 <> $new_array6[0][1] AND $_Child13 <> $new_array5[0][1] AND $_Child13 <> $new_array4[0][1] AND $_Child13 <> $new_array3[0][1] AND $_Child13 <> $new_array2[0][1] AND $_Child13 <> $new_array[0][1] AND $_Child13 <> $Buttons[0][1] Then
																											$titles = _AddTitle($titles, $_Child13)
;~ 																											If $_Child13 <> 1 Then _AddFindForm($_Child13, ControlGetText($new_array12[0][1], "", "[CLASS:" & $new_array12[$m][0] & "; INSTANCE:" & $new_array12[$m][1] & "]"), $new_array12[0][1])
																												
																											Local $new_array13 = _DetectControls($_Child13)
																											If $new_array13 == 1 Then
																												$titles = _DelTitle($titles, $_Child13)
																												If WinExists($_Child13) Then WinClose($_Child13)
																												ContinueLoop
																											EndIf
																										If WinExists($_Child13) AND $new_array13 <> 1 Then WinClose($_Child13)
																											
																										EndIf
																									Next
																									$titles = _DelTitle($titles, $new_array12[0][1])
																									If WinExists($_Child12) AND $new_array12 <> 1 Then WinClose($_Child12)
																								EndIf
																								
																							Next
																							$titles = _DelTitle($titles, $new_array11[0][1])
																							If WinExists($_Child11) AND $new_array11 <> 1 Then WinClose($_Child11)
																							
																						EndIf
																						
																					Next
																					$titles = _DelTitle($titles, $new_array10[0][1])
																					If WinExists($_Child10) AND $new_array10 <> 1 Then WinClose($_Child10)
																				EndIf
																				
																			Next
																			$titles = _DelTitle($titles, $new_array9[0][1])
																			If WinExists($_Child9) AND $new_array9 <> 1 Then WinClose($_Child9)
																		EndIf
																		
																	Next
																	$titles = _DelTitle($titles, $new_array8[0][1])
																	If WinExists($_Child8) AND $new_array8 <> 1 Then WinClose($_Child8)
																EndIf
																
															Next			
															$titles = _DelTitle($titles, $new_array7[0][1])
															If WinExists($_Child7) AND $new_array7 <> 1 Then WinClose($_Child7)
														EndIf
														
													Next
													$titles = _DelTitle($titles, $new_array6[0][1])
													If WinExists($_Child6) AND $new_array6 <> 1 Then WinClose($_Child6)
												EndIf
												
											Next			
											$titles = _DelTitle($titles, $new_array5[0][1])
											If WinExists($_Child5) AND $new_array5 <> 1 Then WinClose($_Child5)
										EndIf
										
									Next				
									$titles = _DelTitle($titles, $new_array4[0][1])
									If WinExists($_Child4) AND $new_array4 <> 1 Then WinClose($_Child4)
								EndIf
								
							Next					
							$titles = _DelTitle($titles, $new_array3[0][1])
							If WinExists($_Child3) AND $new_array3 <> 1 Then WinClose($_Child3)
						EndIf
						
					Next				
					$titles = _DelTitle($titles, $new_array2[0][1])
					If WinExists($_Child2) AND $new_array2 <> 1 Then WinClose($_Child2)
				EndIf
				
			Next
			$titles = _DelTitle($titles, $new_array[0][1])
			If WinExists($_Child) AND $new_array <> 1 Then WinClose($_Child)
		EndIf

	Next
	Return

EndFunc			;- End _ProcessButtons()




#cs----------------------------------------------------------------------------
	Function		: _AddTitle(ByRef $Array, const $title)
	Date			: August 16, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function adds open forms to an array of form titles
						to aid in clean-up as we begin recursion.  The array is
						also used to test for duplicate forms.
	Parameter List	: $Array is a referenced array that has the current list of
						open MainBoss forms.
					 $title is the title of the new MainBoss form to add to the
						array.
	Return Value	: $Array
	Revisions		: October 10, 2007
						- Added to help_test.au3 source file to enable easier
							compiling.
						- Cleaned up DEBUG tags.
#ce----------------------------------------------------------------------------
Func _AddTitle(ByRef $Array, const $title)
	local $szArray = UBound($Array, 1)
	ReDim $Array[$szArray+1]
	$Array[$szArray] = $title
	$Array[0] += 1

	Return $Array
EndFunc			;- End _AddTitle()



#cs----------------------------------------------------------------------------
	Function		: _DelTitle(ByRef $Array, const $title)
	Date			: August 16, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function cleans up dismissed form titles from the
						array created by _AddTitle().
	Parameter List	: $Array is a referenced array that has the current list of
						open MainBoss forms.
					 $title is the title of the MainBoss form that has been 
						dismissed.
	Return Value	: $Array
	Revisions		: October 10, 2007
						- Added to help_test.au3 source file to enable easier
							compiling.
						- Cleaned up DEBUG tags.
#ce----------------------------------------------------------------------------
Func _DelTitle(ByRef $Array, const $title)
	If IsArray($Array) Then
		Local $i = 0
		Local $k = 1
		Local $szArray = UBound($Array, 1)
		Local $newArray[$szArray]
		$newArray[0] = $Array[0] - 1

		For $i = 1 To UBound($Array, 1) -1
			If $Array[$i] <> $title Then
				$newArray[$k] = $Array[$i]
				$k += 1
			EndIf
		Next

		Return $newArray
	EndIf
EndFunc			;- End _DelTitle()



#cs----------------------------------------------------------------------------
	Function		: _IdentifyChildren(const ByRef $open_windows)
	Date			: August 10, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function gets the form titles associated with or are
						linked to the MainBoss PID.  It takes the current 
						maintained list of known child forms and it tests the
						title against the list.  If the title is not found the
						function returns the title.  If the title is found or 
						the function abnormally terminated, then function 
						returns 1.  If a null or blank title is found, the
						function returns 2.
	Parameter List	: $open_windows is an array of current window titles as
						maintained by _AddTitle() and _DelTitle().
	Return Value	: 1 = Form title exists, or the function terminated 
						abnormally.
					  2 = title is NULL or blank. ADDED: Also returned if the 
						deletion dialog box is handled.
					  title = If the form is a new child form of MainBoss then
						the new title is returned.
	Revisions		: October 11, 2007
						- Added "MainBoss Question" dialog box filter.
							Processes this window and returns 2.
						- Added Filtering for MainBoss Error dialogs here
							to reduce repetition in _ProcessButtons().
					  October 10, 2007
						- Added to help_test.au3 source file to enable easier
							compiling.
					  October 4, 2007
						- Added F1 or form help processing.
						- removed F1 or form help processing.
#ce----------------------------------------------------------------------------
Func _IdentifyChildren(const ByRef $open_windows)
	
	Local $_PID = WinGetProcess($_MainBoss)
	Local $_list = WinList()
		
	For $a = 1 to $_list[0][0]
		
		;; TODO: Consider using a switch statement here.
		If StringInStr($_list[$a][0], "End Program", 2) > 0 Then
			_LogEvent("Recieved " & $_list[$a][0] & " message.")	
			FileClose($_logFile)
			Exit
		ElseIf StringInStr($_list[$a][0], "MainBoss Question", 2) > 0 AND _IsVisible($_list[$a][1]) AND WinGetProcess($_list[$a][0]) == $_PID THen
			ControlClick("MainBoss Question", "", "[CLASS:Button; INSTANCE:2]")
			Return 2
		ElseIf StringInStr($_list[$a][0], "MainBoss Error", 2) > 0 AND _IsVisible($_list[$a][1]) AND WinGetProcess($_list[$a][0]) == $_PID Then
			WinClose($_MainBoss_Error)
			
			If WinExists($_MainBoss_Error) Then WinWaitClose($_MainBoss_Error)
			Return 2
		Else
			If $_list[$a][0] <> "" AND _IsVisible($_list[$a][1]) AND $_list[$a][0] <> $_MainBoss AND WinGetProcess($_list[$a][0]) == $_PID Then
				For $k = 1 To $open_windows[0]
					If $_list[$a][0] == $open_windows[$k] Then Return 1
					If $_list[$a][0] == "" Then Return 2
				Next
					
				Return $_list[$a][0]
			EndIf
		EndIf
	Next
		
	Return 1
EndFunc			;- End _IdentifyChildren()	



#cs----------------------------------------------------------------------------
	Function		: _IsVisible(const $handle)
	Date			: August 10, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function tests handle's for their current state.
						If they are visible, then a 1 is returned.  If they are
						not visible, then a 0 is returned.
	Parameter List	: $handle is the handle to the control or window that you
						wish to test for visibility.
	Return Value	: 1 = The handle's window is visible.
					  2 = The handle's window is not visible.
	Revisions		: October 10, 2007
						- Added to help_test.au3 source file to enable easier
							compiling.
#ce----------------------------------------------------------------------------
Func _IsVisible(const $handle)
	If BitAnd( WinGetState($handle), 2 ) Then 
		Return 1
	Else
		Return 0
	EndIf
EndFunc			;- End IsVisible()	



#cs----------------------------------------------------------------------------
	Function		: _NavControl(const $_Destination, const $_Action, const _
						$hDB)
	Date			: July 24, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function utilizes an SQL table to map out the 
						"control panel" in MainBoss 3.0.  These are required 
						because the	control does not release any text.
	Parameter List	: $_Destination is where you want to go.  For a list see
						Navigation_Directives.txt.
					  $_Action is what you wish to do.  
						"fwd" = Goto this location
						"rev" = Restore menu from this location.
					  $hDB is the handle to the already opened Navigation DB.
	Return Value	: None.
	Revisions		: October 10, 2007
					  August 29, 2007
#ce----------------------------------------------------------------------------
Func _NavControl(const $_Destination, const $_Action, const $hDB)
	Opt("SendKeyDelay", 300)
	Opt("TrayIconDebug", 1)
	
	; Vars
	Local $var
	Local $WinText
	Local $OldText
	Local $done = 0
	Local $hQuery
	Local $returned
	Local $z = 1
	Local $i, $k
	Local $_Class, $_Class2
	
	;; Get the class list and isolate the listbox and our static title
		$text = WinGetClassList($_MainBoss, "")
		$text2 = StringSplit(StringTrimRight($text, 1), @CRLF)
			
		For $i=0 To Ubound($text2) -1
			If StringInStr($text2[$i], "LIST", 2) > 1 Then
				$_Class = $text2[$i]
			EndIf
		Next
		
		For $i=0 To UBound($text2) -1
			If StringInStr($text2[$i], "STATIC", 2) > 1 Then
				$_Class2 = $text2[$i]
			EndIf
		Next

	if IsDeclared($WinText) Then 
		$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
	EndIf

	;; Determine the absolute path of the destination.
	_SQLite_Query( $hDB, "SELECT path FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $_Destination & "'" & ";", $hQuery )
	_SQLite_FetchData( $hQuery, $returned)

	;; If there is only one level to process, then jump to else statement.
	If $returned[0] <> "" Then
		Local $absPath[2]
		$absPath[0] = 1

	;; There are two sceneario's here.  
	;; 1.) "fwd" direction
	;; 2.) "rev" direction
	;; Both require processing differently.  That's why there is a second If here.
		If $_Action == "fwd" Then

	;; Here I get the absolute path to the destination.
			While $returned[0] <> "" 
				ReDim $absPath[UBound($absPath, 1) + 1]
				$absPath[$z] = $returned[0]
				$absPath[0] += 1
				
				_SQLite_Query( $hDB, "SELECT path FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $returned[0] & "'" & ";", $hQuery )
				_SQLite_FetchData( $hQuery, $returned)
				$z += 1
			WEnd
					
			$k = $absPath[0] - 1
			
	;; Get the keys from the database for the first stop on the absolute path.
			While true
				_SQLite_Query( $hDB, "SELECT key1,key2,key3,text,name FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $absPath[$k] & "'" & ";", $hQuery )
				_SQLite_FetchData($hQuery, $var)
	;; Do the dirty work -- well the mouse clicks anyway.
				For $i = 0 To 2
					If $var[$i] <> "" Then ControlSend($_MainBoss, "", "[CLASS:" & $_Class & "; INSTANCE:1]", $var[$i])
					
	;; Set $WinNText to the (new) static title
					$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
					$StartLoop = TimerInit()
					
	;; Test that The static title changed ::: if not, then coninute to wait until it does
					While NOT $done
						$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
						$OldText = $WinText
						If $OldText <> $WinText OR TimerDiff($StartLoop) > 15 Then
							$done = 1
						EndIf
					WEnd
				Next
					
				$k -= 1
				If $k == $absPath[0] - 1 OR $k == 0 Then ExitLoop
				
			WEnd
			
			_SQLite_Query( $hDB, "SELECT key1,key2,key3,text,name FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $_Destination & "'" & ";", $hQuery )
			_SQLite_FetchData($hQuery, $var)
			For $i = 0 To 2
				If $var[$i] <> "" Then ControlSend($_MainBoss, "", "[CLASS:" & $_Class & "; INSTANCE:1]", $var[$i])
					
	;; Set $WinNText to the (new) static title
				$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
				$StartLoop = TimerInit()
				
	;; Test that The static title changed ::: if not, then coninute to wait until it does
				While NOT $done
					$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
					$OldText = $WinText
					If $OldText <> $WinText OR TimerDiff($StartLoop) > 15 Then
						$done = 1
					EndIf
				WEnd
			Next
			
		ElseIf $_Action == "rev" Then
			
	;; This is the reverse condition.
			While $returned[0] <> "" 
				ReDim $absPath[UBound($absPath, 1) + 1]
				$absPath[$z] = $returned[0]
				$absPath[0] += 1
				
				_SQLite_Query( $hDB, "SELECT path FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $returned[0] & "'" & ";", $hQuery )
				_SQLite_FetchData( $hQuery, $returned)
				$z += 1
			WEnd
				
			_SQLite_Query( $hDB, "SELECT key1,key2,key3,text,name FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $_Destination & "'" & ";", $hQuery )
			_SQLite_FetchData($hQuery, $var)
			For $i = 0 To 2
				If $var[$i] <> "" Then ControlSend($_MainBoss, "", "[CLASS:" & $_Class & "; INSTANCE:1]", $var[$i])
					
	;; Set $WinNText to the (new) static title
				$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
				$StartLoop = TimerInit()
				
	;; Test that The static title changed ::: if not, then coninute to wait until it does
				While NOT $done
					$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
					$OldText = $WinText
					If $OldText <> $WinText OR TimerDiff($StartLoop) > 15 Then
						$done = 1
					EndIf
				WEnd
			Next
			
	;; Start at the beginning of the array's data elements.
			$k = 1			
			While $k <> $absPath[0]
				_SQLite_Query( $hDB, "SELECT key1,key2,key3,text,name FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $absPath[$k] & "'" & ";", $hQuery )
				_SQLite_FetchData($hQuery, $var)
				For $i = 0 To 2
					If $var[$i] <> "" Then ControlSend($_MainBoss, "", "[CLASS:" & $_Class & "; INSTANCE:1]", $var[$i])
					
	;; Set $WinNText to the (new) static title
					$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
					$StartLoop = TimerInit()
					
	;; Test that The static title changed ::: if not, then coninute to wait until it does
					While NOT $done
						$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
						$OldText = $WinText
						If $OldText <> $WinText OR TimerDiff($StartLoop) > 15 Then
							$done = 1
						EndIf
					WEnd
				Next

	;; In reverse, $absPath[0] is the end of the line.
				$k += 1
				If $k == $absPath[0] OR $k == 0 Then ExitLoop
			WEnd
		EndIf
	Else
		
	;; Pull the data from the database.
		_SQLite_Query( $hDB, "SELECT key1,key2,key3,text,name FROM navcontrol WHERE direction=" & "'" & $_Action & "'" & " AND name=" & "'" & $_Destination & "'" & ";", $hQuery )
		_SQLite_FetchData($hQuery, $var)
		
	;; Processing Code.
		For $i = 0 To 2
			If $var[$i] <> "" Then ControlSend($_MainBoss, "", "[CLASS:" & $_Class & "; INSTANCE:1]", $var[$i])
				
	;; Set $WinNText to the (new) static title
			$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
			$StartLoop = TimerInit()
				
	;; Test that The static title changed ::: if not, then coninute to wait until it does
			While NOT $done
				$WinText = ControlGetText($_MainBoss, "", "[CLASS:" & $_Class2 & "; INSTANCE:1]")
				$OldText = $WinText
				If $OldText <> $WinText OR TimerDiff($StartLoop) > 15 Then
					$done = 1
				EndIf
			WEnd
		Next
	EndIf

	Return
EndFunc			;- End _NavControl()



#cs----------------------------------------------------------------------------
	Function		: _killIEHelp()
	Date			: October 11, 2007
	Written By		: Rob Stevens, Thinkage Ltd.
	Description		: This function locates the open IE Help windows and it
						dismisses them.
	Parameter List	: None.
	Return Value	: None.
	Revisions		: October 11, 2007
						- Added to help_test.au3 source file.
#ce----------------------------------------------------------------------------
Func _killIEHelp()
	Local $_list = WinList()
		
	For $a = 1 to $_list[0][0]
		If StringInStr($_list[$a][0], $_MainBoss_Help, 2) > 0 Then
			WinClose($_list[$a][0])
			Return 
		EndIf
	Next
EndFunc			;- End _killIEHelp()	

