using log4net;
using log4net.Config;
using Oscam;
using Sat2Ip;
using System;
using System.Reflection;
using System.Text;

namespace Test // Note: actual namespace depends on the project name.
{
    public class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        private void Func(Span<byte> span)
        {
            for (int i = 255; i> 0;i--)
            {
                span[i] = 0;
            }
        }
        static void Main(string[] args)
        {
            Program p = new Program();
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new System.IO.FileInfo(@"log4.config"));
            byte[] barr = new byte[1024];
            Span<byte> span = barr;
            for (int i = 0; i < 255; i++)
            {
                span[i] = (byte)i;
            }
            p.Func(span.Slice(50));
        }
    }
}
