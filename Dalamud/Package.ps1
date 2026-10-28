$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    dotnet run --project Dalamud/KeyBrawler.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Grouping tests failed.' }
    dotnet build Dalamud/KeyBrawler/KeyBrawler.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
    $output = Join-Path $repo 'Dalamud/KeyBrawler/bin/Release'
    Copy-Item "$output/KeyBrawler/latest.zip" 'KeyBrawler-Dalamud.zip' -Force
    Compress-Archive -Path 'Dalamud/THIRD-PARTY-NOTICES.txt' -DestinationPath 'KeyBrawler-Dalamud.zip' -Update
    $manifest = Get-Content "$output/KeyBrawler.json" -Raw | ConvertFrom-Json
    $url = 'https://github.com/ekotutar35-cyber/keybrawler/raw/refs/heads/main/KeyBrawler-Dalamud.zip'
    foreach ($name in @('DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting')) {
        $manifest | Add-Member -NotePropertyName $name -NotePropertyValue $url -Force
    }
    $json = ConvertTo-Json -InputObject @($manifest) -Depth 10
    [System.IO.File]::WriteAllText((Join-Path $repo 'pluginmaster.json'), $json)
    Write-Host 'Created KeyBrawler-Dalamud.zip and pluginmaster.json.'
} finally {
    Pop-Location
}
