#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include <ArduinoJson.h>

const int TRIG_PIN = 4; // D2 (GPIO4)
const int ECHO_PIN = 5; // D1 (GPIO5)

#pragma region Configuration
const char* name = "WellWaterLevel";
const char* ssid = "";
const char* password = "!";
#pragma endregion

#pragma region Globals
ESP8266WebServer server(80);
unsigned long bootMillis = 0;
struct Action {
  const char* action;
  const char* commandType;
  const char* command;
  const char* commandArgs;
};

const Action actions[] = {
  { "Distance", "GET", "distance", "" },
  { "RESTART", "POST", "restart", "{}" },
  { "SHUTDOWN", "POST", "shutdown", "{}" }
};
#pragma endregion

void setup() {
  Serial.begin(9600); 
  bootMillis = millis();
  
  pinMode(TRIG_PIN, OUTPUT);
  digitalWrite(TRIG_PIN, LOW);
  pinMode(ECHO_PIN, INPUT);

  connectToWifi();
  if (MDNS.begin(name)) {
    MDNS.addService("http", "tcp", 80);
  }

  server.on("/info", HTTP_GET, handleGetInfo);
  server.on("/distance", HTTP_GET, handleGetDistance);
  server.on("/restart", HTTP_POST, handleRestart);
  server.on("/shutdown", HTTP_POST, handleShutdown);
  server.onNotFound([]() { server.send(404, "text/plain", "Not found"); });
  
  server.begin();
  Serial.println("Setup initialized...");
}

void loop() {
  MDNS.update();
  server.handleClient();
  delay(1); // Explicit yield to prevent background soft-WDT issues
}

float getFilteredDistance() {
  const int SAMPLES = 5;
  float distances[SAMPLES];
  int validSamples = 0;

  for (int i = 0; i < SAMPLES; i++) {
    digitalWrite(TRIG_PIN, LOW);
    delayMicroseconds(2);
    digitalWrite(TRIG_PIN, HIGH);
    delayMicroseconds(10);
    digitalWrite(TRIG_PIN, LOW);

    // Timeout adjusted to 35000µs (~5.9 meters absolute maximum)
    long duration = pulseIn(ECHO_PIN, HIGH, 35000); 
    
    // Adjusted coefficient for ~+10°C environment in a well
    float dist = (duration * 0.03373) / 2.0;

    if (duration > 0 && dist >= 20.0 && dist <= 450.0) {
      distances[validSamples] = dist;
      validSamples++;
    }
    
    // Mandate a recovery delay regardless of whether pulseIn timed out or succeeded
    delay(80); 
  }

  if (validSamples == 0) return -1.0;

  float sum = 0;
  for (int i = 0; i < validSamples; i++) {
    sum += distances[i];
  }
  return sum / validSamples;
}

void handleGetDistance() {
  float distanceCm = getFilteredDistance();

  if (distanceCm < 0) {
    JsonDocument doc;
    doc["type"] = "https://tools.ietf.org/html/rfc7231#section-6.6.5";
    doc["title"] = "Well Sensor Error";
    doc["status"] = 504;
    doc["detail"] = "Failed to get stable ultrasonic echo from water surface. Check for condensation or wall interference.";
    doc["instance"] = "/distance";

    String out;
    serializeJson(doc, out);
    server.send(504, "application/problem+json", out);
  } else {
    JsonDocument doc;
    doc["distanceToWaterCm"] = distanceCm;
    
    String out;
    serializeJson(doc, out);
    server.send(200, "application/json", out);
  }
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
  if(WiFi.status() == WL_CONNECTED) {
    Serial.printf("\nConnected. IP: %s\n", WiFi.localIP().toString().c_str());
  }
}
#pragma endregion