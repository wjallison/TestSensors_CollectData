using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Text;
using System.Diagnostics;

using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestSensors_CollectData
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public List<double> pressList = new List<double>();
        public List<double> currentList = new List<double>();
        public List<double> flowPDiffList = new List<double>();
        public List<double> flowList = new List<double>();
        public List<double> tempList = new List<double>();

        public double avgTemp = 0;

        //var _bmp180;
        private GpioPin _inGpioPin;
        private I2cDevice _converter0;
        private I2cDevice _converter1;

        Bmp180Sensor _bmp180;

        public MainPage()
        {
            this.InitializeComponent();
        }

        public double pDiffToFlow(double pDiff)
        {
            //Assumes input is in Inches Water
            double k = 652694 * avgTemp + 2628143;
            return (Math.Pow(k * pDiff, 0.5));
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            StartStopButton.Content = "Stop";

            InitPressure();

        }

        private async void getValues()
        {
            var sensorData = await _bmp180.GetSensorDataAsync(Bmp180AccuracyMode.UltraLowPower);
            //var temp = sensorData.Temperature.ToString("F1");
            tempList.Add(sensorData.Temperature);
            //var press = sensorData.Pressure.ToString("F2");
            pressList.Add(sensorData.Pressure);
            pressText.Text = pressList.Last().ToString();

            avgPressText.Text = avgOf(pressList);


        }

        public string avgOf(List<double> ls)
        {
            double sum = 0;
            for(int i = 0; i < ls.Count; i++)
            {
                sum += ls[i];
            }
            return (sum / Convert.ToDouble(ls.Count)).ToString();
        }

        private async void InitPressure()
        {
            _bmp180 = new Bmp180Sensor();
            await _bmp180.InitializeAsync();            
        }

        private async void InitADS()
        {
            var i2cSettings = new I2cConnectionSettings(0x48)
            {
                BusSpeed = I2cBusSpeed.FastMode,
                SharingMode = I2cSharingMode.Shared
            };

            var i2C1 = I2cDevice.GetDeviceSelector("I2C1");
            var devices = await DeviceInformation.FindAllAsync(i2C1);

            _converter0 = await I2cDevice.FromIdAsync(devices[0].Id, i2cSettings);
            //_converter1 = await I2cDevice.FromIdAsync(devices[0].Id, i2cSettings);

            _converter0.Write(new byte[] { 0x01, 0xc4, 0xe0 });
            _converter0.Write(new byte[] { 0x02, 0x00, 0x00 });
            _converter0.Write(new byte[] { 0x03, 0xff, 0xff });

            //_converter1.Write(new byte[] { })

            var gpio = GpioController.GetDefault();
            
            _inGpioPin = gpio.OpenPin(27);

            _inGpioPin.ValueChanged += InGpioPinOnValueChanged;
        }

        private async void InGpioPinOnValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //var _converter 
            var byteArray = new byte[2];

            _converter0.WriteRead(new byte[] { 0x0 }, byteArray);

            if (BitConverter.IsLittleEndian) { Array.Reverse(byteArray); }

            var value = BitConverter.ToInt16(byteArray, 0);

            
        }


        public void SaveAll()
        {
            StringBuilder csv = new StringBuilder();
            DateTime current = new DateTime();

            string s = current.Year.ToString() + current.Month.ToString() + current.Day.ToString() + current.TimeOfDay.ToString();
            for(int i = 0; i < pressList.Count; i++)
            {
                csv.Append(pressList[i].ToString() + ",");
                csv.Append(currentList[i].ToString() + ",");
                csv.Append(flowList[i].ToString() + ",");
                csv.AppendLine(tempList[i].ToString());
            }

            File.WriteAllText(Directory.GetCurrentDirectory() + @"/" + s + ".txt", csv.ToString());
        }
    }
}
