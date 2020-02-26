using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DnsTubeCore
{

    public static class Program
    {
        
        static int Main(string[] args)
        {
            var arguments = Debugger.IsAttached ? File.ReadAllText("parameters.txt") : string.Join(" ", args);

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
            var defaultRoutingIp = PublicIPAddress.DefaultRoutingIp;
            IPAddress gatewayIP, publicIP = null;
            if (!string.IsNullOrEmpty(gateway))
            {
                gatewayIP = IPAddress.Parse(gateway);
                publicIP = new PublicIPAddress(new List<IPAddress>() { gatewayIP }).FirstOrDefault();
            }
            else if (defaultRoutingIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                gatewayIP = new RouteTable()
                    .ActiveRoutes
                    .Where(a => a.Interface.ToString() == defaultRoutingIp.ToString())
                    .Where(a => a.NetworkDestination.ToString() == "0.0.0.0")
                    .Select(a => a.Gateway).FirstOrDefault();
                publicIP = new PublicIPAddress(new List<IPAddress>() { gatewayIP }).FirstOrDefault();
            }
            else if(string.IsNullOrEmpty(gateway) && defaultRoutingIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                publicIP = defaultRoutingIp;
            }
             
            var ipType = (publicIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "A" : "AAAA");

            CloudflareClient cloudflareClient = new CloudflareClient()
            {
                EmailAddress = email,
                IsKeyOrToken = string.IsNullOrEmpty(apikey) ? KeyOrToken.Token : KeyOrToken.Key,
                APIKeyOrToken = string.IsNullOrEmpty(apikey) ? token : apikey
            };
            var zone = (from Zone z in cloudflareClient.Zones
                       from CloudFlareDnsResult d in z.DnsEntries
                       where d.Name.Contains(hostname, StringComparison.InvariantCultureIgnoreCase)
                       where d.Type == ipType
                        select new { Zone = z, DnsEntry = d }).FirstOrDefault();
            if(zone != null)
            {
                var result = cloudflareClient.UpdateDnsEntry(zone.Zone.Id,
                    zone.DnsEntry.Id,
                    ipType,
                    zone.DnsEntry.Name,
                    publicIP.ToString(),
                    zone.DnsEntry.Proxied);
                
            }
            else
            {
                throw new ApplicationException($"There is no {ipType} type in any zones for hostname \"{hostname}\"");
            }
        }
    }
}
