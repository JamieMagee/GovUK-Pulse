using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SslScanner.Enums;

namespace SslScanner.Objects
{
    public class ChangeSet
    {
        [JsonProperty("date")]
        private string date;
        [JsonProperty("diffList")]
        public List<Diff> diffList { get; } = new List<Diff>();

        public ChangeSet()
        {
            date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public class Diff
        {
            [JsonProperty("canonical")]
            private string canonical;
            [JsonProperty("domain")]
            private string domain;
            [JsonProperty("oldGrade")]
            private Grade oldGrade;
            [JsonProperty("newGrade")]
            private Grade newGrade;
            [JsonProperty("oldHttps")]
            private Https oldHttps;
            [JsonProperty("newHttps")]
            private Https newHttps;

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