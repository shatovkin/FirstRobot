using QuikSharp;
using QuikSharp.DataStructures;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
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
       
        private int emaPeriodSeven = 7;

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
                    // для отладки
                    //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    //Trace.Listeners.Add(new TextWriterTraceListener("TraceLogging.log"));
                    // для отладки
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
        }

        private void remoteConnection_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                foreach (var secCode in toolList)
                {
                    Run(secCode);
                }
               // Thread.Sleep(20000);
            }
        }

        void Run(string secCode)
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
                        List<Candle> cadles = _quik.Candles.GetLastCandles(classCode, secCode, QuikSharp.DataStructures.CandleInterval.H1, 14).Result;
                        int counter = 0;
                      //  int candleIndex = 0;

                        // Расчёт предыдущего ЕМА
                        foreach (var candle in cadles)
                        {
                            if (counter < 7)
                            { 
                                previousEma += decimal.ToDouble(candle.Close);

                                if (counter == 6 ) {
                                    previousEma = previousEma / emaPeriodSeven;
                                }
                            }
                            else
                            {
                               previousEma = emaCalculation(decimal.ToDouble(candle.Close), previousEma, decimal.ToDouble(emaPeriodSeven));
                               // candleIndex++;
                            }
                            counter++;
                        }

                        double s = previousEma/0.5; 
                        //decimal pricePositive = tool.LastPrice - currentEma;
                        //decimal priceNegative = currentEma - tool.LastPrice;
                        //decimal percent = ((pricePositive - currentEma) / (pricePositive + currentEma) / 2) / 100;

                        ////Цена выше EMA
                        //if (pricePositive > currentEma)
                        //{
                        //    if (decimal.ToDouble(percent) > 0.5)
                        //    {
                        //        MessageBox.Show("Подход к чЕМА:" + tool.Name);
                        //        Thread.Sleep(2000);
                        //    }
                        //}
                        //else
                        //{
                        //    //Цена ниже ЕМА
                        //    if (decimal.ToDouble(percent) < 0.5)
                        //    {
                        //        MessageBox.Show("Подход к чЕМА:" + tool.Name);
                        //        Thread.Sleep(2000);
                        //    }
                        //}
                    }
                }
            }
            catch (Exception e)
            {
                textBoxLogsWindow.AppendText("Ошибка получения данных по инструменту." +e.ToString()+ Environment.NewLine);
            }
        }

        private static double emaCalculation(double closePreise, double previosEma, double emaPeriod)
        {
            double k = 2/(emaPeriod+1);
            double ema = closePreise * k + previosEma * (1-k);
            //double ema = (closePreise - previosEma) * k + previosEma;
            return ema;
        }
    }
}
