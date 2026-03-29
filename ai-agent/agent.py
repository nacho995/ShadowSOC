from dotenv import load_dotenv
import os
import logging

load_dotenv()

from langchain_groq import ChatGroq
from langchain_core.messages import SystemMessage, HumanMessage, ToolMessage
from tools import check_abuseipdb, lookup_mitre_technique
from models import ThreatAnalysis

logger = logging.getLogger(__name__)

GROQ_API_KEY = os.environ.get("GROQ_API_KEY")
if not GROQ_API_KEY:
    raise RuntimeError("Missing required env var: GROQ_API_KEY")

MAX_TOOL_ROUNDS = 5

llm = ChatGroq(
    model="llama-3.3-70b-versatile",
    api_key=GROQ_API_KEY
)

tools = [check_abuseipdb, lookup_mitre_technique]
tools_map = {t.name: t for t in tools}
llm_with_tools = llm.bind_tools(tools)


def run_agent(alert: str) -> ThreatAnalysis:
    messages = [
        SystemMessage(
            content="Eres un analista SOC senior. Usa las herramientas disponibles para investigar la IP y la técnica MITRE. Genera un análisis completo."),
        HumanMessage(content=alert)
    ]

    for round_num in range(MAX_TOOL_ROUNDS):
        response = llm_with_tools.invoke(messages)
        messages.append(response)

        if not response.tool_calls:
            structured_llm = llm.with_structured_output(ThreatAnalysis)
            return structured_llm.invoke(messages)

        for tool_call in response.tool_calls:
            tool_name = tool_call["name"]
            if tool_name not in tools_map:
                logger.warning("Agent requested unknown tool: %s", tool_name)
                messages.append(ToolMessage(content=f"Error: tool '{tool_name}' not found", tool_call_id=tool_call["id"]))
                continue
            tool = tools_map[tool_name]
            logger.info("Round %d: calling tool %s with %s", round_num + 1, tool_name, tool_call["args"])
            result = tool.invoke(tool_call["args"])
            messages.append(ToolMessage(content=result, tool_call_id=tool_call["id"]))

    logger.warning("Agent hit max tool rounds (%d), forcing response", MAX_TOOL_ROUNDS)
    structured_llm = llm.with_structured_output(ThreatAnalysis)
    return structured_llm.invoke(messages)


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    analysis = run_agent("Investiga esta alerta: IP 185.220.101.34 está realizando Brute Force en el puerto 22, severidad Critical, MITRE T1110")
    print(analysis.model_dump_json(indent=2))
