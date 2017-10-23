Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./dotnet-install.ps1"
./dotnet-install.ps1

Write-host "Installing Posh-Git"
Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -force


# Install chocolately 
Invoke-WebRequest 'https://chocolatey.org/install.ps1' -OutFile "./choco-install.ps1"
./choco-install.ps1
SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"

choco install git -y
SET "PATH=%PATH%;c:\program files\git\bin"
New-Item -ItemType Directory -Path c:\git -Force
Set-Location c:\git
Write-host "cloning repo"
git clone https://github.com/georgewallace/StoragePerfandScalabilityExample

write-host "Changing directory to $((Get-Item -Path ".\" -Verbose).FullName)"
Set-Location c:\git\StoragePerfandScalabilityExample

Write-host "restoring nuget packages"
dotnet restore
dotnet build

New-Item -ItemType Directory d:\perffiles
Set-Location d:\perffiles
Write-host "cretting files"
for($i=0; $i -lt 36; $i++)
{
$out = new-object byte[] 107374100; 
(new-object Random).NextBytes($out); 
[IO.File]::WriteAllBytes(".\$([guid]::NewGuid().ToString()).txt", $out)
}

Write-Host $error
