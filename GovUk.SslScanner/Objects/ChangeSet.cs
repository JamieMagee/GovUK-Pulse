using System;
using System.Collections.Generic;
using GovUk.SslScanner.Enums;
using Newtonsoft.Json;

namespace GovUk.SslScanner.Objects
{
    public class ChangeSet
    {
        [JsonProperty("date")] private string date;

        public ChangeSet()
        {
            date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        [JsonProperty("diffList")]
        public List<Diff> diffList { get; } = new List<Diff>();

        public class Diff
        {
            [JsonProperty("canonical")] private string canonical;

            [JsonProperty("domain")] private string domain;

            [JsonProperty("newGrade")] private Grade newGrade;

            [JsonProperty("newHttps")] private Https newHttps;

            [JsonProperty("oldGrade")] private Grade oldGrade;

            [JsonProperty("oldHttps")] private Https oldHttps;

            public Diff(GovDomain previous, GovDomain current)
            {
                canonical = previous.canonical;
                domain = previous.domain;
                oldGrade = previous.grade;
                newGrade = current.grade;
                oldHttps = previous.https;
                newHttps = current.https;
            }
        }
    }
}