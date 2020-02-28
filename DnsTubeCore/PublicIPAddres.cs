using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

namespace DnsTubeCore
{
    public class PublicIPAddress: ObservableCollection<IPAddress>
    {
        const string IPV4_LOOKUP_URL = "http://ipv4bot.whatismyipaddress.com";
        const string IPV6_LOOKUP_URL = "http://ipv6bot.whatismyipaddress.com";

        public PublicIPAddress(List<IPAddress> gateways)
        {
            PopulateAddressesFrom(gateways);
        }

        void PopulateAddressesFrom(List<IPAddress> gateways)
        {
            foreach (IPAddress g in gateways)
            {
                var address = g.AddressFamily == AddressFamily.InterNetwork ? GetIpv4Via(g) : null;
                if (address != null) { Add(address); }
                address = g.AddressFamily == AddressFamily.InterNetworkV6 ? GetIpv6Via(g) : null;
                if (address != null) { Add(address); }
            }
        }

        protected IPAddress GetIpv4Via(IPAddress gateway)
        {
            RouteTable routeTable = new RouteTable();
            var _entries = Dns.GetHostEntry(new Uri(IPV4_LOOKUP_URL).Host);
            if (_entries.AddressList.Length > 0)
            {
                var _targetIP = _entries.AddressList[0];
                if (_targetIP.AddressFamily == gateway.AddressFamily)
                {
                    if (gateway == IPAddress.Parse("0.0.0.0"))
                    {
                        return new RouteTable().PotentiallyPublicIpv4Addresses.FirstOrDefault();
                    }
                    else
                    {
                        RemoveRoute(_targetIP);
                        AddRoute(_targetIP, gateway);
                        var _candiatePublicIPAddress = IPV4_LOOKUP_URL.GetStringAsync().Result.Replace("\n", "");
                        var _publicIP = IPAddress.Parse(_candiatePublicIPAddress);
                        RemoveRoute(_targetIP);
                        return _publicIP;
                    }
                }
                else return null;
            }
            else return null;
        }

        protected IPAddress GetIpv6Via(IPAddress gateway)
        {
            var entries = Dns.GetHostEntry(new Uri(IPV6_LOOKUP_URL).Host);
            if (entries.AddressList.Length > 0)
            {
                var _targetIP = entries.AddressList[0];
                if (_targetIP.AddressFamily == gateway.AddressFamily)
                {
                    if (gateway.ToString() == "::")
                    {
                        return new RouteTable().PotentiallyPublicIpv6Addreses.FirstOrDefault();
                    }
                    else
                    {
                        RemoveRoute(_targetIP);
                        AddRoute(_targetIP, gateway);
                        var _candiatePublicIPAddress = IPV4_LOOKUP_URL.GetStringAsync().Result.Replace("\n", "");
                        var _publicIP = IPAddress.Parse(_candiatePublicIPAddress);
                        RemoveRoute(_targetIP);
                        return _publicIP;
                    }
                }
                else return null;
            }
            else return null;
        }

        public static IPAddress DefaultRoutingIp {
            get {
                IPAddress remoteIp = Dns.GetHostEntry("www.cloudflare.com").AddressList.FirstOrDefault();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    IPEndPoint remoteEndPoint = new IPEndPoint(remoteIp, 0);
                    Socket socket = new Socket(
                                          remoteIp.AddressFamily,
                                          SocketType.Dgram,
                                          ProtocolType.Udp);
                    IPEndPoint localEndPoint = QueryRoutingInterface(socket, remoteEndPoint);
                    return localEndPoint.Address;
                }
                else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)){
                    Regex srcExpression = new Regex(@"(?<ip>(?<=src\s)[abcdef0123456789:]+(?=\s))", RegexOptions.Compiled);
                    var result = $"ip route get {remoteIp.ToString()}".Bash();
                    Match match = srcExpression.Match(result);
                    return IPAddress.Parse(match.Groups["ip"].Value);
                }
                else
                {
                    throw new NotSupportedException("Platform is not supported at this time.");
                }
            }
        }

        private static IPEndPoint QueryRoutingInterface(Socket socket, IPEndPoint remoteEndPoint)
        {
            SocketAddress address = remoteEndPoint.Serialize();

            byte[] remoteAddrBytes = new byte[address.Size];
            for (int i = 0; i < address.Size; i++)
            {
                remoteAddrBytes[i] = address[i];
            }

            byte[] outBytes = new byte[remoteAddrBytes.Length];
            socket.IOControl(
                        IOControlCode.RoutingInterfaceQuery,
                        remoteAddrBytes,
                        outBytes);
            for (int i = 0; i < address.Size; i++)
            {
                address[i] = outBytes[i];
            }

            EndPoint ep = remoteEndPoint.Create(address);
            return (IPEndPoint)ep;
        }

        protected void AddRoute(IPAddress target, IPAddress gateway)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {

                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = "route",
                        Arguments = $"add -p {target.ToString()} mask 255.255.255.255 {gateway.ToString()}",
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.ASCII
                    }
                };
                p.Start();
                var _response = p.StandardOutput.ReadToEnd();
                if (_response.Contains("The requested operation requires elevation."))
                {
                    throw new ApplicationException("Runing the \"route add\" command requires elevation. Run this tool as administrator.");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                $"sudo ip route add {target.ToString()} via {gateway.ToString()}".Bash();
            }
            else
            {
                throw new NotSupportedException("Platform is not supported at this time.");
            }
        }
        protected void RemoveRoute(IPAddress target)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        FileName = "route",
                        Arguments = $"delete {target.ToString()}",
                        RedirectStandardOutput = true,
                        StandardOutputEncoding = Encoding.ASCII
                    }
                };
                p.Start();
                var _response = p.StandardOutput.ReadToEnd();
                if (_response.Contains("The requested operation requires elevation."))
                {
                    throw new ApplicationException("Runing the \"route add\" command requires elevation. Run this tool as administrator.");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                $"sudo ip route delete {target.ToString()}".Bash();
            }
            else
            {
                throw new NotSupportedException("Platform is not supported at this time.");
            }
        }
    }
}
