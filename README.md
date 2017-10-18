# Azure Storage Throughput Test

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fseguler%2Fazuredeploytest%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

1. Click Deploy button to deploy the sample to a Linux VM on Azure
2. Once deployment is completed, connect to the VM via SSH using the administrator account you defined. You will have 200 files each 1GB in /mnt directory as sample data.
3. Navigate to the sample application in /home/<your username>/azuredeploytest
`cd /home/<administrator>/azuredeploytest`
4. Run the application to upload the files to Azure Storage from '/mnt' folder
`dotnet run /mnt`
