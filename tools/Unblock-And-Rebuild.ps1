Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

Write-Host "Unblocking downloaded/marked files (Zone.Identifier)..."

Get-ChildItem -Path $repoRoot -Recurse -File -ErrorAction SilentlyContinue |
	ForEach-Object {
		try { Unblock-File -Path $_.FullName -ErrorAction Stop } catch {}
	}

Write-Host "Removing bin/obj folders..."
Get-ChildItem -Path $repoRoot -Recurse -Directory -ErrorAction SilentlyContinue |
	Where-Object { $_.Name -in @("bin", "obj") } |
	ForEach-Object {
		try { Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction Stop } catch {}
	}

Write-Host "Restoring and rebuilding..."

# Get name of slnx file
$slnxFile = Get-ChildItem -Path $repoRoot -Filter "*.slnx" -File | Select-Object -First 1

dotnet restore $slnxFile.FullName
dotnet build $slnxFile.FullName -c Debug

Write-Host "Done."