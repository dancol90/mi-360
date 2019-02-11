; Source file locations
#define BinaryPath "..\Build\Release\"
#define DriverPath "..\Drivers\"

; Destination folder inside {app} where to put drivers files
#define DestDriversFolder "Drivers"

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
Source: "{#BinaryPath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinaryPath}\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion
;Source: "{#BinaryPath}\HidLibrary.dll"; DestDir: "{app}"; Flags: ignoreversion
;Source: "{#BinaryPath}\Nefarius.ViGEmClient.dll"; DestDir: "{app}"; Flags: ignoreversion

; Drivers
Source: "{#DriverPath}\x86\*"; DestDir: "{app}\{#DestDriversFolder}"; Flags: ignoreversion; Check: not IsWin64
Source: "{#DriverPath}\x64\*"; DestDir: "{app}\{#DestDriversFolder}"; Flags: ignoreversion; Check: IsWin64

; VCRedist 2015 v14.0.24215.1
Source: "{#DriverPath}\vc_redist.x86.exe"; DestDir: "{tmp}"; DestName: "vc_redist.exe"; Flags: deleteafterinstall; Check: not IsWin64
Source: "{#DriverPath}\vc_redist.x64.exe"; DestDir: "{tmp}"; DestName: "vc_redist.exe"; Flags: deleteafterinstall; Check: IsWin64

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}";         Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Install VCRedist
Filename: "{tmp}\vc_redist.exe"; Parameters: "/install /passive"; StatusMsg: "Installing VC++ Redistributables..."; Check: not VCinstalled

; Remove existing driver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\ViGEmBus"; Flags: runhidden runascurrentuser; StatusMsg: "Removing ViGEm Driver..."; Tasks: installdriver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "classfilter HIDClass upper !HidGuardian"; Flags: runhidden runascurrentuser; StatusMsg: "Removing HidGuardian Driver Filter..."; Tasks: installdriver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\HidGuardian"; Flags: runhidden runascurrentuser; StatusMsg: "Removing HidGuardian Driver..."; Tasks: installdriver

; Install driver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "install ""{app}\{#DestDriversFolder}\ViGEmBus.inf"" Root\ViGEmBus"; Flags: runhidden runascurrentuser; StatusMsg: "Installing ViGEm Driver..."; Tasks: installdriver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "install ""{app}\{#DestDriversFolder}\HidGuardian.inf"" Root\HidGuardian"; Flags: runhidden runascurrentuser; StatusMsg: "Installing HidGuardian Driver..."; Tasks: installdriver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "classfilter HIDClass upper -HidGuardian"; Flags: runhidden runascurrentuser; StatusMsg: "Installing HidGuardian Driver Filter..."; Tasks: installdriver

; Post-Install: Run application
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent shellexec; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"

[UninstallRun]
; Remove existing driver
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\ViGEmBus";                    Flags: runhidden runascurrentuser;
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "remove Root\HidGuardian";                 Flags: runhidden runascurrentuser;
Filename: "{app}\{#DestDriversFolder}\devcon.exe"; Parameters: "classfilter HIDClass upper !HidGuardian"; Flags: runhidden runascurrentuser;

[Code]
// Returns True if same or later Microsoft Visual C++ 2015 Redistributable is installed, otherwise False.
function VCinstalled: Boolean;
 var
  major, minor, bld, rbld: Cardinal;
  key: String;
 begin
  Result := False;
  key := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\';
  if IsWin64 then key := key + 'x64' else key := key + 'x86';
  if RegQueryDWordValue(HKEY_LOCAL_MACHINE, key, 'Major', major) then begin
    if RegQueryDWordValue(HKEY_LOCAL_MACHINE, key, 'Minor', minor) then begin
      if RegQueryDWordValue(HKEY_LOCAL_MACHINE, key, 'Bld', bld) then begin
        if RegQueryDWordValue(HKEY_LOCAL_MACHINE, key, 'RBld', rbld) then begin
            Log('VC 2015 Redist Major is: ' + IntToStr(major) + ' Minor is: ' + IntToStr(minor) + ' Bld is: ' + IntToStr(bld) + ' Rbld is: ' + IntToStr(rbld));
            Result := (major >= 14) and (minor >= 0) and (bld >= 24215) and (rbld >= 0)
        end;
      end;
    end;
  end;
 end;