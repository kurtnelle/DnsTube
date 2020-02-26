Command line application to update Cloudflare DNS entries with the public ip address that has been
detected via lookup on http://ipv4bot.whatismyipaddress.com or http://ipv6bot.whatismyipaddress.com

## Parameters

\-\-hostname

	(required)
	the FQND of the target in cloudflare that must be updated. If the gateway is ipv4 the the 'A'
	type will be udpated if the gateway is ipv6 then the 'AAAA' type will be udpated.

\-\-email

	(required)
	the email address (i.e. username) of the cloudflare account

\-\-gateway 

	(optional)
	the remote gateway to use to determine the public ip address. 
	Some networks are multi homed (more than one internet connection = more than one gateway)
	and or multi stack (ipv4 and ipv6 addresses). 
	Gateway in ipv4 form (e.g. "192.168.0.1") indicates that an ipv4 stack gateway should be used.
	If the ipv4 gateway address is actually the public ip, but it is unknown (like dial-up),
	then specify it as "0.0.0.0". The application will lookup the first ipv4 address that can reach
	www.cloudflare.com. It will then use that ip as the value for the record.

	Gateway in ipv6 form (e.g "2001:4860:4802:38::75") indicates that the ipv6 stack should be used.
	Since ipv6 addressed are typically not NATed, the "gateway" address that is specified is what 
	will be used to update the cloudflare entry. If the ipv6 address is unknown or subject to change
	then specify it as "::". The application will lookup the first ipv6 address that can reach
	www.cloudflare.com. It will then use that ip as the value for the record.

\-\-apikey

	(optional if token is not specified)
	The apikey from the cloudflare portal. Treat this as a password!

\-\-token

	(optional if apikey is not specified)
	The token from the cloudflare portal. Treat this as a userid and password!!!!

Examples:

	autodetect best gateway either ipv4 or ipv6 using apikey
	--hostname home.contoso.com --email user@contoso.com --apikey C56a418065aa...

	autodetect best gateway either ipv4 or ipv6 using token
	--hostname home.contoso.com --email user@contoso.com --token DD6a418065aa...

	use specific gateway
	--hostname home.contoso.com --gateway 192.168.0.1 --email user@contoso.com --apikey C56a418065aa...

	autodetect public ipv4 (not the ipv6)
	--hostname home.contoso.com --gateway 0.0.0.0 --email user@contoso.com --apikey C56a418065aa...

	autodetect public ipv6 (not the ipv4)
	--hostname home.contoso.com --gateway :: --email user@contoso.com --apikey C56a418065aa...
