$client = new-object System.Net.WebClient
$client.DownloadFile("https://dot.net/v1/dotnet-install.ps1","./dotnet-install.ps1")
./dotnet-install.ps1

Write-host "Installing Posh-Git"
Install-module posh-git -force

Write-host "cloning repo"
git clone https://github.com/georgewallace/StoragePerfandScalabilityExample

write-host "Changing directory to $((Get-Item -Path ".\" -Verbose).FullName)"
cd StoragePerfandScalabilityExample

Write-host "restoring nuget packages"
dotnet restore
dotnet build

Write-host "cretting files"
for($i=0; $i -lt 36; $i++)
{
$out = new-object byte[] 107374100; 
(new-object Random).NextBytes($out); 
[IO.File]::WriteAllBytes(".\test\$([guid]::NewGuid().ToString()).txt", $out)
}

Write-Host $error
