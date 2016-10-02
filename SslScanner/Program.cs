using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SslScanner.Objects;

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
            UpdateBlobStorage(scores);
        }

        private static HashSet<string> GetDomains(string readFile) => new DomainScanner(readFile).Run();

        private static List<GovDomain> GetScores(HashSet<string> domains) => new SslLabsScanner(domains).Run();

        private static void UpdateBlobStorage(List<GovDomain> scores)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("Data:ConnectionString"));

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("scans");
            container.CreateIfNotExists();

            var latestBlob = container.GetBlockBlobReference("latest.json");
            var todayBlob = container.GetBlockBlobReference("historical/" + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".json");
            var changesBlob = container.GetBlockBlobReference("changes.json");

            var lastScores = GetLastScores(container);

            var changes = new List<ChangeSet>();

            try
            {
                changes.AddRange(JsonConvert.DeserializeObject<List<ChangeSet>>(changesBlob.DownloadText()));
            }
            catch (Exception)
            {
                // ignored
            }

            changes.Add(new ResultsDiff(lastScores, scores).Run());

            var todayJson = JsonConvert.SerializeObject(scores);
            var changesJson = JsonConvert.SerializeObject(changes);

            latestBlob.UploadText(todayJson);
            todayBlob.UploadText(todayJson);
            changesBlob.UploadText(changesJson);
        }

        private static List<GovDomain> GetLastScores(CloudBlobContainer container)
        {
            var lastScores = new List<GovDomain>();
            for (var days = -1; days >= -31; days--)
            {
                try
                {
                    var lastBlob = container.GetBlockBlobReference("historical/" + DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd") + ".json");
                    lastScores = JsonConvert.DeserializeObject<List<GovDomain>>(lastBlob.DownloadText());
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return lastScores;
        }
    }
}