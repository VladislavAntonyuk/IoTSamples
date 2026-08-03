sudo /home/vladislav/.dotnet/dotnet HomeManagement.dll --urls http://+:80

sudo nano /lib/systemd/system/home-management.service

[Unit]
Description=Home Management
Requires=media-vladislav-MyPassport.mount
After=network.target media-vladislav-MyPassport.mount

[Service]
Type=idle
WorkingDirectory=/home/vladislav/Projects/HomeManagement/
ExecStart=/home/vladislav/.dotnet/dotnet /home/vladislav/Projects/HomeManagement/HomeManagement.dll --urls http://+:80
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