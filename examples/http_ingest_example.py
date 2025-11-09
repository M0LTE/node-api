#!/usr/bin/env python3
"""
Example HTTP Ingestion Client for node-api

This script demonstrates how to submit network event datagrams via HTTP
instead of UDP. The data is processed identically through the same pipeline.

Usage:
    python http_ingest_example.py
"""

import requests
import json
import time
from datetime import datetime

# API endpoint (adjust as needed)
API_BASE_URL = "https://node-api.packet.oarc.uk"
# API_BASE_URL = "http://localhost:5000"  # For local testing

def send_single_datagram(datagram):
    """Send a single datagram via HTTP POST"""
    url = f"{API_BASE_URL}/api/ingest"
    
    try:
        response = requests.post(
            url,
            headers={"Content-Type": "application/json"},
            data=json.dumps(datagram),
            timeout=10
        )
        
        response.raise_for_status()
        result = response.json()
        
        print(f"? Datagram submitted successfully")
        print(f"  Status: {result['status']}")
        print(f"  Source IP: {result['sourceIp']}")
        print(f"  Received At: {result['receivedAt']}")
        
        return True
        
    except requests.exceptions.HTTPError as e:
        print(f"? HTTP error: {e}")
        if e.response is not None:
            print(f"  Response: {e.response.text}")
        return False
        
    except Exception as e:
        print(f"? Error: {e}")
        return False

def send_batch_datagrams(datagrams):
    """Send multiple datagrams in a single batch request"""
    url = f"{API_BASE_URL}/api/ingest/batch"
    
    try:
        response = requests.post(
            url,
            headers={"Content-Type": "application/json"},
            data=json.dumps(datagrams),
            timeout=10
        )
        
        response.raise_for_status()
        result = response.json()
        
        print(f"? Batch submitted successfully")
        print(f"  Total Received: {result['totalReceived']}")
        print(f"  Success Count: {result['successCount']}")
        print(f"  Failure Count: {result['failureCount']}")
        
        if result['failureCount'] > 0:
            print(f"  Errors: {result['errors']}")
        
        return result['failureCount'] == 0
        
    except requests.exceptions.HTTPError as e:
        print(f"? HTTP error: {e}")
        if e.response is not None:
            print(f"  Response: {e.response.text}")
        return False
        
    except Exception as e:
        print(f"? Error: {e}")
        return False

def check_service_status():
    """Check the ingestion service status"""
    url = f"{API_BASE_URL}/api/ingest/status"
    
    try:
        response = requests.get(url, timeout=5)
        response.raise_for_status()
        result = response.json()
        
        print("Service Status:")
        print(f"  Service: {result['service']}")
        print(f"  Status: {result['status']}")
        print(f"  RabbitMQ Available: {result['rabbitMq']['available']}")
        print(f"  Processing Mode: {result['rabbitMq']['mode']}")
        
        return True
        
    except Exception as e:
        print(f"? Error checking status: {e}")
        return False

def main():
    """Example usage of HTTP ingestion API"""
    
    print("=" * 60)
    print("HTTP Datagram Ingestion Example")
    print("=" * 60)
    print()
    
    # Check service status first
    print("1. Checking service status...")
    check_service_status()
    print()
    
    # Example 1: Send a NodeUpEvent
    print("2. Sending NodeUpEvent...")
    node_up = {
        "@type": "NodeUpEvent",
        "time": int(time.time()),
        "nodeCall": "TEST-1",
        "nodeAlias": "TESTNODE",
        "locator": "IO91EC",
        "latitude": 51.5074,
        "longitude": -0.1278,
        "software": "test-client",
        "version": "v1.0"
    }
    send_single_datagram(node_up)
    print()
    
    # Example 2: Send a NodeStatusReportEvent
    print("3. Sending NodeStatusReportEvent...")
    node_status = {
        "@type": "NodeStatusReportEvent",
        "time": int(time.time()),
        "nodeCall": "TEST-1",
        "nodeAlias": "TESTNODE",
        "locator": "IO91EC",
        "latitude": 51.5074,
        "longitude": -0.1278,
        "software": "test-client",
        "version": "v1.0",
        "uptimeSecs": 12345,
        "linksIn": 2,
        "linksOut": 3,
        "cctsIn": 1,
        "cctsOut": 2,
        "l3Relayed": 150
    }
    send_single_datagram(node_status)
    print()
    
    # Example 3: Send a LinkUpEvent
    print("4. Sending LinkUpEvent...")
    link_up = {
        "@type": "LinkUpEvent",
        "time": int(time.time()),
        "node": "TEST-1",
        "id": 123,
        "direction": "outgoing",
        "port": "1",
        "local": "TEST-1",
        "remote": "TEST-2"
    }
    send_single_datagram(link_up)
    print()
    
    # Example 4: Send multiple datagrams as a batch
    print("5. Sending batch of datagrams...")
    batch = [
        {
            "@type": "L2Trace",
            "time": int(time.time()),
            "reportFrom": "TEST-1",
            "l2Type": "I",
            "srce": "TEST-1",
            "dest": "TEST-2"
        },
        {
            "@type": "LinkStatus",
            "time": int(time.time()),
            "node": "TEST-1",
            "id": 123,
            "direction": "outgoing",
            "port": "1",
            "local": "TEST-1",
            "remote": "TEST-2",
            "upForSecs": 300,
            "frmsSent": 100,
            "frmsRcvd": 95,
            "frmsResent": 2,
            "frmsQueued": 0,
            "bpsTxMean": 1024,
            "bpsRxMean": 1024,
            "l2rttMs": 50
        }
    ]
    send_batch_datagrams(batch)
    print()
    
    print("=" * 60)
    print("Examples complete!")
    print()
    print("To monitor the events, subscribe to MQTT:")
    print("  mosquitto_sub -h node-api.packet.oarc.uk -t 'out/#' -v")
    print()
    print("Or check the network state via API:")
    print(f"  curl {API_BASE_URL}/api/network/nodes")
    print("=" * 60)

if __name__ == "__main__":
    main()
