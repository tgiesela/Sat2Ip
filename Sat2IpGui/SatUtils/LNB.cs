using System;
using System.Collections.Generic;
using System.IO;
using Sat2Ip;
using System.Text.Json;

namespace Sat2IpGui.SatUtils
{
    public class LNB
    {
        private int m_lnb;
        private List<Transponder> m_transponders;
        private List<Channel> m_channels;
        private String m_lnbFilename;
        public List<Transponder> transponders { get { return m_transponders; } set { m_transponders = value;} }
        public LNB(int lnb)
        {
            m_lnb = lnb;
            load();
        }
        public void load()
        {
            m_lnbFilename = String.Format("Transponderlist{0}.json", m_lnb);
            if (File.Exists(m_lnbFilename))
            {
                String transponders = File.ReadAllText(m_lnbFilename);
                m_transponders =
                    JsonSerializer.Deserialize<List<Transponder>>(transponders);
            }
            else
            {
                m_transponders = new List<Transponder>();
            }

            m_lnbFilename = String.Format("Channellist{0}.json", m_lnb);
            if (File.Exists(m_lnbFilename))
            {
                String channels = File.ReadAllText(m_lnbFilename);
                m_channels =
                    JsonSerializer.Deserialize<List<Channel>>(channels);
            }
            else
            {
                m_channels = new List<Channel>();
            }

        }
        public void save()
        {
            m_lnbFilename = String.Format("Transponderlist{0}.json", m_lnb);
            String transponders = JsonSerializer.Serialize(m_transponders);
            File.WriteAllText(m_lnbFilename, transponders);

            m_lnbFilename = String.Format("Channellist{0}.json", m_lnb);
            String channels = JsonSerializer.Serialize(m_channels);
            File.WriteAllText(m_lnbFilename, channels);
        }
        public List<Transponder> getTransponders()
        {
            return m_transponders;
        }
        public Transponder getTransponder(int frequency)
        {
            return m_transponders.Find(x => x.frequency == frequency);
        }
        public List<Channel> getChannelsOnTransponder(int frequency)
        {
            return m_channels.FindAll(x => x.transponder.frequency == frequency);
        }
        public void setTransponder(Transponder transponder, List<Channel> channels)
        {
            Transponder tsp = getTransponder(transponder.frequency);
            m_transponders.Remove(tsp);
            m_transponders.Add(transponder);

            List<Channel> channelsOnTsp = getChannelsOnTransponder(transponder.frequency);
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
