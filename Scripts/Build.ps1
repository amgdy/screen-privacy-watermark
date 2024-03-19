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
        "--output=$appOutputPath",
        "--configuration=Release",
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

    Write-Host "Running dotnet publish with parameters: $appPublishParams" -ForegroundColor Yellow
    dotnet publish $appPublishParams

    $wixFile = "$srcPath\Magdys.ScreenPrivacyWatermark.Setup\Magdys.ScreenPrivacyWatermark.Setup.wixproj"

    $msiParams = @(
        "$wixFile",
        "/p:Configuration=Release",
        "/p:AppFolder=$tempOutputPath",
        "/p:OutputName=SPW-$Version-Setup",
        "/p:OutputPath=$tempOutputPath"
    )

    Write-Host "Running MSBuild with parameters: $msiParams" -ForegroundColor Yellow
    & $msbuildPath $msiParams
}
finally {
    Write-Host "Cleaning up temp output folder: $tempOutputPath" -ForegroundColor Yellow
    # Uncomment the following line to delete the temp output folder after the build completes
    #Remove-Item -Path $tempOutputPath -Recurse -Force

    Write-Host "Build script completed." -ForegroundColor Yellow
}