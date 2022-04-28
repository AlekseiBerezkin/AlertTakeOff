using System;
using System.Collections.Generic;
using System.Text;

namespace AlertTakeOff.Model
{
     class Candle
    {
        public decimal Volume { get; set; }
        public DateTime timeClose { get; set; }
        public bool isGreen { get; set; }
        public DateTime AlertDateTime { get; set; }
    }

    class Assets
    {
        public List<Candle> Candles { get; set; }
        public DateTime AlertDateTime { get; set; }
    }
}
