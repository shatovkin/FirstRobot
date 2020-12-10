using FirstRobot.Model;
using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Tulpep.NotificationWindow;

namespace FirstRobot
{
    public partial class MainWindow : Window
    {
        public static Quik _quik;
        private Tool tool;
        bool isServerConnected = false;

        string classCode = "";
        string clientCode = "";
        List<string> toolList;
        List<string> blackList = new List<string>();

        bool runThreads = true;


        public MainWindow()
        {
            InitializeComponent();
            toolList = new List<string>() {
             "AFSK","CHMF","VTBR","SNGS","MOEX","PLZL","TATN","AFLT","ALRS","ROSN","NVTK","MGNT","LKOH","GAZP","SBER","IMOEX"
            };
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
            textBoxLogsWindow.AppendText(str + Environment.NewLine);
            textBoxLogsWindow.ScrollToEnd();
        }

        private void remoteConnection_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            RunBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
            Log("Программа запущена...");

            perRun();
        }

        void Run(string secCode, QuikSharp.DataStructures.CandleInterval interval, string message, double percentDifference, int emaInterval)
        {
            try
            {
                try
                {
                    classCode = _quik.Class.GetSecurityClass("SPBFUT,TQBR,TQBS,TQNL,TQLV,TQNE,TQOB,QJSIM", secCode).Result;
                }
                catch
                {
                    textBoxLogsWindow.AppendText("Ошибка определения класса инструмента. Убедитесь, что тикер указан правильно" + Environment.NewLine);
                }
                if (classCode != null && classCode != "")
                {
                    clientCode = _quik.Class.GetClientCode().Result;

                    tool = new Tool(_quik, secCode, classCode);

                    if (tool != null && tool.Name != null && tool.Name != "")
                    {
                        double previousEma = 0;
                        List<Candle> cadles = _quik.Candles.GetLastCandles(classCode, secCode, interval, emaInterval * 4).Result;
                        double toolLastPrice = Convert.ToDouble(_quik.Trading.GetParamEx(classCode, secCode, "LAST").Result.ParamValue);
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
                                blackList.Add(tool.Name);
                                Log(message + tool.Name);
                                MessageBox.Show(message + tool.Name);
                                Thread.Sleep(2000);
                            }
                        }
                        else
                        {
                            double p2 = (previousEma / toolLastPrice - 1) * 100;
                            //Цена ниже ЕМА
                            if (p2 < percentDifference)
                            {
                                blackList.Add(tool.Name);
                                Log(message + tool.Name);
                                MessageBox.Show(message + tool.Name);
                                Thread.Sleep(2000);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log("Ошибка получения данных по инструменту." + e.ToString());
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

        void perRun()
        {
            runThreads = true;

            while (runThreads)
            {
                foreach (var secCode in toolList)
                {
                    Run(secCode, QuikSharp.DataStructures.CandleInterval.H1, ToolUtil.messageH7, Convert.ToDouble(chemaPercentTxt.Text), 7);
                    Run(secCode, QuikSharp.DataStructures.CandleInterval.D1, ToolUtil.messageD7, Convert.ToDouble(chemaPercentTxt.Text), 7);
                    Run(secCode, QuikSharp.DataStructures.CandleInterval.H1, ToolUtil.messageH14, Convert.ToDouble(chemaPercentTxt.Text), 14);
                    Run(secCode, QuikSharp.DataStructures.CandleInterval.D1, ToolUtil.messageD14, Convert.ToDouble(chemaPercentTxt.Text), 14);
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
    }
}
