; Tumbang Preso, Windows installer (Inno Setup 6.3 or later).
;
; Built from a GameBuilder output folder by tools/build_installer.py, which passes the defines:
;
;     python tools/build_installer.py --source Builds/<candidate>
;
; WHY THERE IS AN INSTALLER AT ALL (LAN-VPN-JOIN-1020, docs/NETWORKING.md "LAN over a VPN")
;
; A LAN room over Hamachi sat on "Joining..." because the HOST's Windows Firewall dropped every
; inbound packet to the game. Windows files Hamachi, Radmin and ZeroTier adapters under PUBLIC
; networks, and its first-run prompt ticks Private only, so a zip copy that was "allowed" is
; blocked exactly where a VPN room lives. Adding a firewall rule needs an administrator, and the
; game must never run elevated. An installer is the one moment a player expects to say yes to
; admin, so the rule is added here, once, and removed again on uninstall.
;
; THE ONE RULE, AND NOTHING ELSE
;
; Inbound, allow, UDP, Private and Public profiles, scoped to the installed TumbangPreso.exe by
; path. No port rule, no rule for any other program, no TCP: the game listens on UDP only (the
; game port and the LAN beacon, which answers discovery queries). The command text below is the
; text Core's LanFirewallRules.NetshAddArguments / NetshDeleteArguments build for "{app}\
; TumbangPreso.exe", and Core.Tests LanFirewallRulesTests reads this file to keep it that way.
; The in-game "Allow LAN play" control for zip copies runs the same two commands, so both routes
; write one rule and the uninstaller removes either.
;
; ⚠️ THE DELETE RUNS BEFORE THE ADD, so reinstalling over an existing copy never doubles the rule.
; netsh answers 1 when nothing matched; [Run] does not stop on an exit code, which is wanted here.
; ⚠️ THE GAME IS LAUNCHED AS THE ORIGINAL USER (runasoriginaluser). Without that flag the
; "Launch Tumbang Preso" box at the end would start the game with the installer's admin token.

#ifndef SourceDir
  #error Pass /DSourceDir=<GameBuilder output folder holding TumbangPreso.exe>. tools/build_installer.py does.
#endif
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef OutputName
  #define OutputName "TumbangPreso-Setup-" + AppVersion
#endif

[Setup]
; The AppId never changes between versions: it is how a newer installer finds and upgrades an older one.
AppId={{D9A909F1-5BF5-4B80-BFCC-30EC1D94CBE8}
AppName=Tumbang Preso
AppVersion={#AppVersion}
AppVerName=Tumbang Preso {#AppVersion}
AppPublisher=BH Studios
DefaultDirName={autopf}\Tumbang Preso
DefaultGroupName=Tumbang Preso
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename={#OutputName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\TumbangPreso.exe
UninstallDisplayName=Tumbang Preso
CloseApplications=yes
#ifdef SetupIcon
SetupIconFile={#SetupIcon}
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Unity's Burst symbols folder says in its own name not to ship it.
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*_BurstDebugInformation_DoNotShip,*_BurstDebugInformation_DoNotShip\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Tumbang Preso"; Filename: "{app}\TumbangPreso.exe"
Name: "{autodesktop}\Tumbang Preso"; Filename: "{app}\TumbangPreso.exe"; Tasks: desktopicon

[Run]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Tumbang Preso (LAN)"" dir=in program=""{app}\TumbangPreso.exe"""; Flags: runhidden; StatusMsg: "Allowing LAN play in Windows Firewall..."
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Tumbang Preso (LAN)"" dir=in action=allow program=""{app}\TumbangPreso.exe"" protocol=UDP profile=private,public enable=yes description=""Lets players on your network or VPN join rooms you host in Tumbang Preso."""; Flags: runhidden; StatusMsg: "Allowing LAN play in Windows Firewall..."
Filename: "{app}\TumbangPreso.exe"; Description: "{cm:LaunchProgram,Tumbang Preso}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
; Exactly the rule the install added: same name, same program path. Other copies keep theirs.
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Tumbang Preso (LAN)"" dir=in program=""{app}\TumbangPreso.exe"""; Flags: runhidden; RunOnceId: "RemoveLanFirewallRule"
