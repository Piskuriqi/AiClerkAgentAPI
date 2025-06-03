# AiClerkAgentAPI

**AiClerkAgentAPI** is an ASP.NET Core Web API that acts as a chat assistant for product consultation in e-commerce scenarios.  
It leverages Semantic Kernel and OpenAI to interpret user queries and deliver relevant product information dynamically.  
Via plugins, the AI can access various shop functions like product search, category listing, and personalized recommendations.

---

## **Contents**

- [Project Overview](#project-overview)
- [Architecture](#architecture)
- [How a Chat Request Works](#how-a-chat-request-works)
- [Key Components](#key-components)
- [Technologies Used](#technologies-used)
- [Security Aspects](#security-aspects)
- [Extensibility](#extensibility)
- [Sample Request](#sample-request)
- [Contact & Notes](#contact--notes)

---

## Project Overview

This project provides a Web API that enables users to communicate with a chat assistant in natural language.  
The assistant understands requests such as “Show me new products for my home office” or “Do you have gift ideas for a birthday?”  
Responses are powered by OpenAI (GPT-4o), extended with shop plugins that fetch and filter product information from a sample product database (dummy API).

The chat stores the conversation history for each session, allowing continuous and contextual dialogues.

---

## Architecture

**Main Components:**

- **Controllers:**  
  - `ChatController` – Handles incoming chat messages and manages session history.
- **Services:**  
  - `ProductService` – Loads product data from an external dummy API and caches it in memory.
- **Plugins:**  
  - `ShopPlugin` – Provides methods that can be directly called by the AI (e.g., product search, listing categories).
- **Models:**  
  - Describe products, requests, chat history, etc.
- **Semantic Kernel/OpenAI:**  
  - Uses GPT-4o, but can also invoke plugins for specific shop queries.
- **Caching:**  
  - MemoryCache holds the conversation history per session.



 ## How a Chat Request Works

1. **Client** sends a POST request to `/api/chat` with a message.
2. **ChatController** checks if a ConversationId is provided – if not, it creates a new one.
3. The current chat history is loaded from the cache (or a new one is started).
4. The user's new message is added to the history.
5. The **Semantic Kernel** processes the conversation, optionally invoking **ShopPlugin** functions (e.g., product search).
6. The AI-generated response is added as an assistant message to the history.
7. The updated history is saved back into the cache.
8. The API returns the response along with the current ConversationId.

---

## Key Components

### 1. **ChatController**
- Main API controller at `/api/chat`
- Handles requests, manages session and chat history
- Integrates with OpenAI/Semantic Kernel and the ShopPlugin

### 2. **ShopPlugin**
- Provides functions that can be invoked by Semantic Kernel
- Enables features like product search, category listing, and personalized recommendations
- Allows the AI to include real product data in its responses

### 3. **ProductService**
- Loads product data from an external API (`https://dummyjson.com/products`)
- Stores the product list in memory for better performance
- Acts as the data source for ShopPlugin functions

### 4. **Models**
- `ProductModel`: Holds product information (name, price, category, rating, etc.)
- `ChatRequest`: User request including ConversationId and message
- `MetaModel`: Metadata about products

---

## Technologies Used

- **ASP.NET Core Web API**  
- **Microsoft Semantic Kernel** (with OpenAI GPT-4o)
- **MemoryCache** for session management
- **Swagger** for API documentation
- **HttpClient** for connecting to external data sources

---

## Security Aspects

- **API Keys:**  
  The OpenAI API key is currently hardcoded in the source code.  
  **Recommendation:** In production, move all secrets to environment variables or a secure vault!

- **MemoryCache:**  
  Conversation history is stored in memory (temporary, not persisted long-term).

---

## Extensibility

- **Plugins:**  
  New plugins can be added easily as C# classes and registered with the Semantic Kernel.
- **External Data Sources:**  
  ProductService can be adapted to fetch real product data.
- **Frontend Integration:**  
  The API can be easily connected to a web frontend or chatbot UI (e.g., React, Angular, mobile app).

---

## Sample Request

```http
POST /api/chat
Content-Type: application/json

{
  "ConversationId": "", // leave empty for a new session, or use from previous responses
  "Message": "Do you have any new products suitable for a birthday?"
}



Sample Response:
{
  "ConversationId": "ef3e...123", 
  "Reply": "Here are some new gift ideas for a birthday: ..."
}