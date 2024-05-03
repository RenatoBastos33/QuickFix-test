using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using QuickFix;
using QuickFix.Fields;

namespace Executor
{
    public class Executor : QuickFix.MessageCracker, QuickFix.IApplication
    {
        Dictionary<string, decimal> exposition = new Dictionary<string, decimal>();

        int orderID = 0;
        int execID = 0;
        const int EXPOSITION_TRESHOLD = 100000;

        private string GenOrderID() { return (++orderID).ToString(); }
        private string GenExecID() { return (++execID).ToString(); }

        #region Connection Functions

        public void FromApp(Message message, SessionID sessionID)
        {
            if (message is QuickFix.FIX44.NewOrderSingle newOrderSingle)
            {
                Console.WriteLine();
                Console.WriteLine($"=========  Order {newOrderSingle.ClOrdID} received   =============");
                Console.WriteLine();
            }
            Crack(message, sessionID);
        }

        public void ToApp(Message message, SessionID sessionID)
        {
            Console.WriteLine();
            Console.WriteLine("Sending message to client");
        }

        public void FromAdmin(Message message, SessionID sessionID) { }
        public void OnCreate(SessionID sessionID) { }
        public void OnLogout(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) { }
        public void ToAdmin(Message message, SessionID sessionID) { }
        #endregion

        #region


        public void OnMessage(QuickFix.FIX44.NewOrderSingle order, SessionID s)
        {
            ProcessOrder(order, s);
        }

        public void ProcessOrder(QuickFix.FIX44.NewOrderSingle order, SessionID s)
        {
            if (IsOrderValid(order))
            {
                if (IsExpositionValid(order))
                {
                    ApproveOrder(order, s);
                    return;
                }
                RejectOrder(order, s, $"The order {order.ClOrdID} was reject because the exposition was exceeded.");
                return;
            }
            RejectOrder(order, s, $"The order {order.ClOrdID} was reject because the order is invalid.");
        }

        public void RejectOrder(QuickFix.FIX44.NewOrderSingle order, SessionID s, string reason)
        {
            Console.WriteLine();
            Console.WriteLine($" >>>Order {order.ClOrdID} was rejected");
            try
            {
                var rejection = new QuickFix.FIX44.Reject(
                    new RefSeqNum()
                );
                rejection.Text = new Text(reason);
                Session.SendToTarget(rejection, s);
            }
            catch (SessionNotFound ex)
            {
                Console.WriteLine("==session not found exception!==");
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
        public void ApproveOrder(QuickFix.FIX44.NewOrderSingle order, SessionID s)
        {
            Console.WriteLine();
            Console.WriteLine($"Order {order.ClOrdID} was approved");
            var report = FillReport(order);
            try
            {
                Session.SendToTarget(report, s);
            }
            catch (SessionNotFound ex)
            {
                Console.WriteLine("==session not found exception!==");
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        
        public QuickFix.FIX44.ExecutionReport FillReport(QuickFix.FIX44.NewOrderSingle order)
        {
            Symbol symbol = order.Symbol;
            string symbolValue = symbol.getValue();
            Side side = order.Side;
            OrdType ordType = order.OrdType;
            OrderQty orderQty = order.OrderQty;
            Price price = order.Price;
            ClOrdID clOrdID = order.ClOrdID;


            QuickFix.FIX44.ExecutionReport exReport = new QuickFix.FIX44.ExecutionReport(
                new OrderID(GenOrderID()),
                new ExecID(GenExecID()),
                new ExecType(ExecType.FILL),
                new OrdStatus(OrdStatus.FILLED),
                symbol,
                side,
                new LeavesQty(0),
                new CumQty(orderQty.getValue()),
                new AvgPx(price.getValue()));

            exReport.Set(clOrdID);
            exReport.Set(symbol);
            exReport.Set(orderQty);
            exReport.Set(ordType);
            
            // updating the exposition
            exposition[symbolValue] = (exposition.ContainsKey(symbolValue) ? exposition[symbolValue] : 0) + GetOrderExposition(order);
            
            return exReport;
        }

        public static bool IsOrderValid(QuickFix.FIX44.NewOrderSingle order)
        {
            return IsSideValid(order.Side) && IsQtyValid(order.OrderQty) && IsPriceValid(order.Price);
        }
        public static bool IsSideValid(Side side)
        {
            var value = side.getValue();
            return value == Side.BUY || value == Side.SELL;
        }

        public static bool IsQtyValid(OrderQty qty)
        {
            var value = qty.getValue();
            return value > 0 && value < 100000;
        }

        public static bool IsPriceValid(Price price)
        {
            var value = price.getValue();
            return value > 0 && value < 1000;
        }

        public bool IsExpositionValid(QuickFix.FIX44.NewOrderSingle order) 
        {
            var symbol = order.Symbol.getValue();
            decimal symbolExposition = 0;
            if (exposition.ContainsKey(symbol))
            {
                symbolExposition = exposition[symbol];
            }

            var newOrderExposition = GetOrderExposition(order);
            Console.WriteLine($"Exposition for {symbol}: " + symbolExposition);
            Console.WriteLine($"Order exposition {newOrderExposition}");
            return symbolExposition + newOrderExposition < EXPOSITION_TRESHOLD;
        }

        public static decimal GetOrderExposition(QuickFix.FIX44.NewOrderSingle order)
        {
            var price = order.Price.getValue();
            var qty = order.OrderQty.getValue();
            var exposition = price * qty;
            if (order.Side.getValue() == Side.SELL) return - (exposition);
            return exposition;
        }

        #endregion 
    }
}