from langchain_core.tools import tool
import requests
import os
from dotenv import load_dotenv
from rag import search_mitre

load_dotenv()

@tool
def check_abuseipdb(ip: str) -> str:
    """Check if an IP address has been reported as malicious on AbuseIPDB."""
    response = requests.get("https://api.abuseipdb.com/api/v2/check",
                            headers={
                                "Key": os.environ.get("ABUSEIPDB_API_KEY"),
                                "Accept": "application/json"
                            },
                            params={
                                "ipAddress": ip
                            }
                            )
    data = response.json()
    result = data["data"]
    return f"IP: {result['ipAddress']}, Country: {result['countryCode']}, Abuse Score: {result['abuseConfidenceScore']}, Total Reports: {result['totalReports']}, ISP: {result['isp']}, Is Tor: {result['isTor']}"

@tool
def lookup_mitre_technique(query: str) -> str:
  """Search the MITRE ATT&CK knowledge base for information about attack techniques, mitigations and detection methods."""
  return search_mitre(query)
