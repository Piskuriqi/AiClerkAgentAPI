# AiClerkAgentAPI

**AiClerkAgentAPI** ist eine ASP.NET Core Web API, die als Chat-Assistent für Produktberatung im E-Commerce dient.  
Sie nutzt Semantic Kernel und OpenAI, um Nutzereingaben zu verstehen und passende Produktinformationen dynamisch zu liefern.  
Über Plugins kann die KI auf verschiedene Shopfunktionen wie Produktsuche, Kategorieübersicht und Empfehlungen zugreifen.

---

## **Inhalt**

- [Projektüberblick](#projektüberblick)
- [Architektur](#architektur)
- [Ablauf einer Chat-Anfrage](#ablauf-einer-chat-anfrage)
- [Wichtige Komponenten](#wichtige-komponenten)
- [Verwendete Technologien](#verwendete-technologien)
- [Sicherheitsaspekte](#sicherheitsaspekte)
- [Erweiterbarkeit](#erweiterbarkeit)

---

## Projektüberblick

Dieses Projekt stellt eine Web-API bereit, über die Nutzer in natürlicher Sprache mit einem Chat-Assistenten kommunizieren können.  
Der Assistent versteht Anfragen wie „Zeig mir neue Produkte für das Home-Office“ oder „Hast du Geschenke für einen Geburtstag?“.  
Die Antworten basieren auf OpenAI (GPT-4o), erweitert um Shop-Plugins, die gezielt Produkte aus einer Produktdatenbank (Dummy API) filtern.

Der Chat speichert Konversationen für eine Session, sodass ein fortlaufender Dialog möglich ist.

---

## Architektur

**Hauptbestandteile:**

- **Controllers:**  
  - `ChatController` – nimmt Chat-Nachrichten entgegen und verwaltet den Session-Verlauf.
- **Services:**  
  - `ProductService` – lädt Produktdaten von einer externen Dummy-API und hält sie im Speicher.
- **Plugins:**  
  - `ShopPlugin` – stellt Methoden bereit, die von der KI gezielt aufgerufen werden können (z.B. Produktsuche, Kategorieübersicht etc).
- **Modelle:**  
  - Beschreiben Produkte, Anfragen, Historie usw.
- **Semantic Kernel/OpenAI:**  
  - Nutzt GPT-4o, kann aber durch Plugins auf spezifische Shopdaten zugreifen.
- **Caching:**  
  - MemoryCache speichert die Chat-Historie für jede Konversation.




  ## Ablauf einer Chat-Anfrage

1. **Client** sendet POST `/api/chat` mit einer Nachricht.
2. **ChatController** prüft, ob eine ConversationId mitgegeben wurde – sonst wird eine neue erstellt.
3. Die bisherige Chat-Historie wird aus dem Cache geladen (oder neu erstellt).
4. Die neue Nachricht des Users wird zur Historie hinzugefügt.
5. Der **Semantic Kernel** verarbeitet die Konversation, nutzt dabei ggf. Funktionen des **ShopPlugins** (z.B. Produktsuche).
6. Die generierte Antwort der KI wird als neue Nachricht gespeichert.
7. Der aktualisierte Verlauf wird wieder im Cache gespeichert.
8. Die API gibt die Antwort und die aktuelle ConversationId zurück.

---

## Wichtige Komponenten

### 1. **ChatController**
- API-Controller für die Route `/api/chat`
- Nimmt Anfragen entgegen, verwaltet die Session und Chat-Historie
- Bindet OpenAI/Semantic Kernel und ShopPlugin ein

### 2. **ShopPlugin**
- Stellt Funktionen bereit, die Semantic Kernel gezielt aufrufen kann
- z.B. Suche nach Produkten, Kategorien, Empfehlungen
- Ermöglicht der KI, echte Produktdaten in Antworten einzubauen

### 3. **ProductService**
- Holt Produktdaten einmalig von einer externen API (`https://dummyjson.com/products`)
- Speichert Produktliste im Speicher, um Performance zu verbessern
- Dient als Datenquelle für das ShopPlugin

### 4. **Modelle**
- `ProductModel`: Produktdaten (Name, Preis, Kategorie, Bewertung etc.)
- `ChatRequest`: Nutzereingaben mit ConversationId
- `MetaModel`: Metadaten zu Produkten

---

## Verwendete Technologien

- **ASP.NET Core Web API**  
- **Microsoft Semantic Kernel** (OpenAI GPT-4o)
- **MemoryCache** für Sessions
- **Swagger** für API-Dokumentation
- **HttpClient** für externe Datenquellen
- **Dependency Injection** für flexible Architektur

---

## Sicherheitsaspekte

- **API-Schlüssel:**  
  Aktuell ist der OpenAI-API-Key im Code hinterlegt.  
  **Empfehlung:** In Produktion unbedingt in Umgebungsvariablen oder Secret Manager auslagern!

- **MemoryCache:**  
  Speichert Chat-Historie im Arbeitsspeicher (nur temporär, nicht persistent).

---

## Erweiterbarkeit

- **Plugins:**  
  Neue Plugins können einfach als zusätzliche C#-Klassen eingebunden und im Kernel registriert werden.
- **Externe Datenquellen:**  
  ProductService kann auf echte Produktdaten umgestellt werden.
- **Frontend:**  
  Ein passendes Web-Frontend oder Chatbot-UI kann leicht angebunden werden (z.B. über React, Angular, mobile App).

---

## Beispielaufruf

```http
POST /api/chat
Content-Type: application/json

{
  "ConversationId": "", // leer für neue Session, sonst von vorherigem Response übernehmen
  "Message": "Hast du neue Produkte für einen Geburtstag?"
}



Antwort
{
  "ConversationId": "ef3e...123", 
  "Reply": "Hier sind neue Geschenkideen für einen Geburtstag: ..."
}