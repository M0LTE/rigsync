using System;
using System.Collections.Generic;

namespace SeleniumTest
{
    class Program
    {
        static long offset = 10057500000;
        static long ft818Freq;
        static long sdrFreq;
        static IRigController sdrController;
        static IRigController ft818Controller;

        public static void Main(string sdrConsolePort, string yaesuPort)
        {
            //TODO: validate comms with the radios

            sdrController = new Ts2000Controller(sdrConsolePort, 57600, rigPollInterval: TimeSpan.FromMilliseconds(250));
            ft818Controller = new Ft818(yaesuPort, 38400, rigPollInterval: TimeSpan.FromMilliseconds(50));

            sdrController.FrequencyChanged += SdrFrequencyChanged;
            ft818Controller.FrequencyChanged += Ft818_knob_twiddled;

            sdrFreq = 10489700000;
            sdrController.SetFrequencyHz(sdrFreq);
            ft818Freq = sdrFreq - offset;
            ft818Controller.SetFrequencyHz(ft818Freq);

            PrintLayout();
            Redraw();

            Dictionary<ConsoleKey, int> offsets = new Dictionary<ConsoleKey, int> {
                {ConsoleKey.PageUp, 500 },
                {ConsoleKey.PageDown, -500 },
                {ConsoleKey.UpArrow, 100 },
                {ConsoleKey.DownArrow, -100 },
            };
            while (true)
            {
                var key = Console.ReadKey();

                if (offsets.TryGetValue(key.Key, out int o))
                {
                    offset += o;
                    ChangeTxFreq();
                }
                else if (key.Key == ConsoleKey.R)
                {
                    PrintLayout();
                    Redraw();
                }
            }
        }

        private static void PrintLayout()
        {
            lock (lockobj)
            {
                Console.CursorVisible = false;
                Console.SetCursorPosition(0, 0);
                Console.Write("TX IF:");
                Console.SetCursorPosition(0, 1);
                Console.Write("Uplink:");
                Console.SetCursorPosition(0, 2);
                Console.Write("Downlink:");
                Console.SetCursorPosition(0, 3);
                Console.Write("Offset:");
                Console.SetCursorPosition(0, 4);
                Console.Write("Segment:");
            }
        }

        private static void ChangeTxFreq()
        {
            ft818Freq = sdrController.GetFrequencyHz() - offset;
            ft818Controller.SetFrequencyHz(ft818Freq);
            Redraw();
        }

        static object lockobj = new object();
        private static void Redraw()
        {
            lock (lockobj)
            {
                Console.SetCursorPosition(10, 0);
                Console.Write("{0,11:0.00000} MHz", ft818Freq / 1000000.0);
                Console.SetCursorPosition(10, 1);
                Console.Write("{0,11:0.00000} MHz", (ft818Freq + 1968000000) / 1000000.0);
                Console.SetCursorPosition(10, 2);
                Console.Write("{0,11:0.00000} MHz", sdrFreq / 1000000.0);
                Console.SetCursorPosition(10, 3);
                Console.Write("{0,11:0.00000} MHz", offset / 1000000.0);
                Console.SetCursorPosition(10, 4);

                var colBefore = Console.ForegroundColor;
                var segment = GetSegment(sdrFreq);
                if (segment == Segment.SSB || segment == Segment.MixedModes)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (segment == Segment.NarrowDigi || segment == Segment.Digimodes)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.Write("{0,11}", segment);
                Console.ForegroundColor = colBefore;
            }
        }

        private static Segment GetSegment(long freq)
        {
            if (freq < 10489550000)
            {
                return Segment.Below;
            }
            else if (freq < 10489555000)
            {
                return Segment.LowerBeacon;
            }
            else if (freq < 10489600000)
            {
                return Segment.CW;
            }
            else if (freq < 10489620000)
            {
                return Segment.NarrowDigi;
            }
            else if (freq < 10489640000)
            {
                return Segment.Digimodes;
            }
            else if (freq < 10489690000)
            {
                return Segment.MixedModes;
            }
            else if (freq < 10489795000)
            {
                return Segment.SSB;
            }
            else if (freq < 10489800000)
            {
                return Segment.UpperBeacon;
            }
            else
            {
                return Segment.Above;
            }
        }

        private enum Segment
        {
            Below,
            LowerBeacon,
            CW,
            NarrowDigi,
            Digimodes,
            MixedModes,
            SSB,
            UpperBeacon,
            Above
        }

        private static void SdrFrequencyChanged(object sender, FreqEventArgs e)
        {
            sdrFreq = e.FrequencyHz;
            ChangeTxFreq();
        }

        private static void Ft818_knob_twiddled(object sender, FreqEventArgs e)
        {
            ft818Freq = e.FrequencyHz;
            sdrFreq = e.FrequencyHz + offset;
            sdrController.SetFrequencyHz(sdrFreq);
            Redraw();
        }
    }
}