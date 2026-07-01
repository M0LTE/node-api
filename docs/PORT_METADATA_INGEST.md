# Port metadata ingest & band-annotated links

node-api's ingest model is event-based (node up/down, link up/down, circuits, L2 traces). It knows which
**port** a link uses, but it never receives the port's frequency, band, mode, etc. — those come from the
node's own config (LinBPQ `M0LTEMapInfo` / `PortFreq`, XRouter port config), which node-api doesn't ingest.

This feature lets a single trusted external source push that per-port metadata in, so node-api can
annotate its links with `band` / `freqHz` / `mode`. In practice the poster is
[`packetnodes`](https://github.com/M0LTE/packetnodes), which already derives port frequencies from
operator comments and pushes the full set every ~60 seconds.

## Ingest endpoint

```
POST /api/ingest/port-metadata
Content-Type: application/json
X-Api-Key: <key>
```

The body is the **full** current set (it replaces the previous set each time):

```json
[
  {
    "node": "GB7RDG",
    "port": "3",
    "linkType": "RF",
    "freqHz": 7051600,
    "band": "40m",
    "freqSource": "reported",
    "mode": "ax.25",
    "modulation": "BPSK",
    "baud": 300,
    "bitrate": 300,
    "usage": "Mixed",
    "comment": "7051.6kHz BPSK300 IL2P+CRC"
  }
]
```

Only `node` and `port` are required. The set is held in memory (`IPortMetadataStore`); on restart it is
empty until the next push (~1 minute), which is fine — it only annotates links, never gates them.

**Responses:** `200` `{ "received": <n> }` · `400` empty body · `401` missing/invalid key.

## Authentication

The endpoint is API-key authenticated (`IngestApiKeyAttribute`) and **secure by default** — if no key is
configured it rejects everything. The key is compared in constant time.

Set the key via config `Ingest:PortMetadataApiKey` (User Secrets in development, or the
`INGEST__PORTMETADATAAPIKEY` environment variable in deployment):

```sh
dotnet user-secrets set "Ingest:PortMetadataApiKey" "<key>"
# or, in the container/systemd environment:
#   INGEST__PORTMETADATAAPIKEY=<key>
```

The same key is configured on the poster's side (packetnodes: `NodeGraph:NodeApiIngestKey`).

## Effect on `/api/network/links`

Each link is annotated (transiently — never persisted or dirty-tracked) with the band derived from its
endpoints' port metadata:

- top-level `band` on the link, and
- `band` / `freqHz` / `mode` on each endpoint in `endpoints`.

Endpoints named with an SSID that wasn't pushed fall back to the base call's metadata (e.g. `GB7RDG-2`
resolves to `GB7RDG`'s port 3). Links with no matching metadata simply have `band: null`.
