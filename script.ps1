$client = new-object System.Net.WebClient
$client.DownloadFile(“https://dot.net/v1/dotnet-install.ps1",“./dotnet-install.ps1”)
./dotnet-install.ps1

Install-module posh-git

git clone https://github.com/georgewallace/StoragePerfandScalabilityExample
cd StoragePerfandScalabilityExample

dotnet restore
dotnet build

{
$out = new-object byte[] 107374100; 
(new-object Random).NextBytes($out); 
[IO.File]::WriteAllBytes(".\test\$([guid]::NewGuid().ToString()).txt", $out)
}
