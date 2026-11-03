param([switch]$RealSql)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$flutterTool = Join-Path $projectRoot '.tools/flutter/bin/flutter.bat'
if (-not (Test-Path -LiteralPath $flutterTool)) { $flutterTool = (Get-Command flutter -ErrorAction Stop).Source }
# O compilador nativo de shaders também exige caminho ASCII para o SDK neste Windows.
if ($flutterTool -match '[^\x00-\x7F]') {
    $sdkRoot = Split-Path -Parent (Split-Path -Parent $flutterTool)
    $sdkAlias = Join-Path ([IO.Path]::GetTempPath()) ('SantosConectaFlutterSdk-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Junction -Path $sdkAlias -Target $sdkRoot | Out-Null
    $flutterTool = Join-Path $sdkAlias 'bin/flutter.bat'
}
$validationDir = Join-Path ([IO.Path]::GetTempPath()) ('SantosConectaFlutter-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $validationDir | Out-Null
foreach ($name in @('lib','test','assets','pubspec.yaml','pubspec.lock','analysis_options.yaml')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "mobile/$name") -Destination $validationDir -Recurse
}
Write-Output "Cópia de validação: $validationDir"
Push-Location -LiteralPath $validationDir
try {
    & $flutterTool pub get
    if ($LASTEXITCODE -ne 0) { throw 'flutter pub get falhou' }
    & $flutterTool analyze
    if ($LASTEXITCODE -ne 0) { throw 'flutter analyze falhou' }
    if ($RealSql) {
        $apiTestUrl = if ($env:API_TEST_URL) { $env:API_TEST_URL } else { 'http://127.0.0.1:5086' }
        & $flutterTool test --dart-define=ALLOW_HTTP=true --dart-define=RUN_REAL_SQL=true "--dart-define=API_URL=$apiTestUrl"
    } else { & $flutterTool test --dart-define=ALLOW_HTTP=true }
    if ($LASTEXITCODE -ne 0) { throw 'flutter test falhou' }
} finally { Pop-Location }
