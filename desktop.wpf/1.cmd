"C:\Users\Sergii Lutai\.nuget\packages\wix\3.11.0\tools\heat.exe"  dir bin\Debug -o Wix\ProductDir1.wxs -gg -scom -srd -sreg -suid -dr TARGETDIR	-pog Binaries -pog Satellites -pog Content -var wix.BinariesDir
"C:\Users\Sergii Lutai\.nuget\packages\wix\3.11.0\tools\candle.exe"  -nologo Wix\Product.wxs Wix\StandardUI.wxs Wix\ProductDir1.wxs -out obj\wix\
"C:\Users\Sergii Lutai\.nuget\packages\wix\3.11.0\tools\light.exe" obj\wix\Product.wixobj obj\wix\StandardUI.wixobj obj\wix\ProductDir1.wixobj -out bin\1.msi -dBinariesDir=C:\Data\Work\git\prototype\desktop.wpf\bin\Debug
:-b bin\Debug

:-dBinariesDir=C:\Data\Work\git\prototype\desktop.wpf\bin\Debug


exit



Directory="bin\Debug"
OutputFile="Wix\ProductDir2.wxs "
GenerateGuidsNow="yes"
SuppressCom="yes"
SuppressRootDirectory="yes"
SuppressRegistry="yes"
SuppressUniqueIds="yes"
DirectoryRefId="TARGETDIR"
ComponentGroupName="ComponentGroupFexSync"
AdditionalOptions=" -svb6 -generate container -pog Binaries -pog Satellites -pog Content "



Windows Installer XML Toolset Toolset Harvester version 3.11.0.1701
Copyright (c) .NET Foundation and contributors. All rights reserved.

 usage:  heat.exe harvestType harvestSource <harvester arguments> -o[ut] sourceFile.wxs

Supported harvesting types:

   dir      harvest a directory
   file     harvest a file
   payload  harvest a bundle payload as RemotePayload
   perf     harvest performance counters
   project  harvest outputs of a VS project
   reg      harvest a .reg file
   website  harvest an IIS web site

Options:
   -ag      autogenerate component guids at compile time
   -cg <ComponentGroupName>  component group name (cannot contain spaces e.g -cg MyComponentGroup)
   -configuration  configuration to set when harvesting the project
   -directoryid  overridden directory id for generated directory elements
   -dr <DirectoryName>  directory reference to root directories (cannot contain spaces e.g. -dr MyAppDirRef)
   -ext     <extension>  extension assembly or "class, assembly"
   -g1      generated guids are not in brackets
   -generate  
            specify what elements to generate, one of:
                components, container, payloadgroup, layout, packagegroup
                (default is components)
   -gg      generate guids now
   -indent <N>  indentation multiple (overrides default of 4)
   -ke      keep empty directories
   -nologo  skip printing heat logo information
   -out     specify output file (default: write to current directory)
   -platform  platform to set when harvesting the project
   -pog     
            specify output group of VS project, one of:
                Binaries,Symbols,Documents,Satellites,Sources,Content
              This option may be repeated for multiple output groups.
   -projectname  overridden project name to use in variables
   -scom    suppress COM elements
   -sfrag   suppress fragments
   -srd     suppress harvesting the root directory as an element
   -sreg    suppress registry harvesting
   -suid    suppress unique identifiers for files, components, & directories
   -svb6    suppress VB6 COM elements
   -sw<N>   suppress all warnings or a specific message ID
            (example: -sw1011 -sw1012)
   -swall   suppress all warnings (deprecated)
   -t       transform harvested output with XSL file
   -template  use template, one of: fragment,module,product
   -v       verbose output
   -var <VariableName>  substitute File/@Source="SourceDir" with a preprocessor or a wix variable
(e.g. -var var.MySource will become File/@Source="$(var.MySource)\myfile.txt" and 
-var wix.MySource will become File/@Source="!(wix.MySource)\myfile.txt"
   -wixvar  generate binder variables instead of preprocessor variables
   -wx[N]   treat all warnings or a specific message ID as an error
            (example: -wx1011 -wx1012)
   -wxall   treat all warnings as errors (deprecated)
   -? | -help  this help information

For more information see: http://wixtoolset.org/
