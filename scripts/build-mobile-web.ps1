$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$flutterTool = Join-Path $projectRoot '.tools/flutter/bin/flutter.bat'
if (-not (Test-Path -LiteralPath $flutterTool)) { $flutterTool = (Get-Command flutter -ErrorAction Stop).Source }
if ($flutterTool -match '[^\x00-\x7F]') {
    $sdkRoot = Split-Path -Parent (Split-Path -Parent $flutterTool)
    $sdkAlias = Join-Path ([IO.Path]::GetTempPath()) ('SantosConectaFlutterSdk-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Junction -Path $sdkAlias -Target $sdkRoot | Out-Null
    $flutterTool = Join-Path $sdkAlias 'bin/flutter.bat'
}
$previewDir = Join-Path ([IO.Path]::GetTempPath()) ('SantosConectaWeb-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $previewDir | Out-Null
foreach ($name in @('lib','test','assets','web','pubspec.yaml','pubspec.lock','analysis_options.yaml')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "mobile/$name") -Destination $previewDir -Recurse
}
$outputDir = Join-Path $projectRoot 'mobile/build/web'
Push-Location -LiteralPath $previewDir
try {
    & $flutterTool pub get
    if ($LASTEXITCODE -ne 0) { throw 'flutter pub get falhou' }
    & $flutterTool analyze
    if ($LASTEXITCODE -ne 0) { throw 'flutter analyze falhou' }
    & $flutterTool test --dart-define=ALLOW_HTTP=true
    if ($LASTEXITCODE -ne 0) { throw 'flutter test falhou' }
    & $flutterTool build web --release --dart-define=API_URL= --dart-define=ALLOW_HTTP=true --output $outputDir
    if ($LASTEXITCODE -ne 0) { throw 'flutter build web falhou' }
} finally { Pop-Location }
