using System;
using System.Collections.Generic;
using System.Text;

namespace AlertTakeOff.Model
{
     class Candle
    {
        public decimal Volume { get; set; }
        public DateTime timeClose { get; set; }
        public decimal PriceClose { get; set; }
        public decimal PriceOpen { get; set; }
    }
}
