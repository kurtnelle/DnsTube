using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Text;

namespace DnsTubeCore
{
    public class CloudflareClient
    {
        const string CLOUD_FLARE_ENDPOINT = "https://api.cloudflare.com/client/v4/";
        protected Dictionary<string, string> headers = new Dictionary<string, string>();
        protected string apiKeyOrToken = string.Empty;
        protected KeyOrToken keyOrToken;
        public string EmailAddress { get; set; }
        public string APIKeyOrToken {
            get {
                return apiKeyOrToken;
            }
            set {
                apiKeyOrToken = value;
                AdjustHeadersBasedOnKeyOrToken();
            }
        }
        public KeyOrToken IsKeyOrToken {
            get {
                return keyOrToken;
            }
            set {
                keyOrToken = value;
                AdjustHeadersBasedOnKeyOrToken();
            }
        }
        protected void AdjustHeadersBasedOnKeyOrToken()
        {
            headers.Clear();
            if (IsKeyOrToken == KeyOrToken.Token)
            {
                headers.Add("Authorization", $" Bearer {APIKeyOrToken}");
            }
            else
            {
                headers.Add("X-Auth-Key", APIKeyOrToken);
                headers.Add("X-Auth-Email", EmailAddress);
            }
        }

        public CloudflareClient()
        {
        }

        public List<Zone> Zones {
            get {
                try
                {
                    return ($"{CLOUD_FLARE_ENDPOINT}zones?status=active&page=1&per_page=50&order=name&direction=asc&match=all"
                        .WithHeaders(headers)
                        .GetJsonAsync<ZoneJsonObjects.ListZonesResponse>()
                        .Result
                        .result
                        .ToList()
                        .ConvertAll<Zone>(z => new Zone(this, z)));
                }
                catch (FlurlHttpException ex)
                {
                    throw GenerateCloudFlareException(ex);
                }
            }
        }

        internal List<CloudFlareDnsResult> GetDnsRecordsFor(Zone zone)
        {
            try
            {
                var ipv4Results = new List<DnsJsonObjects.Result>($"{CLOUD_FLARE_ENDPOINT}zones/{zone.Id}/dns_records?type=A&page=1&per_page=100&order=name&direction=asc&match=all"
                   .WithHeaders(headers)
                   .GetJsonAsync<DnsJsonObjects.DnsRecordsResponse>().Result.result);

                var ipv6Results = new List<DnsJsonObjects.Result>($"{CLOUD_FLARE_ENDPOINT}zones/{zone.Id}/dns_records?type=AAAA&page=1&per_page=100&order=name&direction=asc&match=all"
                   .WithHeaders(headers)
                   .GetJsonAsync<DnsJsonObjects.DnsRecordsResponse>().Result.result);

                return (ipv4Results
                    .Concat(ipv6Results)
                    .ToList()
                    .ConvertAll(i => new CloudFlareDnsResult(i)));
            }
            catch (FlurlHttpException ex)
            {
                throw GenerateCloudFlareException(ex);
            }
        }

        public DnsJsonObjects.DnsUpdateResponse UpdateDnsEntry(string zoneIdentifier, string dnsRecordIdentifier, string recordType, string dnsRecordName,  string content, bool proxied)
        {
            try
            {
                return $"{CLOUD_FLARE_ENDPOINT}zones/{zoneIdentifier}/dns_records/{dnsRecordIdentifier}"
                    .WithHeaders(headers)
                    .PutJsonAsync(new
                    {
                        type = recordType,
                        name = dnsRecordName,
                        content = content,
                        proxied = proxied
                    })
                    .ReceiveJson<DnsJsonObjects.DnsUpdateResponse>()
                    .Result;
            }
            catch(FlurlHttpException ex)
            {
                throw GenerateCloudFlareException(ex);
            }
        }

        protected Exception GenerateCloudFlareException(FlurlHttpException ex)
        {
            if (IsKeyOrToken == KeyOrToken.Token)
            {
                return new Exception($"Unable to get zone ids. If you are updating all zones, token permissions should be similar to [All zones - Zone:Read, DNS:Edit]. If your token only has permissions for specific zones, click Settings and configure the Zone IDs with a comma-separated list.");
            }
            else
            {
                var apiError = ex.GetResponseJsonAsync<CloudflareApiError>().Result;
                return new Exception(apiError.errors?.FirstOrDefault().message);
            }
        }
    }

    public enum KeyOrToken
    {
        Key,
        Token
    }
}
