using System.Collections.Generic;

namespace DeviceEvents
{
    // documentation https://www.cumulocity.com/guides/reference/real-time-notifications/

    public class EventKeyValue
    {
        public string unit { get; set; }
        public decimal value { get; set; }
    }

    public class deviceType01
    {
        public EventKeyValue Dampfleistung { get; set; }
        public EventKeyValue Dampftemperatur { get; set; }
        public EventKeyValue Dampfdruck { get; set; }
        public EventKeyValue Sollwert_Dampfdruck { get; set; }
    }
    public class deviceType02
    {
        public EventKeyValue Dampfdruck { get; set; }
    }
    public class deviceType03
    {
        public EventKeyValue Flammenstrom { get; set; }
    }
    public class deviceType04
    {
        public EventKeyValue Gleitschieberventil { get; set; }
    }
    public class deviceType05
    {
        public EventKeyValue Kaminzug { get; set; }
        public EventKeyValue Lufteintrittstemperatur { get; set; }
        public EventKeyValue Luftdruck_Aufstellraum { get; set; }
        public EventKeyValue Rauchgastemperatur { get; set; }
    }
    public class deviceType06
    {
        public EventKeyValue Kesselgroesse { get; set; }
    }
    public class deviceType07
    {
        public EventKeyValue Leistungswert { get; set; }
    }
    public class deviceType08
    {
        public EventKeyValue Netzspannung_LME7 { get; set; }
    }
    public class deviceType09
    {
        public EventKeyValue Temperatur { get; set; }
    }
    public class deviceType10
    {
        public EventKeyValue Vordruck { get; set; }
        public EventKeyValue Speisewassertemperatur_Kesseleintritt { get; set; }
        public EventKeyValue Speisewassertemperatur_Behaelter_CVE { get; set; }
        public EventKeyValue Speisepumpe { get; set; }
        public EventKeyValue Sollwert_Speisepumpe { get; set; }
        public EventKeyValue Durchfluss { get; set; }
    }

    public class CumulocitySource
    {
        public string id { get; set; }
        public string self { get; set; }
    }
    public class CumulocityEventData
    {
        public string id { get; set; }
        public string self { get; set; }
        public CumulocitySource source { get; set; }
        public string time { get; set; }
        public string type { get; set; }
        public deviceType01 certuss_Dampf { get; set; }
        public deviceType02 certuss_Dampfnetz { get; set; }
        public deviceType03 certuss_Flammenstrom { get; set; }
        public deviceType04 certuss_Gleitschieberventil { get; set; }
        public deviceType05 certuss_Kamin { get; set; }
        public deviceType06 certuss_Kesselgroesse { get; set; }
        public deviceType07 certuss_LME7 { get; set; }
        public deviceType08 certuss_Netzspannung { get; set; }
        public deviceType09 certuss_Schaltschrank { get; set; }
        public deviceType10 certuss_Speisung { get; set; }
        public string creationTime { get; set; }
        public string text { get; set; }
    }
    public class ConnectionEventData
    {
        public CumulocityEventData data { get; set; }
        public string realtimeAction { get; set; }
    }
    public class HandshakeRequest
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string version { get; set; }
        public string minimumVersion { get; set; }
        public string[] supportedConnectionTypes { get; set; }
        public Advice advice { get; set; }
    }
    public class Advice
    {
        public int timeout { get; set; }
        public int interval { get; set; }
    }
    public class HandshakeResponse
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string version { get; set; }
        public string minimumVersion { get; set; }
        public string[] supportedConnectionTypes { get; set; }
        public string clientId { get; set; }
        public bool successful { get; set; }
        public string error { get; set; }
    }
    public class SubscribeRequest
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public string subscription { get; set; }
    }
    public class SubscribeResponse
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public string subscription { get; set; }
        public bool successful { get; set; }
        public string error { get; set; }
    }
    public class UnsubscribeRequest
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public string subscription { get; set; }
    }
    public class UnsubscribeResponse
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public string subscription { get; set; }
        public bool successful { get; set; }
        public string error { get; set; }
    }
    public class ConnectRequest
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public string connectionType { get; set; }
        public Advice advice { get; set; }
    }
    public class ConnectResponse
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public bool successful { get; set; }
        public ConnectionEventData data { get; set; }
        public string error { get; set; }
    }
    public class DisconnectRequest
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
    }
    public class DisconnectResponse
    {
        public int id { get; set; }
        public string channel { get; set; }
        public string clientId { get; set; }
        public bool successful { get; set; }
        public string error { get; set; }
    }
}
