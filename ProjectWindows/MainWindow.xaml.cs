using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;

namespace ProjectWindows
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {

        SerialPort port;
        String[] test = { };
        String[] ports = { };
        List<int> byteList = new List<int>();


        public MainWindow()
        {
            InitializeComponent();
            Thread checkPortsThread = new Thread(getAvailablePorts);
            checkPortsThread.Start();

        }

        void getAvailablePorts()
        {


            while (true)
            {

                ports = SerialPort.GetPortNames();
                if (test.SequenceEqual(ports) == false)
                {
                    comboBox.Dispatcher.Invoke(() => comboBox.Items.Clear());
                    Array.Clear(test, 0, test.Length);

                    foreach (String port in ports)
                    {
                        comboBox.Dispatcher.Invoke(() => comboBox.Items.Add(port));
                        test = (string[])ports.Clone();
                    }
                }
              


                Thread.Sleep(1000);
            }
        }

        private void buttonResetClick(object sender, RoutedEventArgs e)
        {

        }

        private void buttonConnectClick(object sender, RoutedEventArgs e)
        {

                port = new SerialPort(comboBox.Text, 9600, Parity.None, 8, StopBits.One);
                Thread readFromPortThread = new Thread(readFromComPort);
                readFromPortThread.Start();

        }

        private void readFromComPort()
        {
            port.Open();
            while (true)
            {

                if (port.BytesToRead > 0)
                {
                    
                    byteList.Add(port.ReadByte());
                    foreach (int element1 in byteList)
                    {
                        Console.WriteLine(element1);
                    }

                }
                

                foreach(int element1 in byteList)
                {
                    Console.WriteLine(element1);
                }
            }
            port.Close();
        }

        private void buttonRefreshClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
