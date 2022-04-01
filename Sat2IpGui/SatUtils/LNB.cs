using System;
using System.Collections.Generic;
using System.IO;
using Sat2Ip;
using System.Text.Json;
using Interfaces;

namespace Sat2IpGui.SatUtils
{
    public class LNB
    {
        private List<Transponder> m_transponders;
        private List<Channel> m_channels;
        private List<Network> m_networks;
        private String m_lnbFilename;
        private List<Bouquet> m_bouquets;
        private int m_diseqcposition = -1;

        public List<Transponder> transponders { get { return m_transponders; } set { m_transponders = value;} }
        public List<Network> networks{ get { return m_networks; } set { m_networks = value; } }
        public List<Channel> channels { get { return m_channels; } set { m_channels = value; } }
        public List<Bouquet> bouquets { get { return m_bouquets; } set { m_bouquets = value;  } }
        public int diseqcposition { get { return m_diseqcposition;} set { m_diseqcposition = value; } }
        public string satellitename { get; set; }

        public LNB(int lnb)
        {
            diseqcposition = lnb;
            //load();
        }
        public LNB()
        {
        }
        public int orbit()
        {
            string[] parts = satellitename.Split(' ');
            decimal decorbit = decimal.Parse(parts[0]);
            return Decimal.ToInt32(decorbit);
        }
        public void load()
        {
            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Transponderlist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String transponders = File.ReadAllText(m_lnbFilename);
                m_transponders = JsonSerializer.Deserialize<List<Transponder>>(transponders);
            }
            else
            {
                m_transponders = new List<Transponder>();
            }

            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Channellist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String channels = File.ReadAllText(m_lnbFilename);
                m_channels = JsonSerializer.Deserialize<List<Channel>>(channels);
            }
            else
            {
                m_channels = new List<Channel>();
            }

            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Networklist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String networks = File.ReadAllText(m_lnbFilename);
                m_networks = JsonSerializer.Deserialize<List<Network>>(networks);
            }
            else
            {
                m_networks = new List<Network>();
            }

            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Bouquetlist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String bouquets = File.ReadAllText(m_lnbFilename);
                m_bouquets = JsonSerializer.Deserialize<List<Bouquet>>(bouquets);
            }
            else
            {
                m_bouquets = new List<Bouquet>();
            }
        }
        public void save()
        {
            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Transponderlist{0}.json", diseqcposition);
            String transponders = JsonSerializer.Serialize(m_transponders);
            File.WriteAllText(m_lnbFilename, transponders);

            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Channellist{0}.json", diseqcposition);
            String channels = JsonSerializer.Serialize(m_channels);
            File.WriteAllText(m_lnbFilename, channels);

            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Networklist{0}.json", diseqcposition);
            String networks = JsonSerializer.Serialize(m_networks);
            File.WriteAllText(m_lnbFilename, networks);

            m_lnbFilename = String.Format(Utils.Utils.getStorageFolder() + "Bouquetlist{0}.json", diseqcposition);
            String bouquets = JsonSerializer.Serialize(m_bouquets);
            File.WriteAllText(m_lnbFilename, bouquets);
        }
        public Transponder getTransponder(int frequency)
        {
            return m_transponders.Find(x => x.frequency == frequency);
        }
        public List<Channel> getChannelsOnTransponder(int frequency, int diseqcposition)
        {
            return m_channels.FindAll(x => x.transponder.frequency == frequency && 
                                           x.transponder.diseqcposition == diseqcposition);
        }
        public void setTransponder(Transponder transponder, List<Channel> channels)
        {
            if (channels.Count == 0)
            {
                return;
            }
            Transponder tsp = getTransponder(transponder.frequency);
            m_transponders.Remove(tsp);
            m_transponders.Add(transponder);

            List<Channel> channelsOnTsp = getChannelsOnTransponder(transponder.frequency, transponder.diseqcposition);
            foreach (Channel c in channelsOnTsp)
            {
                m_channels.Remove(c);
            }
            foreach (Channel c in channels)
            {
                m_channels.Add(c);
            }
        }
    }
}
