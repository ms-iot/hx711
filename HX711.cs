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
                * Initialize the clock pin and set to "Low"
                *
                * Instantiate the clock pin object
                * Write the GPIO pin value of low on the pin
                * Set the GPIO pin drive mode to output
                */
            clockPin = gpio.OpenPin(clockPinNumber, GpioSharingMode.Exclusive);
            clockPin.Write(GpioPinValue.Low);
            clockPin.SetDriveMode(GpioPinDriveMode.Output);

            /*
                * Initialize the data pin and set to "Low"
                * 
                * Instantiate the data pin object
                * Set the GPIO pin drive mode to input for reading
                */
            dataPin = gpio.OpenPin(dataPinNumber, GpioSharingMode.Exclusive);
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
                lock (this)
                {
                    //TODO: Figure out how mystic ADC units converts to Grams
                    return ReadData();
                }
            }
        }

        // Byte:     0        1        2        3
        // Bits:  76543210 76543210 76543210 76543210
        // Data: |--------|--------|--------|--------|
        // Bit#:  33222222 22221111 11111100 00000000
        //        10987654 32109876 54321098 76543210
        private int ReadData()
        {
            uint value = 0;
            byte[] data = new byte[4];

            // Wait for chip to become ready
            for (; GpioPinValue.Low != dataPin.Read() ;);

            // Clock in data
            data[1] = ShiftInByte();
            data[2] = ShiftInByte();
            data[3] = ShiftInByte();

            // Clock in gain of 128 for next reading
            clockPin.Write(GpioPinValue.High);
            clockPin.Write(GpioPinValue.Low);

            // Replicate the most significant bit to pad out a 32-bit signed integer
            if (0x80 == (data[1] & 0x80))
            {
                data[0] = 0xFF;
            } else {
                data[0] = 0x00;
            }

            // Construct a 32-bit signed integer
            value = (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);

            // Datasheet indicates the value is returned as a two's complement value
            // https://cdn.sparkfun.com/datasheets/Sensors/ForceFlex/hx711_english.pdf

            // flip all the bits
            value = ~value;

            // ... and add 1
            return (int)(++value);
        }

        private byte ShiftInByte()
        {
            byte value = 0x00;

            // Convert "GpioPinValue.High" and "GpioPinValue.Low" to 1 and 0, respectively.
            // NOTE: Loop is unrolled for performance
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 7);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 6);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 5);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 4);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 3);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 2);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)((byte)(dataPin.Read()) << 1);
            clockPin.Write(GpioPinValue.Low);
            clockPin.Write(GpioPinValue.High);
            value |= (byte)dataPin.Read();
            clockPin.Write(GpioPinValue.Low);

            return value;
        }
    }
}
