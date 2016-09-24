using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
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
            var storageAccount = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable("Data:ConnectionString"));

            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("scans");
            container.CreateIfNotExists();

            var latestBlob = container.GetBlockBlobReference("latest.json");
            var todayBlob = container.GetBlockBlobReference("historical/" + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".json");
            var yesterdayBlob = container.GetBlockBlobReference("historical/" + DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd") + ".json");
            var changesBlob = container.GetBlockBlobReference("changes.json");

            var changes = JsonConvert.DeserializeObject<List<ChangeSet>>(changesBlob.DownloadText());
            var yesterdayScores = JsonConvert.DeserializeObject<List<GovDomain>>(yesterdayBlob.DownloadText());

            changes.Add(new ResultsDiff(yesterdayScores, scores).Run());

            var todayJson = JsonConvert.SerializeObject(scores);
            var changesJson = JsonConvert.SerializeObject(changes);

            latestBlob.UploadText(todayJson);
            todayBlob.UploadText(todayJson);
            changesBlob.UploadText(changesJson);
        }
    }
}