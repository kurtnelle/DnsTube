using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Net;
using System.Data;
using System.IO;

namespace DnsTubeCore
{
    public class RouteParser
    {
        public List<ActiveRouteIpv4> ActiveRoutes { get; protected set; } = new List<ActiveRouteIpv4>();
        public List<PersistentRouteIpv4> PersistentRoutes { get; protected set; } = new List<PersistentRouteIpv4>();

        public List<Ipv6Route> Ipv6Routes { get; protected set; } = new List<Ipv6Route>();

        public List<IPAddress> Ipv4Gateways {
            get {
                return (from ActiveRouteIpv4 activeRoute in ActiveRoutes
                        where activeRoute.Gateway != null && activeRoute.NetworkDestination == IPAddress.Parse("0.0.0.0")
                        select activeRoute.Gateway).ToList();
            }
        }

        public RouteParser(string responseFromRoutePrint)
        {
            Regex sectionSplit = new Regex(@"(=+(?<Table>[^=]+))+", RegexOptions.Compiled | RegexOptions.Singleline);
            CaptureCollection _captures = sectionSplit.Match(responseFromRoutePrint).Groups["Table"].Captures;
            var _routeTypes = new string[] { "Active Routes:", "Persistent Routes:" };
            foreach (Capture _capture in _captures)
            {
                var _captureData = _capture.Value;

                if (!_captureData.Contains("If"))
                {
                    if (_captureData.Contains(_routeTypes[0]))
                    {
                        _captureData = _captureData.Replace(_routeTypes[0], string.Empty).Replace("  ", "\t").Trim();
                        var _dataTable = Parse(_captureData);
                        foreach (DataRow _row in _dataTable.Rows)
                        {
                            var _activeRoute = new ActiveRouteIpv4()
                            {
                                NetworkDestination = IPAddress.Parse((string)_row["Network Destination"]),
                                Netmask = IPAddress.Parse((string)_row["Netmask"]),
                                Gateway = (string)_row["Gateway"] != "On-link" ? IPAddress.Parse((string)_row["Gateway"]) : null,
                                Interface = IPAddress.Parse((string)_row["Interface"]),
                                Metric = int.Parse((string)_row["Metric"])
                            };
                            ActiveRoutes.Add(_activeRoute);
                        }
                    }
                    else if (_captureData.Contains(_routeTypes[1]))
                    {
                        _captureData = _captureData.Replace(_routeTypes[1], string.Empty).Replace("  ", "\t").Trim();
                        var _dataTable = Parse(_captureData);
                        foreach (DataRow _row in _dataTable.Rows)
                        {
                            var _persistentRoute = new PersistentRouteIpv4()
                            {
                                NetworkAddress = IPAddress.Parse((string)_row["Network Address"]),
                                Netmask = IPAddress.Parse((string)_row["Netmask"]),
                                GatewayAddress = IPAddress.Parse((string)_row["Gateway Address"]),
                                Metric = (string)_row["Metric"]
                            };
                            PersistentRoutes.Add(_persistentRoute);
                        }
                    }
                }
                else
                {
                    Ipv6RouteType _type = _captureData.Contains(_routeTypes[0]) ? Ipv6RouteType.Active : Ipv6RouteType.Persistent;
                    _captureData = _captureData.Replace("Network Destination", "NetworkDestination");
                    _captureData = _captureData.Replace(_routeTypes[0], string.Empty)
                        .Replace(_routeTypes[1], string.Empty)
                        .Replace(" ", "\t")
                        .Trim();

                    var _dataTable = Parse(_captureData);
                    int _index = 0;
                    while(_index < _dataTable.Rows.Count)
                    {
                        int _result = 0;
                        if (!int.TryParse((string)_dataTable.Rows[_index]["If"], out _result))
                        {
                            _dataTable.Rows[_index - 1]["Gateway"] = _dataTable.Rows[_index]["If"];
                            _dataTable.Rows.RemoveAt(_index);
                        }
                        else
                        {
                            _index++;
                        }
                    }
                    foreach (DataRow _row in _dataTable.Rows)
                    {
                        var _ipv6Route = new Ipv6Route()
                        {
                            RouteType = _type,
                            If = int.Parse((string)_row["If"]),
                            Metric = int.Parse((string)_row["Metric"]),
                            NetworkDestination = IPAddress.Parse(((string)_row["NetworkDestination"]).Split('/')[0]),
                            Gateway = (string)_row["Gateway"] != "On-link" ? IPAddress.Parse(((string)_row["Gateway"]).Split('/')[0]) : null
                        };
                        Ipv6Routes.Add(_ipv6Route);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableData"></param>
        /// <returns></returns>
        protected DataTable Parse(string tableData)
        {
            DataTable _dataTable = new DataTable();
            StringReader _strReader = new StringReader(tableData);
            char[] _delimiter = new char[] { '\t' };
            string[] _column = _strReader.ReadLine().Split(_delimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (string columnheader in _column)
            {
                _dataTable.Columns.Add(columnheader.Trim());
            }

            while (_strReader.Peek() > 0) 
            {
                var _row = _dataTable.NewRow();
                _row.ItemArray = (from string _s in _strReader.ReadLine().Split(_delimiter, StringSplitOptions.RemoveEmptyEntries)
                                  select _s.Trim()).ToArray();
                _dataTable.Rows.Add(_row);
            }
            return _dataTable;
        }
    }

    public class ActiveRouteIpv4
    {
        public IPAddress NetworkDestination { get; set; }
        public IPAddress Netmask { get; set; }
        public IPAddress Gateway { get; set; }
        public IPAddress Interface { get; set; }
        public int Metric { get; set; }
    }

    public class PersistentRouteIpv4
    {
        public IPAddress NetworkAddress { get; set; }
        public IPAddress Netmask { get; set; }
        public IPAddress GatewayAddress { get; set; }
        public string Metric { get; set; }
    }

    public class Ipv6Route
    {
        public int If { get; set; }
        public int Metric { get; set; }
        public IPAddress NetworkDestination { get; set; }     
        public IPAddress Gateway { get; set; }
        public Ipv6RouteType RouteType { get; set; }
    }

    public enum Ipv6RouteType
    {
        Active,
        Persistent
    }

}