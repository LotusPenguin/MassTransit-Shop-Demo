using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiadomosci;
using MassTransit;

namespace KlientA
{
    public class StartZamówienia : IStartZamówienia
    {
        public int Ilosc { get; set; }
        public string ClientID { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public class Potwierdzenie : IPotwierdzenie
    {
        public Guid CorrelationId { get; set; }
    }

    public class BrakPotwierdzenia : IBrakPotwierdzenia 
    {
        public Guid CorrelationId { get; set; }
    }

    //Utils class
    class Utils
    {
        public static string formatTimestamp(string input)
        {
            return "[" + input + "] ";
        }
        public static string printTimestamp()
        {
            return formatTimestamp(DateTime.Now.ToString());
        }
    }

    class HandlerClass : IConsumer<IPytanieOPotwierdzenie>, IConsumer<IAkceptacjaZamówienia>, IConsumer<IOdrzucenieZamówienia>
    {
        public Task Consume(ConsumeContext<IPytanieOPotwierdzenie> context)
        {
            throw new NotImplementedException();
        }

        public Task Consume(ConsumeContext<IAkceptacjaZamówienia> context)
        {
            throw new NotImplementedException();
        }

        public Task Consume(ConsumeContext<IOdrzucenieZamówienia> context)
        {
            throw new NotImplementedException();
        }
    }

    internal class Program
    {
        static void DisplayStatus()
        {
            Console.Clear();
            Console.WriteLine("Client A initialized. Press ESC to quit");
        }

        static void Main(string[] args)
        {
            bool exitFlag = false;
            var instance = new HandlerClass();

            var bus = Bus.Factory.CreateUsingRabbitMq(sbc => {
                sbc.Host(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd"), h => {
                    h.Username("xgjwajpd");
                    h.Password("gMGsMovgDYfZHxL1F7ca2sjkY_zhWKiN");
                });
                sbc.ReceiveEndpoint("recvqueueA", ep => {
                    ep.Instance(instance);
                });
            });

            bus.Start();

            //Keyboard input resoultion task
            Task.Factory.StartNew(() =>
            {
                ConsoleKey consoleKey = Console.ReadKey().Key;

                //Dummy input loop for future use
                while (consoleKey != ConsoleKey.Escape)
                {
                    switch (consoleKey)
                    {
                        default:
                            break;
                    }
                    consoleKey = Console.ReadKey().Key;
                }
                exitFlag = true;
            });

            DisplayStatus();

            //Main loop
            while (!exitFlag)
            {
                continue;
            }

            bus.Stop();
        }
    }
}
