export type Severity = 'Low' | 'Medium' | 'High' | 'Critical';
export type Protocol = 'Tcp' | 'Udp' | 'Icmp';

export interface Alert {
  Id: string;
  OriginIp: string;
  DestinationIp: string;
  TypeOfAttack: string;
  Severity: Severity;
  Protocol: Protocol;
  SourcePort: number;
  DestinationPort: number;
  MitreID: string;
  WhenStarted: string;
  WhenEnded: string;
  Latitude?: number;
  Longitude?: number;
  Country?: string;
  Timestamp?: Date;
}
export interface ThreatAnalysis {
  risk_level: string;
  summary: string;
  mitre_explanation: string;
  recommendation: string;
}

