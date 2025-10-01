#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include <ArduinoJson.h>

#pragma region Variables
const char* name = "YOUR DEVICE NAME";
const char* ssid = "YOUR SSID";
const char* password = "YOUR_PASSWORD";

ESP8266WebServer server(80);

struct PinMap {
  int dNumber;  // e.g., 1 for D1
  int gpio;     // e.g., D1
};

struct Action {
  const char* action;
  const char* commandType;
  const char* command;
  const char* commandArgs = "";
};

Action actions[] = {
  { "ON", "POST", "pins", "{\"pin\":\"D1\",\"action\":\"on\"}" },
  { "OFF", "POST", "pins", "{\"pin\":\"D1\",\"action\":\"off\"}" },
  { "STATUS", "GET", "pins", "pin=D1" },
  { "RESTART", "POST", "restart", "{}" },
  { "SHUTDOWN", "POST", "shutdown", "{}" }
};

// Only safe pins
PinMap availablePins[] = {
  { 1, D1 },
  { 2, D2 },
  { 5, D5 },
  { 6, D6 },
  { 7, D7 }
};

unsigned long bootMillis = 0;
#pragma endregion Variables

void setup() {
  Serial.begin(9600);
  bootMillis = millis();

  resetPins();
  connectToWifi();

  MDNS.begin(name);  // Access via http://second.local/

  server.on("/info", HTTP_GET, handleGetInfo);
  server.on("/pins", HTTP_GET, handleGetPinStatus);
  server.on("/pins", HTTP_POST, handlePostPinStatus);

  server.on("/shutdown", HTTP_POST, handleShutdown);
  server.on("/restart", HTTP_POST, handleRestart);

  server.onNotFound([]() {
    server.send(404, "text/plain", "Not found");
  });

  server.begin();
}

void loop() {
  MDNS.update();
  server.handleClient();
}

#pragma region Endpoints
void handleGetInfo() {
  server.send(200, "application/json", buildInfoJson());
}

void handleGetPinStatus() {
  if (!server.hasArg("pin")) {
    server.send(400, "text/plain", "Missing pin");
    return;
  }
  String pinStr = server.arg("pin");
  int pin = pinFromString(pinStr);
  if (pin < 0) {
    server.send(400, "text/plain", "Invalid Pin");
    return;
  }

  int value = digitalRead(pin);
  String status = (value == LOW) ? "on" : "off";

  JsonDocument doc;
  doc["pin"] = pinStr;
  doc["status"] = status;
  String json = "";
  serializeJson(doc, json);

  server.send(200, "application/json", json);
}

void handlePostPinStatus() {
  String pinStr = "";
  String action = "";
  bool parsed = false;

  if (server.arg("plain").length() > 0) {
    String body = server.arg("plain");
    JsonDocument doc;
    deserializeJson(doc, body);

    pinStr = doc["pin"].as<String>();
    action = doc["action"].as<String>();
    parsed = true;
  }

  if (!parsed) {
    server.send(400, "text/plain", "Missing pin or action");
    return;
  }

  int pin = pinFromString(pinStr);
  if (pin < 0) {
    server.send(400, "text/plain", "Invalid Pin");
    return;
  }

  action.trim();
  action.toLowerCase();
  digitalWrite(pin, action == "on" ? LOW : HIGH);
  Serial.println(pinStr + " " + action);

  JsonDocument doc;
  doc["pin"] = pinStr;
  doc["action"] = action;

  String json = "";
  serializeJson(doc, json);

  server.send(200, "application/json", json);
}

void handleRestart() {
  Serial.println("Restarting...");
  server.send(200, "text/plain", "Restarting...");
  delay(300);
  ESP.restart();
}

void handleShutdown() {
  Serial.println("Shutting down...");
  server.send(200, "text/plain", "Shutting down...");
  delay(300);
  resetPins();
  WiFi.disconnect(true);
  WiFi.mode(WIFI_OFF);
  ESP.deepSleep(0);
}

#pragma endregion Endpoints

int pinFromString(String pinStr) {
  pinStr.trim();
  pinStr.toUpperCase();
  if (pinStr.length() < 2 || pinStr[0] != 'D') return -1;

  int requestedD = pinStr.substring(1).toInt();

  // Look for requested D-number in array
  for (int i = 0; i < sizeof(availablePins) / sizeof(availablePins[0]); i++) {
    if (availablePins[i].dNumber == requestedD) {
      return availablePins[i].gpio;
    }
  }

  return -1;  // not allowed
}

void resetPins() {
  // Initialize all available pins as OUTPUT and turn OFF (HIGH)
  for (int i = 0; i < sizeof(availablePins) / sizeof(availablePins[0]); i++) {
    pinMode(availablePins[i].gpio, OUTPUT);
    digitalWrite(availablePins[i].gpio, HIGH);  // OFF for active LOW
  }
}

void connectToWifi() {
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.println("Connecting to WiFi...");
  }

  Serial.println("");
  Serial.print("Connected! IP: ");
  Serial.println(WiFi.localIP());
}

String buildInfoJson() {
  // uptime seconds
  unsigned long upSeconds = (millis() - bootMillis) / 1000UL;
  JsonDocument doc;

  doc["name"] = name;
  doc["ip"] = WiFi.localIP().toString();
  doc["uptimeSeconds"] = upSeconds;
  
  JsonArray actionsJson = doc["actions"].to<JsonArray>();
  const size_t count = sizeof(actions) / sizeof(actions[0]);
  for (size_t i = 0; i < count; i++) {
    Action action = actions[i];
    JsonObject actions_0 = actionsJson.add<JsonObject>();
    actions_0["action"] = action.action;
    actions_0["command"] = action.command;
    actions_0["commandType"] = action.commandType;
    actions_0["commandArgs"] = action.commandArgs;
  }

  String json = "";
  serializeJson(doc, json);
  return json;
}