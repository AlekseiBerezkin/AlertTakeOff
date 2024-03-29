﻿using AlertTakeOff.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AlertTakeOff.Provider
{
    class TelegaBot
    {
        static string botToken = Properties.Settings.Default.TGtoken;
        private TelegramBotClient Bot;

         public TelegaBot()
        {
            Bot= new TelegramBotClient(botToken);
        }
        private static string getLink(string name)
        {
            string baseLinkBinance = "https://www.binance.com/ru/trade/";
            string currency = name.Replace("0", "");
            currency = currency.Replace("1", "");
            return baseLinkBinance + currency.Replace("USDT", "_USDT");
        }

        public async Task sendAlert(string name, decimal volume, int klineCount, decimal factor, List<Candle> candles)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("\nВремя закрытия| объем\n");

            foreach (var c in candles)
            {
                sb.Append($"{c.timeClose.AddHours(3)}|{c.Volume}\n");
            }

            string []chatIdList = Properties.Settings.Default.ChatId.Split(',');

            
            foreach (string chatId in chatIdList)
            {
                await Bot.SendTextMessageAsync(chatId.ToString(), $"Актив #{name} сформировал заданный патерн.\n Все {klineCount} свечи превысили средний объем {volume/factor}.\n Множитель {factor}. Рассчетный объем {volume}.\n {sb}" +
                    $"[Binance]({getLink(name)})", ParseMode.Markdown, disableWebPagePreview: true);
            }
            
        }

        public async Task sendStart()
        {
            string[] chatIdList = Properties.Settings.Default.ChatId.Split(',');


            foreach (string chatId in chatIdList)
            {
                await Bot.SendTextMessageAsync(chatId.ToString(), $"Программа запущена");
            }
        }

        public async Task sendMessage(string message)
        {
            string[] chatIdList = Properties.Settings.Default.ChatId.Split(',');

            foreach (string chatId in chatIdList)
            {
                await Bot.SendTextMessageAsync(chatId.ToString(), $"{message}");
            }
        }
    }
}
