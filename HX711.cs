using Windows.Devices.Gpio;

namespace Microsoft.Maker.Devices.Hx711
{
    public sealed class Hx711
    {
        private GpioPin clockPin;
        private GpioPin dataPin;

        public Hx711(GpioPin clockPin, GpioPin dataPin)
        {
            this.clockPin = clockPin;
            this.dataPin = dataPin;
            clockPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public double Grams
        {
            private set { }
            get
            {
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
