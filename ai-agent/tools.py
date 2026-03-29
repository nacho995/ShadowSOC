from langchain_core.tools import tool
import requests
import os
import logging
from dotenv import load_dotenv
from rag import search_mitre

load_dotenv()
logger = logging.getLogger(__name__)

@tool
def check_abuseipdb(ip: str) -> str:
    """Check if an IP address has been reported as malicious on AbuseIPDB."""
    api_key = os.environ.get("ABUSEIPDB_API_KEY")
    if not api_key:
        return "Error: ABUSEIPDB_API_KEY not configured"
    try:
        response = requests.get("https://api.abuseipdb.com/api/v2/check",
                                headers={
                                    "Key": api_key,
                                    "Accept": "application/json"
                                },
                                params={"ipAddress": ip},
                                timeout=10
                                )
        response.raise_for_status()
        result = response.json()["data"]
        logger.info("AbuseIPDB lookup for %s: score=%s", ip, result['abuseConfidenceScore'])
        return f"IP: {result['ipAddress']}, Country: {result['countryCode']}, Abuse Score: {result['abuseConfidenceScore']}, Total Reports: {result['totalReports']}, ISP: {result['isp']}, Is Tor: {result['isTor']}"
    except requests.RequestException as e:
        logger.error("AbuseIPDB request failed for %s: %s", ip, e)
        return f"Error querying AbuseIPDB for {ip}: {e}"

@tool
def lookup_mitre_technique(query: str) -> str:
    """Search the MITRE ATT&CK knowledge base for information about attack techniques, mitigations and detection methods."""
    return search_mitre(query)
