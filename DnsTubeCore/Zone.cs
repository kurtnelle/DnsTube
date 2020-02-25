using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace DnsTubeCore
{
    public class Zone
    {
        string zoneId = string.Empty;
        CloudflareClient cloudflare;
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public DateTime CreatedOn { get; protected set; }
        public DateTime ModifiedOn { get; protected set; }
        public string Status { get; protected set; }
        public bool Paused { get; protected set; }
        public string[] NameServers { get; protected set; }
        public string[] Permissions { get; protected set; }
        public string OwnerId { get; protected set; }
        public string OwnerEmail { get; protected set; }
        public string OwnerType { get; protected set; }

        public Zone(CloudflareClient client, ZoneJsonObjects.Result r)
        {
            cloudflare = client;
            Id = r.id;
            Name = r.name;
            CreatedOn = r.created_on;
            ModifiedOn = r.modified_on;
            Status = r.status;
            Paused = r.paused;
            NameServers = r.name_servers;
            Permissions = r.permissions;
            OwnerId = r.owner.id;
            OwnerEmail = r.owner.email;
            OwnerType = r.owner.owner_type;
        }

        public List<CloudFlareDnsResult> DnsEntries {
            get {
                return cloudflare.GetDnsRecordsFor(this);
            }
        }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }

}
