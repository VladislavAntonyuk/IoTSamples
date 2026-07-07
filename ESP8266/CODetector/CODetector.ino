#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include <ArduinoJson.h>

#pragma region Configuration
const char* name = "CODetector";
const char* ssid = "";
const char* password = "!";

const int analogPin = A0;   
const int digitalPin = D1;

// CALIBRATION:
int baseline = 45;       
const int DANGER_VAL = 200; 

const unsigned long CLEANING_PHASE = 60000;  // 60s @ 5V
const unsigned long MEASURING_PHASE = 90000; // 90s @ 1.4V (simulated)
#pragma endregion

#pragma region Globals
ESP8266WebServer server(80);
unsigned long bootMillis = 0;
unsigned long stateStartTime = 0;
unsigned long lastLoopTick = 0;
bool isCleaning = true;

struct SensorData {
    int rawA = 0;
    int rawD = 1;
    float coPercent = 0.0;
    const char* phase = "Warm-up";
    const char* severity = "SAFE";
} currentReadings;

struct Action {
  const char* action;
  const char* commandType;
  const char* command;
  const char* commandArgs;
};

const Action actions[] = {
  { "STATUS", "GET", "status", "" },
  { "RESTART", "POST", "restart", "{}" },
  { "SHUTDOWN", "POST", "shutdown", "{}" }
};
#pragma endregion

void setup() {
  Serial.begin(9600);
  bootMillis = millis();
  stateStartTime = bootMillis;
  
  pinMode(digitalPin, INPUT);
  
  connectToWifi();
  if (MDNS.begin(name)) {
    MDNS.addService("http", "tcp", 80);
  }

  server.on("/info", HTTP_GET, handleGetInfo);
  server.on("/status", HTTP_GET, handleGetStatus);
  server.on("/restart", HTTP_POST, handleRestart);
  server.on("/shutdown", HTTP_POST, handleShutdown);
  server.onNotFound([]() { server.send(404, "text/plain", "Not found"); });
  
  server.begin();
  Serial.println("System Ready.");
}

void loop() {
  MDNS.update();
  server.handleClient();

  unsigned long now = millis();
  
  if (now - lastLoopTick >= 1000) {
    lastLoopTick = now;
    processSensorLogic(now);
    if (currentReadings.severity != "SAFE") {
      sendWebhookMessage("ALERT: CO level is " + String(currentReadings.coPercent, 2) + "% - Status: " + String(currentReadings.severity));
    }
  }
}

bool sendWebhookMessage(const char* action) {
  String payload = "{\"message\": \"";
  payload += action;
  payload += "\"}";

  String request = "POST ";
  request += webhookPath;
  request += " HTTP/1.1\r\n";
  request += "Host: ";
  request += webhookHost;
  request += "\r\n";
  request += "Content-Type: application/json\r\n";
  request += "Content-Length: ";
  request += payload.length();
  request += "\r\n";
  request += "key: ";
  request += webhookKey;
  request += "\r\n";
  request += "\r\n";
  request += payload;
  
  if (!client.connect(webhookHost, 80)) {
    setStatus("Failed to connect to webhook host");
    return false;
  }

  setStatus("Connected! Sending request...");
  size_t expected = request.length();
  size_t sent = client.print(request);
  client.flush();

  setStatus("Bytes sent: " + String(sent) + " / " + String(expected));

  bool success = (sent == expected);
  client.stop();
  return success;
}

void processSensorLogic(unsigned long now) {
  unsigned long elapsed = now - stateStartTime;

  if (isCleaning) {
    currentReadings.phase = "Cleaning";
    if (elapsed >= CLEANING_PHASE) {
      isCleaning = false;
      stateStartTime = now;
    }
  } else {
    currentReadings.phase = "Measuring";
    currentReadings.rawA = analogRead(analogPin);
    currentReadings.rawD = digitalRead(digitalPin);
    
    // Auto-adjust baseline if current air is cleaner than stored baseline
    if (currentReadings.rawA < baseline && currentReadings.rawA > 10) {
        baseline = currentReadings.rawA;
    }

    float rawPercent = ((float)(currentReadings.rawA - baseline) / (DANGER_VAL - baseline)) * 100.0;
    currentReadings.coPercent = constrain(rawPercent, 0.0, 500.0);

    if (currentReadings.coPercent >= 300.0){
      currentReadings.severity = "CRITICAL";
    } 
    else if (currentReadings.coPercent >= 100.0){
      currentReadings.severity = "VERY BAD";
    } 
    else if (currentReadings.coPercent >= 15.0){
      currentReadings.severity = "RISK";
    }
    else {
      currentReadings.severity = "SAFE";
    }

    Serial.printf("P: %s | A: %d | D: %d | CO: %.2f%% | Status: %s\n", 
                  currentReadings.phase, 
                  currentReadings.rawA, 
                  currentReadings.rawD, 
                  currentReadings.coPercent,
                  currentReadings.severity);

    if (elapsed >= MEASURING_PHASE) {
      isCleaning = true;
      stateStartTime = now;
    }
  }
}

#pragma region Handlers
void handleGetStatus() {
  JsonDocument doc;
  doc["phase"] = currentReadings.phase;
  doc["analog"] = currentReadings.rawA;
  doc["digital"] = currentReadings.rawD;
  doc["co_percent"] = serialized(String(currentReadings.coPercent, 2));
  doc["severity"] = currentReadings.severity;

  String out;
  serializeJson(doc, out);
  server.send(200, "application/json", out);
}

void handleGetInfo() {
  unsigned long upSeconds = (millis() - bootMillis) / 1000UL;
  JsonDocument doc;

  doc["name"] = name;
  doc["ip"] = WiFi.localIP().toString();
  doc["uptimeSeconds"] = upSeconds;
  
  JsonArray actionsJson = doc["actions"].to<JsonArray>();
  for (const auto& action : actions) {
    JsonObject obj = actionsJson.add<JsonObject>();
    obj["action"] = action.action;
    obj["command"] = action.command;
    obj["commandType"] = action.commandType;
    obj["commandArgs"] = action.commandArgs;
  }

  String json;
  serializeJson(doc, json);
  server.send(200, "application/json", json);
}

void handleRestart() {
  server.send(200, "text/plain", "Restarting");
  delay(500);
  ESP.restart();
}

void handleShutdown() {
  server.send(200, "text/plain", "Entering Deep Sleep");
  delay(500);
  ESP.deepSleep(0); 
}

void connectToWifi() {
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);
  Serial.print("Connecting");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.printf("\nConnected. IP: %s\n", WiFi.localIP().toString().c_str());
}
#pragma endregion