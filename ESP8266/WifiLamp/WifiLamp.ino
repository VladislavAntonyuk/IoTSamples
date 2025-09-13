#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>

const char* name = "YOUR DEVICE NAME";
const char* ssid = "YOUR SSID";
const char* password = "YOUR_PASSWORD";

ESP8266WebServer server(80);

struct PinMap {
    int dNumber;   // e.g., 1 for D1
    int gpio;      // e.g., D1
};

struct Action {
  const char* action;
  const char* command;
};

// Action list (extend here if more are added)
Action actions[] = {
  {"ON", "/pins?pin=D1&action=on"},
  {"OFF", "/pins?pin=D1&action=off"},
  {"RESTART", "/restart"},
  {"SHUTDOWN", "/shutdown"}
};

// Only safe pins
PinMap availablePins[] = {
    {1, D1},
    {2, D2},
    {5, D5},
    {6, D6},
    {7, D7}
};

unsigned long bootMillis = 0;

int pinFromString(String pinStr) {
  pinStr.trim();
  pinStr.toUpperCase();
  if (pinStr.length() < 2 || pinStr[0] != 'D') return -1;

  int requestedD = pinStr.substring(1).toInt();

  // Look for requested D-number in array
  for (int i = 0; i < sizeof(availablePins)/sizeof(availablePins[0]); i++) {
      if (availablePins[i].dNumber == requestedD) {
          return availablePins[i].gpio;
      }
  }

  return -1; // not allowed
}

void resetPins(){
  // Initialize all available pins as OUTPUT and turn OFF (HIGH)
  for (int i = 0; i < sizeof(availablePins)/sizeof(availablePins[0]); i++) {
    pinMode(availablePins[i].gpio, OUTPUT);
    digitalWrite(availablePins[i].gpio, HIGH); // OFF for active LOW
  }
}

void connectToWifi(){
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }

  Serial.println("");
  Serial.print("Connected! IP: ");
  Serial.println(WiFi.localIP());
}

String buildActionsJson() {
  String json = "[";
  const size_t count = sizeof(actions)/sizeof(actions[0]);
  for (size_t i = 0; i < count; i++) {
    json += "{\"action\":\""; json += actions[i].action; json += "\",\"command\":\""; json += actions[i].command; json += "\"}";
    if (i < count - 1) json += ",";
  }
  json += "]";
  return json;
}

String buildInfoJson() {
  // uptime seconds
  unsigned long upSeconds = (millis() - bootMillis) / 1000UL;
  String json = "{";
  json += "\"name\":\""; json += name; json += "\",";
  json += "\"ip\":\""; json += WiFi.localIP().toString(); json += "\",";
  json += "\"uptimeSeconds\":"; json += upSeconds; json += ",";
  json += "\"actions\":"; json += buildActionsJson();
  json += "}";
  return json;
}

void setup() {
  Serial.begin(9600);
  bootMillis = millis();

  resetPins();
  connectToWifi();

  MDNS.begin(name); // Access via http://second.local/

  // Info endpoint with device metadata
  server.on("/info", [](){
    server.send(200, "application/json", buildInfoJson());
  });

  server.on("/pins", []() {
    if (!server.hasArg("pin") || !server.hasArg("action")) {
      server.send(400, "text/plain", "Missing pin or action");
      return;
    }

    String pinStr = server.arg("pin");      // e.g., "D1"

    String action = server.arg("action");   // "on" or "off"
    int pin = pinFromString(pinStr);
    if (pin < 0) {
      server.send(400, "text/plain", "Invalid Pin");
      return;
    }

    action.trim();
    action.toLowerCase();
    digitalWrite(pin, action == "on" ? LOW : HIGH);
    Serial.println(pinStr + " " + action);
    server.send(200, "text/plain", pinStr + " " + action);
  });

  server.on("/shutdown", []() {
    Serial.println("Shutting down...");
    server.send(200, "text/plain", "Shutting down...");
    delay(300);
    resetPins();
    WiFi.disconnect(true);
    WiFi.mode(WIFI_OFF);
    ESP.deepSleep(0);
  });

  server.on("/restart", [](){
    Serial.println("Restarting...");
    server.send(200, "text/plain", "Restarting...");
    delay(300);
    ESP.restart();
  });

  server.onNotFound([]() {
    server.send(404, "text/plain", "Not found");
  });

  server.begin();
}

void loop() {
  MDNS.update();
  server.handleClient();
}