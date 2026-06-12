[Setup]
AppID=Ringkes
AppName=Ringkes
AppVersion=1.1.0

DefaultDirName={autopf}\Ringkes
DefaultGroupName=Ringkes

OutputDir=output
OutputBaseFilename=Ringkes-Windows-V1.1.0

Compression=lzma
SolidCompression=yes
WizardStyle=modern

SetupIconFile=Assets\logo.ico

PrivilegesRequired=admin

; tutup aplikasi lama otomatis
CloseApplications=yes
RestartApplications=no

; overwrite file lama
UninstallDisplayIcon={app}\Ringkes.exe

[Files]

; hapus semua file lama lalu copy baru
Source: "bin\Release\**"; \
DestDir: "{app}"; \
Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]

Name: "{group}\Ringkes"; \
Filename: "{app}\Ringkes.exe"

Name: "{autodesktop}\Ringkes"; \
Filename: "{app}\Ringkes.exe"

[Run]

Filename: "{app}\Ringkes.exe"; \
Description: "Launch Ringkes"; \
Flags: nowait postinstall skipifsilent