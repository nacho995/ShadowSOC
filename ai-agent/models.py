from pydantic import BaseModel, Field

class ThreatAnalysis(BaseModel):
    risk_level: str = Field(description="Low, Medium, High, or Critical")
    summary: str = Field(description="Brief description of the threat")
    mitre_explanation: str = Field(description="What the MITRE technique does")
    recommendation: str = Field(description="Recommendation action to take")