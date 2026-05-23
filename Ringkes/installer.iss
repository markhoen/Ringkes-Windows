[Setup]
AppName=Ringkes
AppVersion=1.1
DefaultDirName={autopf}\Ringkes
DefaultGroupName=Ringkes

OutputDir=output
OutputBaseFilename=RingkesSetup

Compression=lzma
SolidCompression=yes
WizardStyle=modern

SetupIconFile=Assets\logo.ico

PrivilegesRequired=admin

; supaya installer bisa update/install ulang
CloseApplications=yes
RestartApplications=no

[Files]

; ambil SEMUA isi folder publish
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]

Name: "{group}\Ringkes"; Filename: "{app}\Ringkes.exe"

Name: "{autodesktop}\Ringkes"; Filename: "{app}\Ringkes.exe"

[Run]

Filename: "{app}\Ringkes.exe"; \
Description: "Launch Ringkes"; \
Flags: nowait postinstall skipifsilent