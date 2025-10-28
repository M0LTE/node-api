# IP Address and GeoIP Feature

## Overview

This feature adds IP address tracking and GeoIP location lookup for packet radio nodes. IP addresses are **obfuscated** (showing only the last two octets for IPv4) to preserve privacy while still providing useful network information.

## Features

### Server-Side IP Obfuscation
- IP addresses are obfuscated **on the server** before being stored or sent to clients
- IPv4: Shows only last 2 octets (e.g., `192.168.1.100` ? `***.***. 1.100`)
- IPv6: Shows only last half (e.g., `2001:db8::1` ? `****:****::1`)
- Full IP addresses are **never** stored in the database or transmitted to clients

### GeoIP Location Lookup
- Uses MaxMind GeoLite2 database for IP geolocation
- Provides:
  - Country code (e.g., "GB", "US")
  - Country name (e.g., "United Kingdom", "United States")
  - City (e.g., "London", "New York")
- Geographic information is stored and displayed alongside node data

### Display on Node Details Page
The `/node.html` page now shows (when available):
- Obfuscated IP address (last 2 octets only)
- Geographic location (city, country)
- Last update time for IP address

## Setup

### 1. Database Migration

Run the SQL migration to add the new columns to the `nodes` table:

```bash
mysql -u your_user -p your_database < migrations/001_add_ip_and_geoip_columns.sql
```

The migration adds:
- `ip_address_obfuscated` VARCHAR(45) - Stores obfuscated IP
- `geoip_country_code` VARCHAR(2) - ISO country code
- `geoip_country_name` VARCHAR(100) - Full country name
- `geoip_city` VARCHAR(100) - City name
- `last_ip_update` DATETIME - When IP was last updated
- Index on `geoip_country_code` for faster queries

### 2. GeoIP Database (Optional but Recommended)

Download the **GeoLite2 City** database from MaxMind:

1. Sign up for a free account at https://dev.maxmind.com/geoip/geolite2-free-geolocation-data
2. Download `GeoLite2-City.mmdb`
3. Place it in one of these locations:
   - `/usr/share/GeoIP/GeoLite2-City.mmdb` (Linux standard)
   - `/var/lib/GeoIP/GeoLite2-City.mmdb` (Alternative Linux)
   - Application directory: `./GeoLite2-City.mmdb`
   - Custom path via environment variable: `GEOIP_DB_PATH`

**Note:** The service will work without the GeoIP database, but location information will not be available.

### 3. No Code Changes Required

The feature is automatically enabled once:
- The database migration is applied
- (Optional) The GeoIP database is in place
- The service is restarted

## How It Works

### IP Address Capture
1. When a UDP datagram is received, the source IP address is extracted from `UdpReceiveResult.RemoteEndPoint`
2. The IP address is associated with the reporting callsign from the datagram
3. IP is immediately obfuscated using the `GeoIpService.ObfuscateIpAddress()` method
4. **Only the obfuscated IP** is stored in memory and database

### GeoIP Lookup
1. If the GeoLite2 database is available, a lookup is performed on the full IP address
2. Geographic information (country, city) is extracted
3. Geographic data is stored in the `NodeState` and database
4. The full IP address is **never** stored - only used for the lookup

### Client Display
1. Frontend fetches node data via API (e.g., `/api/nodes/{callsign}`)
2. Response includes obfuscated IP and GeoIP fields
3. UI displays:
   - `ipAddressObfuscated`: "***.***.1.100"
   - Location: "London, United Kingdom" (if available)
   - Last update time

## Privacy Considerations

### IP Address Privacy
- **Full IP addresses are never stored** in the database
- **Full IP addresses are never sent** to clients
- Only the **last two octets** (IPv4) or last half (IPv6) are visible
- This provides network diagnostic value while preserving privacy

### What Can Be Determined
- **Can**: Identify if multiple nodes are on the same /16 network
- **Can**: See approximate geographic location (city/country level)
- **Cannot**: Determine exact IP address
- **Cannot**: Connect directly to the node using the obfuscated IP

### Compliance
- This approach complies with privacy best practices
- IP obfuscation happens **server-side**, so clients cannot access full IPs
- Geographic data is publicly available via ISP assignments

## API Response Example

```json
{
  "callsign": "M0LTE",
  "alias": "TESTER",
  "locator": "IO91wm",
  "latitude": 51.4934,
  "longitude": -0.1276,
  "software": "xrlin",
  "version": "504j",
  "status": "Online",
  "ipAddressObfuscated": "***.***. 1.100",
  "geoIpCountryCode": "GB",
  "geoIpCountryName": "United Kingdom",
  "geoIpCity": "London",
  "lastIpUpdate": "2025-01-02T10:30:00Z"
}
```

## Configuration

### Environment Variable
Set a custom GeoIP database location:
```bash
export GEOIP_DB_PATH=/custom/path/to/GeoLite2-City.mmdb
```

### Logging
The service logs GeoIP initialization:
- Success: `Loaded GeoIP database from {Path}`
- Not found: `GeoIP database not found. GeoIP lookups will be disabled.`
- Error: `Failed to load GeoIP database from {Path}`

## Maintenance

### Updating GeoIP Database
MaxMind updates the GeoLite2 database regularly. To update:
1. Download the latest `GeoLite2-City.mmdb`
2. Replace the existing file
3. Restart the service

### Database Cleanup
The `last_ip_update` field tracks when IP address information was last refreshed. You can use this to:
- Identify stale data
- Trigger re-validation
- Monitor IP address changes

## Testing

### Manual Testing
1. Send a UDP datagram from a known IP address
2. Check the node details page (`/node.html?callsign=YOURCALL`)
3. Verify IP shows only last 2 octets
4. Verify geographic location (if GeoIP database is present)

### Verification Queries
```sql
-- Check IP data for a specific node
SELECT 
  callsign, 
  ip_address_obfuscated, 
  geoip_country_code, 
  geoip_country_name, 
  geoip_city, 
  last_ip_update 
FROM nodes 
WHERE callsign = 'M0LTE';

-- Nodes by country
SELECT callsign, geoip_city, last_seen
FROM nodes 
WHERE geoip_country_code = 'GB' 
ORDER BY last_seen DESC;
```

## Troubleshooting

### GeoIP Not Working
- Check if `GeoLite2-City.mmdb` exists in expected locations
- Check logs for "Loaded GeoIP database from" message
- Verify file permissions (must be readable by the service)

### IP Address Not Showing
- Ensure the node is actively sending UDP datagrams
- Check `last_ip_update` field in database
- Verify the UDP listener is running
- Check that callsign in datagram matches the node being viewed

### Old IP Address Data
- IP address is only updated when new datagrams are received
- Check `last_ip_update` to see when it was last refreshed
- If a node changes IP, it will be updated on next datagram

## Future Enhancements

Potential improvements:
- IP change history tracking
- Alerts when IP address changes
- Network topology visualization based on IP ranges
- ASN (Autonomous System Number) lookup
- ISP information display
- Distance calculations between nodes
