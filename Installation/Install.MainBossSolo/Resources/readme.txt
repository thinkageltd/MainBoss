The unsigned_bootstrap_setup.exe was generated using Visual Studio 2015 property pages' on the Thinkage.MainBoss.MainBossSolo project in the MainBossSolo.sln.
It is created UNSIGNED (In the Signing Tab, make sure 'Sign the ClickOnce manifests' option is cleared)
In the Publish tab, use the Prerequistes button and click all the bootstrapper packages you want included. Ensure 'Create setup program to install prerequisite components' is checked.
Use a dummy publish folder location (I used c:\unsaved\MainBossSoloPublished) and dummy installation folder URL.

Run the Publish Now operation.

In your dummy publish folder location, find the 'setup.exe' file. This is the unsigned_bootstrap_setup.exe file.

The following NO LONGER WORKS IN VISUAL STUDIO 2015; SIGNTOOL will not SIGN the setup.exe file after -URL option is used. This has been
reported to Microsoft Connect as https://connect.microsoft.com/VisualStudio/feedback/details/1947784/cannot-use-signtool-to-resign-setup-exe-after-using-url-option
We will use a signed setup_for_solo_no_url.exe for now.
UPDATE: 
Thank you for reporting this issue. Our investigation has shown that the same behavior can be observed in Visual Studio 2010 as well.

We don't have a fix at this time, however, I can offer you a workaround:
1. Open the project in Visual Studio.
2. Open the project properties page.
3. Switch to the Publish tab.
4. Enter the URL in the "Installation Folder URL" edit box.
5. Publish.

The resulting setup.exe will have the URL embedded in it and can be correctly signed using signtool.exe.

This is now called unsigned_bootstrap_setup_with_url.exe in the resource tree

------
To make a signed, usable 'setup_for_solo_on_mainboss.exe' use the following steps:
1) copy the 'unsigned_bootstrap_setup_with_url.exe' file to 'setup_for_solo_on_mainboss.exe'
2) execute 'setup_for_solo_on_mainboss.exe -url=http://www.mainboss.com/MainBossSolo'
3) using signtool with appropriate certificates, sign the file 'setup_for_solo_on_mainboss.exe'
(currently:
signtool sign /t http://tsa.starfieldtech.com /n "Thinkage Ltd." /i "Go Daddy Secure Certification Authority" /v setup*.exe

To make a test version for web.thinkage.ca, repeat the above steps for 'setup_for_solo_on_webthinkage.exe' and use
the url 'http://web.thinkage.ca/MainBossSolo"

Of course the url's specified above are assuming that is where the MainBossSolo clickonce deployment will be placed.


