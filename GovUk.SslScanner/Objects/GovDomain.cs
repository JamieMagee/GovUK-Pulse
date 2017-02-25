using GovUk.SslScanner.Enums;
using Newtonsoft.Json;

namespace GovUk.SslScanner.Objects
{
    public class GovDomain
    {
        private Grade _grade;

        public GovDomain(string canonical, string domain)
        {
            this.canonical = canonical;
            this.domain = domain;
        }

        [JsonProperty("canonical")]
        public string canonical { get; }

        [JsonProperty("domain")]
        public string domain { get; }

        [JsonProperty("grade")]
        public Grade grade
        {
            get { return _grade; }
            set
            {
                _grade = value;
                https = GradeToHttps(value);
            }
        }

        [JsonProperty("https")]
        public Https https { get; private set; }

        private bool Equals(GovDomain other)
        {
            return _grade == other._grade && https == other.https;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((GovDomain) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _grade * 397) ^ (int) https;
            }
        }

        private static Https GradeToHttps(Grade grade)
        {
            switch (grade)
            {
                case Grade.NoHttps:
                    return Https.No;
                case Grade.M:
                case Grade.T:
                    return Https.WithCertIssues;
                default:
                    return Https.Yes;
            }
        }
    }
}