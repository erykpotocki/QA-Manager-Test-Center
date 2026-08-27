param(
    [string]$Version = "0.2.4",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path $projectRoot "artifacts\publish\$Runtime"
$installerDirectory = Join-Path $projectRoot "artifacts\installer"
$installerScript = Join-Path $projectRoot "installer\QARegressionManager.iss"

$innoCompilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
)

$innoCompiler = $innoCompilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if (-not $innoCompiler) {
    throw "Nie znaleziono Inno Setup 6. Zainstaluj Inno Setup przed generowaniem installpacka."
}

New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $installerDirectory | Out-Null

dotnet publish (Join-Path $projectRoot "QARegressionManager.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --no-restore `
    --self-contained true `
    -p:PublishSingleFile=false `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "Publikowanie aplikacji nie powiodło się."
}

$forbiddenReleaseFiles = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File |
    Where-Object {
        $_.Name -match '^(assignments|assignment-input-presets.*|profiles|application-settings.*|language-settings|network-sync.*|session.*|project-data|TestCases.*)\.json$' -or
        $_.Extension -match '^\.(pfx|p12|pem|key|db|sqlite)$'
    }

if ($forbiddenReleaseFiles) {
    $names = $forbiddenReleaseFiles.FullName -join [Environment]::NewLine
    throw "Publikacja zawiera lokalne lub poufne pliki i nie może zostać spakowana:$([Environment]::NewLine)$names"
}

# Symbole debugowania nie są potrzebne w paczce użytkownika ani instalatorze.
Get-ChildItem -LiteralPath $publishDirectory -Filter "*.pdb" -File |
    Remove-Item -Force

& $innoCompiler `
    "/DMyAppVersion=$Version" `
    "/DPublishDir=$publishDirectory" `
    "/DOutputDir=$installerDirectory" `
    $installerScript

if ($LASTEXITCODE -ne 0) {
    throw "Tworzenie installpacka nie powiodło się."
}

Write-Host "Installpack utworzono w $installerDirectory"
