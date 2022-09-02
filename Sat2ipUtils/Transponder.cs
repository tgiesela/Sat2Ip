using Interfaces;
using System;
using System.Collections.Generic;

namespace Sat2Ip
{
    [Serializable]
    public class Transponder
    {
        public enum e_dvbsystem
        {
            DVB_S = 1,
            DVB_S2 = 2,
            DVB_C = 3,
            DVB_C2 = 4
        };
        public enum e_fec
        {
            fec_12 = 12,
            fec_23 = 23,
            fec_34 = 34,
            fec_56 = 56,
            fec_78 = 78,
            fec_89 = 89,
            fec_35 = 35,
            fec_45 = 45,
            fec_910 = 910,
            undefined = 0,
            none = 1,
            reserved = 2
        }
        public enum e_mtype { 
            qpsk, 
            psk8,
            auto,
            qam16, 
            qam32, 
            qam64, 
            qam128,
            qam256
        }
        public enum e_polarisation { Horizontal = 'h', Vertical = 'v', circular_left = 'l', circular_right = 'r', none = '-'}
        private e_fec _fec;
        public e_fec fec { get { return _fec; } set { _fec = value; } }
        public int frequency { get; set; }
        public decimal? frequencydecimal { get; set; }
        public int diseqcposition { get; set; }
        public int samplerate { get; set; }
        public e_polarisation polarisation { get; set; }
        public e_dvbsystem dvbsystem { get; set; }
        public e_mtype mtype { get; set; }
        public Transponder(int disqeqcposition, int frequency, int samplerate, e_polarisation pol, e_dvbsystem msys, e_fec fec, e_mtype mtype)
        {
            this.diseqcposition = disqeqcposition;
            this.frequency = frequency;
            this.samplerate = samplerate;
            this.polarisation = pol;
            this.mtype = mtype;
            this.dvbsystem = msys;
            _fec = fec;
        }
        public Transponder()
        {
        }
        public String getQuery()
        {
            string strdvbsystem;
            string strmtype;
            switch (dvbsystem)
            {
                case e_dvbsystem.DVB_S: strdvbsystem = "dvbs"; break;
                case e_dvbsystem.DVB_S2: strdvbsystem = "dvbs2"; break;
                case e_dvbsystem.DVB_C: strdvbsystem = "dvbc"; break;
                case e_dvbsystem.DVB_C2: strdvbsystem = "dvbc2"; break;
                default: throw new Exception("Unsupported dvbsystem type");
            }
            if (dvbsystem == e_dvbsystem.DVB_S)
                strmtype = "qpsk";
            else
                switch (mtype)
                {
                    case e_mtype.psk8: strmtype = "8psk"; break;
                    case e_mtype.qpsk: strmtype = "qpsk"; break;
                    case e_mtype.auto: strmtype = "qpsk"; break;
                    case e_mtype.qam16: strmtype = "16qam"; break;
                    case e_mtype.qam32: strmtype = "32qam"; break;
                    case e_mtype.qam64: strmtype = "64qam"; break;
                    case e_mtype.qam128: strmtype = "128qam"; break;
                    case e_mtype.qam256: strmtype = "256qam"; break;
                    default: throw new Exception("Unsupported modulation type for SAT2IP");
                }
            if (dvbsystem == e_dvbsystem.DVB_S || dvbsystem == e_dvbsystem.DVB_S2)
            {
                if (frequencydecimal.HasValue)
                    return String.Format("?src={0}&freq={1}&sr={2}&pol={3}&msys={4}&plts=off&ro=0.35&fec={5}&mtype={6}", diseqcposition, frequencydecimal.Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-us")), samplerate, (char)polarisation, strdvbsystem, (int)_fec, strmtype);
                else
                    return String.Format("?src={0}&freq={1}&sr={2}&pol={3}&msys={4}&plts=off&ro=0.35&fec={5}&mtype={6}", diseqcposition, frequency, samplerate, (char)polarisation, strdvbsystem, (int)_fec, strmtype);
            }
            else
            {
                if (frequencydecimal.HasValue)
                    return String.Format("?freq={1}&sr={2}&msys={4}&mtype={6}&specinv=0", diseqcposition, (frequencydecimal/1000).Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-us")), samplerate, (char)polarisation, strdvbsystem, (int)_fec, strmtype);
                else
                    return String.Format("?freq={1}&sr={2}&msys={4}&mtype={6}&specinv=0", diseqcposition, frequency/1000, samplerate, (char)polarisation, strdvbsystem, (int)_fec, strmtype);
            }
        }
        public void polarisationFromString(string spolarisation)
        {
            if (spolarisation.ToUpper().Equals("H") || spolarisation.ToUpper().Equals("HORIZONTAL"))
                this.polarisation = e_polarisation.Horizontal;
            else
            if (spolarisation.ToUpper().Equals("V") || spolarisation.ToUpper().Equals("VERTICAL"))
                this.polarisation = e_polarisation.Vertical;
            else
            if (spolarisation.ToUpper().Equals("L") || spolarisation.ToUpper().Equals("CIRCULAR_LEFT"))
                this.polarisation = e_polarisation.circular_left;
            else
            if (spolarisation.ToUpper().Equals("R") || spolarisation.ToUpper().Equals("CIRCULAR_RIGHT"))
                this.polarisation = e_polarisation.circular_right;
            else
                this.polarisation = e_polarisation.none;
        }
        public List<PAT.PATEntry> pids { get; set; }

        public void fecFromString(string sfec)
        {
            if (sfec.Equals("12"))
            {
                _fec = e_fec.fec_12;
            }
            else
            if (sfec.Equals("23"))
            {
                _fec = e_fec.fec_23;
            }
            else
            if (sfec.Equals("34"))
            {
                _fec = e_fec.fec_34;
            }
            else
            if (sfec.Equals("45"))
            {
                _fec = e_fec.fec_45;
            }
            else
            if (sfec.Equals("56"))
            {
                _fec = e_fec.fec_56;
            }
            else
            if (sfec.Equals("78"))
            {
                _fec = e_fec.fec_78;
            }
            else
            if (sfec.Equals("89"))
            {
                _fec = e_fec.fec_89;
            }
            else
            if (sfec.Equals("91"))
            {
                _fec = e_fec.fec_910;
            }
            else
            {
                _fec = e_fec.fec_12;
            }
        }
        public void dvbsystemFromString(string sdvbtype)
        {
            if (sdvbtype.Equals("S2") || sdvbtype.Equals("DVB_S2"))
            {
                dvbsystem = e_dvbsystem.DVB_S2;
            }
            else
            if (sdvbtype.Equals("DVB-S"))
            {
                dvbsystem = e_dvbsystem.DVB_S;
            }
            else
            if (sdvbtype.ToUpper().Equals("DVBC/ANNEX_A") || sdvbtype.Equals("DVB_C"))
            {
                dvbsystem = e_dvbsystem.DVB_C;
            }
            else
            if (sdvbtype.ToUpper().Equals("DVBC/ANNEX_C") || sdvbtype.Equals("DVB_C2"))
            {
                dvbsystem = e_dvbsystem.DVB_C2;
            }
            else
                dvbsystem = e_dvbsystem.DVB_S;
        }
        public void mtypeFromString(string smtype)
        {
            if (smtype.ToUpper().Equals("QPSK"))
            {
                mtype = e_mtype.qpsk;
            }
            else
            if (smtype.ToUpper().Equals("8PSK"))
            {
                mtype = e_mtype.psk8;
            }
            else
            if (smtype.ToUpper().Equals("QAM/16") || smtype.ToUpper().Equals("QAM16"))
            {
                mtype = e_mtype.qam16;
            }
            else
            if (smtype.ToUpper().Equals("QAM/32") || smtype.ToUpper().Equals("QAM32"))
            {
                mtype = e_mtype.qam32;
            }
            else
            if (smtype.ToUpper().Equals("QAM/64") || smtype.ToUpper().Equals("QAM64"))
            {
                mtype = e_mtype.qam64;
            }
            else
            if (smtype.ToUpper().Equals("QAM/128") || smtype.ToUpper().Equals("QAM128"))
            {
                mtype = e_mtype.qam128;
            }
            else
            if (smtype.ToUpper().Equals("QAM/256") || smtype.ToUpper().Equals("QAM256"))
            {
                mtype = e_mtype.qam256;
            }
            else
            {
                mtype = e_mtype.auto;
            }
        }

        public byte[] orbit { get; set; }
        public ushort transportstreamid { get; set; }
        public ushort network_id { get; set; }
        public List<ServiceListItem> services { get; set; }
        public string displayName { get { return getDisplayName(); } }
        private string getDisplayName()
        {
            // Display string to show in ComboBox for examples
            string freq = frequencydecimal.Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
            string line = string.Format("{0},{1},{2},{3},{4},{5}", freq, (char)polarisation, samplerate, (int)fec, dvbsystem, mtype);
            return line;
        }
    }
}
