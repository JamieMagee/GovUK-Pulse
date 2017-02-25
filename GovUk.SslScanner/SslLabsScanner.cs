using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GovUk.SslScanner.Enums;
using GovUk.SslScanner.Objects;
using Newtonsoft.Json.Linq;
using SslLabsLib;
using SslLabsLib.Enums;
using SslLabsLib.Objects;
using Timer = System.Timers.Timer;

namespace GovUk.SslScanner
{
    public class SslLabsScanner
    {
        private static readonly List<string> SslLabsError = new List<string>
        {
            "Failed to obtain certificate",
            "Unable to connect to the server",
            "IP address is from private address space (RFC 1918)",
            "Failed to communicate with the secure server"
        };

        private readonly HashSet<string> _input;
        private readonly List<string> _preloadList = GetChromePreloadList();
        private readonly List<GovDomain> _resultsList = new List<GovDomain>();

        public SslLabsScanner(HashSet<string> input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            _input = input;
        }

        public List<GovDomain> Run()
        {
            var completedTasks = 0;
            var client = new SslLabsClient(new Uri("https://api.ssllabs.com/api/v2/"));

            var sslLabsInfo = client.GetInfo();

            var domains = new Queue<string>();
            foreach (var canonical in _input)
            {
                var domain = canonical.Replace("http://", "").Replace("https://", "");
                _resultsList.Add(new GovDomain(canonical, domain));
                domains.Enqueue(domain);
            }

            var lastStatus = DateTime.UtcNow;
            var lastStatusLock = new object();
            Action printStatus = () =>
            {
                lock (lastStatusLock)
                {
                    if ((DateTime.UtcNow - lastStatus).TotalSeconds < 5)
                        return;

                    lastStatus = DateTime.UtcNow;

                    Console.WriteLine("Queue: " + domains.Count +
                                      ", running: " + client.CurrentAssesments +
                                      " (of " + client.MaxAssesments + "), " +
                                      "completed: " + completedTasks);
                }
            };

            var limitChangedEvent = new AutoResetEvent(false);

            client.MaxAssesmentsChanged += () => limitChangedEvent.Set();
            client.CurrentAssesmentsChanged += () => limitChangedEvent.Set();

            while (domains.Any())
            {
                var domain = domains.Peek();

                // Is it done already?
                Analysis analysis;

                // Attempt to start the task
                while (true)
                {
                    TryStartResult didStart;
                    try
                    {
                        didStart = client.TryStartAnalysis(domain, 23, out analysis, AnalyzeOptions.ReturnAllIfDone);
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine(
                            "(Name: " + domain + ") Webexception starting scan, waiting 3s: " + ex.Message);
                        limitChangedEvent.WaitOne(3000);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("(Name: " + domain + ") Exception while starting scan, waiting 30s: " +
                                          ex.Message);
                        Thread.Sleep(30000);
                        continue;
                    }

                    if (didStart == TryStartResult.RateLimit)
                    {
                        Thread.Sleep(sslLabsInfo.NewAssessmentCoolOff);
                        continue;
                    }
                    if (didStart == TryStartResult.Ok)
                    {
                        printStatus();
                        break;
                    }

                    // Wait for one to free up, fall back to trying every 30s
                    limitChangedEvent.WaitOne(30000);

                    printStatus();
                }

                // The task was started
                domains.Dequeue();
                Console.WriteLine("Started " + domain);

                Task.Factory.StartNew(() =>
                {
                    Analysis innerAnalysis = null;
                    if (analysis != null && analysis.Status == AnalysisStatus.READY)
                        // Use the one we fetched immediately
                        innerAnalysis = analysis;

                    while (innerAnalysis == null)
                        try
                        {
                            // Block till we have an analysis
                            innerAnalysis = client.GetAnalysisBlocking(domain);
                        }
                        catch (WebException ex)
                        {
                            Console.WriteLine("(Name: " + domain + ") Webexception waiting for scan, waiting 3s: " +
                                              ex.Message);
                            Thread.Sleep(3000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("(Name: " + domain + ") Exception while waiting for scan, waiting 30s: " +
                                              ex.Message);
                            Thread.Sleep(30000);
                        }

                    var tmp = _resultsList.Find(x => x.domain.Equals(domain));
                    tmp.grade = GetWorstEndpoint(innerAnalysis);
                    Console.WriteLine("Completed " + domain);

                    Interlocked.Increment(ref completedTasks);

                    limitChangedEvent.Set();
                });
            }

            var timer = new Timer(2000);
            timer.Elapsed += (sender, eventArgs) => printStatus();
            timer.Start();

            while (true)
            {
                var info = client.GetInfo();
                if (info.CurrentAssessments == 0)
                    break;

                // Wait for tasks to finish, fall back to checking every 15s
                limitChangedEvent.WaitOne(15000);
            }

            timer.Stop();

            return _resultsList;
        }

        private static Grade GetWorstEndpoint(Analysis analysis)
        {
            return analysis.Endpoints.Aggregate(Grade.Aplus,
                (current, endpoint) => current < ConvertToEnum(endpoint) ? current : ConvertToEnum(endpoint));
        }

        private static Grade ConvertToEnum(Endpoint endpoint)
        {
            return endpoint.Grade == null
                ? SslLabsError.Contains(endpoint.StatusMessage)
                    ? Grade.NoHttps
                    : EnumEx.GetValueFromDescription<Grade>(endpoint.StatusMessage)
                : EnumEx.GetValueFromDescription<Grade>(endpoint.Grade);
        }

        private static List<string> GetChromePreloadList()
        {
            var preloadList = new List<string>();
            var res =
                new HttpClient().GetStringAsync(
                        "https://chromium.googlesource.com/chromium/src/net/+/master/http/transport_security_state_static.json?format=text")
                    .Result;
            var plain = Convert.FromBase64String(res);
            var iso = Encoding.GetEncoding("UTF-8");
            var newData = iso.GetString(plain);

            var json = JObject.Parse(newData);
            foreach (var entry in json.SelectToken("entries"))
                try
                {
                    preloadList.Add(entry.SelectToken("name").ToString());
                }
                catch (Exception)
                {
                    // ignored
                }
            return preloadList;
        }

        private static class EnumEx
        {
            public static T GetValueFromDescription<T>(string description)
            {
                var type = typeof(T);
                if (!type.IsEnum) throw new InvalidOperationException();
                foreach (var field in type.GetFields())
                {
                    var attribute = Attribute.GetCustomAttribute(field,
                        typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attribute != null)
                    {
                        if (attribute.Description == description)
                            return (T) field.GetValue(null);
                    }
                    else
                    {
                        if (field.Name == description)
                            return (T) field.GetValue(null);
                    }
                }
                throw new ArgumentException("Not found.", nameof(description));
            }
        }
    }
}