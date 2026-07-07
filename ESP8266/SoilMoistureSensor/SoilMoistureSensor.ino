#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include <ArduinoJson.h>

#define SENSOR_PIN A0
const int VAL_DRY = 785;  // 0% Moisture (In Air)
const int VAL_WET = 284;  // 100% Moisture (In Water)

const int WATER_TRIGGER_PCT = 20;  // Trigger watering alert when moisture falls below 20%

#pragma region Configuration
const char* name = "SoilMoistureSensor";
const char* ssid = "";
const char* password = "!";
#pragma endregion

#pragma region Globals
ESP8266WebServer server(80);
unsigned long lastLoopTick = 0;
unsigned long bootMillis = 0;
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
  pinMode(SENSOR_PIN, INPUT);

  connectToWifi();
  if (MDNS.begin(name)) {
    MDNS.addService("http", "tcp", 80);
  }

  server.on("/info", HTTP_GET, handleGetInfo);
  server.on("/status", HTTP_GET, handleGetStatus);
  server.on("/restart", HTTP_POST, handleRestart);
  server.on("/shutdown", HTTP_POST, handleShutdown);
  server.onNotFound([]() {
    server.send(404, "text/plain", "Not found");
  });

  server.begin();
  Serial.println("\n================================================");
  Serial.println("--- Automated Soil Moisture System Started ---");
  Serial.print("Calibration Limits: Dry=");
  Serial.print(VAL_DRY);
  Serial.print(" | Wet=");
  Serial.println(VAL_WET);
  Serial.print("Watering Trigger Threshold: ");
  Serial.print(WATER_TRIGGER_PCT);
  Serial.println("%");
  Serial.println("================================================");
}

void loop() {
  MDNS.update();
  server.handleClient();

  unsigned long now = millis();
  
  if (now - lastLoopTick >= 5 * 60 * 1000) { // Every 5 minutes
    lastLoopTick = now;
    String result = handleGetStatus();
    if (result != "") {
      sendWebhookMessage(result);
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

String handleGetStatus() {
  long totalRaw = 0;
  const int samples = 10;

  // Smooth out readings to handle minor electrical noise
  for (int i = 0; i < samples; i++) {
    totalRaw += analogRead(SENSOR_PIN);
    delay(10);  // 10ms gap between samples
  }

  int avgRaw = totalRaw / samples;

  // Calculate voltage based on the ESP8266 3.3V ADC reference
  float voltage = (avgRaw * 3.3) / 1023.0;

  // Map and clamp the inverted range to 0-100%
  int moisturePercent = map(avgRaw, VAL_DRY, VAL_WET, 0, 100);
  moisturePercent = constrain(moisturePercent, 0, 100);

  JsonDocument doc;
  doc["ADC"] = avgRaw;
  doc["voltage"] = voltage;
  doc["moisture"] = moisturePercent;

  String result = '';
  // Automation logic handling thresholds
  if (moisturePercent < WATER_TRIGGER_PCT) {
    doc["status"] = "ALERT: Soil is DRY! Needs watering.";
    result = "ALERT: Soil is DRY! Needs watering." + String(moisturePercent) + "%";
  } else if (moisturePercent > 75) {
    doc["status"] = "STATUS: Soil is highly saturated / wet.";
    result = "STATUS: Soil is highly saturated / wet." + String(moisturePercent) + "%";
  } else {
    doc["status"] = "STATUS: Soil moisture level is optimal.";
  }

  String out;
  serializeJson(doc, out);
  server.send(200, "application/json", out);
  return result;
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
  Serial.print("Connecting to Wi-Fi");

  unsigned long startAttemptTime = millis();

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");

    // Fallback if network credentials fail or router is unreachable after 15 seconds
    if (millis() - startAttemptTime > 15000) {
      Serial.println("\nWi-Fi connection timeout. Proceeding to loop initialization...");
      break;
    }
  }
  if (WiFi.status() == WL_CONNECTED) {
    Serial.printf("\nConnected. IP: %s\n", WiFi.localIP().toString().c_str());
  }
}