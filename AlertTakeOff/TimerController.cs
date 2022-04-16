using AlertTakeOff.Model;
using AlertTakeOff.Provider;
using Binance.Net;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlertTakeOff
{
    internal class TimerController
    {
        Timer timer;

        static Dictionary<string, decimal> mean = new Dictionary<string, decimal>();
        static Dictionary<string,List<Candle>> candles = new Dictionary<string, List<Candle>>();
        static Dictionary<string, Candle> candlesTemp = new Dictionary<string, Candle>();
        static BinanceSocketClient socketClient = new BinanceSocketClient();
        static DateTime dtOpen= new DateTime();
        static CallResult<UpdateSubscription> socet;
        static Logger logger = LogManager.GetCurrentClassLogger();

        static int CountCandle = Properties.Settings.Default.NumberСandles;

        public TimerController()
        {
        }

       public async Task Start()
        {
            try
            {
                TelegaBot tg = new TelegaBot();
                await tg.sendStart();

                int startHuor = int.Parse(Properties.Settings.Default.TimeStart) - 3;

                logger.Info("Процесс запущен");

                TimeSpan timeNow = DateTime.UtcNow.TimeOfDay;
                TimeSpan tStart = new TimeSpan(startHuor, 0, 0);
                TimeSpan interval = new TimeSpan(24, 0, 0);
                if (timeNow <= tStart)
                {
                    timer = new Timer(callback, null, interval - (tStart - timeNow), interval);
                }
                else
                {
                    TimeSpan del = interval - (timeNow - tStart);
                    timer = new Timer(callback, null, del, interval);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);

                System.Windows.MessageBox.Show("Ошибка при запуске программы","ОШИБКА",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Error);
            }
        }

        static async private void callback(object o)
        {

            mean.Clear();
            candles.Clear();
            candlesTemp.Clear();

            if (socet != null)
                await socet.Data.CloseAsync();

            TelegaBot tg = new TelegaBot();
            
            try
            {
                await tg.sendMessage("Получаем данные по свечам");

                mean.Clear();

                int hStart = StrToInt(Properties.Settings.Default.StartHours)-3;
                int hStop = StrToInt(Properties.Settings.Default.StopHours)-3;
                int days = 0;

                if (hStart < 0)
                {
                    hStart = 24 - hStart;
                    days = 1;
                }
                    if (hStop < 0)
                    hStop = 24 - hStop;

                DateTime dtStart = new DateTime(
                            DateTime.Now.Year,
                            DateTime.Now.Month,
                            DateTime.Now.Day,
                            hStart,
                            00, 00);

                dtStart = dtStart.AddDays(days);

                DateTime dtStop = new DateTime(
                            DateTime.Now.Year,
                            DateTime.Now.Month,
                            DateTime.Now.Day,
                            hStop,
                            00, 00);

                var pairs = FileProvider.ReadFile();
                var factor = Properties.Settings.Default.Factor;

                foreach (var p in pairs)
                {
                    int redKline = 0;
                    int greenKline = 0;
                    decimal volume = 0;
                    int zeroRedNumb = 0;
                    int zeroGreenNumb = 0;

                    using (BinanceClient binanceClient = new BinanceClient())
                    {
                        var res = await binanceClient.Spot.Market.GetKlinesAsync(p, Binance.Net.Enums.KlineInterval.OneMinute, dtStart, dtStop, 1000);

                        foreach (var kline in res.Data)
                        {
                            if (kline.Open > kline.Close)
                            {
                                redKline++;
                                if (kline.QuoteVolume == 0)
                                {
                                    zeroRedNumb++;
                                }
                            }
                            else if (kline.Open <= kline.Close)
                            {
                                greenKline++;
                                volume += kline.QuoteVolume;
                                if (kline.QuoteVolume == 0)
                                {
                                    zeroGreenNumb++;
                                }
                            }
                        }

                        mean.Add(p, factor * Math.Round(((volume) / (greenKline==0 ? 1 : greenKline)), 3));
                    }
                }

                StringBuilder str = new StringBuilder();
                str.Append("Средние значения\n");

                foreach (var v in mean)
                {
                    str.Append($"{v.Key} {v.Value}\n");
                }
                logger.Info(str);
                str.Clear();

                foreach (var s in pairs)
                {
                    var l = new List<Candle>();
                    candles.Add(s, l);
                    candlesTemp.Add(s, new Candle
                    {
                        timeClose = DateTime.Now,
                        Volume = 0
                    });
                }

                await tg.sendMessage("Средние значения активов получены");

                socet = await socketClient.Spot.SubscribeToKlineUpdatesAsync(pairs, Binance.Net.Enums.KlineInterval.OneMinute, zbs => {

                    candlesTemp[zbs.Data.Symbol].Volume = zbs.Data.Data.QuoteVolume;


                    if (candlesTemp[zbs.Data.Symbol].timeClose != zbs.Data.Data.CloseTime)
                    {
                        candles[zbs.Data.Symbol].Add(new Candle
                        {
                            Volume = candlesTemp[zbs.Data.Symbol].Volume,
                            timeClose = candlesTemp[zbs.Data.Symbol].timeClose
                        });

                        candlesTemp[zbs.Data.Symbol].timeClose = zbs.Data.Data.CloseTime;

                        if (candles[zbs.Data.Symbol].Count >= 4)
                        {
                            if (candles[zbs.Data.Symbol].Count > 4)
                                candles[zbs.Data.Symbol].RemoveAt(0);

                            Task.Run(async () => {

                                for (int i = 0; i < candles[zbs.Data.Symbol].Count; i++)
                                {
                                    if (candles[zbs.Data.Symbol][i].Volume < mean[zbs.Data.Symbol])
                                        break;

                                    if (i == candles[zbs.Data.Symbol].Count - 1 && candles[zbs.Data.Symbol][i].Volume < mean[zbs.Data.Symbol])
                                    {
                                        TelegaBot bot = new TelegaBot();
                                        await bot.sendAlert(zbs.Data.Symbol,mean[zbs.Data.Symbol],Properties.Settings.Default.NumberСandles);
                                        logger.Info($"Сформирован заданный патерн:{zbs.Data.Symbol} среднее значение объема {mean[zbs.Data.Symbol]}");
                                    }
                                }
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                await tg.sendMessage("ОШИБКА: "+ex);
                logger.Error(ex);
            }
            }

        private static int StrToInt(string data)
        {
            return int.Parse(data);
        }
    }
}
