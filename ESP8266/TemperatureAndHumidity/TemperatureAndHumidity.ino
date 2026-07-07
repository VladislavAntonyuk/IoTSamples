#include <Wire.h>
#include <Adafruit_AHTX0.h>
#include <U8g2lib.h>
#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <ESP8266mDNS.h>
#include <ArduinoJson.h>

const char* name = "Temperature And Humidity";
const char* ssid = "";
const char* password = "";
const char* webhookHost = "192.168.50.164";
const char* webhookPath = "/api/webhook";
const char* webhookKey = "";
const int LOW_TEMP_TRIGGER = 5;
const int HIGH_TEMP_TRIGGER = 35;
unsigned long screenTurnedOnAt = 0;
bool isScreenActive = false;
float currentTemperature = 0.0;
float currentHumidity = 0.0;

ESP8266WebServer server(80);
unsigned long bootMillis = 0;
unsigned long lastLoopTick = 0;
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

// Конфігурація апаратної шини (Датчик AHT10)
#define AHT_SDA 4  // D2
#define AHT_SCL 5  // D1

Adafruit_AHTX0 aht;

// Конфігурація програмної шини (Екран OLED)
// Використовуємо конструктор програмного I2C (SW_I2C): clock = 14 (D5 - SCL), data = 12 (D6 - SDA)
U8G2_SSD1306_128X64_NONAME_F_SW_I2C u8g2(U8G2_R0, /* clock=*/14, /* data=*/12, /* reset=*/U8X8_PIN_NONE);

void setup() {
  Serial.begin(9600);
  bootMillis = millis();

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

  // Ініціалізація екрану
  u8g2.begin();
  u8g2.enableUTF8Print();  // Обов'язкова функція для підтримки української мови

  // Ініціалізація AHT10 на апаратній шині
  Wire.begin(AHT_SDA, AHT_SCL);
  if (!aht.begin()) {
    Serial.println(F("Помилка AHT10 на D2/D1"));
    while (1) delay(10);
  }
}

void loop() {
  MDNS.update();
  server.handleClient();
  if (isScreenActive && (millis() - screenTurnedOnAt >= 5000)) {
    turnOffScreen();
    isScreenActive = false;
  }

  unsigned long now = millis();

  if (now - lastLoopTick >= 10 * 60 * 1000) { // Every 10 minutes
    lastLoopTick = now;
    readSensorData();
    if (currentTemperature < LOW_TEMP_TRIGGER) {;
      sendWebhookMessage("ALERT: LOW Temperature (" + String(currentTemperature) + " °C), Humidity: " + String(currentHumidity) + " %");
    }
    
    if (currentTemperature > HIGH_TEMP_TRIGGER) {;
      sendWebhookMessage("ALERT: HIGH Temperature (" + String(currentTemperature) + " °C), Humidity: " + String(currentHumidity) + " %");
    }
  }
}

void readSensorData() {
  sensors_event_t humidity, temp;
  aht.getEvent(&humidity, &temp);

  currentTemperature = temp.temperature;
  currentHumidity = humidity.relative_humidity;
}

void printTemperature() {
  u8g2.setCursor(0, 40);
  u8g2.print("Темп: ");
  u8g2.print(currentTemperature, 1);
  u8g2.print(" °C");
}

void printHumidity() {
  u8g2.setCursor(0, 60);
  u8g2.print("Вол:  ");
  u8g2.print(currentHumidity, 1);
  u8g2.print(" %");
}

void printSensorData() {
  turnOnScreen();
  u8g2.clearBuffer();

  u8g2.setFont(u8g2_font_unifont_t_cyrillic);
  u8g2.setCursor(0, 15);
  u8g2.print(name);
  u8g2.drawHLine(0, 20, 128);

  printTemperature();
  printHumidity();

  u8g2.sendBuffer();

  screenTurnedOnAt = millis();
  isScreenActive = true;
}

void turnOnScreen() {
  u8g2.setPowerSave(0);
}

void turnOffScreen() {
  u8g2.setPowerSave(1);
}

void handleGetStatus() {
  readSensorData();
  printSensorData();

  JsonDocument doc;
  doc["temperature"] = currentTemperature;
  doc["humidity"] = currentHumidity;

  if (currentTemperature < LOW_TEMP_TRIGGER) {
    doc["status"] = "ALERT: LOW Temperature (" + String(currentTemperature) + " °C)";
  }
  if (currentTemperature > HIGH_TEMP_TRIGGER) {
    doc["status"] = "ALERT: HIGH Temperature (" + String(currentTemperature) + " °C)";
  }

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