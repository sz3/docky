
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Gdk;
using GLib;

namespace NetworkMonitorDocklet {
    enum OutputDevice {
        AUTO = 0
    }

    class DeviceInfo {
        public String name;
        public String ip = "xxx.xxx.xxx.xxx";
        public long tx;
        public long rx;
        public double tx_rate;
        public double rx_rate;
        public long bytesIn {get; set; }
        public long bytesOut {get; set; }
        public DateTime last_update;

        override public string ToString() {
            return string.Format("Device: {0,10}: {2,10} down {1,10} up (Total: {3,10}/{4,10})", this.name, bytes_to_string(this.tx_rate), bytes_to_string(this.rx_rate), bytes_to_string(this.tx,false),bytes_to_string(this.rx,false));
        }
		
        public String formatUpDown(bool up) {
            double rate = rx_rate;
            if (up) {
                rate = tx_rate;
            }
            if (rate < 1) {
                return "-";
            }             
            return this.bytes_to_string(rate,true);
        }
		public String bytes_to_string(double bytes) {
			return bytes_to_string(bytes,false);
		}
        public String bytes_to_string(double bytes,bool per_sec)
        {   
            int kilo = 1024;
            String format,unit;
            if(bytes < kilo)
            {
                format = "{0:0} {1}";
                if(per_sec) {
                    unit = ("B/s");
                } else {
                    unit = ("B");
                }
            } 
            else if (bytes < (kilo * kilo)) 
            {
            //kilo
                if(bytes < (100*kilo)) {
                    format = "{0:0.0} {1}";
                } else {
                    format = "%.0f %s";
                    format = "{0:0} {1}";
                }
                bytes /= kilo;
                if (per_sec) {
                        unit = ("K/s");
                } else {
                        unit = ("KB");
                }
            } else {
                format = "{0:0.0} {1}";
                bytes /= (kilo * kilo);

                if (per_sec) {
                        unit = ("M/s");
                } else {
                        unit = ("M");
                }
            }
            return String.Format(format,bytes,unit);
        }

    }
    class NetworkMonitor {
        uint timer;

        Dictionary<string, DeviceInfo> devices;
        public void printResults() {
            foreach (KeyValuePair<string, DeviceInfo> pair in this.devices) {
                Console.WriteLine(pair.Value.ToString());
            }
        }
        public void update()
        {
            using (StreamReader reader = new StreamReader ("/proc/net/dev")) {
                    //try 
                    {
                            string data = reader.ReadToEnd();
                            char[] delimiters = new char[] { '\r', '\n' };
                            //Console.WriteLine(data);
                            foreach (string row in data.Split(delimiters)) {
                                this.parseRow(row);
                            }

                    } 
/*catch {
                            // we dont care
                    }
*/
            }
        }
        
        public void parseRow(string row) {
            if(row.IndexOf(":") < 1) {
                return;
            }
            //Console.WriteLine(row);
            string devicename = row.Substring(0,row.IndexOf(':')).Trim();
            if(devicename == "lo") {
                return;
            }
            //Console.WriteLine(devicename);
            row = row.Substring(row.IndexOf(":"),row.Length-row.IndexOf(":"));
            //Console.WriteLine(row);
            Regex regex = new Regex("\\d+");
            //The row has the following format:
            //Inter-|   Receive                                                |  Transmit
            //face  |bytes    packets errs drop fifo frame compressed multicast|bytes    packets errs drop fifo colls carrier compressed
            //So we need fields 0(bytes-sent) and 8(bytes-received)
            MatchCollection collection = regex.Matches (row);
            //debug_collection(collection);
            DeviceInfo d;
            long rx, tx;
            double tx_rate, rx_rate;
            rx = Convert.ToInt64 (collection [0].Value);
            tx = Convert.ToInt64 (collection [8].Value);
            DateTime now  = DateTime.Now;
            
            if (devices.ContainsKey(devicename)) {
                d = devices[devicename];
                TimeSpan diff = now - d.last_update;
                //todo: use now and d.last_update to calc the correct rate in bytes/seconds
                tx_rate = (tx - d.tx) / diff.TotalSeconds; //todo, adjust to seconds according to update_interval
                rx_rate = (rx - d.rx) / diff.TotalSeconds; //todo, adjust to seconds according to update_interval
                
            } else {
                d = new DeviceInfo();
                d.name = devicename;
                d.rx = rx;
                d.tx = tx;
                tx_rate = 0;
                rx_rate = 0;
                this.devices.Add(devicename,d);
            }
            d.last_update = now;
            d.tx_rate = tx_rate;
            d.rx_rate = rx_rate;
            d.tx = tx;
            d.rx = rx;
        }
        public DeviceInfo getDevice(OutputDevice n) {
            DeviceInfo d  = null ;
            if(n == 0) {
                foreach (KeyValuePair<string, DeviceInfo> pair in this.devices) {
                    if (d == null) {
                        d = pair.Value;
                        continue;
                    }
                    if ( (pair.Value.tx + pair.Value.rx) > (d.tx+d.rx)) {
                        d = pair.Value;                    
                    }
                }
            }
            return d;
        }

        public NetworkMonitor()
        {
            devices = new Dictionary<string, DeviceInfo>();
        }
        static void Main() {
            NetworkMonitor nm = new NetworkMonitor();  
            nm.Start();
        }
        public void Start() {
            GLib.MainLoop l = new GLib.MainLoop();      
            timer = GLib.Timeout.Add (2000, UpdateUtilization);
            if(timer > 0) {
                Console.WriteLine("running...");
            }
            l.Run();
        }
        bool UpdateUtilization()
        {
            Console.Clear();
            this.update();
            Console.WriteLine(this.getDevice(0));
            return true;
        }
    }
}
