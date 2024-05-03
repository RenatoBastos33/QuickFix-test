using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using QuickFix;

namespace TradeClient
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {

            if (args.Length != 1)
            {
                Environment.Exit(2);
            }

            string file = args[0];

            try
            {
                SessionSettings settings = new SessionSettings(file);
                TradeClientApp application = new TradeClientApp();
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                NullLogFactory logFactory = new NullLogFactory();
                QuickFix.Transport.SocketInitiator initiator = new QuickFix.Transport.SocketInitiator(application, storeFactory, settings, logFactory);
                application.MyInitiator = initiator;
                initiator.Start();
                var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
                while (await periodicTimer.WaitForNextTickAsync())
                {
                    application.NewOrderSingle();
                }
                initiator.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            Environment.Exit(1);
        }
    }
}
