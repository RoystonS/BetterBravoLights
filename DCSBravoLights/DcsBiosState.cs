using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BravoLights.Common;

namespace DCSBravoLights
{
    public class DcsBiosState : IDisposable
    {
        private UdpClient client;
        private IPEndPoint addr;

        private Thread udpThread;
        private CancellationTokenSource threadCancellation;

        /// <summary>
        /// The combined data from DCS
        /// </summary>
        private readonly byte[] dcsMemory = new byte[65536];
        private readonly bool[] dcsMemoryValid = new bool[65536];
        private readonly ISet<DataDefinition>[] dcsDefs = new ISet<DataDefinition>[65536];

        public void StartListening()
        {
            client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 5010));

            client.JoinMulticastGroup(IPAddress.Parse("239.255.50.10"));
            addr = new(IPAddress.Any, 0);

            udpThread = new Thread(RunUdpThread);

            threadCancellation = new CancellationTokenSource();
            udpThread.Start(threadCancellation.Token);
        }

        public void StopListening()
        {
            // We can't actually cancel the UDP receive, but we can tell that thread 
            if (udpThread != null)
            {
                threadCancellation.Cancel();
                threadCancellation = null;
                udpThread = null;
                client.Dispose();
                client = null;
            }
        }

        private void RunUdpThread(object obj)
        {
            var cancellationToken = (CancellationToken)obj;

            while (true)
            {
                var data = client.Receive(ref addr);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                else
                {
                    Process(data, data.Length);
                }
            }
        }

        private void Process(byte[] data, int length)
        {
            var defsChanged = new HashSet<DataDefinition>();
            var defsValues = new Dictionary<DataDefinition, object>();

            lock (this)
            {
                if (data[0] == 0x55 && data[1] == 0x55 && data[2] == 0x55 && data[3] == 0x55)
                {
                    var pos = 4;
                    while (pos < length)
                    {
                        var address = (ushort)(data[pos++] + 256 * data[pos++]);
                        var dataLength = data[pos++] + 256 * data[pos++];

                        for (var i = 0; i < dataLength; i++)
                        {
                            var newByte = data[pos++];
                            var oldByte = dcsMemory[address];

                            var firstValueForByte = !dcsMemoryValid[address];

                            if (oldByte != newByte || firstValueForByte)
                            {
                                // A byte has changed or it's the first time we've seen the byte
                                var dcsDefsForLocation = dcsDefs[address];
                                if (dcsDefsForLocation != null)
                                {
                                    foreach (var dcsDef in dcsDefsForLocation)
                                    {
                                        if (dcsDef.WouldChange(dcsMemory, address, newByte) || firstValueForByte)
                                        {
                                            defsChanged.Add(dcsDef);
                                            dcsMemoryValid[address] = true;
                                        }
                                    }
                                }

                                dcsMemory[address] = newByte;
                                dcsMemoryValid[address] = true;
                            }
                            address++;
                        }
                    }


                    foreach (var def in defsChanged)
                    {
                        var value = def.GetValue(dcsMemory, dcsMemoryValid);
                        if (value != null)
                        {
                            defsValues[def] = value;
                        }
                    }
                }

                foreach (var kvp in defsValues)
                {
                    var def = kvp.Key;
                    var value = kvp.Value;
                    if (handlers.TryGetValue(def, out var defListeners))
                    {
                        defListeners(def, new ValueChangedEventArgs { NewValue = value });
                    }
                }
            }
        }

        private readonly Dictionary<DataDefinition, EventHandler<ValueChangedEventArgs>> handlers = new();

        public void AddHandler(DataDefinition def, EventHandler<ValueChangedEventArgs> handler)
        {
            lock (this)
            {
                for (var i = 0; i < def.Length; i++)
                {
                    var address = def.Address + i;
                    var set = dcsDefs[address];
                    if (set == null)
                    {
                        set = new HashSet<DataDefinition>();
                        dcsDefs[address] = set;
                    }
                    set.Add(def);
                }

                handlers.TryGetValue(def, out var existingHandlersForDefinition);
                var newListeners = (EventHandler<ValueChangedEventArgs>)Delegate.Combine(existingHandlersForDefinition, handler);
                handlers[def] = newListeners;

                var existingValue = def.GetValue(dcsMemory, dcsMemoryValid);
                if (existingValue == null)
                {
                    handler(def, new ValueChangedEventArgs { NewValue = new Exception("No value yet") });
                }
                else
                {
                    handler(def, new ValueChangedEventArgs { NewValue = existingValue });
                }
            }
        }

        public void RemoveHandler(DataDefinition def, EventHandler<ValueChangedEventArgs> handler)
        {
            lock (this)
            {
                handlers.TryGetValue(def, out var existingHandlersForDefinition);
                var newListeners = (EventHandler<ValueChangedEventArgs>)Delegate.Remove(existingHandlersForDefinition, handler);
                handlers[def] = newListeners;

                if (newListeners == null)
                {
                    for (var i = 0; i < def.Length; i++)
                    {
                        var address = def.Address + i;
                        var set = dcsDefs[address];
                        set.Remove(def);
                        if (set.Count == 0)
                        {
                            dcsDefs[address] = null;
                            handlers.Remove(def);
                        }
                    }
                }
            }
        }

        public object GetValue(DataDefinition def)
        {
            return def.GetValue(dcsMemory, dcsMemoryValid);
        }
        
        public string GetStringValue(DataDefinition def)
        {
            var result = def.GetStringValue(dcsMemory, dcsMemoryValid);
            return result ?? "No value yet";
        }

        public static string DcsJsonDocFolder
        {
            get
            {
                var savedGamesFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games");
                var dcsBiosFolder = Path.Join(savedGamesFolder, "DCS", "Scripts", "DCS-BIOS");
                return Path.Join(dcsBiosFolder, "doc", "json");
            }
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            StopListening();
        }
    }
}
