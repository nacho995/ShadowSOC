import logging
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from agent import run_agent

logging.basicConfig(level=logging.INFO)
app = FastAPI()


class AlertRequest(BaseModel):
    alert: str


@app.post("/analyze")                                                                                                                                              
def analyze_alert(request: AlertRequest):
    try:                                                                                                                                                           
        result = run_agent(request.alert)
        return result.model_dump()                                                                                                                                 
    except Exception as e:                                   
        logging.error("Agent failed: %s", e, exc_info=True)
        raise HTTPException(              
            status_code=500,                                
            detail="Internal server error"                   
        )                                                                                                                                                          
                


@app.get("/health")
def health():
    return {"status": "ok"}
