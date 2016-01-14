using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Script.Serialization;

namespace DeviceEvents
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>

    internal sealed class DeviceEvents : StatelessService
    {

        //public string service_url = "https://certuss.cumulocity.com/cep/realtime";
        public string devicecloud_url = WebConfigurationManager.AppSettings["DeviceCloudServiceUrl"];
        public string devicecloud_user = WebConfigurationManager.AppSettings["DeviceCloudUser"];
        public string devicecloud_password = WebConfigurationManager.AppSettings["DeviceCloudPassword"];
        public string connectionString = WebConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
        public string eventHubName = WebConfigurationManager.AppSettings["EventHubName"];
        public HttpStatusCode deviceCloudResult = HttpStatusCode.Created;

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            // TODO: If your service needs to handle user requests, return a list of ServiceReplicaListeners here.
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancelServiceInstance">Canceled when Service Fabric terminates this instance.</param>
        protected override async Task RunAsync(CancellationToken cancelServiceInstance)
        {
            // ServiceMassage Status
            ServiceEventSource.Current.ServiceMessage(this, "Start FabricService - DeviceEvents Service");

            // Initializing Cumulocity Device Cloud Communication
            List<ConnectResponse> connectResponse = new List<ConnectResponse>();
            List<DisconnectResponse> disconnectResponse = new List<DisconnectResponse>();
            Advice handshakeAdvise = new Advice();
            // extended timeout to 9 min.
            handshakeAdvise.timeout = 90000;
            handshakeAdvise.interval = 0;
            string[] connectionTypes = new string[1] { "long-polling" };
            HandshakeRequest handshake = new HandshakeRequest();
            handshake.channel = "/meta/handshake";
            handshake.version = "1.0";
            handshake.minimumVersion = "0.9";
            handshake.supportedConnectionTypes = connectionTypes;
            handshake.advice = handshakeAdvise;

            // This service instance continues processing until the instance is terminated.
            while (!cancelServiceInstance.IsCancellationRequested)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Start Cumulocity Communication");
                // ======================================================================================
                // Implementation of Cumulocity Device Cloud 
                // ======================================================================================

                HandshakeResponse[] handshakeResponse = DeviceCloudHandshake(handshake, devicecloud_url);
                handshake = null;
                if (handshakeResponse[0].successful == true)
                {
                    SubscribeRequest subscribeRequest = new SubscribeRequest();
                    subscribeRequest.channel = "/meta/subscribe";
                    subscribeRequest.clientId = handshakeResponse[0].clientId;
                    subscribeRequest.subscription = "/measurements/*";
                    SubscribeResponse[] subscribeResponse = DeviceCloudSubscribe(subscribeRequest, devicecloud_url);
                    subscribeRequest = null;
                    if (subscribeResponse[0].successful == true)
                    {
                        DateTime endTime = DateTime.Now.AddMinutes(10);
                        bool connectionTimeOut = false;
                        while (!connectionTimeOut)
                        {
                            //ConnectRequest connectRequest = new ConnectRequest();
                            //connectRequest.clientId = handshakeResponse[0].clientId;
                            //connectRequest.channel = "/meta/connect";
                            //connectRequest.connectionType = "long-polling";
                            //connectRequest.advice = handshakeAdvise;

                            //// call SendIoTEvents here

                            //connectRequest = null;
                            //connectResponse = null;

                            //DisconnectRequest disconnectRequest = new DisconnectRequest();
                            //disconnectRequest.clientId = handshakeResponse[0].clientId;
                            //disconnectRequest.channel = "/meta/disconnect";
                            //disconnectResponse = DeviceCloudDisconnect(disconnectRequest, devicecloud_url);
                            //disconnectRequest = null;
                            //disconnectResponse = null;

                            // ======================================================================================
                            if (DateTime.Now <= endTime) connectionTimeOut = true;
                            ServiceEventSource.Current.ServiceMessage(this, "Cumulocity communication - reached communicationTimeout 10 min.");
                        }
                        UnsubscribeRequest unsubscribeRequest = new UnsubscribeRequest();
                        unsubscribeRequest.channel = "/meta/unsubscribe";
                        unsubscribeRequest.clientId = handshakeResponse[0].clientId;
                        unsubscribeRequest.subscription = "/measurements/*";
                        UnsubscribeResponse[] unsubscribeResponse = DeviceCloudUnsubscribe(unsubscribeRequest, devicecloud_url);
                        unsubscribeRequest = null;
                        unsubscribeResponse = null;
                    }
                    else
                    {
                        ServiceEventSource.Current.ServiceMessage(this, "Cumulocity communication - subscribeResponse[0].succesfuls == false");
                        deviceCloudResult = HttpStatusCode.BadRequest;
                    }
                }
                else
                {
                    ServiceEventSource.Current.ServiceMessage(this, "Cumulocity communication - handshakeResponse[0].succesfuls == false");
                    deviceCloudResult = HttpStatusCode.BadRequest;
                }
                // Pause for 1 second before continue processing.
                await Task.Delay(TimeSpan.FromSeconds(1), cancelServiceInstance);
            }
        }

        // ======================================================================================
        // Implementation of Cumulocity Device Cloud 
        // ======================================================================================

        // Cumulocity Handshake - Request - Response
        public HandshakeResponse[] DeviceCloudHandshake(HandshakeRequest handshakeRequest, string service_url)
        {
            HandshakeResponse[] handshakeResp = new HandshakeResponse[0];
            try
            {
                HttpWebRequest request = deviceCloudHttpRequest(service_url);
                DataContractJsonSerializer ser = new DataContractJsonSerializer(handshakeRequest.GetType());
                MemoryStream ms = new MemoryStream();
                ser.WriteObject(ms, handshakeRequest);

                String json = Encoding.UTF8.GetString(ms.ToArray());
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                ms.Close();
                writer.Write(json);
                writer.Close();

                using (var httpwebResponse = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(httpwebResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        string responseJsonText = reader.ReadToEnd();
                        handshakeResp = js.Deserialize<HandshakeResponse[]>(responseJsonText);
                        // ==========================
                        ServiceEventSource.Current.ServiceMessage(this, "Cumulocity handshake established - {0}", handshakeResp[0].successful);
                        // ==========================
                        reader.Close();
                    }
                }
                request.Abort();
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(this, "Exception caught - {0} - unknown error during Handshake request", System.Convert.ToString(e));
                //System.Diagnostics.Trace.WriteLine("Exception caught" + System.Convert.ToString(e), "Error");
                handshakeResp[0].successful = false;
                handshakeResp[0].error = "Exeption - unknown error during Handshake request";
            }
            return handshakeResp;
        }

        // Cumulocity Subscribe - Request - Response
        public SubscribeResponse[] DeviceCloudSubscribe(SubscribeRequest subscribeRequest, string service_url)
        {
            SubscribeResponse[] subscribeResp = new SubscribeResponse[0];
            try
            {
                HttpWebRequest request = deviceCloudHttpRequest(service_url);
                DataContractJsonSerializer ser = new DataContractJsonSerializer(subscribeRequest.GetType());
                MemoryStream ms = new MemoryStream();
                ser.WriteObject(ms, subscribeRequest);
                String json = Encoding.UTF8.GetString(ms.ToArray());
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                ms.Close();
                writer.Write(json);
                writer.Close();

                using (var httpwebResponse = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(httpwebResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        string responseJsonText = reader.ReadToEnd();
                        subscribeResp = js.Deserialize<SubscribeResponse[]>(responseJsonText);
                        // ==========================
                        ServiceEventSource.Current.ServiceMessage(this, "Subscribe to Cumulocity succeeded - {0}", subscribeResp[0].successful);
                        // ==========================
                        reader.Close();
                    }
                }
                request.Abort();
            }
            catch (Exception e)
            {
                // ==========================
                ServiceEventSource.Current.ServiceMessage(this, "Exception caught - {0} - unknown error during Subscribe request", System.Convert.ToString(e));
                // ==========================
                //System.Diagnostics.Trace.WriteLine("Exception caught" + System.Convert.ToString(e), "Error");
                subscribeResp[0].successful = false;
                subscribeResp[0].error = "Exeption - unknown error during Subscribe request";
            }

            return subscribeResp;
        }

        // Cumulocity Unsubscribe - Request - Response
        public UnsubscribeResponse[] DeviceCloudUnsubscribe(UnsubscribeRequest unsubscribeRequest, string service_url)
        {
            List<UnsubscribeResponse> unsubscribeResp = new List<UnsubscribeResponse>();
            try
            {
                HttpWebRequest request = deviceCloudHttpRequest(service_url);
                DataContractJsonSerializer ser = new DataContractJsonSerializer(unsubscribeRequest.GetType());
                MemoryStream ms = new MemoryStream();
                ser.WriteObject(ms, unsubscribeRequest);
                String json = Encoding.UTF8.GetString(ms.ToArray());
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                ms.Close();
                writer.Write(json);
                writer.Close();

                using (var httpwebResponse = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(httpwebResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        string responseJsonText = reader.ReadToEnd();
                        unsubscribeResp = js.Deserialize<List<UnsubscribeResponse>>(responseJsonText);
                        // ==========================
                        ServiceEventSource.Current.ServiceMessage(this, "Unsubscribe to Cumulocity succeeded - {0}", unsubscribeResp[0].successful);
                        // ==========================
                        reader.Close();
                    }
                }
                request.Abort();
            }
            catch (Exception e)
            {
                // ==========================
                ServiceEventSource.Current.ServiceMessage(this, "Exception caught - {0} - unknown error during Unsubscribe request", System.Convert.ToString(e));
                // ==========================
                //System.Diagnostics.Trace.WriteLine("Exception caught" + System.Convert.ToString(e), "Error");
                unsubscribeResp[0].successful = false;
                unsubscribeResp[0].error = "Exeption - unknown error during Unsubscribe request";
            }
            UnsubscribeResponse[] unsubscribeResponse = unsubscribeResp.ToArray();
            return unsubscribeResponse;
        }

        // Cumulocity Connect - Request - Response
        public List<ConnectResponse> DeviceCloudConnect(ConnectRequest connectRequest, string service_url)
        {
            List<ConnectResponse> connectResp = new List<ConnectResponse>();
            try
            {
                HttpWebRequest request = deviceCloudHttpRequest(service_url);
                DataContractJsonSerializer ser = new DataContractJsonSerializer(connectRequest.GetType());
                MemoryStream ms = new MemoryStream();
                ser.WriteObject(ms, connectRequest);
                string json = Encoding.UTF8.GetString(ms.ToArray());
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                ms.Close();
                writer.Write(json);
                writer.Close();

                using (var httpwebResponse = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(httpwebResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        string responseJsonText = reader.ReadToEnd();
                        // justification for strange certuss Element Names
                        responseJsonText = responseJsonText.Replace("Luftdruck Aufstellraum", "Luftdruck_Aufstellraum");
                        responseJsonText = responseJsonText.Replace("Sollwert Dampfdruck", "Sollwert_Dampfdruck");
                        responseJsonText = responseJsonText.Replace("Speisewassertemperatur Kesseleintritt", "Speisewassertemperatur_Kesseleintritt");
                        responseJsonText = responseJsonText.Replace("Speisewassertemperatur Beh\\u00e4lter CVE", "Speisewassertemperatur_Behaelter_CVE");
                        responseJsonText = responseJsonText.Replace("Sollwert Speisepumpe", "Sollwert_Speisepumpe");
                        responseJsonText = responseJsonText.Replace("Netzspannung LME7", "Netzspannung_LME7");
                        connectResp = js.Deserialize<List<ConnectResponse>>(responseJsonText);
                        reader.Close();
                    }
                }
                request.Abort();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Exception caught" + System.Convert.ToString(e), "Error");
                connectResp[0].successful = false;
                connectResp[0].error = "Exeption - unknown error during Connect request";
            }
            return connectResp;
        }

        // Cumulocity Disconnect - Request - Response
        public List<DisconnectResponse> DeviceCloudDisconnect(DisconnectRequest disconnectRequest, string service_url)
        {
            List<DisconnectResponse> disconnectResp = new List<DisconnectResponse>();
            try
            {
                HttpWebRequest request = deviceCloudHttpRequest(service_url);
                DataContractJsonSerializer ser = new DataContractJsonSerializer(disconnectRequest.GetType());
                MemoryStream ms = new MemoryStream();
                ser.WriteObject(ms, disconnectRequest);
                String json = Encoding.UTF8.GetString(ms.ToArray());
                StreamWriter writer = new StreamWriter(request.GetRequestStream());
                ms.Close();
                writer.Write(json);
                writer.Close();

                using (var httpwebResponse = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(httpwebResponse.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        string responseJsonText = reader.ReadToEnd();
                        disconnectResp = js.Deserialize<List<DisconnectResponse>>(responseJsonText);
                        reader.Close();
                    }
                }
                request.Abort();
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Exception caught" + System.Convert.ToString(e), "Error");
                disconnectResp[0].successful = false;
                disconnectResp[0].error = "Exeption - unknown error during Disconnect request";
            }
            return disconnectResp;
        }

        //create HttpWebRequest
        public HttpWebRequest deviceCloudHttpRequest(string service_url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(service_url);
            request.Method = "POST";
            request.Accept = "application/json";
            request.ContentType = "application/json; charset=utf-8";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(devicecloud_user + ":" + devicecloud_password));
            return request;
        }

        //Send IoT Event-Messages into Azure EventHub
        public void SendIoTEvents(ConnectResponse ioTEvent)
        {
            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
            try
            {
                MemoryStream ms = new MemoryStream();
                EventDataStructure DeviceEvent = new EventDataStructure();
                //DeviceEvent.event_type = ioTEvent.data.data.type;
                DeviceEvent.event_measurement_id = ioTEvent.data.data.id;
                DeviceEvent.event_source_id = ioTEvent.data.data.source.id;
                DeviceEvent.event_time = ioTEvent.data.data.time;
                DeviceEvent.event_measurement = new Dictionary<string, decimal>();
                switch (ioTEvent.data.data.type)
                {
                    case "certuss_Dampf":
                        DeviceEvent.event_type = "SteamSensor01";
                        DeviceEvent.event_measurement.Add("Pressure_" + ioTEvent.data.data.certuss_Dampf.Dampfdruck.unit, ioTEvent.data.data.certuss_Dampf.Dampfdruck.value);
                        DeviceEvent.event_measurement.Add("Power_" + ioTEvent.data.data.certuss_Dampf.Dampfleistung.unit, ioTEvent.data.data.certuss_Dampf.Dampfleistung.value);
                        DeviceEvent.event_measurement.Add("Target_" + ioTEvent.data.data.certuss_Dampf.Sollwert_Dampfdruck.unit, ioTEvent.data.data.certuss_Dampf.Sollwert_Dampfdruck.value);
                        DeviceEvent.event_measurement.Add("Temperature_" + ioTEvent.data.data.certuss_Dampf.Dampftemperatur.unit, ioTEvent.data.data.certuss_Dampf.Dampftemperatur.value);
                        break;
                    case "certuss_Dampfnetz":
                        DeviceEvent.event_type = "SteamSensor02";
                        DeviceEvent.event_measurement.Add("Pressure_" + ioTEvent.data.data.certuss_Dampfnetz.Dampfdruck.unit, ioTEvent.data.data.certuss_Dampfnetz.Dampfdruck.value);
                        break;
                    case "certuss_Flammenstrom":
                        DeviceEvent.event_type = "SteamSensor03";
                        DeviceEvent.event_measurement.Add("Status_" + ioTEvent.data.data.certuss_Flammenstrom.Flammenstrom.unit, ioTEvent.data.data.certuss_Flammenstrom.Flammenstrom.value);
                        break;
                    case "certuss_Gleitschieberventil":
                        DeviceEvent.event_type = "SteamSensor04";
                        DeviceEvent.event_measurement.Add("Status_" + ioTEvent.data.data.certuss_Gleitschieberventil.Gleitschieberventil.unit, ioTEvent.data.data.certuss_Gleitschieberventil.Gleitschieberventil.value);
                        break;
                    case "certuss_Kamin":
                        DeviceEvent.event_type = "SteamSensor05";
                        DeviceEvent.event_measurement.Add("Draught_" + ioTEvent.data.data.certuss_Kamin.Kaminzug.unit, ioTEvent.data.data.certuss_Kamin.Kaminzug.value);
                        DeviceEvent.event_measurement.Add("Pressure_" + ioTEvent.data.data.certuss_Kamin.Luftdruck_Aufstellraum.unit, ioTEvent.data.data.certuss_Kamin.Luftdruck_Aufstellraum.value);
                        DeviceEvent.event_measurement.Add("TemperatureIn_" + ioTEvent.data.data.certuss_Kamin.Lufteintrittstemperatur.unit, ioTEvent.data.data.certuss_Kamin.Lufteintrittstemperatur.value);
                        DeviceEvent.event_measurement.Add("TemperatureOut_" + ioTEvent.data.data.certuss_Kamin.Rauchgastemperatur.unit, ioTEvent.data.data.certuss_Kamin.Rauchgastemperatur.value);
                        break;
                    case "certuss_Kesselgroesse":
                        DeviceEvent.event_type = "SteamSensor06";
                        DeviceEvent.event_measurement.Add("Capacity_" + ioTEvent.data.data.certuss_Kesselgroesse.Kesselgroesse.unit, ioTEvent.data.data.certuss_Kesselgroesse.Kesselgroesse.value);
                        break;
                    case "certuss_LME7":
                        DeviceEvent.event_type = "SteamSensor07";
                        DeviceEvent.event_measurement.Add("Power_" + ioTEvent.data.data.certuss_LME7.Leistungswert.unit, ioTEvent.data.data.certuss_LME7.Leistungswert.value);
                        break;
                    case "certuss_Netzspannung":
                        DeviceEvent.event_type = "SteamSensor08";
                        DeviceEvent.event_measurement.Add("Voltage_" + ioTEvent.data.data.certuss_Netzspannung.Netzspannung_LME7.unit, ioTEvent.data.data.certuss_Netzspannung.Netzspannung_LME7.value);
                        break;
                    case "certuss_Schaltschrank":
                        DeviceEvent.event_type = "SteamSensor09";
                        DeviceEvent.event_measurement.Add("Temperature_" + ioTEvent.data.data.certuss_Schaltschrank.Temperatur.unit, ioTEvent.data.data.certuss_Schaltschrank.Temperatur.value);
                        break;
                    case "certuss_Speisung":
                        DeviceEvent.event_type = "SteamSensor10";
                        DeviceEvent.event_measurement.Add("Flow_" + ioTEvent.data.data.certuss_Speisung.Durchfluss.unit, ioTEvent.data.data.certuss_Speisung.Durchfluss.value);
                        DeviceEvent.event_measurement.Add("Power_" + ioTEvent.data.data.certuss_Speisung.Speisepumpe.unit, ioTEvent.data.data.certuss_Speisung.Speisepumpe.value);
                        DeviceEvent.event_measurement.Add("Target_" + ioTEvent.data.data.certuss_Speisung.Sollwert_Speisepumpe.unit, ioTEvent.data.data.certuss_Speisung.Sollwert_Speisepumpe.value);
                        DeviceEvent.event_measurement.Add("Pressure_" + ioTEvent.data.data.certuss_Speisung.Vordruck.unit, ioTEvent.data.data.certuss_Speisung.Vordruck.value);
                        DeviceEvent.event_measurement.Add("Temperature_" + ioTEvent.data.data.certuss_Speisung.Speisewassertemperatur_Behaelter_CVE.unit, ioTEvent.data.data.certuss_Speisung.Speisewassertemperatur_Behaelter_CVE.value);
                        DeviceEvent.event_measurement.Add("TemperatureIn_" + ioTEvent.data.data.certuss_Speisung.Speisewassertemperatur_Kesseleintritt.unit, ioTEvent.data.data.certuss_Speisung.Speisewassertemperatur_Kesseleintritt.value);
                        break;
                }
                DataContractJsonSerializer ser = new DataContractJsonSerializer(DeviceEvent.GetType());
                ser.WriteObject(ms, DeviceEvent);
                DeviceEvent = null;
                ser = null;
                String jsonIoTEvent = Encoding.UTF8.GetString(ms.ToArray());
                if (jsonIoTEvent.Length != 0)
                {
                    jsonIoTEvent = jsonIoTEvent.Replace("{\"event_measurement\":[", "{");
                    jsonIoTEvent = jsonIoTEvent.Replace("{\"Key\":\"", "\"");
                    jsonIoTEvent = jsonIoTEvent.Replace(",\"Value\":", ":");
                    jsonIoTEvent = jsonIoTEvent.Replace("},", ",");
                    jsonIoTEvent = jsonIoTEvent.Replace("}],", ",");
                    jsonIoTEvent = jsonIoTEvent.Replace("l\\/min", "l/min");

                    eventHubClient.Send(new EventData(Encoding.UTF8.GetBytes(jsonIoTEvent)));
                }
                eventHubClient.Close();
                ms.Close();
                jsonIoTEvent = null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Exception caught" + System.Convert.ToString(e), "Error");
            }
            return;
        }

    }
}