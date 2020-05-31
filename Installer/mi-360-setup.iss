; Source file locations
#define BinaryPath "..\Build\Release\"

; Application metadata
#define MyAppExeName "mi-360.exe"
#define MyAppName "mi-360"
#define MyAppPublisher "Daniele Colanardi"
#define MyAppURL "http://github.com/dancol90/mi-360"

; Application (and setup) version
#dim ver[4]
#expr ParseVersion(BinaryPath + MyAppExeName, ver[0], ver[1], ver[2], ver[3])
; Use the short format x.x.x
#define MyAppVersion Str(ver[0]) + "." + Str(ver[1]) + "." + Str(ver[2])

[Setup]
AppId={{0E5364C7-90A5-4750-8996-078A9DC88962}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
LicenseFile=..\LICENSE
OutputBaseFilename={#MyAppName}-v{#MyAppVersion}-setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
OutputDir=.
AllowCancelDuringInstall=False
ShowLanguageDialog=auto
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Application files
Source: "{#BinaryPath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\HidLibrary.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\Nefarius.ViGEm.Client.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\Serilog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\Serilog.Sinks.Console.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\Serilog.Sinks.File.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}";         Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]

; Post-Install: Run application
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent shellexec; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"
