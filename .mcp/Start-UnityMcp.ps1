$ErrorActionPreference = 'Continue'
$project = 'C:\Projects\vltk-mobile'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe'
$uvx = 'C:\Users\zet\AppData\Local\Microsoft\WinGet\Packages\astral-sh.uv_Microsoft.Winget.Source_8wekyb3d8bbwe\uvx.exe'
$log = Join-Path $project '.mcp\server.log'
$errorLog = Join-Path $project '.mcp\server-error.log'

function Start-UnityEditor {
    $running = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" |
        Where-Object { $_.CommandLine -like "* -projectPath $project*" }
    if (-not $running) {
        Start-Process -FilePath $unity -ArgumentList '-projectPath', $project
    }
}

Start-UnityEditor

while ($true) {
    $server = Get-NetTCPConnection -LocalAddress '127.0.0.1' -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
    if (-not $server) {
        $p = Start-Process -FilePath $uvx `
            -ArgumentList '--from', 'mcpforunityserver', 'mcp-for-unity', '--transport', 'http', '--http-url', 'http://127.0.0.1:8080' `
            -RedirectStandardOutput $log -RedirectStandardError $errorLog -PassThru -WindowStyle Hidden
        while (-not $p.HasExited) {
            Start-Sleep -Seconds 10
        }
        Start-Sleep -Seconds 5
    } else {
        Start-Sleep -Seconds 30
    }
}
