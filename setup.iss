[Setup]
AppName=Description Key Code Updater
AppVersion=1.0
DefaultDirName={pf}\DescriptionKeyCodeUpdater
DefaultGroupName=Description Key Code Updater
UninstallDisplayIcon={app}\PointDescriptionUpdater.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Installer
OutputBaseFilename=DescriptionKeyCodeUpdater_Setup

[Files]
Source: "bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Description Key Code Updater"; Filename: "{app}\PointDescriptionUpdater.exe"
Name: "{commondesktop}\Description Key Code Updater"; Filename: "{app}\PointDescriptionUpdater.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\PointDescriptionUpdater.exe"; Description: "Launch Description Key Code Updater"; Flags: nowait postinstall skipifsilent
