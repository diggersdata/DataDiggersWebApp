using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataDiggersWebApp.AzureUpload
{
    public static class AzureOperations
    {
        #region ConfigParams  
        public static string tenantId="e5727ff0-84f8-42ba-9c30-de24a8e8ceec";
        public static string applicationId = "60d2a6f2-c2f5-4859-9a80-f1ddeb71f158";
        public static string clientSecret= "Zxa8Q~F~xbnB0BJK97F7cZe9wm2GSULTFBHnmayJ";
        #endregion
        public static void UploadFile(AzureOperationHelper azureOperationHelper)
        {
            //CloudBlobContainer blobContainer = CreateCloudBlobContainer(tenantId, applicationId, clientSecret, azureOperationHelper.storageAccountName, azureOperationHelper.containerName, azureOperationHelper.storageEndPoint);
            //blobContainer.CreateIfNotExistsAsync();
            //CloudBlockBlob blob = blobContainer.GetBlockBlobReference(azureOperationHelper.blobName);
            //blob.UploadFromFileAsync(azureOperationHelper.srcPath);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=bsdatadiggers;AccountKey=dvvxdeETFsnRDHxI9yIA0yb2V3wNL2FTWAazT6a3loBpnb1Maw8TI99HdgbPxe82JVH0KdaYdeAq+AStRPfSyA==;EndpointSuffix=core.windows.net");
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(azureOperationHelper.containerName);
            CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference(azureOperationHelper.filename);
            using (var fileStream = System.IO.File.OpenRead(azureOperationHelper.srcPath))
            {
                blob.UploadFromStreamAsync(fileStream).Wait();
            }
        }
        public static void DownloadFile(AzureOperationHelper azureOperationHelper)
        {
            CloudBlobContainer blobContainer = CreateCloudBlobContainer(tenantId, applicationId, clientSecret, azureOperationHelper.storageAccountName, azureOperationHelper.containerName, azureOperationHelper.storageEndPoint);
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(azureOperationHelper.blobName);
            azureOperationHelper.destinationPath= @"C:\Users\phaneendra\Desktop\DataDiggers\";
            blob.DownloadToFileAsync(azureOperationHelper.destinationPath, FileMode.OpenOrCreate);
        }
        private static CloudBlobContainer CreateCloudBlobContainer(string tenantId, string applicationId, string clientSecret, string storageAccountName, string containerName, string storageEndPoint)
        {
            string accessToken = GetUserOAuthToken(tenantId, applicationId, clientSecret);
            TokenCredential tokenCredential = new TokenCredential(accessToken);
            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);
            CloudStorageAccount cloudStorageAccount = new CloudStorageAccount(storageCredentials, storageAccountName, storageEndPoint, useHttps: true);
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            return blobContainer;
        }
        static string GetUserOAuthToken(string tenantId, string applicationId, string clientSecret)
        {
            const string ResourceId = "https://storage.azure.com/";
            const string AuthInstance = "https://login.microsoftonline.com/{0}/";
            string authority = string.Format(CultureInfo.InvariantCulture, AuthInstance, tenantId);
            AuthenticationContext authContext = new AuthenticationContext(authority);
            var clientCred = new ClientCredential(applicationId, clientSecret);
            AuthenticationResult result = authContext.AcquireTokenAsync(ResourceId, clientCred).Result;
            return result.AccessToken;
        }
    }

    public class AzureOperationHelper
    {
        public string storageAccountName { get; set; }
        public string blobName { get; set; }
        public string destinationPath { get; set; }
        public string srcPath { get; set; }
        public string containerName { get; set; }
        public string storageEndPoint { get; set; }
        public string filename { get; set; }
    }
}
