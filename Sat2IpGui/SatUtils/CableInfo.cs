using Sat2Ip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sat2IpGui.SatUtils
{
    public class CableInfo : InfoBase
    {
        private static string PROVIDERFOLDER = "dtv-scan-tables\\dvb-c\\";
        private List<CableProvider> m_providers = new();
        public CableProvider currentProvider { get; set; }
        public CableInfo()
        {
            foreach (string filename in Directory.GetFiles(Utils.Utils.getStorageFolder() + PROVIDERFOLDER))
            {
                CableProvider cp = new();
                cp.file = new FileInfo(filename);
                cp.name = new FileInfo(filename).Name;
                m_providers.Add(cp);
            }
        }
        public BindingSource datasourceProviders()
        {
            BindingSource bs = new();
            bs.DataSource = m_providers;
            return bs;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public override BindingSource datasourceTransponders()
        {
            if (currentProvider == null)
                throw new Exception("CableProvider not set!");
            IniFileNew inifile = new IniFileNew(currentProvider.file.FullName);
            List<Match> matches = inifile.getSections();
            m_transponders.Clear();
            foreach (Match match in matches)
            {
                Transponder tsp = extractInfoFromInifile(inifile, match);
                m_transponders.Add(tsp);
            }
            return base.datasourceTransponders();
        }
        public BindingSource updateDatasourceTransponders()
        {
            return base.datasourceTransponders();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private Transponder extractInfoFromInifile(IniFileNew inifile, Match match)
        {
            Transponder tsp = new Transponder();
            decimal frequencydecimal = decimal.Parse(inifile.getValue(match, "FREQUENCY"), System.Globalization.CultureInfo.CreateSpecificCulture("en-us")) / 1000;
            int frequency = (int)frequencydecimal;
            string polarisation = "-";
            int samplerate = int.Parse(inifile.getValue(match, "SYMBOL_RATE")) / 1000;
            string errorcorrections = "";
            string dvbtype = inifile.getValue(match, "DELIVERY_SYSTEM");
            string mtype = inifile.getValue(match, "MODULATION");
            tsp.diseqcposition = 0;
            tsp.frequency = (int)frequencydecimal;
            tsp.frequencydecimal = frequencydecimal;
            tsp.samplerate = samplerate;
            tsp.polarisationFromString(polarisation);
            tsp.dvbsystemFromString(dvbtype);
            tsp.fecFromString(errorcorrections);
            tsp.mtypeFromString(mtype);
            return tsp;
        }

    }
    public class CableProvider
    {
        public string name { get ; set; }
        public FileInfo file { get; set; }
    }
}
