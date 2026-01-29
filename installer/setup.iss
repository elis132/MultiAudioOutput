; Multi Audio Output - Inno Setup Installer Script
; Inno Setup 6.x or later required

#define MyAppName "Multi Audio Output"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "elis132"
#define MyAppURL "https://github.com/elis132/MultiAudioOutput"
#define MyAppExeName "MultiAudioOutput.exe"

[Setup]
; Unique App ID (DO NOT CHANGE - used for upgrades)
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}

; App information
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
AppCopyright=Copyright © 2026 {#MyAppPublisher}

; Install directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; License and info
LicenseFile=EULA.txt
InfoBeforeFile=..\README.md

; Output
OutputDir=Output
OutputBaseFilename=MultiAudioOutput-Setup-{#MyAppVersion}

; Icons
SetupIconFile=..\Resources\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; UI
WizardStyle=modern
WizardImageFile=..\Resources\installer-sidebar.bmp
WizardSmallImageFile=..\Resources\installer-logo.bmp
DisableWelcomePage=no

; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Uninstall
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}\uninstall

; Version info
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright=Copyright © 2026 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "sv"; MessagesFile: "compiler:Languages\Swedish.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "pt"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "nl"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "pl"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "no"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "da"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "fi"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
; Main executable
Source: "..\artifacts\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Resources
Source: "..\Resources\icon.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion
Source: "..\Resources\background.png"; DestDir: "{app}\Resources"; Flags: ignoreversion skipifsourcedoesntexist

; Documentation
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Stream audio to multiple devices"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Desktop
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Quick Launch
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
; Start with Windows (if selected)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MultiAudioOutput"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
; Offer to launch after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Close app before uninstall
Filename: "taskkill"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillApp"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Check if .NET 8.0 Runtime is installed
  if not RegKeyExists(HKLM64, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') then
  begin
    if MsgBox('.NET 8.0 Runtime is required but not installed.' + #13#10 + #13#10 +
              'Would you like to download and install it now?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0/runtime', '', '', SW_SHOW, ewNoWait, ResultCode);
      Result := False;
    end
    else
      Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create settings directory
    if not DirExists(ExpandConstant('{userappdata}\MultiAudioOutput')) then
      CreateDir(ExpandConstant('{userappdata}\MultiAudioOutput'));
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SettingsDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Ask to remove settings
    SettingsDir := ExpandConstant('{userappdata}\MultiAudioOutput');
    if DirExists(SettingsDir) then
    begin
      if MsgBox('Do you want to remove your settings and preferences?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(SettingsDir, True, True, True);
      end;
    end;
  end;
end;
