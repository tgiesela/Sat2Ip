using Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
    public class Config
    {
        private List<FastScanLocation> m_fastscanlocations;
        private List<FastScanBouquet> m_fastscanbouquets;
        private ConfigItems m_configitems;
        public List<FastScanLocation> Fastscanlocations { get { return m_fastscanlocations; } set { m_fastscanlocations = value; } }
        public List<FastScanBouquet> FastcanBouquets { get { return m_fastscanbouquets; } set { m_fastscanbouquets = value; } }
        public ConfigItems configitems { get { return m_configitems; } set { m_configitems = value; } }
        public Config()
        {
            m_configitems = new ConfigItems();
            m_fastscanlocations = new List<FastScanLocation>();
            m_fastscanbouquets = new List<FastScanBouquet>();
        }
        public void load()
        {
            string filename;
            filename = Utils.Utils.getStorageFolder() + "config.json";
            if (File.Exists(filename))
            {
                string config = File.ReadAllText(filename);
                m_configitems = JsonSerializer.Deserialize<ConfigItems>(config);
            }
            else
            {
                m_fastscanlocations = new List<FastScanLocation>();
            }

            filename = Utils.Utils.getStorageFolder() + "fastscan.json";
            if (File.Exists(filename))
            {
                string fastscan = File.ReadAllText(filename);
                m_fastscanlocations = JsonSerializer.Deserialize<List<FastScanLocation>>(fastscan);
            }
            else
            {
                m_fastscanlocations = new List<FastScanLocation>();
            }
            foreach (FastScanLocation location in m_fastscanlocations)
            {
                filename = Utils.Utils.getStorageFolder() + MakeValidFileName("FSBouquet" + location.name + location.frequency + ".json");
                if (File.Exists(filename))
                {
                    string fsbstring = File.ReadAllText(filename);
                    FastScanBouquet fsb = JsonSerializer.Deserialize<FastScanBouquet>(fsbstring);
                    m_fastscanbouquets.Add(fsb);
                }
            }
        }
        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
        public void save()
        {
            string filename;
            filename = Utils.Utils.getStorageFolder() + "config.json";
            string configfile = JsonSerializer.Serialize(m_configitems);
            File.WriteAllText(filename, configfile);

            foreach(FastScanBouquet fsb in m_fastscanbouquets)
            {
                filename = Utils.Utils.getStorageFolder() + MakeValidFileName("FSBouquet" + fsb.location.name + fsb.location.frequency + ".json");
                string fsbstring = JsonSerializer.Serialize(fsb);
                File.WriteAllText(filename, fsbstring);
            }
        }
    }

    public class FastScanResult
    {
        public FastScanLocation location { get; set; }
        public Network network { get; set; }
    }
}

