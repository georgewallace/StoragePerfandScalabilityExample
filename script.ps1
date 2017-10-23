Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile "./dotnet-install.ps1" 
./dotnet-install.ps1
$dotnetpath = "C:\Windows\System32\config\systemprofile\AppData\Local\Microsoft\dotnet\"

Write-host "Installing Posh-Git"
Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -force


# Install chocolately 
Invoke-WebRequest 'https://chocolatey.org/install.ps1' -OutFile "./choco-install.ps1"
./choco-install.ps1

$chocopath = "C:\Documents and Settings\All Users\chocolatey\bin\"

Invoke-Expression -Command "$($chocopath)choco.exe" install git -y

$gitpath = "c:\program files\git\bin\"

New-Item -ItemType Directory -Path c:\git -Force
Set-Location c:\git
Write-host "cloning repo"
Invoke-Expression -Command "$($gitpath)git.exe" clone https://github.com/georgewallace/StoragePerfandScalabilityExample

write-host "Changing directory to $((Get-Item -Path ".\" -Verbose).FullName)"
Set-Location c:\git\StoragePerfandScalabilityExample

Write-host "restoring nuget packages"
Invoke-Expression -Command "$($dotnetpath)dotnet.exe" restore
Invoke-Expression -Command "$($dotnetpath)dotnet.exe" build

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
