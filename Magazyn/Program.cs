using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiadomosci;
using MassTransit;
using System.Threading;

namespace Magazyn
{
    class OdpowiedzWolne : IOdpowiedzWolne
    {
        public Guid CorrelationId {  get; set; }
    }

    class OdpowiedzWolneNegatywna : IOdpowiedzWolneNegatywna
    {
        public Guid CorrelationId { get; set; }
    }

    class HandlerClass : IConsumer<IPytanieoWolne>, IConsumer<IAkceptacjaZamówienia>, IConsumer<IOdrzucenieZamówienia>
    {
        public int Free { get; set; }
        public int Reserved { get; set; }
        public ISendEndpoint sendEndpoint { get; set; }

        public HandlerClass() 
        {
            Free = 0;
            Reserved = 0;

            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                sbc.Host(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd"), h =>
                {
                    h.Username("xgjwajpd");
                    h.Password("gMGsMovgDYfZHxL1F7ca2sjkY_zhWKiN");
                });
            });

            var task = bus.GetSendEndpoint(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/storequeue"));
            task.Wait();
            sendEndpoint = task.Result;
        }

        public Task Consume(ConsumeContext<IPytanieoWolne> context)
        {
            if (Free >= context.Message.Ilosc)
            {
                Free -= context.Message.Ilosc;
                Reserved += context.Message.Ilosc;
                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Reserved {context.Message.Ilosc} items");
                Utils.DisplayStockAmount(Free, Reserved);
                return sendEndpoint.Send(new OdpowiedzWolne() { CorrelationId = context.Message.CorrelationId });
            }
            else
            {
                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Refused reservation of {context.Message.Ilosc} items - not enough stock");
                Utils.DisplayStockAmount(Free, Reserved);
                return sendEndpoint.Send(new OdpowiedzWolneNegatywna() { CorrelationId = context.Message.CorrelationId });
            }
        }

        public Task Consume(ConsumeContext<IAkceptacjaZamówienia> context)
        {
            Reserved -= context.Message.Ilosc;
            Console.Out.WriteLineAsync($"{Utils.printTimestamp()}{context.Message.Ilosc} items prepared for shipping");
            Utils.DisplayStockAmount(Free, Reserved);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<IOdrzucenieZamówienia> context)
        {
            if(context.Message.Reason != "Out of stock")
            {
                Reserved -= context.Message.Ilosc;
                Free += context.Message.Ilosc;
                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}{context.Message.Ilosc} reserved items changed status to free");
            }
            else
            {
                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Cancellation of order {context.Message.CorrelationId} confirmed");
            }
            Utils.DisplayStockAmount(Free, Reserved);
            return Task.CompletedTask;
        }
    }

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
        public static void DisplayStockAmount(int free, int reserved)
        {
            Console.WriteLine("### Stock amount ###");
            Console.WriteLine($"Free: {free}\tReserved: {reserved}");
        }
    }

    internal class Program
    {
        static void DisplayStatus(int free, int reserved)
        {
            Console.Clear();
            Console.WriteLine("Warehouse initialized. Press ESC to quit");
            Console.WriteLine("Press ENTER to add new stock");
            Utils.DisplayStockAmount(free, reserved);
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
                sbc.ReceiveEndpoint("warehousequeue", ep => {
                    ep.Instance(instance);
                });
            });

            bus.Start();

            DisplayStatus(instance.Free, instance.Reserved);

            //Main loop
            while (!exitFlag)
            {
                ConsoleKey consoleKey = Console.ReadKey().Key;

                //Dummy input loop for future use
                while (consoleKey != ConsoleKey.Escape)
                {
                    switch (consoleKey)
                    {
                        case ConsoleKey.Enter:
                            int quantity;
                            Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Enter item quantity: ");
                            try
                            {
                                quantity = int.Parse(Console.ReadLine());
                                instance.Free += quantity;
                                Utils.DisplayStockAmount(instance.Free, instance.Reserved);
                            }
                            catch (FormatException)
                            {
                                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Incorrect input. Stocking procedure cancelled.");
                            }
                            break;
                        default:
                            break;
                    }
                    consoleKey = Console.ReadKey().Key;
                }
                exitFlag = true;
                continue;
            }

            bus.Stop();
        }
    }
}
