#include <ESP8266WiFi.h>
#include <ESP8266mDNS.h>
#include <WiFiClientSecure.h>
#include <UniversalTelegramBot.h>

const char* name = "YOUR DEVICE NAME";
const char* ssid = "YOUR SSID";
const char* password = "YOUR_PASSWORD";
const char* botToken = "YOUR_BOT_TOKEN";  // Token from BotFather
const char* chatId = "YOUR_CHAT_ID";  // Your Telegram chat ID

WiFiClientSecure client;
UniversalTelegramBot bot(botToken, client);

void connectToInternet() {
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }

  Serial.println("");
  Serial.print("Connected! IP: ");
  Serial.println(WiFi.localIP());

  while (!isTelegramAvailable()) {
    Serial.print("Waiting for Telegram...");
    delay(1000);
  }

  Serial.println("Connected to the Internet");
}

bool isTelegramAvailable() {
  WiFiClientSecure testClient;
  testClient.setInsecure();

  if (!testClient.connect("api.telegram.org", 443)) {
    return false;
  }

  testClient.stop();
  return true;
}

void setup() {
  Serial.begin(9600);

  connectToInternet();

  MDNS.begin(name);  // Access via http://second.local/

  client.setInsecure();  // Simplified TLS for HTTP API Telegram
}

void loop() {
  MDNS.update();
  bool ok = bot.sendMessage(chatId, "Power is back", "");

  if (ok) {
    Serial.println("Message sent successfully!");
    WiFi.disconnect(true);
    WiFi.mode(WIFI_OFF);
    ESP.deepSleep(0);
  } else {
    Serial.println("Failed to send message!");
  }
}