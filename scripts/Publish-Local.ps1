$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "DesktopAiTranslator\DesktopAiTranslator.csproj"
$publishDir = Join-Path $root "publish\DesktopAiTranslator"

dotnet publish $project -c Release -o $publishDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& (Join-Path $PSScriptRoot "Create-DesktopShortcut.ps1") -PublishDir $publishDir
