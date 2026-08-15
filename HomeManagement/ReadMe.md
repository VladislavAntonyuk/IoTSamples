sudo /home/vladislav/.dotnet/dotnet HomeManagement.dll --urls http://+:80

sudo nano /lib/systemd/system/home-management.service

[Unit]
Description=Home Management
Requires=media-orangepi-MyPassport.mount
After=network.target media-orangepi-MyPassport.mount

[Service]
User=orangepi
Group=orangepi
AmbientCapabilities=CAP_NET_BIND_SERVICE
Type=idle
WorkingDirectory=/home/orangepi/Projects/HomeManagement/
ExecStart=/home/orangepi/.dotnet/dotnet /home/orangepi/Projects/HomeManagement/HomeManagement.dll --urls http://+:80
Restart=on-failure

[Install]
WantedBy=default.target






sudo systemctl daemon-reload
sudo systemctl enable home-management.service
sudo systemctl start home-management.service
sudo systemctl status home-management.service




ssh vladislav@raspberrypi-5.local
rm -r Projects/HomeManagement


scp -r "./" vladislav@raspberrypi-5.local:/home/vladislav/Projects/HomeManagement