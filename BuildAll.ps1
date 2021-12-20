#! /usr/bin/env pwsh
#Requires -Version 5.0

param(
  [Parameter()]
  [Alias("p")]
  [switch]$ShowMSBuildOutput = $false,
  [Parameter()]
  [Alias("c")]
  [string]$Configuration = "Release"
)

$platforms = @(
  "Windows",
  "WindowsGL",
  "FNAFramework",
  "XNAFramework"
)

function Get-MSBuild {
  switch ([System.Environment]::OSVersion.Platform) {
    Win32NT {
      $msbuild = & "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires "Microsoft.Component.MSBuild" -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -first 1
    }
    Unix {
      $msbuild = Get-Command msbuild
    }
    Default {
      throw "Unsupported OS"
    }
  }

  return Get-Command $msbuild
}

function Build-Release {
  param (
    $msbuild,
    $platform
  )
  if ($ShowMSBuildOutput) {
    & $msbuild .\Rampastring.XNAUI.csproj /r /m /v:m /t:Rebuild /p:Configuration=$Configuration /p:Platform=$platform
  }
  else {
    & $msbuild .\Rampastring.XNAUI.csproj /r /m /v:m /t:Rebuild /p:Configuration=$Configuration /p:Platform=$platform | Out-Null
  }
}


function Build-All {
  $buildStatus = New-Object System.Collections.Generic.List"[bool]"

  $msbuild = Get-MSBuild

  Write-Host ""
  Write-Host "Building" -ForegroundColor Blue
  Write-Host ""

  for ($i = 0; $i -lt $platforms.Count; $i++) {
    Write-Host ""
    Write-Host "[$($i + 1) / $($platforms.Count)] Building $($platforms[$i])" -ForegroundColor Blue
    Write-Host ""
    Build-Release $msbuild $platforms[$i]
    $buildStatus.Add($LASTEXITCODE -eq 0)
  }

  $isFailure = $false
  Write-Host ""
  Write-Host "  Platform   Build Status" -ForegroundColor Green
  Write-Host "------------ ------------" -ForegroundColor Green
  for ($i = 0; $i -lt $platforms.Count; $i++) {
    Write-Host "$($platforms[$i].PadLeft(12)) " -NoNewline
    if ($buildStatus[$i]) {
      Write-Host "Success" -ForegroundColor Green
    }
    else {
      Write-Host "Failure" -ForegroundColor Red
      $isFailure = $true
    }
  }
  if ($isFailure) {
    Write-Host ""
    Write-Host "Build failed, Use .\BuildAll.ps1 -p to show MSBuild Output." -ForegroundColor Red
    Write-Host ""
    exit 1
  }

}

Build-All