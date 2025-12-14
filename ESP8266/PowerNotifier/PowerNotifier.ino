#include <ESP8266WiFi.h>
#include <ESP8266mDNS.h>
#include <ESP8266WebServer.h>

const char* name = "YOUR DEVICE NAME";
const char* ssid = "YOUR SSID";
const char* password = "YOUR_PASSWORD";
const char* webhookHost = "192.168.1.1";
const char* webhookPath = "/api/webhook";
const char* webhookKey = "key";

WiFiClient client;
ESP8266WebServer server(80);
String status = "Not connected";

void ensureWiFiConnected() {
  if (!WiFi.isConnected()) {
    setStatus("WiFi disconnected, reconnecting...");
    WiFi.reconnect();
  } else {
    setStatus("Connected! IP: " + WiFi.localIP().toString());
  }
}

bool sendWebhookMessage(const char* action) {
  String payload = "{\"action\": \"";
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

void handleGetStatus() {
  server.send(200, "text/plain", status);
}

void setStatus(String sts){
  status = sts;
  Serial.println(sts);
}

void setup() {
  Serial.begin(9600);

  WiFi.begin(ssid, password);
  
  // Wait for WiFi to connect before starting MDNS and server
  while (!WiFi.isConnected()) {
    setStatus("Waiting for WiFi connection...");
    delay(500);
  }

  if (MDNS.begin(name)) {
    setStatus("mDNS started");
  } else {
    setStatus("Error starting mDNS");
  }
  
  server.on("/", HTTP_GET, handleGetStatus);
  server.begin();
}

void loop() {
  MDNS.update();
  server.handleClient();
  
  ensureWiFiConnected();
  
  if (WiFi.isConnected()) {
    bool ok = sendWebhookMessage("PowerOn");
    if (ok) {
      setStatus("Webhook message sent successfully!");
      WiFi.disconnect(true);
      ESP.deepSleep(0);
    } else {
      setStatus("Failed to send webhook message, will retry...");
    }
  }
  
  delay(1000);
}