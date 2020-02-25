using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;

namespace DnsTubeCore
{
    public class PublicIPAddress: ObservableCollection<IPAddress>
    {
        const string IPV4_LOOKUP_URL = "http://ipv4bot.whatismyipaddress.com";
        const string IPV6_LOOKUP_URL = "http://ipv6bot.whatismyipaddress.com";
        public PublicIPAddress()
        {
            List<IPAddress> _defaultGateway = new List<IPAddress>() { DefaultGateway() };
            PopulateAddressesFrom(_defaultGateway);
        }

        public PublicIPAddress(List<IPAddress> gateways)
        {
            PopulateAddressesFrom(gateways);
        }

        public PublicIPAddress(string gatewayIPs)
        {
            PopulateAddressesFrom(
                gatewayIPs.Split(new char[] { ' ', ',' })
                .ToList()
                .ConvertAll(g => IPAddress.Parse(g)));
        }

        void PopulateAddressesFrom(List<IPAddress> gateways)
        {
            foreach (IPAddress g in gateways)
            {
                var address = GetIpv4Via(g);
                if (address != null) { Add(address); }
                address = GetIpv6Via(g);
                if (address != null) { Add(address); }
            }
        }

        protected IPAddress GetIpv4Via(IPAddress gateway)
        {
            var _entries = Dns.GetHostEntry(new Uri(IPV4_LOOKUP_URL).Host);
            if (_entries.AddressList.Length > 0)
            {
                var _targetIP = _entries.AddressList[0];
                if (_targetIP.AddressFamily == gateway.AddressFamily)
                {
                    RemoveRoute(_targetIP);
                    AddRoute(_targetIP, gateway);
                    var _candiatePublicIPAddress = IPV4_LOOKUP_URL.GetStringAsync().Result.Replace("\n", "");
                    var _publicIP = IPAddress.Parse(_candiatePublicIPAddress);
                    RemoveRoute(_targetIP);
                    return _publicIP;
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
                    RemoveRoute(_targetIP);
                    AddRoute(_targetIP, gateway);
                    var _candiatePublicIPAddress = IPV4_LOOKUP_URL.GetStringAsync().Result.Replace("\n", "");
                    var _publicIP = IPAddress.Parse(_candiatePublicIPAddress);
                    RemoveRoute(_targetIP);
                    return _publicIP;
                }
                else return null;
            }
            else return null;
        }

        protected IPAddress DefaultGateway()
        {
            IPAddress remoteIp = IPAddress.Parse("8.8.8.8");
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIp, 0);
            Socket socket = new Socket(
                                  AddressFamily.InterNetwork,
                                  SocketType.Dgram,
                                  ProtocolType.Udp);
            IPEndPoint localEndPoint = QueryRoutingInterface(socket, remoteEndPoint);
            return localEndPoint.Address;
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
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    FileName = "route",
                    Arguments = $"add -p {target.ToString()} mask 255.255.255.255 {gateway.ToString()}",
                    RedirectStandardInput = true,
                    StandardOutputEncoding = Encoding.ASCII
                }
            };
            p.Start();
            var _response = p.StandardOutput.ReadToEnd();
            if(_response.Contains("The requested operation requires elevation."))
            {
                throw new ApplicationException("Runing the \"route add\" command requires elevation. Run this tool as administrator.");
            }
        }
        protected void RemoveRoute(IPAddress target)
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

        public static RouteParser GetRouteTables()
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = "route",
                    Arguments = "print",
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.ASCII
                }
            };
            p.Start();
            var _response = p.StandardOutput.ReadToEnd();
            return new RouteParser(_response);
        }
    }
}
