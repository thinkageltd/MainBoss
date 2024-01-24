This collection of command files locates translatable strings in the mainboss
source and libraries from the compiled RELEASE version of the assemblies,
and converts them to the .NET resource files necessary for translation.

These command files assume the existence of the Thinkage.ToolKit package
and the SourceGear vault client package being installed on the host computer.

The process requires first compiling the RELEASE configuration
version of the source, then running the 'CONVERTALL.CMD'.

After running the CONVERTALL.CMD file, a review of the output files
under 'a/output' can be done to see if any source corrections should
be made. Typically, one reviews the 'UnknownStrings.xml' files for each
assembly to see if perhaps some strings should be classified as
'Invariant', or should have a propery context key associated with them.

When satisfied that the strings are correct, the command file 'UPDATEALL.CMD'
will perform a vault checkout of the project/TranslationResources/messages
files and copy the updated resource files (that were put under
project/TranslationResources/UPDATES) to the checked out resource file.

The program should then be REBUILT using the INSTALLATION configuration
with the updated resource files.

The SETUP.CMD file can be reviewed if necessary; it sets up environment
variables describing the location of the input and the tools used for
the classification process.

The command files assume a consistent structure in the project source,
and in many cases the project name MUST be the same as the assembly name
for things to operate properly (see examples in CONVERTALL.CMD where 2
arguments are provided to resxconversion.cmd when the assembly name
and the pattern for the assembly name need to be different).
The command files are built with the assumption that they live 2 levels
below the main project source (e.g. in Installation/StringClassification
under the 'Head' directory).

