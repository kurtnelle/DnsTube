using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace DnsTubeCore
{
	public class CloudFlareDnsResult : INotifyPropertyChanged
	{
		string id, type, name, content, zone_id, zone_name = string.Empty;
		int ttl;
		DateTime created_on, modified_on;
		bool locked, proxiable, proxied;
		public string Id { get { return id; } set { id = value; OnPropertyChanged(); } }
		public string Type { get { return type; } set { type = value; OnPropertyChanged(); } }
		public string Name { get { return name; } set { name = value; OnPropertyChanged(); } }
		public string Content { get { return content; } set { content = value; OnPropertyChanged(); } }
		public bool Proxiable { get { return proxiable; } set { proxiable = value; OnPropertyChanged(); } }
		public bool Proxied { get { return proxied; } set { proxied = value; OnPropertyChanged(); } }
		public int TTL { get { return ttl; } set { ttl = value; OnPropertyChanged(); } }
		public bool Locked { get { return locked; } set { locked = value; OnPropertyChanged(); } }
		public string ZoneId { get { return zone_id; } set { zone_id = value; OnPropertyChanged(); } }
		public string ZoneName { get { return zone_name; } set { zone_name = value; OnPropertyChanged(); } }
		public DateTime CreatedOn { get { return created_on; } set { created_on = value; OnPropertyChanged(); } }
		public DateTime ModifiedOn { get { return modified_on; } set { modified_on = value; OnPropertyChanged(); } }

		public CloudFlareDnsResult(DnsJsonObjects.Result r)
		{
			Id = r.id;
			Type = r.type;
			Name = r.name;
			Content = r.content;
			Proxiable = r.proxiable;
			Proxied = r.proxied;
			TTL = r.ttl;
			Locked = r.locked;
			ZoneId = r.zone_id;
			ZoneName = r.zone_name;
			CreatedOn = r.created_on;
			ModifiedOn = r.modified_on;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		public override string ToString()
		{
			return $"{Name} ({Id})";
		}
	}
}
