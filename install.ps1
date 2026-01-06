$TargetDir = "$env:LocalAppData\JblKeepAlive"
$ShortcutPath = "$env:AppData\Microsoft\Windows\Start Menu\Programs\Startup\JblKeepAlive.lnk"
$ExePath = "$TargetDir\JblKeepAlive.exe"

# 1. Publica e copia os arquivos
dotnet publish -c Release -r win-x64 --self-contained true -o $TargetDir

# 2. Cria o atalho no Startup do Windows
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $ExePath
$Shortcut.Save()

# 3. Inicia agora
Start-Process $ExePath