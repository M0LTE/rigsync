using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;

namespace SeleniumTest
{
    /// <summary>
    /// Implement a client for the TS-2000, or radios that emulate it, or SDR Console
    /// </summary>
    public class Ts2000Controller : IRigController
    {
        public event EventHandler<FreqEventArgs> FrequencyChanged;
        private readonly SerialPort serialPort;
        private readonly TimeSpan rigPollInterval;
        private readonly object lockObj = new object();
        private long freqHz;

        public Ts2000Controller(string comPort, int baudRate, TimeSpan rigPollInterval)
        {
            this.rigPollInterval = rigPollInterval;

            serialPort = new SerialPort(comPort, baudRate);
            serialPort.Open();

            Task.Factory.StartNew(PollRig, TaskCreationOptions.LongRunning);
        }

        private void PollRig()
        {
            while (true)
            {
                long hz = ReadFrequencyFromRig();

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

        public long GetFrequencyHz()
        {
            return freqHz;
        }

        private long ReadFrequencyFromRig()
        {
            string response;

            lock (lockObj)
            {
                serialPort.Write("FA;");

                response = ReadResponse(); // FA00500000000;
            }

            if (!response.StartsWith("FA") || response.Length != 14 || !response.EndsWith(';'))
            {
                return 0;
            }

            if (!long.TryParse(new String(response.Skip(2).Take(11).ToArray()), out long hz))
            {
                return 0;
            }

            return hz;
        }

        private string ReadResponse()
        {
            var chars = new List<char>();
            while (true)
            {
                int b = serialPort.ReadByte();
                chars.Add((char)b);
                if (b == ';')
                    break;
            }

            string response = new string(chars.ToArray());

            return response;
        }

        public void SetFrequencyHz(long hz)
        {
            lock (lockObj)
            {
                serialPort.Write($"FA{hz:D11};");
                freqHz = hz;
                while (true)
                {
                    if (ReadFrequencyFromRig() == hz)
                    {
                        return;
                    }
                }
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

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}