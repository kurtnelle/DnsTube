# DnsTube

A Windows client for dynamically updating Cloudflare DNS entries with your public IP address.

## Features

* Can update IPv4 (A) records, IPv6 records (AAAA), or both
* Support both Cloudflare API keys and tokens
* Supports API tokens scoped to specific zones
* Does updates on an adjustable timer, e.g., every 30 minutes
* Supports minimize on load, check for updates

### Notes

1. DnsTube only updates existing Cloudflare records. It will not create or remove records.
2. DnsTube must currently be run as a logged-in user. The next release will support running it as a service.

## Building

This solution was built using Visual Studio 2019. It's probably best to use that version. The Microsoft Visual Studio Installer Projects extension was used to make the Setup project work in VS2019.

## Downloading

Head over to the [Releases](https://github.com/drittich/DnsTube/releases/latest) page to download the latest binary.

## Contributing

Contributions are welcome!

## Authors

* **D'Arcy Rittich**
* **kurtnelle**

## Roadmap

### Recently Added
* Support for Cloudflare API tokens
* Support for IPv6
* Check for updates
* Dotnet Core, command line utility that can update cloudflare based on multiple gateway/ip configurations (windows only)

### Next Up:
* Run as a Windows service
* Optional Startup shortcut creation
* Debian support of command line utility
## License

This project is licensed under the MIT License - see the [LICENSE](/LICENSE) file for details.

## Acknowledgments

Some of the UI was inspired by [CloudFlare-DDNS-Updater](https://github.com/birkett/CloudFlare-DDNS-Updater). 
