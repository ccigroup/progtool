@echo off
> batches\core_results\core_results_p%1.txt (DATE /T & TIME /T)
avrdude -c usbtiny -p atmega328p -P usb:bus-0:\\.\libusb0-000%1--0x1781-0x0c9f -U flash:w:%2:i -v >> batches\core_results\core_results_p%1.txt 2>&1
exit