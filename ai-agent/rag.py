import json
import os
from langchain_community.vectorstores import Chroma
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_core.documents import Document

CHROMA_DIR = os.path.join(os.path.dirname(__file__), "chroma", "db")
DATA_FILE = os.path.join(os.path.dirname(__file__), "data", "mitre_techniques.json")

embeddings = HuggingFaceEmbeddings(model_name="all-MiniLM-L6-v2")

if os.path.exists(CHROMA_DIR):
    vectorstore = Chroma(persist_directory=CHROMA_DIR, embedding_function=embeddings)
else:
    with open(DATA_FILE) as f:
        techniques = json.load(f)

    documents = []
    for tech in techniques:
        text = f"{tech['id']} - {tech['name']}\n{tech['description']}\nMitigation: {tech['mitigation']}\nDetection: {tech['detection']}"
        doc = Document(page_content=text, metadata={"id": tech["id"], "name": tech["name"]})
        documents.append(doc)

    vectorstore = Chroma.from_documents(documents, embeddings, persist_directory=CHROMA_DIR)


def search_mitre(query: str) -> str:
    results = vectorstore.similarity_search(query, k=2)
    return "\n\n".join([doc.page_content for doc in results])


if __name__ == "__main__":
    print(search_mitre("Brute Force SSH Attack"))
