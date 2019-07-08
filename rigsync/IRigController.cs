using System;

namespace SeleniumTest
{
    public interface IRigController : IDisposable
    {
        event EventHandler<FreqEventArgs> FrequencyChanged;

        long GetFrequencyHz();
        void SetFrequencyHz(long hz);
    }
}