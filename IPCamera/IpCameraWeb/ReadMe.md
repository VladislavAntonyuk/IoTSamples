sudo /home/vladislav/.dotnet/dotnet IpCameraWeb.dll --urls http://+:80

sudo nano /lib/systemd/system/ipcamera.service

[Unit]
Description=IpCameraWeb
After=network.target

[Service]
Type=idle
WorkingDirectory=/home/vladislav/Projects/IpCamera/
ExecStart=/home/vladislav/.dotnet/dotnet /home/vladislav/Projects/IpCamera/IpCameraWeb.dll --urls http://+80
Restart=on-failure

[Install]
WantedBy=default.target