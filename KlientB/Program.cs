using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiadomosci;
using MassTransit

namespace KlientB
{
    class Utils
    {
        public static string formatTimestamp(string input)
        {
            return "[" + input + "] ";
        }
    }

    class HandlerClass : IConsumer<Wiadomosci.IOdpA>, IConsumer<Wiadomosci.IOdpB>
    {
        static Random rnd = new Random();
        ISendEndpoint sendEpA;
        ISendEndpoint sendEpB;

        public HandlerClass()
        {
            var busA = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                sbc.Host(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd"), h =>
                {
                    h.Username("xgjwajpd");
                    h.Password("gMGsMovgDYfZHxL1F7ca2sjkY_zhWKiN");
                });
            });
            var tsk = busA.GetSendEndpoint(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/recvqueueA"));
            tsk.Wait();
            var sendEpA = tsk.Result;
            this.sendEpA = sendEpA;

            var busB = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                sbc.Host(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd"), h =>
                {
                    h.Username("xgjwajpd");
                    h.Password("gMGsMovgDYfZHxL1F7ca2sjkY_zhWKiN");
                });
            });
            tsk = busB.GetSendEndpoint(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/recvqueueB"));
            tsk.Wait();
            var sendEpB = tsk.Result;
            this.sendEpB = sendEpB;
        }

        public Task Consume(ConsumeContext<IOdpA> ctx)
        {
            int randInt = rnd.Next(1, 4);
            if (randInt == 3)
            {
                try
                {
                    throw new Exception("Message processing exception");
                }
                catch (Exception e)
                {
                    Console.Out.WriteLineAsync(
                    Utils.formatTimestamp(ctx.Headers.Get<string>("timestamp")) +
                    "Throwing Exception to OdpA" + $" (#{ctx.Headers.Get<string>("message_no")})");

                    //Message, gdy wywołany wyjątek od abonenta A
                    sendEpA.Send(new Publ("Message from A processing exception, retrying..."), responsectx =>
                    {
                        responsectx.Headers.Set("timestamp", DateTime.Now.ToString());
                    });

                    return Task.FromException(e);
                }
            }
            else
            {
                return Console.Out.WriteLineAsync(
                    Utils.formatTimestamp(ctx.Headers.Get<string>("timestamp")) +
                    "#" + ctx.Headers.Get<string>("message_no") + " " +
                    ctx.Message.kto);
            }
        }
    }

    internal class Program
    {
        static void DisplayStatus()
        {
            Console.Clear();
            Console.WriteLine("Publisher initialized. Press ESC to quit");
        }

        static void Main(string[] args)
        {
            bool exitFlag = false;
            int counter = 0;


        }
    }
}
