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
using System.Collections.Concurrent;

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
        ConcurrentQueue<byte> getByteQueue = new ConcurrentQueue<byte>();
        ConcurrentQueue<byte[]> sendByteQueue = new ConcurrentQueue<byte[]>(); 

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
            port.RtsEnable = true;

            //port.Write(new byte[] {0x02, 0x00, 0x3C, 0x3C, 0x00}, 0 , 5);

            sendByteQueue.Enqueue(new byte[] { 0x02, 0x00, 0x3C, 0x3C, 0x00 });

        }

        private void buttonConnectClick(object sender, RoutedEventArgs e)
        {

            port = new SerialPort(comboBox.Text, 9600, Parity.None, 8, StopBits.One);
            Thread addByteToQueueThread = new Thread(addByteToQueue);
            addByteToQueueThread.Start();
            Thread checkFrameThread = new Thread(checkFrame);
            checkFrameThread.Start();

        }

        private void addByteToQueue()
        {
            port.Open();
            while (true)
            {
                if (port.BytesToRead > 0)
                {
                    getByteQueue.Enqueue((byte)port.ReadByte());
                }
            }
            port.Close();
        }

        private void checkFrame()
        {
            byte bajt;
            int frameStatus = 0;
            int frameDataLenght = 0;
            int commandCode = 0;
            List<int> byteList = new List<int>();
            List<string> frameList = new List<string>();
            List<string> sendList = new List<string>();
            int sumControll = 0;
            int a = 0;
            int checkSumControll = 0;

            while (true)
            {

                if (getByteQueue.TryDequeue(out bajt))
                {

                    switch (frameStatus)
                    {
                        case 0:
                            if (bajt == 0x02 || bajt == 0x03)
                            {
                                frameList.Add(bajt.ToString("X2"));
                                frameStatus++;
                            }
                            else if (bajt == 0x3f)
                            {
                                frameList.Add(bajt.ToString("X2"));
                                byte[] sendByteQueue1;

                                if(sendByteQueue.TryDequeue(out sendByteQueue1))
                                {
                                    port.Write(sendByteQueue1, 0, sendByteQueue.Count);
                                    
                                    foreach(byte dataByte in sendByteQueue1)
                                        {
                                        sendList.Add(dataByte.ToString("X2"));
                                        }

                                    reciveTextBox.Dispatcher.Invoke(() => reciveTextBox.Text = String.Join(" ", sendList));
                                    reciveTextBox.Dispatcher.Invoke(() => reciveTextBox.AppendText(Environment.NewLine));
                                    sendList.Clear();

                                    port.RtsEnable = false;
                                }
                            }
                            break;

                        case 1:
                            frameDataLenght = bajt;
                            frameList.Add(bajt.ToString("X2"));
                            frameStatus++;
                            checkSumControll += bajt;
                            break;

                        case 2:
                            commandCode = bajt;
                            frameList.Add(bajt.ToString("X2"));
                            frameStatus++;
                            checkSumControll += bajt;
                            break;

                        case 3:
                            checkSumControll += bajt;
                            frameList.Add(bajt.ToString("X2"));
                            byteList.Add(bajt);
                            if (byteList.Count == frameDataLenght)
                            {
                                frameStatus++;
                            }

                            break;

                        case 4:
                            if (a == 0)
                            {
                                sumControll |= bajt;
                                frameList.Add(bajt.ToString("X2"));
                                a++;
                            }
                            else if (a == 1)
                            {
                                sumControll |= (bajt << 8);;
                                a = 0;
                                frameList.Add(bajt.ToString("X2"));

                                if (checkSumControll == sumControll)
                                {
                                    Console.WriteLine("ZGADZA SIĘ SUMA KONTROLNA");
                                    reciveTextBox.Dispatcher.Invoke(() => reciveTextBox.Text = String.Join(" ", frameList));
                                    reciveTextBox.Dispatcher.Invoke(() => reciveTextBox.AppendText(Environment.NewLine));

                                    port.Write(new byte[] { 0x06 }, 0, 1);
                                    sendTextBox.Dispatcher.Invoke(() => sendTextBox.Text = String.Join(" ", "0x06"));
                                    sendTextBox.Dispatcher.Invoke(() => sendTextBox.AppendText(Environment.NewLine));
                                    frameList.Clear();
                                }
                                else
                                {
                                    Console.WriteLine("SUMA SIĘ NIE ZGADZA, PONOWNE ZAPYTANIE O RAMKĘ");
                                    port.Write(new byte[] { 0x15 }, 0, 1);
                                    sendTextBox.Dispatcher.Invoke(() => sendTextBox.Text = String.Join(" ", "0x15"));
                                    sendTextBox.Dispatcher.Invoke(() => sendTextBox.AppendText(Environment.NewLine));
                                    frameList.Clear();
                                }

                                frameStatus = 0;
                                frameDataLenght = 0;
                                commandCode = 0;
                                byteList.Clear();
                                sumControll = 0;
                                checkSumControll = 0;

                            }

                            break;
                    }

                }

            }

        }
        /*
        private void layerButtonClick(object sender, RoutedEventArgs e)
        {
            List<string> frameList = new List<string>();
            frameList.Add("0x02");
            frameList.Add("0x01");
            frameList.Add("0xFF");
            frameList.Add("0x3a");
            frameList.Add("0x5c");
            frameList.Add("0x00");
            reciveTextBox.Dispatcher.Invoke(() => reciveTextBox.Text += String.Join(" ", frameList));
            reciveTextBox.Dispatcher.Invoke(() => reciveTextBox.AppendText(Environment.NewLine));
        }*/
    }
}
