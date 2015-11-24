using Windows.Devices.Gpio;

namespace Microsoft.Maker.Devices.Hx711
{
    public sealed class Hx711
    {
        private int clockPinNumber;
        private int dataPinNumber;
        private GpioPin clockPin;
        private GpioPin dataPin;

        /// <summary>
        /// Used to signal that the device is properly initialized and ready to use
        /// </summary>
        private bool available = false;

        public Hx711(int clockPinNumber, int dataPinNumber)
        {
            this.clockPinNumber = clockPinNumber;
            this.dataPinNumber = dataPinNumber;
        }

        /// <summary>
        /// Initialize the load sensing device.
        /// </summary> 
        /// <returns>
        /// Async operation object.
        /// </returns>
        public bool Begin()
        {
            /*
                * Acquire the GPIO controller
                * MSDN GPIO Reference: https://msdn.microsoft.com/en-us/library/windows/apps/windows.devices.gpio.aspx
                * 
                * Get the default GpioController
                */
            GpioController gpio = GpioController.GetDefault();

            /*
                * Test to see if the GPIO controller is available.
                *
                * If the GPIO controller is not available, this is
                * a good indicator the app has been deployed to a
                * computing environment that is not capable of
                * controlling the weather shield. Therefore we
                * will disable the weather shield functionality to
                * handle the failure case gracefully. This allows
                * the invoking application to remain deployable
                * across the Universal Windows Platform.
                */
            if (null == gpio)
            {
                available = false;
                return false;
            }

            /*
                * Initialize the blue LED and set to "off"
                *
                * Instantiate the blue LED pin object
                * Write the GPIO pin value of low on the pin
                * Set the GPIO pin drive mode to output
                */
            clockPin = gpio.OpenPin(clockPinNumber, GpioSharingMode.Exclusive);
            clockPin.Write(GpioPinValue.Low);
            clockPin.SetDriveMode(GpioPinDriveMode.Output);

            /*
                * Initialize the green LED and set to "off"
                * 
                * Instantiate the green LED pin object
                * Write the GPIO pin value of low on the pin
                * Set the GPIO pin drive mode to output
                */
            dataPin = gpio.OpenPin(dataPinNumber, GpioSharingMode.Exclusive);
            dataPin.Write(GpioPinValue.Low);
            dataPin.SetDriveMode(GpioPinDriveMode.Input);

            available = true;
            return true;
        }

        public double Grams
        {
            private set { }
            get
            {
                if (!available) { return 0.0f; }
                //TODO: Figure out how mystic ADC units converts to Grams
                return ReadData();
            }
        }

        // Byte:     0        1        2
        // Bits:  76543210 76543210 76543210
        // Data: |--------|--------|--------|
        // Bit#:  00000000 11111100 22221111
        //        76543210 54321098 32109876
        private int ReadData()
        {
            uint data = 0;
            byte[] rawData = new byte[3];

            // Clock in data
            for (int i = 2; i >= 0; --i)
            {
                rawData[i] = ShiftIn();
            }
            data = (uint)((rawData[2] << 16) | (rawData[1] << 8) | rawData[0]);

            // Data is returned in 2's compliment
            // https://cdn.sparkfun.com/datasheets/Sensors/ForceFlex/hx711_english.pdf
            data = ~data + 1;

            // Because "bodge" did it...
            // data ^= 0x800000;

            clockPin.Write(GpioPinValue.High);
            clockPin.Write(GpioPinValue.Low);

            return (int)data;
        }

        private byte ShiftIn()
        {
            byte value = 0x00;
            dataPin.SetDriveMode(GpioPinDriveMode.Input);

            // Wait for chip to become ready
            while (GpioPinValue.Low != dataPin.Read());

            for (int i = 7; i >= 0; --i)
            {
                uint pinValue = 0x00;
                // Convert "GpioPinValue.High" and "GpioPinValue.Low" to 1 and 0, respectively.
                if (GpioPinValue.High == dataPin.Read()) { pinValue = 0x01; }

                clockPin.Write(GpioPinValue.High);
                value |= (byte)(pinValue << i);
                clockPin.Write(GpioPinValue.Low);
            }

            return value;
        }
    }
}
