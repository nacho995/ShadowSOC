from dotenv import load_dotenv
import os

load_dotenv()

from langchain_groq import ChatGroq
from langchain_core.messages import SystemMessage, HumanMessage, ToolMessage
from tools import check_abuseipdb, lookup_mitre_technique
from models import ThreatAnalysis

llm = ChatGroq(
    model="llama-3.3-70b-versatile",
    api_key=os.environ.get("GROQ_API_KEY")
)

tools = [check_abuseipdb, lookup_mitre_technique]
tools_map = {t.name: t for t in tools}
llm_with_tools = llm.bind_tools(tools)


def run_agent(alert: str) -> str:
    messages = [
        SystemMessage(
            content="Eres un analista SOC senior. Usa las herramientas disponibles para investigar la IP y la técnica MITRE. Genera un análisis completo."),
        HumanMessage(content=alert)
    ]

    while True:
        response = llm_with_tools.invoke(messages)
        messages.append(response)

        if not response.tool_calls:
            structured_llm = llm.with_structured_output(ThreatAnalysis)
            structured = structured_llm.invoke(messages)
            return structured

        for tool_call in response.tool_calls:
            tool = tools_map[tool_call["name"]]
            result = tool.invoke(tool_call["args"])
            messages.append(ToolMessage(content=result, tool_call_id=tool_call["id"]))



if __name__ == "__main__":
      analysis = run_agent("Investiga esta alerta: IP 185.220.101.34 está realizando Brute Force en el puerto 22, severidad Critical, MITRE T1110")
      print(analysis.model_dump_json(indent=2))