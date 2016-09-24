using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SslScanner.Enums;

namespace SslScanner.Objects
{
    public class ChangeSet
    {
        private string date;
        public List<Diff> diffList { get; } = new List<Diff>();

        public ChangeSet()
        {
            date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public class Diff
        {
            private string canonical;
            private string domain;
            [JsonProperty("old_grade")]
            private Grade oldGrade;
            [JsonProperty("new_grade")]
            private Grade newGrade;
            [JsonProperty("old_https")]
            private Https oldHttps;
            [JsonProperty("new_https")]
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