[Setup]
AppName=Ringkes
AppVersion=1.0
DefaultDirName={autopf}\Ringkes
DefaultGroupName=Ringkes
OutputDir=output
OutputBaseFilename=RingkesSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

SetupIconFile=Assets\logo.ico

[Files]
Source: "bin\Release\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Ringkes"; Filename: "{app}\Ringkes.exe"
Name: "{autodesktop}\Ringkes"; Filename: "{app}\Ringkes.exe"

[Run]
Filename: "{app}\Ringkes.exe"; Description: "Launch Ringkes"; Flags: nowait postinstall skipifsilent