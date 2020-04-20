using NRig;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTest
{
    /// <summary>
    /// Implement a pretend TS-480 which SdrConsole can connect to using its Track Radio feature (using Omnirig)
    /// </summary>
    public class EmulatedTs480Controller : IRigController
    {
        private readonly SerialPort serialPort;
        private readonly List<char> commandBuffer = new List<char>();
        private long freqHz;

        public event EventHandler<FrequencyEventArgs> FrequencyChanged;

        public EmulatedTs480Controller(string comPort, int baud)
        {
            serialPort = new SerialPort(comPort, baud, Parity.None, 8, StopBits.Two);
            serialPort.Open();
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[serialPort.BytesToRead];
            serialPort.Read(buffer, 0, buffer.Length);
            commandBuffer.AddRange(Encoding.ASCII.GetString(buffer));

            InterpretCommandBuffer();
        }

        /// <summary>
        /// See if there's a command in the buffer. If there is, do something with it, then clear the buffer.
        /// </summary>
        private void InterpretCommandBuffer()
        {
            if (commandBuffer.EndsWith("AI0;"))
            {
                // reply with the same
                serialPort.Write("AI0;");
                commandBuffer.Clear();
            }
            else if (commandBuffer.EndsWith("IF;"))
            {
                int rit = 0;
                bool ritOn = false;
                bool xitOn = false;
                int mChBankNumber = 0;
                int mChNumber = 0;
                bool tx = false;
                int opMode = 0; // refer to MD command
                int p10 = 0;
                int p11 = 0;
                bool split = false;
                int tone = 0;
                int toneNumber = 0;

                string reply = $"IF{freqHz.ToString("D11")}{new String(' ', 5)}{rit.ToString("D5")}{(ritOn ? 1 : 0)}{(xitOn ? 1 : 0)}{mChBankNumber.ToString("D1")}{mChNumber.ToString("D2")}{(tx ? 1 : 0)}{opMode.ToString("D1")}{p10.ToString("D1")}{p11.ToString("D1")}{(split ? 1 : 0)}{tone.ToString("D1")}{toneNumber.ToString("D2")}{new String(' ', 1)};";

                if (reply.Length != 38)
                {
                    throw new InvalidOperationException("bad IF response prevented");
                }

                serialPort.Write(reply);
                commandBuffer.Clear();
            }
            else if (commandBuffer.EndsWith("FA;"))
            {
                string reply = $"FA{freqHz.ToString("D11")};";
                serialPort.Write(reply);
                commandBuffer.Clear();
            }
            else if (commandBuffer.EndsWith("FB;"))
            {
                string reply = $"FB{freqHz.ToString("D11")};";
                serialPort.Write(reply);
                commandBuffer.Clear();
            }
        }

        public Task<Frequency> GetFrequency(Vfo vfo) => Task.FromResult<Frequency>(freqHz);

        public Task SetFrequency(Vfo vfo, Frequency frequency)
        {
            freqHz = frequency;
            return Task.CompletedTask;
        }

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

        public Task SetActiveVfo(Vfo bfo) => throw new NotImplementedException();
        public Task SetPttState(bool value) => throw new NotImplementedException();
        public Task<bool> GetPttState() => throw new NotImplementedException();
        public Task SetMode(Vfo vfo, Mode mode) => throw new NotImplementedException();
        public Task SetTunerState(bool value) => throw new NotImplementedException();
        public Task<bool> GetTunerState() => throw new NotImplementedException();
        public Task RunTuningCycle() => throw new NotImplementedException();
        public Task<MeterReadings> ReadMeters() => throw new NotImplementedException();
        public Task SetAgcState(AgcMode agcMode) => throw new NotImplementedException();
        public Task<AgcMode> GetAgcState() => throw new NotImplementedException();
        public Task SetNoiseBlankerState(bool value) => throw new NotImplementedException();
        public Task<bool> GetNoiseBlankerState() => throw new NotImplementedException();
        public Task BeginTransmitTuningCarrier(TimeSpan maxDuration) => throw new NotImplementedException();
        public Task EndTransmitTuningCarrier() => throw new NotImplementedException();
        public Task SetAttenuatorState(bool value) => throw new NotImplementedException();
        public Task<bool> GetAttenuatorState() => throw new NotImplementedException();
        public Task SetPreampState(bool value) => throw new NotImplementedException();
        public Task<bool> GetPreampState() => throw new NotImplementedException();
        public Task SetClarifierOffset(Frequency frequency) => throw new NotImplementedException();
        public Task<Frequency> GetClarifierOffset() => throw new NotImplementedException();

        public Task BeginRigStatusUpdates(Action<RigStatus> callback, TimeSpan updateFrequency)
        {
            throw new NotImplementedException();
        }

        public Task EndRigStatusUpdates()
        {
            throw new NotImplementedException();
        }
    }
}
