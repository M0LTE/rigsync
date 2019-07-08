using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SeleniumTest
{
    /// <summary>
    /// Implement a rig controller for the QO-100 websdr
    /// </summary>
    public class WebSdrController : IRigController
    {
        private readonly IWebDriver webDriver;
        private readonly IWebElement freqInputElement;

        public WebSdrController()
        {
            string url = "https://eshail.batc.org.uk/nb/";

            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var dir = System.IO.Path.GetDirectoryName(location);

            webDriver = new ChromeDriver(dir) { Url = url };
            webDriver.Navigate();

            IWebElement startButton = webDriver.FindElement(By.CssSelector("#autoplay-start"));
            if (startButton.Displayed)
            {
                startButton.Click();
            }

            string freqInputSelector = "#receiver-undercontrols > div > div:nth-child(1) > form > span > input[type=text]:nth-child(2)";

            freqInputElement = webDriver.FindElement(By.CssSelector(freqInputSelector));
        }

        public event EventHandler<FreqEventArgs> FrequencyChanged;

        public long GetFrequencyHz()
        {
            if (double.TryParse(freqInputElement.GetAttribute("value"), out double khz))
            {
                return (long)(khz * 1000);
            }

            return 0;
        }

        public void SetFrequencyHz(long hz)
        {
            double khz = hz / 1000.0;

            ((IJavaScriptExecutor)webDriver).ExecuteScript($"setfreqif('{khz}');document.freqform.frequency.value='{khz}';");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WebSdrController()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
