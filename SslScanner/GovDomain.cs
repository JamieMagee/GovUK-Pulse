using System.ComponentModel;

namespace SslScanner
{
    public class GovDomain
    {
        public enum Grade
        {
            [Description("A+")] Aplus = 7,
            A = 6,
            [Description("A-")] Aminus = 5,
            B = 4,
            C = 3,
            T = 2,
            F = 1,
            [Description("Certificate not valid for domain name")] M = 0,
            [Description("No secure protocols supported")] NoHttps = -1
        }

        public enum Https
        {
            Yes = 2,
            WithCertIssues = 1,
            No = 0
        }

        public string Canonical;
        public string Domain;
        public Grade grade;
        public Https https;

        public static Https GradeToHttps(Grade _grade)
        {
            switch (_grade)
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