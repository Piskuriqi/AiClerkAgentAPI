const chatLog = document.getElementById("chat-log")!;
const form = document.getElementById("chat-form") as HTMLFormElement;
const input = document.getElementById("user-input") as HTMLInputElement;

// Hole die ConversationId oder erstelle eine neue
let conversationId: string = localStorage.getItem("conversationId") || crypto.randomUUID();
localStorage.setItem("conversationId", conversationId);

// Inaktivitäts-Timer
let inactivityTimer: ReturnType<typeof setTimeout>;
resetInactivityTimer(); // sofort starten

function resetInactivityTimer() {
  clearTimeout(inactivityTimer);
  inactivityTimer = setTimeout(() => {
    if (conversationId) {
      fetch(`https://localhost:7240/api/chat/${conversationId}`, { method: "DELETE" });
      localStorage.removeItem("conversationId");
      conversationId = crypto.randomUUID(); // neue Konversation starten
      localStorage.setItem("conversationId", conversationId);
      appendMessage("bot", "🕒 Deine Sitzung ist abgelaufen. Eine neue Konversation wurde gestartet.");
    }
  }, 30 * 60 * 1000); // 30 Minuten
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  const message = input.value.trim();
  if (!message) return;

  appendMessage("user", message);
  input.value = "";
  input.disabled = true;
  form.querySelector("button")!.disabled = true;

  appendMessage("bot", "⏳ Bot schreibt...");

  try {
    const response = await fetch("https://localhost:7240/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ conversationId, message }),
    });

    removeLastBotMessage(); // Ladeanzeige entfernen

    if (response.ok) {
      const data = await response.json();
      if (data.conversationId) {
        conversationId = data.conversationId;
        localStorage.setItem("conversationId", conversationId);
      }
      appendMessage("bot", data.reply || "Keine Antwort vom Bot.");
    } else {
      appendMessage("bot", "Fehler beim Server.");
    }
  } catch (error) {
    removeLastBotMessage();
    appendMessage("bot", "Verbindungsfehler. API nicht erreichbar.");
    console.error(error);
  } finally {
    input.disabled = false;
    form.querySelector("button")!.disabled = false;
    input.focus();
    resetInactivityTimer(); // Timer neu starten
  }
});

function appendMessage(sender: "user" | "bot", text: string) {
  const wrapper = document.createElement("div");
  wrapper.className = sender;

  const bubble = document.createElement("div");
  bubble.textContent = text;

  wrapper.appendChild(bubble);
  chatLog.insertBefore(wrapper, chatLog.firstChild); // bei column-reverse: oben einfügen
}

function removeLastBotMessage() {
  const messages = chatLog.querySelectorAll(".bot");
  if (messages.length > 0) {
    chatLog.removeChild(messages[0]); // neueste (ganz oben im DOM) entfernen
  }
}
