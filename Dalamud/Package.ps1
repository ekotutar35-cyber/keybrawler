$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    dotnet run --project Dalamud/KeyBrawler.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Grouping tests failed.' }
    dotnet build Dalamud/KeyBrawler/KeyBrawler.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
    $output = Join-Path $repo 'Dalamud/KeyBrawler/bin/Release'
    $worker = Join-Path $repo 'Dalamud/KeyBrawler.OcrWorker/bin/worker-publish'
    dotnet publish Dalamud/KeyBrawler.OcrWorker/KeyBrawler.OcrWorker.csproj -c Release -o $worker
    if ($LASTEXITCODE -ne 0) { throw 'OCR worker publish failed.' }
    $stage = Join-Path $repo 'Dalamud/KeyBrawler/obj/package-stage'
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Path "$stage/OcrWorker" -Force | Out-Null
    foreach ($file in @('KeyBrawler.dll', 'KeyBrawler.json', 'KeyBrawler.deps.json')) {
        Copy-Item (Join-Path $output $file) $stage
    }
    Copy-Item "$worker/KeyBrawler.OcrWorker.exe" "$stage/OcrWorker/"
    Copy-Item 'Dalamud/THIRD-PARTY-NOTICES.txt' $stage
    Compress-Archive -Path "$stage/*" -DestinationPath 'KeyBrawler-Dalamud.zip' -Force
    $manifest = Get-Content "$output/KeyBrawler.json" -Raw | ConvertFrom-Json
    $url = 'https://github.com/ekotutar35-cyber/keybrawler/raw/refs/heads/main/KeyBrawler-Dalamud.zip?v=' + $manifest.AssemblyVersion
    foreach ($name in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
        $manifest | Add-Member -NotePropertyName $name -NotePropertyValue $url -Force
    }
    $json = ConvertTo-Json -InputObject @($manifest) -Depth 10
    [System.IO.File]::WriteAllText((Join-Path $repo 'pluginmaster.json'), $json)
    Write-Host 'Created KeyBrawler-Dalamud.zip and pluginmaster.json.'
} finally {
    Pop-Location
}
