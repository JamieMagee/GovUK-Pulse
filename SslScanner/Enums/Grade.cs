using System.ComponentModel;

namespace SslScanner.Enums
{
    public enum Grade
    {
        [Description("A+")]
        Aplus = 7,
        A = 6,
        [Description("A-")]
        Aminus = 5,
        B = 4,
        C = 3,
        T = 2,
        F = 1,
        [Description("Certificate not valid for domain name")]
        M = 0,
        [Description("No secure protocols supported")]
        NoHttps = -1
    }
}