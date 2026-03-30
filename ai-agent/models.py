from pydantic import BaseModel, Field

class ThreatAnalysis(BaseModel):
    risk_level: str = Field(description="Low, Medium, High, or Critical")
    summary: str = Field(description="Brief description of the threat")
    mitre_explanation: str = Field(description="What the MITRE technique does")
    recommendation: str = Field(description="Recommendation action to take")

class AttackDecision(BaseModel):                                                                                                                                   
    phase: str = Field(description="Current attack phase: reconnaissance, initial_access, execution, persistence, lateral_movement, exfiltration")
    technique: str = Field(description="Attack technique: Port Scan, Brute Force, SQL Injection, RCE, SSH Exploit, XSS, DDoS, Directory Traversal")                
    mitre_id: str = Field(description="MITRE ATT&CK ID: T1046, T1110, T1190, T1059, T1133, T1071, T1048")                                                          
    target_port: str = Field(description="Target port number as string: 22, 80, 443, 3306, 8080, 3389, 21, 25")                                                                     
    reasoning: str = Field(description="Why this attack was chosen as the next step")    