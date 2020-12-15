using FirstRobot.Model;
using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tulpep.NotificationWindow;

namespace FirstRobot
{
    public partial class MainWindow : Window
    {
        public delegate void UpdatePopup(PopupNotifier notificationPopup);
        //public delegate void DelUpdateUITextBox(string secCode, QuikSharp.DataStructures.CandleInterval interval, 
        //    string message, double percentDifference, int emaInterval);
        public static Quik _quik;
        private Tool tool;
        bool isServerConnected = false;
        bool checkBoxChema = false;
        bool checkBoxDema = false;
        string classCodes = "";
        string clientCode = "";
        List<string> toolList = new List<string>();
        List<string> blackList = new List<string>();
        bool runThreads = true;
        string classes = "";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            if (Properties.Settings.Default.LicenceExists == true)
            {
                this.ConnectBnt.IsEnabled = true;
                this.OpenFilesBtn.IsEnabled = true;
                createFile(ToolUtil.ToolsWhiteListPath,ToolUtil.ToolsString,false);
                createFile(ToolUtil.ToolsBlackListPath,"",false);
                createFile(ToolUtil.CodesListPath,ToolUtil.CodesString,false);

                fillBlackListTxt();
                string[] tools = System.IO.File.ReadAllText(ToolUtil.ToolsWhiteListPath).Replace('"', ' ').Replace(" ", "").Split(',');
                classes = System.IO.File.ReadAllText(ToolUtil.CodesListPath);
                toolList.AddRange(tools);
            }
            else
            {
                LicenceWindow lw = new LicenceWindow(this);
                lw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                lw.Show();
            }
        }

        private void connectToQuik_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("Подключаемся к терминалу Quik...");
                _quik = new Quik(Quik.DefaultPort, new InMemoryStorage());
            }
            catch
            {
                Log("Ошибка инициализации объекта Quik...");
            }

            if (_quik != null)
            {
                Log("Экземпляр Quik создан.");
                try
                {
                    Log("Получаем статус соединения с сервером....");
                    isServerConnected = _quik.Service.IsConnected().Result;
                    if (isServerConnected)
                    {
                        Log("Соединение с сервером установлено.");
                        //RunBtn.IsEnabled = true;
                        RunBtn.IsEnabled = true;
                    }
                    else
                    {
                        Log("Соединение с сервером НЕ установлено.");
                        //RunBtn.IsEnabled = false;
                        RunBtn.IsEnabled = false;
                    }
                }
                catch
                {
                    textBoxLogsWindow.AppendText("Неудачная попытка получить статус соединения с сервером." + Environment.NewLine);
                }
            }
        }

        private void Log(string str)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                textBoxLogsWindow.AppendText(str + Environment.NewLine);
                textBoxLogsWindow.ScrollToEnd();
            }));
        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            runThreads = true;
            checkBoxChema = chemaCheckbox.IsChecked.Value;
            checkBoxDema = demaCheckbox.IsChecked.Value;

            createFile(ToolUtil.ToolsBlackListPath, this.blackListTxt.Text.ToUpper(),true);

            addToBlacklist(this.blackListTxt.Text);

            RunBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
            Log("Программа запущена...");

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                perRun();
            }).Start();
        }

        private void addToBlacklist(string toolsString)
        {
            string[] tools = toolsString.ToUpper().Split(',');

            for (int i = 0; i < tools.Length; i++)
            {
                if (!blackList.Contains(tools[i]))
                {
                    blackList.Add(tools[i].Trim());
                }
            }
        }

        private void Run(string secCode, QuikSharp.DataStructures.CandleInterval interval, string message, double percentDifference, int emaInterval)
        {
            try
            {
                try
                {
                    classCodes = _quik.Class.GetSecurityClass(classes, secCode).Result;

                    if (classCodes == "")
                    {
                        showPopup("Ошибка получения данных по инструменту: " + secCode, "Неправильное имя инструмента");
                        Log("Ошибка получения данных по инструменту: " + secCode);
                    }
                }
                catch
                {
                    Log("Ошибка определения класса инструмента. Убедитесь, что тикер указан правильно");
                }
                if (classCodes != null && classCodes != "")
                {
                    clientCode = _quik.Class.GetClientCode().Result;

                    tool = new Tool(_quik, secCode, classCodes);

                    if (tool != null && tool.Name != null && tool.Name != "")
                    {
                        double previousEma = 0;
                        List<Candle> cadles = _quik.Candles.GetLastCandles(classCodes, secCode, interval, emaInterval * 4).Result;
                        double toolLastPrice = Convert.ToDouble(_quik.Trading.GetParamEx(classCodes, secCode, "LAST").Result.ParamValue);
                        int counter = 0;

                        // Расчёт предыдущего ЕМА
                        foreach (var candle in cadles)
                        {
                            if (counter < emaInterval)
                            {
                                previousEma += toDouble(candle.Close);

                                if (counter == emaInterval - 1)
                                {
                                    previousEma = previousEma / emaInterval;
                                }
                            }
                            else
                            {
                                previousEma = emaCalculation(toDouble(candle.Close), previousEma, toDouble(emaInterval));
                            }
                            counter++;
                        }
                        string messagtest = message;
                        double pricePositive = toolLastPrice - previousEma;

                        ////Цена выше EMA
                        if (pricePositive > 0)
                        {
                            double p1 = (toolLastPrice / previousEma - 1) * 100;

                            if (p1 < percentDifference)
                            {
                                Log(message + tool.Name + " " + DateTime.Now.TimeOfDay.ToString().Substring(0, 5) + " " + secCode);
                                showPopup(message + tool.Name + " (" + secCode + ") " + DateTime.Now.TimeOfDay.ToString().Substring(0, 5), tool.Name + " " + secCode);
                            }
                        }
                        else
                        {
                            double p2 = (previousEma / toolLastPrice - 1) * 100;
                            //Цена ниже ЕМА
                            if (p2 < percentDifference)
                            {

                                Log(message + tool.Name + " " + DateTime.Now.TimeOfDay.ToString().Substring(0, 5) + " " + secCode);
                                showPopup(message + tool.Name + " (" + secCode + ") " + DateTime.Now.TimeOfDay.ToString().Substring(0, 5), tool.Name + " " + secCode);
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log("Ошибка получения данных по инструменту: " + tool.Name);
            }
        }

        private double toDouble(decimal value)
        {
            return decimal.ToDouble(value);
        }

        private static double emaCalculation(double closePrice, double previosEma, double emaPeriod)
        {
            double k = 2 / (emaPeriod + 1);
            double ema = closePrice * k + previosEma * (1 - k);
            return ema;
        }

        private void perRun()
        {
            while (runThreads)
            {
                foreach (var secCode in toolList)
                {
                    if (runThreads)
                    {
                        //Blacklist
                        if (!blackList.Contains(secCode.Trim()))
                        {
                            string val1 = null, val2 = null;
                            this.Dispatcher.Invoke((Action)(() =>
                            {//this refer to form in WPF application 
                            val1 = chemaPercentTxt.Text;
                                val2 = demaPercentTxt.Text;
                            }));

                            if (checkBoxChema == true)
                            {
                                Run(secCode, QuikSharp.DataStructures.CandleInterval.H1, ToolUtil.messageH7, Convert.ToDouble(val1), 7);
                                Run(secCode, QuikSharp.DataStructures.CandleInterval.H1, ToolUtil.messageH14, Convert.ToDouble(val1), 14);
                            }

                            if (checkBoxDema == true)
                            {
                                Run(secCode, QuikSharp.DataStructures.CandleInterval.D1, ToolUtil.messageD7, Convert.ToDouble(val2), 7);
                                Run(secCode, QuikSharp.DataStructures.CandleInterval.D1, ToolUtil.messageD14, Convert.ToDouble(val2), 14);
                            }
                        }
                    }
                }
                Thread.Sleep(5000);
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            runThreads = false;
            StopBtn.IsEnabled = false;
            RunBtn.IsEnabled = true;
            Log("Программа остановлена...");
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            myWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            myWindow.Left = System.Windows.SystemParameters.PrimaryScreenWidth - 290;
            myWindow.Top = System.Windows.SystemParameters.PrimaryScreenHeight - 535;
        }

        public void showPopup(string message, string title)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {   //this refer to form in WPF application 
                PopupNotifier popUp = new PopupNotifier();
                popUp.TitleText = title;
                popUp.ContentText = message;
                popUp.ContentFont = new System.Drawing.Font("Arial", 14F);
                popUp.Size = new System.Drawing.Size(600, 100);
                popUp.Popup();
            }));
        }

        private void createFile(string pathString,string toolsString,bool delete)
        {

            if (!Directory.Exists(ToolUtil.OrderPath))
            {
                Directory.CreateDirectory(ToolUtil.OrderPath);
            }

            if (delete == true)
            {
                System.IO.File.Delete(pathString);
            }

            if (!System.IO.File.Exists(pathString))
            {
               
                using (System.IO.FileStream fs = System.IO.File.Create(pathString))
                {
                }

                using (System.IO.StreamWriter file =
                          new System.IO.StreamWriter(pathString, false))
                {
                    file.Write(toolsString);
                }
            }
            else
            {
                Console.WriteLine("File \"{0}\" already exists.");
                return;
            }
        }

        private void OpenFiles_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer", ToolUtil.OrderPath);
        }

        private void blackListTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void fillBlackListTxt()
        {
            var line = "";
            var lines = File.ReadAllLines(ToolUtil.ToolsBlackListPath);
            for (var i = 0; i < lines.Length; i += 1)
            {
                line = lines[i];
            }

            this.blackListTxt.Text = line.Trim().Replace(" ", "");
        }
    }
}
