using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace SslScanner
{
    internal static class Program
    {
        private static void Main()
        {
            const string readFile =
                @"https://www.gov.uk/government/uploads/system/uploads/attachment_data/file/465282/gov.uk_domains_as_of_01Oct_2015.csv";

            var domains = GetDomains(readFile);
            var scores = GetScores(domains);
            UpdateDatabase(scores);
        }

        private static HashSet<string> GetDomains(string readFile)
        {
            return new DomainScanner(readFile).Run();
        }

        private static IEnumerable<GovDomain> GetScores(HashSet<string> domains)
        {
            return new SslLabsScanner(domains).Run();
        }

        private static void UpdateDatabase(IEnumerable<GovDomain> scores)
        {
            var storageAccount = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable("Data:ConnectionString"));

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("scans");
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobReference("latest.json");

            blob.UploadText(JsonConvert.SerializeObject(scores));
        }
    }
}