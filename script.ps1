## Install .NET Core 2.0
Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./dotnet-install.ps1" 
./dotnet-install.ps1 -Channel 2.0 -InstallDir c:\dotnet

Write-host "Installing Posh-Git"
Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -force


# Install chocolately 
Invoke-WebRequest 'https://chocolatey.org/install.ps1' -OutFile "./choco-install.ps1"
./choco-install.ps1

# Install Git with choco
choco install git -y

# clone the sample repo
New-Item -ItemType Directory -Path D:\git -Force
Set-Location D:\git
Write-host "cloning repo"
& 'C:\Program Files\git\cmd\git.exe' clone https://github.com/georgewallace/StoragePerfandScalabilityExample

write-host "Changing directory to $((Get-Item -Path ".\" -Verbose).FullName)"
Set-Location D:\git\StoragePerfandScalabilityExample

# Restore NuGet packages and build applocation
Write-host "restoring nuget packages"
c:\dotnet\dotnet.exe restore
c:\dotnet\dotnet.exe build

# Set the path for dotnet.
$OldPath=(Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name PATH).Path

$dotnetpath = "c:\dotnet"
IF(Test-Path -Path $dotnetpath)
{
$NewPath=$OldPath+';'+$dotnetpath
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name PATH -Value $NewPath
}

# Create 32 1GB files to be used for the sample
New-Item -ItemType Directory D:\git\StoragePerfandScalabilityExample\upload
Set-Location D:\git\StoragePerfandScalabilityExample\upload
Write-host "Creating files"
for($i=0; $i -lt 32; $i++)
{
$out = new-object byte[] 1073741824; 
(new-object Random).NextBytes($out); 
[IO.File]::WriteAllBytes("D:\git\StoragePerfandScalabilityExample\upload\$([guid]::NewGuid().ToString()).txt", $out)
}



