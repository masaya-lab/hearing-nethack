; NetHack 3.6.7 (Educational Edition) - Inno Setup Script
; Builds a Release x64 installer without NetHackW.exe

#define MyAppName      "NetHack 3.6.7"
#define MyAppVersion   "3.6.7"
#define MyAppPublisher "NetHack DevTeam"
#define MyAppURL       "https://www.nethack.org/"
#define MyAppExe       "NetHack.exe"
#define SrcDir         "..\bin\Release\x64"

[Setup]
AppId={{B1E4F2D3-5A7C-4E8B-9F01-2D3E4F5A6B7C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\NetHack 3.6.7
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile={#SrcDir}\license
OutputDir=.\output
OutputBaseFilename=NetHack367_Setup_x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64os
ArchitecturesAllowed=x64os
UninstallDisplayIcon={app}\{#MyAppExe}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english";  MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";    Description: "{cm:CreateDesktopIcon}";    GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main executable (TTY version)
Source: "{#SrcDir}\NetHack.exe";          DestDir: "{app}"; Flags: ignoreversion

; Recovery tool
Source: "{#SrcDir}\recover.exe";          DestDir: "{app}"; Flags: ignoreversion

; Game data
Source: "{#SrcDir}\nhdat367";             DestDir: "{app}"; Flags: ignoreversion

; Browser-based screen viewer
Source: "{#SrcDir}\nethack_viewer.html";  DestDir: "{app}"; Flags: ignoreversion

; Documentation
Source: "{#SrcDir}\Guidebook.txt";        DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\nethack.txt";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\recover.txt";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\license";              DestDir: "{app}"; Flags: ignoreversion

; Configuration templates
Source: "{#SrcDir}\opthelp";              DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\symbols.template";     DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\sysconf.template";     DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\.nethackrc.template";  DestDir: "{app}"; Flags: ignoreversion

[Dirs]
; Ensure save directory exists and is writable
Name: "{app}\save"

[Icons]
Name: "{group}\{#MyAppName}";         Filename: "{app}\{#MyAppExe}"
Name: "{group}\NetHack Viewer (HTML)"; Filename: "{app}\nethack_viewer.html"
Name: "{group}\Guidebook";             Filename: "{app}\Guidebook.txt"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";   Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
