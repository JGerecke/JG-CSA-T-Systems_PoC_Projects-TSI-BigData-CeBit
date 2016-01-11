using System.Collections.Generic;

namespace DeviceEvents
{
    public class EventDataStructure
    {
        public string event_type { get; set; }
        public string event_measurement_id { get; set; }
        public string event_source_id { get; set; }
        public string event_time { get; set; }
        public Dictionary<string, decimal> event_measurement;
    }
}
