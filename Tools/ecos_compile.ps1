$ErrorActionPreference = "Stop"
$proj = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $proj "Assets"))) { $proj = "c:\Users\soila\mini game" }
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data"
$dotnet = Join-Path $unity "NetCoreRuntime\dotnet.exe"
$csc = Get-ChildItem (Join-Path $unity "DotNetSdk\sdk") -Recurse -Filter csc.dll | Where-Object { $_.FullName -like "*bincore*" } | Select-Object -First 1
$out = Join-Path $env:TEMP "ecos_build"
New-Item -ItemType Directory -Force $out | Out-Null

function Get-Refs($csproj) {
    $xml = [xml](Get-Content (Join-Path $proj $csproj) -Raw)
    $refs = @()
    foreach ($h in $xml.SelectNodes("//*[local-name()='HintPath']")) {
        $p = $h.InnerText
        if (-not [System.IO.Path]::IsPathRooted($p)) { $p = Join-Path $proj $p }
        if ((Test-Path $p) -and ($p -notlike "*Assembly-CSharp*")) { $refs += $p }
    }
    $define = ($xml.SelectNodes("//*[local-name()='DefineConstants']") | Select-Object -First 1).InnerText
    return @{ Refs = $refs; Define = $define }
}

function Compile($name, $csproj, $sources, $extraRefs) {
    $info = Get-Refs $csproj
    $rsp = Join-Path $env:TEMP "ecos_$name.rsp"
    $lines = @("-target:library", "-nologo", "-nostdlib+", "-langversion:9.0", "-nowarn:CS0649,CS0414,CS0169", "-out:`"$out\$name.dll`"", "-define:$($info.Define)")
    foreach ($r in ($info.Refs + $extraRefs)) { $lines += "-r:`"$r`"" }
    foreach ($s in $sources) { $lines += "`"$s`"" }
    Set-Content -Path $rsp -Value $lines -Encoding UTF8
    & $dotnet $csc.FullName "@$rsp" 2>&1 | Where-Object { $_ -match "error|warning CS" } | ForEach-Object { $_ }
    Write-Output "== $name exit: $LASTEXITCODE"
}

$runtime = Get-ChildItem (Join-Path $proj "Assets\Scripts") -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
Compile "Assembly-CSharp" "Assembly-CSharp.csproj" $runtime @()
$editor = Get-ChildItem (Join-Path $proj "Assets\Editor") -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
Compile "Assembly-CSharp-Editor" "Assembly-CSharp-Editor.csproj" $editor @("$out\Assembly-CSharp.dll")
