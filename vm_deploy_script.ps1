Login-AzureRmAccount
$location = "eastus"
$resourceGroup = "<resourcegroupname>"
$storageaccount = "<storageaccountname>"
New-AzureRmResourceGroup -Name $resourceGroup -Location $location 

$storageAccount = New-AzureRmStorageAccount -ResourceGroupName $resourceGroup `
  -Name $storageaccount `
  -Location $location `
  -SkuName Standard_LRS `
  -Kind Storage `
  -EnableEncryptionService Blob

# Define a credential object
$cred = Get-Credential

# Create a subnet configuration
$subnetConfig = New-AzureRmVirtualNetworkSubnetConfig -Name mySubnet -AddressPrefix 192.168.1.0/24

# Create a virtual network
$vnet = New-AzureRmVirtualNetwork -ResourceGroupName $resourcegroup -Location $location `
    -Name MYvNET -AddressPrefix 192.168.0.0/16 -Subnet $subnetConfig

# Create a public IP address and specify a DNS name
$pip = New-AzureRmPublicIpAddress -ResourceGroupName $resourcegroup -Location $location `
    -AllocationMethod Static -IdleTimeoutInMinutes 4 -Name "mypublicdns$(Get-Random)"

    # Create a virtual network card and associate with public IP address and NSG
$nic = New-AzureRmNetworkInterface -Name myNic -ResourceGroupName $resourcegroup -Location $location `
    -SubnetId $vnet.Subnets[0].Id -PublicIpAddressId $pip.Id 

# Create a virtual machine configuration
$vmConfig = New-AzureRmVMConfig -VMName myVM -VMSize Standard_DS14_v2 | `
    Set-AzureRmVMOperatingSystem -Windows -ComputerName myVM -Credential $cred | `
    Set-AzureRmVMSourceImage -PublisherName MicrosoftWindowsServer -Offer WindowsServer `
    -Skus 2016-Datacenter -Version latest | Add-AzureRmVMNetworkInterface -Id $nic.Id



    New-AzureRmVM -ResourceGroupName $resourcegroup -Location $location -VM $vmConfig

        Set-AzureRMVMCustomScriptExtension -ResourceGroupName $resourcegroup `
    -VMName myVM `
    -Location $location `
    -FileUri https://raw.githubusercontent.com/georgewallace/StoragePerfandScalabilityExample/master/script.ps1 `
    -Run 'script.ps1' `
    -Name DemoScriptExtension

Write-host "Your public IP address is $($pip.Ipaddress)"