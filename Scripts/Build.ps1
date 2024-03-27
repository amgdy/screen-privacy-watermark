param (
    [string]$Version = "1.0.0",
    [bool]$SelfContained = $true,
    [string]$RootTempPath = "$($env:SystemDrive)\temp"
)
try {
    Write-Host "Starting build script..." -ForegroundColor Yellow

    $srcPath = (Resolve-Path "..\src")
    Write-Host "Source path resolved to: $srcPath" -ForegroundColor Yellow

    $msbuildEnterprisePath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    $msbuildProfessionalPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    $msbuildCommunityPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

    $msbuildPath = $null

    # Test the paths in sequence and set the first found one as the msbuildPath
    if (Test-Path $msbuildEnterprisePath) {
        $msbuildPath = $msbuildEnterprisePath
    }
    elseif (Test-Path $msbuildProfessionalPath) {
        $msbuildPath = $msbuildProfessionalPath
    }
    elseif (Test-Path $msbuildCommunityPath) {
        $msbuildPath = $msbuildCommunityPath
    }
    else {
        Write-Host "MSBuild path not found. Please install Visual Studio 2022." -ForegroundColor Red
        return
    }

    Write-Host "MSBuild path set to: $msbuildPath" -ForegroundColor Yellow

    $tempOutputPath = Join-Path $RootTempPath "SPW-$Version-$([System.Guid]::NewGuid().ToString())"

    New-Item -ItemType Directory -Path $tempOutputPath -Force

    Write-Host "Temp output path created: $tempOutputPath" -ForegroundColor Yellow

    $appOutputPath = Join-Path $tempOutputPath "App"
    New-Item -ItemType Directory -Path $appOutputPath -Force

    $projectFile = "$srcPath\Magdys.ScreenPrivacyWatermark.App\Magdys.ScreenPrivacyWatermark.App.csproj"

    Write-Host "Project file set to: $projectFile" -ForegroundColor Yellow

    $appPublishParams = @(
        "$projectFile",
        "-o=$appOutputPath",
        "-c=Release",
        "-p:FileVersion=$Version",
        "-p:AssemblyVersion=$Version",
        "-p:Version=$Version"
    )

    if ($SelfContained) {
        $appPublishParams += @(
            "--runtime=win-x64",
            "--self-contained=true"
        )
    }

    Write-Host "Running dotnet publish $appPublishParams" -ForegroundColor Yellow
    dotnet publish $appPublishParams

    $wixFile = "$srcPath\Magdys.ScreenPrivacyWatermark.Setup\Magdys.ScreenPrivacyWatermark.Setup.wixproj"

    $msiParams = @(
        $wixFile,
        "-c=Release",
        "-o=$tempOutputPath",
        "-p:Version=$Version",
        "-p:AppDir=$appOutputPath",
        "-p:OutputName=Setup"
    )

    Write-Host "Running: dotnet build $msiParams" -ForegroundColor Yellow

    dotnet build $msiParams
}
finally {
    Write-Host "Cleaning up temp output folder: $tempOutputPath" -ForegroundColor Yellow
    # Uncomment the following line to delete the temp output folder after the build completes
    #Remove-Item -Path $tempOutputPath -Recurse -Force

    Write-Host "Build script completed." -ForegroundColor Yellow
}