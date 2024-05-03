using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using QuickFix;

namespace Executor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            if (args.Length != 1)
            {
                Environment.Exit(2);
            }

            try
            {
                SessionSettings settings = new SessionSettings(args[0]);
                IApplication executorApp = new Accumulator();
                IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                NullLogFactory logFactory = new NullLogFactory();
                ThreadedSocketAcceptor acceptor = new ThreadedSocketAcceptor(executorApp, storeFactory, settings, logFactory);
                
                acceptor.Start();
                Console.Read();
                acceptor.Stop();
            }
            catch (System.Exception e)
            {
                Console.WriteLine("==FATAL ERROR==");
                Console.WriteLine(e.ToString());
            }
        }
    }
}
