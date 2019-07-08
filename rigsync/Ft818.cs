using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumTest
{
    public class Ft818 : IRigController
    {
        public event EventHandler<FreqEventArgs> FrequencyChanged;
        private readonly SerialPort serialPort;
        private readonly byte[] freqRequestCommand = new byte[] { 0, 0, 0, 0, 0x03 };
        private readonly TimeSpan rigPollInterval;
        private readonly object lockObj = new object();
        private long freqHz;

        public Ft818(string comPort, int baudRate, TimeSpan rigPollInterval)
        {
            this.rigPollInterval = rigPollInterval;
            serialPort = new SerialPort(comPort, baudRate, Parity.None, 8, StopBits.Two);
            serialPort.Open();

            Task.Factory.StartNew(PollRig, TaskCreationOptions.LongRunning);
        }
        
        private int ReadFrequencyFromRig()
        {
            lock (lockObj)
            {
                serialPort.Write(freqRequestCommand, 0, freqRequestCommand.Length);

                var rxbuf = new List<byte>
                {
                    (byte)serialPort.ReadByte(),
                    (byte)serialPort.ReadByte(),
                    (byte)serialPort.ReadByte(),
                    (byte)serialPort.ReadByte(),
                    (byte)serialPort.ReadByte(),
                };

                string first = GetFreqChars(rxbuf, 0);
                string second = GetFreqChars(rxbuf, 1);
                string third = GetFreqChars(rxbuf, 2);
                string fourth = GetFreqChars(rxbuf, 3);

                if (!int.TryParse($"{first}{second}{third}{fourth}", out int deciHertz))
                {
                    return 0;
                }

                int hz = deciHertz * 10;

                return hz;
            }
        }

        private void PollRig()
        {
            while (true)
            {
                int hz;

                hz = ReadFrequencyFromRig();

                if (freqHz != hz)
                {
                    if (freqHz != 0)
                    {
                        FrequencyChanged?.Invoke(null, new FreqEventArgs { FrequencyHz = hz });
                    }
                    freqHz = hz;
                }

                Thread.Sleep(rigPollInterval);
            }
        }

        private static string GetFreqChars(List<byte> rxbuf, int index)
        {
            string hex = rxbuf[index].ToString("X");
            if (hex.Length == 1)
            {
                hex = "0" + hex;
            }

            return hex;
        }

        public long GetFrequencyHz()
        {
            return freqHz;
        }

        public void SetFrequencyHz(long hz)
        {
            if (hz == freqHz)
                return;

            // to set 439700000 send
            // 0x43 0x97 0x00 0x00 followed by opcode 0x01

            if (hz >= 1000000000)
            {
                return;
            }

            string hertzStr = hz.ToString("D9");

            byte[] digits = new byte[5];

            digits[0] = byte.Parse(hertzStr.Substring(0, 2), NumberStyles.HexNumber);
            digits[1] = byte.Parse(hertzStr.Substring(2, 2), NumberStyles.HexNumber);
            digits[2] = byte.Parse(hertzStr.Substring(4, 2), NumberStyles.HexNumber);
            digits[3] = byte.Parse(hertzStr.Substring(6, 2), NumberStyles.HexNumber);
            digits[4] = 0x01;

            lock (lockObj)
            {
                serialPort.Write(digits, 0, digits.Length);
                freqHz = hz;
                serialPort.ReadByte();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    serialPort.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
