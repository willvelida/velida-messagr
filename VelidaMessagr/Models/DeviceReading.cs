using System;
using System.Collections.Generic;
using System.Text;

namespace VelidaMessagr.Models
{
    public class DeviceReading
    {
        public string ReadingId { get; set; }
        public decimal Temperature { get; set; }
        public string Location { get; set; }
    }
}
