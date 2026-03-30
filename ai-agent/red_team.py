
from dotenv import load_dotenv                                                                                                                                     
import os
import json                                                                                                                                                        
import time     
import logging
import uuid
from datetime import datetime, timezone
load_dotenv()

from langchain_groq import ChatGroq                                                                                                                                
from langchain_core.messages import SystemMessage, HumanMessage
from models import AttackDecision                                                                                                                                  
from confluent_kafka import Producer
                                                                                                                                                                    
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)                                                                                                                               
                
llm = ChatGroq(
    model="llama-3.3-70b-versatile",
    api_key=os.environ.get("GROQ_API_KEY")
)                                                                                                                                                                  

structured_llm = llm.with_structured_output(AttackDecision)                                                                                                        
                
KAFKA_BROKER = os.environ.get("KAFKA_BOOTSTRAP_SERVERS", "localhost:9093")                                                                                         
producer = Producer({"bootstrap.servers": KAFKA_BROKER})
                                                                                                                                                                    
campaign_state = {
    "phase": "reconnaissance",
    "attacks_executed": [],
    "detected_count": 0,                                                                                                                                           
    "target_ip": "10.0.0.1"
}                                                                                                                                                                  
                
SYSTEM_PROMPT = """You are an advanced red team operator simulating an APT (Advanced Persistent Threat) campaign.                                                  
                
Your goal is to compromise the target network by progressing through these phases IN ORDER:                                                                        
1. reconnaissance - Port scanning to discover services
2. initial_access - Exploit vulnerabilities or brute force credentials                                                                                             
3. execution - Run commands on compromised systems (RCE, web shells)
4. persistence - Maintain access (SSH backdoors, scheduled tasks)                                                                                                  
5. lateral_movement - Move to other systems in the network                                                                                                         
6. exfiltration - Extract sensitive data                                                                                                                           
                                                                                                                                                                    
Rules:                                                                                                                                                             
- Progress to the next phase only after 2-3 attacks in the current phase
- Choose realistic attack techniques for each phase                                                                                                                
- Vary your target ports and techniques
- If detected multiple times, adapt your strategy (change techniques, slow down)                                                                                   
"""                                                                                                                                                                
FALLBACK_IPS = [                                                                                                                                                   
    "185.220.101.34", "103.235.46.39", "77.88.55.242",
    "45.148.10.174", "89.248.167.131", "222.186.15.96"                                                                                                             
]                                                                                                                                                                  
                                                                                                                                                                    
def send_attack(decision: AttackDecision):                                                                                                                         
    import random
    event = {                                                                                                                                                      
        "Id": str(uuid.uuid4()),
        "OriginIp": random.choice(FALLBACK_IPS),
        "DestinationIp": campaign_state["target_ip"],                                                                                                              
        "TypeOfAttack": decision.technique,
        "Severity": "Critical" if decision.phase in ["execution", "exfiltration"] else "High",                                                                     
        "Protocol": "Tcp",                                                                                                                                         
        "SourcePort": random.randint(1024, 65535),                                                                                                                 
        "DestinationPort": int(decision.target_port),                                                                                                                   
        "MitreID": decision.mitre_id,
        "WhenStarted": datetime.now(timezone.utc).isoformat(),                                                                                                     
        "WhenEnded": datetime.now(timezone.utc).isoformat()                                                                                                        
    }
    producer.produce("security-events", key=event["Id"], value=json.dumps(event))                                                                                  
    producer.flush()                                                                                                                                               
    logger.info("ATTACK: [%s] %s -> port %s (%s)", decision.phase, decision.technique, decision.target_port, decision.mitre_id)
    return event                                                                                                                                                   
                
                                                                                                                                                                    
def run_campaign():
    messages = [SystemMessage(content=SYSTEM_PROMPT)]
                                                                                                                                                                    
    while campaign_state["phase"] != "complete":
        state_summary = f"""                                                                                                                                       
Current campaign state:                                                                                                                                            
- Phase: {campaign_state['phase']}
- Attacks executed: {json.dumps(campaign_state['attacks_executed'][-5:])}                                                                                          
- Times detected by Blue Team: {campaign_state['detected_count']}                                                                                                  
- Target: {campaign_state['target_ip']}
                                                                                                                                                                    
Decide your next attack."""                                                                                                                                        

        messages.append(HumanMessage(content=state_summary))                                                                                                       
        decision = structured_llm.invoke(messages)
        logger.info("DECISION: %s", decision.model_dump_json())                                                                                                    

        send_attack(decision)                                                                                                                                      
        campaign_state["attacks_executed"].append({
            "phase": decision.phase,                                                                                                                               
            "technique": decision.technique,
            "port": decision.target_port                                                                                                                           
        })      

        # Advance phase after 3 attacks in current phase                                                                                                           
        current_phase_attacks = [a for a in campaign_state["attacks_executed"] if a["phase"] == decision.phase]
        if len(current_phase_attacks) >= 3:                                                                                                                        
            phases = ["reconnaissance", "initial_access", "execution", "persistence", "lateral_movement", "exfiltration", "complete"]
            idx = phases.index(campaign_state["phase"])                                                                                                            
            campaign_state["phase"] = phases[idx + 1]
            logger.info("PHASE ADVANCE: -> %s", campaign_state["phase"])                                                                                           
                                                                                                                                                                    
        time.sleep(8)
                                                                                                                                                                    
                
if __name__ == "__main__":
    run_campaign()