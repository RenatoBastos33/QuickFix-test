using System;
using QuickFix;
using QuickFix.Fields;
using System.Collections.Generic;
using System.Timers;
using QuickFix.FIX44;

namespace TradeClient
{
    public class Generator : QuickFix.MessageCracker, QuickFix.IApplication
    {
        Session? _session = null;

        public IInitiator? MyInitiator = null;


        private readonly string[] _orderSymbols = { "PETR4", "VALE3", "VIIA4"};
        private readonly int _maximumQnt = 100000;
        private readonly int _maximumPrice = 1000;

        int clOrderID = 0;

        private string GenOrderID() { return (++clOrderID).ToString(); }


        #region Connection handlers
        public void OnCreate(SessionID sessionID)
        {
            _session = Session.LookupSession(sessionID);
        }
        public void OnLogon(SessionID sessionID) { _session = Session.LookupSession(sessionID); }
        public void OnLogout(SessionID sessionID) { _session = null; }
        public void FromAdmin(QuickFix.Message message, SessionID sessionID) {

            if (message is QuickFix.FIX44.Reject newOrderSingle)
            {
                try
                {
                    Crack(message, sessionID);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("==Cracker exception==");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        public void ToAdmin(QuickFix.Message message, SessionID sessionID) { 
        }
        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
            try
            {
                Crack(message, sessionID);
            }
            catch (Exception)
            {
            }
        }

        public void ToApp(QuickFix.Message message, SessionID sessionID)
        {
            try
            {
                bool possDupFlag = false;
                if (message.Header.IsSetField(QuickFix.Fields.Tags.PossDupFlag))
                {
                    possDupFlag = QuickFix.Fields.Converters.BoolConverter.Convert(
                        message.Header.GetString(QuickFix.Fields.Tags.PossDupFlag));
                }

                if (message is QuickFix.FIX44.NewOrderSingle newOrderSingle)
                {
                    Console.WriteLine();
                    Console.WriteLine("=============  Order sent to server  ==============");
                    Console.WriteLine();
                    Console.WriteLine("Symbol: " + newOrderSingle.Symbol.getValue());
                    Console.WriteLine("Qty: " + newOrderSingle.OrderQty.getValue());
                    Console.WriteLine("Price: " + newOrderSingle.Price.getValue());
                    Console.WriteLine("Operation: " + newOrderSingle.Side.getValue());
                    Console.WriteLine();
                }

                if (possDupFlag)
                    throw new DoNotSend();


            }
            catch (FieldNotFoundException)
            { }

            Console.WriteLine();
        }
        public bool IsConnectionActive(SessionID sessionID)
        {
            // Verificar se a sessão está conectada e autenticada
            return Session.LookupSession(sessionID).IsLoggedOn;
        }
        #endregion

        public void NewOrderSingle()
        {
           
            if(_session == null || !_session.IsLoggedOn)
            {
                Console.WriteLine("connection was not established");
                return;
            }

            QuickFix.FIX44.NewOrderSingle newOrderSingle = new QuickFix.FIX44.NewOrderSingle(
                new ClOrdID(GenOrderID()),
                new Symbol(GetRandomSymbol()),
                new Side(GetRandomSide()),
                new TransactTime(DateTime.Now),
                new OrdType(OrdType.LIMIT));
            newOrderSingle.Set(new OrderQty(GetRandomQty()));
            newOrderSingle.Set(new TimeInForce(TimeInForce.DAY));
            newOrderSingle.Set(GetRandomPrice());
            _session.Send(newOrderSingle);
        }



        public void OnMessage(QuickFix.FIX44.ExecutionReport r, SessionID s)
        {
            Console.WriteLine($"Order {r.ClOrdID} was accepted");
            Console.WriteLine();
        }
        public void OnMessage(QuickFix.FIX44.Reject r, SessionID s)
        {
            Console.WriteLine($"Order was rejected");
            Console.WriteLine(r.Text);
            Console.WriteLine();
        }




        public string GetRandomSymbol()
        {
            Random random = new Random();
            int randomIndex = random.Next(0, _orderSymbols.Length);
            return _orderSymbols[randomIndex];
        }
        public static char GetRandomSide()
        {
            Random random = new Random();
            int randomSide = random.Next(0, 2);

            if (randomSide == 0) return Side.BUY;
            return Side.SELL;
        }
        public Price GetRandomPrice() 
        {
            Random random = new Random();
            decimal randomPrice = random.Next(0, _maximumPrice * 100) / 100.0m;
            return new Price(randomPrice);
        }       
        public decimal GetRandomQty() 
        {
            Random random = new Random();
            return random.Next(0, _maximumQnt);
        }
    }
}
