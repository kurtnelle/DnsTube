using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DnsTubeCore
{

    public static class Program
    {
        
        static int Main(string[] args)
        {
            var arguments = string.Join(" ", args);

            MethodInfo doWork = typeof(Program).GetMethod("DoWork", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var _paramaters = doWork.GetParameters();
            if (!string.IsNullOrEmpty(arguments))
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                Regex commandParse = new Regex(@"(?<Argument>(?<Parameter>(?<=--)[^\s]+)\s+((?<Value>[^\s]+|(?<Value>""[^""]+"")))\s*)+", RegexOptions.Compiled);
                MatchCollection matches = commandParse.Matches(arguments);
                foreach (ParameterInfo pinfo in _paramaters)
                {
                    parameters.Add(pinfo.Name, (from Match match in matches
                                                where match.Groups["Parameter"].Value.Contains(pinfo.Name, StringComparison.InvariantCultureIgnoreCase)
                                                select match.Groups["Value"].Value).FirstOrDefault());
                }
                doWork.Invoke(null, parameters.Values.ToArray());
            }
            else
            {
                //show help text
            }
            
            return 0; 
        }

        static void DoWork(string hostname, string gateway, string email, string apikey, string token)
        {
            CloudflareClient cloudflareClient = new CloudflareClient()
            {
                EmailAddress = email,
                IsKeyOrToken = string.IsNullOrEmpty(apikey) ? KeyOrToken.Token : KeyOrToken.Key,
                APIKeyOrToken = string.IsNullOrEmpty(apikey) ? token : apikey
            };
            var zones = cloudflareClient.Zones;
            var dnsEntries = zones[3].DnsEntries;

        }
    }
}
