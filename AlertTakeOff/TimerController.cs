using AlertTakeOff.Model;
using AlertTakeOff.Provider;
using Binance.Net;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlertTakeOff
{
    internal class TimerController
    {
        static Timer timer;

        static Dictionary<string, decimal> mean = new Dictionary<string, decimal>();
        static Dictionary<string,List<Candle>> candles = new Dictionary<string, List<Candle>>();
        static Dictionary<string, Candle> candlesTemp = new Dictionary<string, Candle>();
        static BinanceSocketClient socketClient = new BinanceSocketClient();
        static CallResult<UpdateSubscription> socet;
        static Logger logger = LogManager.GetCurrentClassLogger();

        static int CountCandle = Properties.Settings.Default.NumberСandles;

        public TimerController()
        {
        }

       public async Task Start()
        {
            callback(0);
            try
            {
                TelegaBot tg = new TelegaBot();
                await tg.sendStart();

                int startHuor = int.Parse(Properties.Settings.Default.TimeStart) - 3;

                logger.Info("Процесс запущен");

                var timeNow = DateTime.UtcNow.TimeOfDay;
                var tStart = new TimeSpan(startHuor, 0, 0);
                var interval = new TimeSpan(24, 2, 0);
                if (timeNow <= tStart)
                {
                    var t = tStart - timeNow;
                    timer = new Timer(callback, null, t, interval);
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

                        var res = await GetKlines(p, dtStart, dtStop, binanceClient);
                        if (res == null)
                            continue;

                        foreach (var kline in res)
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

                await tg.sendMessage("Средние значения активов получены");
                var str = new StringBuilder();
                str.Append($"Средние значения за интервал с {dtStart.AddHours(3)} по {dtStop.AddHours(3)}\n");

                foreach (var v in mean)
                {
                    str.Append($"{v.Key} {v.Value}\n");
                }

                logger.Info(str.ToString());
                await tg.sendMessage(str.ToString());
                str.Clear();

                foreach (var s in pairs)
                {
                    var l = new List<Candle>();
                    candles.Add(s, l);
                    candlesTemp.Add(s, new Candle
                    {
                        timeClose =new DateTime(),
                        Volume = 0
                    });
                }

                

                socet = await socketClient.Spot.SubscribeToKlineUpdatesAsync(pairs, Binance.Net.Enums.KlineInterval.OneMinute,async zbs => {

                    if (candlesTemp[zbs.Data.Symbol].timeClose != zbs.Data.Data.CloseTime)
                    {

                        candles[zbs.Data.Symbol].Add(new Candle
                        {
                            Volume = candlesTemp[zbs.Data.Symbol].Volume,
                            timeClose = candlesTemp[zbs.Data.Symbol].timeClose,
                            PriceClose = candlesTemp[zbs.Data.Symbol].PriceClose,
                            PriceOpen = candlesTemp[zbs.Data.Symbol].PriceOpen
                        });

                        candlesTemp[zbs.Data.Symbol].timeClose = zbs.Data.Data.CloseTime;

                        if (candles[zbs.Data.Symbol].Count >= CountCandle)
                        {
                            if (candles[zbs.Data.Symbol].Count > CountCandle)
                                candles[zbs.Data.Symbol].RemoveAt(0);

                            for (int i = 0; i < candles[zbs.Data.Symbol].Count; i++)
                                {
                                    if (candles[zbs.Data.Symbol][i].Volume < mean[zbs.Data.Symbol] || candles[zbs.Data.Symbol][i].Volume==0 || candles[zbs.Data.Symbol][i].PriceOpen== candles[zbs.Data.Symbol][i].PriceClose)
                                        break;

                                    if (i == candles[zbs.Data.Symbol].Count - 1 && candles[zbs.Data.Symbol][i].Volume >= mean[zbs.Data.Symbol])
                                    {
                                        var green = candles[zbs.Data.Symbol].Where(p => p.PriceOpen>p.PriceClose);

                                        if (green.Count() != 0)
                                            break;

                                        TelegaBot bot = new TelegaBot();
                                        await bot.sendAlert(zbs.Data.Symbol,mean[zbs.Data.Symbol],Properties.Settings.Default.NumberСandles);
                                        logger.Info($"Сформирован заданный патерн:{zbs.Data.Symbol} среднее значение объема {mean[zbs.Data.Symbol]}");
                                    }
                                }

                        }
                    }
                    else
                    {
                        candlesTemp[zbs.Data.Symbol].Volume = zbs.Data.Data.QuoteVolume;
                        candlesTemp[zbs.Data.Symbol].PriceOpen = zbs.Data.Data.Open;
                        candlesTemp[zbs.Data.Symbol].PriceClose = zbs.Data.Data.Close;
                    }
                });
            }
            catch (Exception ex)
            {
                await tg.sendMessage("ОШИБКА: "+ex);
                logger.Error(ex);
            }
            }

        private static async Task<IEnumerable<IBinanceKline>> GetKlines(string name,DateTime timeStart,DateTime timeStop, BinanceClient binanceClient)
        {
            try
            {
                var res = await binanceClient.Spot.Market.GetKlinesAsync(name, Binance.Net.Enums.KlineInterval.OneMinute, timeStart, timeStop, 1000);
                return res.Data;
            }
            catch (Exception ex)
            {
                TelegaBot tg = new TelegaBot();
                await tg.sendMessage($"Ошибка по токену {name}. Из анализа исключен");

                return null;
            }

            
        }

        private static int StrToInt(string data)
        {
            return int.Parse(data);
        }
    }
}
