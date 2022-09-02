using System.Text.Json;
using Interfaces;
using Sat2Ip;

namespace Sat2ipUtils
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

        private Config m_config;
        private string m_serverfolder;
        private string m_dvbtype;
        public LNB(int lnb):this()
        {
            diseqcposition = lnb;
        }
        public LNB()
        {
            m_config = new();
            networks = new List<Network>();
            channels = new List<Channel>();
            bouquets = new List<Bouquet>();
            transponders = new List<Transponder>();
        }
        public int orbit()
        {
            string[] parts = satellitename.Split(' ');
            decimal decorbit = decimal.Parse(parts[0]);
            return Decimal.ToInt32(decorbit);
        }
        public void load()
        {
            m_config.load();
            m_serverfolder = m_config.configitems.IpAddressDevice;
            m_dvbtype = m_config.configitems.dvbtype;
            m_lnbFilename = String.Format(getStorageFolder() + "Transponderlist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String transponders = File.ReadAllText(m_lnbFilename);
                m_transponders = JsonSerializer.Deserialize<List<Transponder>>(transponders);
            }
            if (m_transponders != null)
                m_transponders = new();

            m_lnbFilename = String.Format(getStorageFolder() + "Channellist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String channels = File.ReadAllText(m_lnbFilename);
                m_channels = JsonSerializer.Deserialize<List<Channel>>(channels);
            }
            else
            {
                m_channels = new List<Channel>();
            }

            m_lnbFilename = String.Format(getStorageFolder() + "Networklist{0}.json", diseqcposition);
            if (File.Exists(m_lnbFilename))
            {
                String networks = File.ReadAllText(m_lnbFilename);
                m_networks = JsonSerializer.Deserialize<List<Network>>(networks);
            }
            else
            {
                m_networks = new List<Network>();
            }

            m_lnbFilename = String.Format(getStorageFolder() + "Bouquetlist{0}.json", diseqcposition);
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
        public string getStorageFolder()
        {
            return Utils.Utils.getStorageFolderForDVBType(m_config.configitems.IpAddressDevice,m_config.configitems.dvbtype);
        }
        public void save()
        {
            m_config.load();
            m_serverfolder = m_config.configitems.IpAddressDevice;
            m_dvbtype = m_config.configitems.dvbtype;

            m_lnbFilename = String.Format(getStorageFolder() + "Transponderlist{0}.json", diseqcposition);
            String transponders = JsonSerializer.Serialize(m_transponders);
            File.WriteAllText(m_lnbFilename, transponders);

            m_lnbFilename = String.Format(getStorageFolder() + "Channellist{0}.json", diseqcposition);
            String channels = JsonSerializer.Serialize(m_channels);
            File.WriteAllText(m_lnbFilename, channels);

            m_lnbFilename = String.Format(getStorageFolder() + "Networklist{0}.json", diseqcposition);
            String networks = JsonSerializer.Serialize(m_networks);
            File.WriteAllText(m_lnbFilename, networks);

            m_lnbFilename = String.Format(getStorageFolder() + "Bouquetlist{0}.json", diseqcposition);
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
