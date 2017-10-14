; Source file locations
#define BinaryPath "..\Build\Release\"
#define DriverPath "..\Drivers\"

; Destination folder inside {app} where to put drivers files#define DestDriversFolder "Drivers"

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
Name: "installdriver"; Description: "Install (or reinstall) ViGem drivers"; GroupDescription: "Drivers"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Application files
Source: "{#BinaryPath}\{#MyAppExeName}";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\{#MyAppExeName}.config";   DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\HidLibrary.dll";           DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\Nefarius.ViGEmClient.dll"; DestDir: "{app}"; Flags: ignoreversion

; 32bit Drivers     
Source: "{#DriverPath}\x86\*";          DestDir: "{app}\{#DestDriversFolder}"; Flags: ignoreversion 32bit
; 64bit Drivers
Source: "{#DriverPath}\x64\*";          DestDir: "{app}\{#DestDriversFolder}"; Flags: ignoreversion 64bit

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}";         Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Remove existing driver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\ViGEmBus";                    Tasks: installdriver; Flags: runascurrentuser; StatusMsg: "Removing ViGEm Driver..."
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\HidGuardian";                 Tasks: installdriver; Flags: runascurrentuser; StatusMsg: "Removing HidGuardian Driver..."
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "classfilter HIDClass upper !HidGuardian"; Tasks: installdriver; Flags: runascurrentuser; StatusMsg: "Removing HidGuardian Driver Filter..."

; Install driver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "install ""{app}\{#DestDriversFolder}\ViGEmBus.inf"" Root\ViGEmBus";       Tasks: installdriver; Flags: runascurrentuser; StatusMsg: "Installing ViGEm Driver..."
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "install ""{app}\{#DestDriversFolder}\HidGuardian.inf"" Root\HidGuardian"; Tasks: installdriver; Flags: runascurrentuser; StatusMsg: "Installing HidGuardian Driver..."
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "classfilter HIDClass upper -HidGuardian";                   Tasks: installdriver; Flags: runascurrentuser; StatusMsg: "Installing HidGuardian Driver Filter..."

; Post-Install: Run application
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
; Remove existing driver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\ViGEmBus";                    Flags: runascurrentuser;
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\HidGuardian";                 Flags: runascurrentuser;
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "classfilter HIDClass upper !HidGuardian"; Flags: runascurrentuser;
