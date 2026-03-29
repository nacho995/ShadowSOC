from fastapi import FastAPI
from pydantic import BaseModel
from agent import run_agent

app = FastAPI()


class AlertRequest(BaseModel):
    alert: str


@app.post("/analyze")
def analyze_alert(request: AlertRequest):
    result = run_agent(request.alert)
    return result.model_dump()