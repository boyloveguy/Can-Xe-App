; Script Inno Setup tạo bộ cài đặt ứng dụng Cân Xe
#define MyAppName "Cân Xe App"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Can Xe"
#define MyAppExeName "CanXeApp.exe"

[Setup]
; Thông tin nhận diện ứng dụng
AppId={{9F7F448F-7BD2-4A73-B5B3-61E9A294AC3E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

; File đầu ra của bộ cài
OutputDir=installer_output
OutputBaseFilename=CanXeApp_Setup_v1.1
SetupIconFile=app_icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Cấu hình kiến trúc 64-bit
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Lấy toàn bộ file đã publish single-file từ thư mục publish
Source: "bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Shortcut trong Start Menu
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Shortcut ngoài Desktop
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
; Tùy chọn chạy ứng dụng sau khi cài đặt xong
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
