#define AppName "ProxiFyre UI"
#define AppVersion GetEnv("APP_VERSION")
#define PublishDir "..\publish"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\ProxiFyreUI
DefaultGroupName=ProxiFyre UI
OutputDir=..\dist
OutputBaseFilename=ProxiFyreUI-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
SetupIconFile=app.ico
UninstallDisplayIcon={app}\ProxiFyre.UI.exe

[Files]
; Published WinUI app (self-contained) + bundled ProxiFyre.exe live in publish/
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\ProxiFyre UI"; Filename: "{app}\ProxiFyre.UI.exe"
Name: "{autodesktop}\ProxiFyre UI"; Filename: "{app}\ProxiFyre.UI.exe"

[Run]
; App manifest requests requireAdministrator (needed for Windows service control).
; shellexec => ShellExecuteEx, which honors the manifest and elevates via UAC.
; Plain CreateProcess (the default) fails with "code 740: requires elevation".
Filename: "{app}\ProxiFyre.UI.exe"; Description: "Launch ProxiFyre UI"; \
  Flags: nowait postinstall skipifsilent shellexec
