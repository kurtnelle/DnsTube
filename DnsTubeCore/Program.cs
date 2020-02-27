using LogManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DnsTubeCore
{

    public static class Program
    {
        static DateTime startedTime = DateTime.Now;
        static int Main(string[] args)
        {
            //WaitForDebugger();

            var arguments = Debugger.IsAttached ? File.ReadAllText("parameters.txt") : string.Join(" ", args);


            MethodInfo doWork = typeof(Program).GetMethod("DoWork", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var _paramaters = doWork.GetParameters();
            if (!string.IsNullOrEmpty(arguments))
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Log.LogFileName = Log.DefaultLogFileName;
                Log.LogEvent += Log_LogEvent;
                log($"Started at {startedTime}");

                Dictionary<string, string> parameters = new Dictionary<string, string>();
                Regex commandParse = new Regex(@"(?<Argument>(?<Parameter>(?<=--)[^\s]+)\s+((?<Value>[^\s]+|(?<Value>""[^""]+"")))\s*)+", RegexOptions.Compiled);
                MatchCollection matches = commandParse.Matches(arguments);

                foreach (ParameterInfo pinfo in _paramaters)
                {
                    parameters.Add(pinfo.Name, (from Match match in matches
                                                where match.Groups["Parameter"].Value.Contains(pinfo.Name, StringComparison.InvariantCultureIgnoreCase)
                                                select match.Groups["Value"].Value).FirstOrDefault());
                }
                var _stringOfParam = string.Join(' ', (from ParameterInfo pmInfo in _paramaters
                                                       where parameters[pmInfo.Name] != null
                                                       select $"--{pmInfo.Name} {((pmInfo.Name == "apikey" || pmInfo.Name == "token") ? parameters[pmInfo.Name] : new String('*', parameters[pmInfo.Name].Length)) }").ToArray());

                log($"Invoking doWork with parameters: { _stringOfParam }");
                doWork.Invoke(null, parameters.Values.ToArray());
            }
            else
            {
                Console.Write(Resource.help);
            }

            return 0;
        }

        private static void WaitForDebugger()
        {
            uint counter = 0;
            var charArray = new char[] { '-', '\\', '|', '/' };
            while (true)
            {
                if (Debugger.IsAttached)
                {
                    break;
                }
                else
                {
                    Console.Write($"\rWaiting on debugger { charArray[counter % 4] } ");
                    counter++;
                    Thread.Sleep(100);
                }
            }
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
                log($"No gateway specified. Detected default gateway \"{gatewayIP.ToString()}\"");
            }
            else if(string.IsNullOrEmpty(gateway) && defaultRoutingIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                publicIP = defaultRoutingIp;
            }
            log($"Public Ip detected as \"{publicIP.ToString()}\"");

            var ipType = (publicIP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "A" : "AAAA");

            CloudflareClient cloudflareClient = new CloudflareClient()
            {
                EmailAddress = email,
                IsKeyOrToken = string.IsNullOrEmpty(apikey) ? KeyOrToken.Token : KeyOrToken.Key,
                APIKeyOrToken = string.IsNullOrEmpty(apikey) ? token : apikey
            };
            var record = (from Zone z in cloudflareClient.Zones
                        from CloudFlareDnsResult d in z.DnsEntries
                        where d.Name.Contains(hostname, StringComparison.InvariantCultureIgnoreCase)
                        where d.Type == ipType
                        select new { Zone = z, DnsEntry = d }).FirstOrDefault();
            if(record != null)
            {
                log($"Updating Zone \"{record.Zone.Name}({record.Zone.Id})\", DNSEntry \"{record.DnsEntry.Name}({record.DnsEntry.Id})\" of type \"{ipType}\"");
                var result = cloudflareClient.UpdateDnsEntry(record.Zone.Id,
                    record.DnsEntry.Id,
                    ipType,
                    record.DnsEntry.Name,
                    publicIP.ToString(),
                    record.DnsEntry.Proxied);
            }
            else
            {
                throw new ApplicationException($"There is no {ipType} type in any zones for hostname \"{hostname}\". Perhaps it has not been added? ;)");
            }
        }
        private static void Log_LogEvent(string message, string threadName, LogManagement.LogLevel level)
        {
            switch (level)
            {
                case LogManagement.LogLevel.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogManagement.LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogManagement.LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    break;
            }
            Console.WriteLine(message);
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Informational($"Terminating with error at {DateTime.Now}, with a duration of {(DateTime.Now - startedTime).TotalMinutes}");
            Log.Exception((Exception)e.ExceptionObject);
            Log.LogFileName = string.Empty;
            Environment.Exit(-1);
        }

        private static void log(string str)
        {
            Log.Informational(str);
        }
    }
}
