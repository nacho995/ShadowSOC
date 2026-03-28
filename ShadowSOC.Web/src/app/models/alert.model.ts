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
