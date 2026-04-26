from dotenv import load_dotenv
import os
load_dotenv()
from langchain_groq import ChatGroq
from langchain_core.messages import SystemMessage, HumanMessage, ToolMessage
from tools import check_abuseipdb, lookup_mitre_technique


llm = ChatGroq(
    model="llama-3.3-70b-versatile",
    api_key = os.environ.get("GROQ_API_KEY")
)

llm_with_tools = llm.bind_tools([check_abuseipdb, lookup_mitre_technique])
tools_map = {"check_abuseipdb": check_abuseipdb, "lookup_mitre_technique": lookup_mitre_technique}

messages = [
    SystemMessage(content="Eres un analista SOC senior. Cuando recibes una alerta, analizas la amenaza, explicas la técnica MITRE utilizada, y recomiendas acciones."),
    HumanMessage(content="Investiga esta alerta: IP 185.220.101.34 está realizando Brute Force en el puerto 22, severidad Critical, MITRE T1110")
]

response = llm_with_tools.invoke(messages)
messages.append(response)

for tool_call in response.tool_calls:
    tool = tools_map[tool_call["name"]]
    result = tool.invoke(tool_call["args"])
    messages.append(ToolMessage(content = result, tool_call_id= tool_call["id"]))

final = llm_with_tools.invoke(messages)
print(final.content)

