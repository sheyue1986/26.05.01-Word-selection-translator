param(
    [string]$PublishDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "publish\DesktopAiTranslator")
)

$exePath = Join-Path $PublishDir "DesktopAiTranslator.exe"
if (-not (Test-Path $exePath)) {
    throw "Published executable not found: $exePath. Run dotnet publish first."
}

$desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)
$shortcutName = "AI " + [char]0x5212 + [char]0x8BCD + [char]0x7FFB + [char]0x8BD1 + [char]0x5668 + ".lnk"
$shortcutPath = Join-Path $desktop $shortcutName
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $PublishDir
$shortcut.IconLocation = "$exePath,0"
$shortcut.Description = "Desktop AI Translator"
$shortcut.Save()

Write-Host "Created desktop shortcut: $shortcutPath"
