> results_%2.txt (DATE /T & TIME /T)
set var=%2%
avrdude -c usbtiny -p atmega328p -P usb:bus-0:\\.\libusb0-000%var:~1%--0x1781-0x0c9f -v >> \results\results_%2.txt 2>&1
sleep 5
exit