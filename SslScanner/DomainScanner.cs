using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;

namespace SslScanner
{
    public class DomainScanner
    {
        private const string Http = "http://";
        private const string Www = "www.";
        private readonly HashSet<string> _hostList = new HashSet<string>();

        private readonly HashSet<string> _resultsList = new HashSet<string>();

        public DomainScanner(string input)
        {
            Input = input;
        }

        private string Input { get; }

        public HashSet<string> Run()
        {
            var domains = LoadDomains(Input);

            Console.WriteLine("Loaded " + domains.Count + " domains from CSV");

            Parallel.ForEach(
                domains,
                domain =>
                {
                    var client = new HttpClient();
                    try
                    {
                        var response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, Http + domain)).Result;
                        if (response.IsSuccessStatusCode && response.RequestMessage.RequestUri.Host.EndsWith("gov.uk"))
                        {
                            lock (_resultsList)
                            {
                                var finalDomain = response.RequestMessage.RequestUri.Scheme + "://" +
                                                  response.RequestMessage.RequestUri.Host;
                                var host = response.RequestMessage.RequestUri.Host.Replace(Www, "");

                                if (_hostList.Add(host) && _resultsList.Add(finalDomain))
                                    Console.WriteLine("Added: " + finalDomain);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                    client.Dispose();
                });

            Console.WriteLine("Returning " + _resultsList.Count + " domains");

            return _resultsList;
        }

        private List<string> LoadDomains(string input)
        {
            var domains = new List<string>();
            TextReader reader;
            if (input.StartsWith("https://"))
            {
                var file = new WebClient().DownloadString(Input);
                reader = new StringReader(file);
            }
            else
            {
                reader = new StreamReader(input);
            }

            var csv = new CsvReader(reader);
            csv.Configuration.IgnoreHeaderWhiteSpace = true;
            while (csv.Read())
            {
                domains.Add(csv.GetField<string>(0));
            }
            return domains;
        }
    }
}