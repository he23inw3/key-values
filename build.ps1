# KeyValues クリーンビルド & パブリッシュ スクリプト
Write-Host "--- KeyValues クリーンビルド & パブリッシュ スクリプト ---" -ForegroundColor Cyan

# 1. 実行中のプロセスを終了
Write-Host "1. 実行中のプロセスを終了しています..."
$proc = Get-Process -Name KeyValues -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "  実行中の KeyValues.exe を終了します..." -ForegroundColor Yellow
    Stop-Process -Name KeyValues -Force
    Start-Sleep -Seconds 1
}

# 2. dotnet ビルドサーバーの終了
Write-Host "2. MSBuild サーバーをシャットダウンしています..."
dotnet build-server shutdown

# 3. 物理クリーン（bin/objフォルダの完全削除）
Write-Host "3. 物理クリーン (bin, obj フォルダの完全削除) を実行しています..." -ForegroundColor Yellow
$directories = @("src/KeyValues/bin", "src/KeyValues/obj", "tests/KeyValues.Tests/bin", "tests/KeyValues.Tests/obj", "bin", "obj")
foreach ($dir in $directories) {
    if (Test-Path $dir) {
        Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# 4. パブリッシュの実行
Write-Host "4. 自己完結型単一ファイルEXEをクリーンパブリッシュしています..." -ForegroundColor Green
dotnet publish src/KeyValues/KeyValues.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[成功] ビルド＆パブリッシュが完了しました！" -ForegroundColor Green
    $publishPath = Resolve-Path "src/KeyValues/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish"
    Write-Host "出力先: $publishPath" -ForegroundColor Cyan
    Write-Host "このディレクトリにある KeyValues.exe を実行してください。"
}
else {
    Write-Host ""
    Write-Error "[Error] Publish failed."
}
