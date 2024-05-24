using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiadomosci;
using MassTransit;
using MassTransit.Saga;
using System.Threading;
using MassTransit.Transports;
using static MassTransit.Logging.DiagnosticHeaders.Messaging;

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
        public bool IsOrderOngoing { get; set; }

        public HandlerClass()
        {
            IsOrderOngoing = false;
        }

        public Task Consume(ConsumeContext<IPytanieOPotwierdzenie> context)
        {
            Console.Out.WriteLineAsync(Utils.printTimestamp() + $"Do you want to confirm the order of {context.Message.Ilosc} items (y/n)? ");
            ConsoleKey input = Console.ReadKey().Key;
            if (input == ConsoleKey.Y)
            {
                Console.Out.WriteLineAsync($"\n{Utils.printTimestamp()}Confirmation request sent to shop");
                return Task.Run(() => context.RespondAsync(new Potwierdzenie() { CorrelationId = context.Message.CorrelationId }, ctx =>
                {
                    ctx.ResponseAddress = new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/recvqueueA");
                }));
            }
            else if (input == ConsoleKey.N)
            {
                Console.Out.WriteLineAsync($"\n{Utils.printTimestamp()}Cancellation request sent to shop");
                return Task.Run(() => context.RespondAsync(new BrakPotwierdzenia() { CorrelationId = context.Message.CorrelationId}, ctx =>
                {
                    ctx.ResponseAddress = new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/recvqueueA");
                }));
            }
            else if (input == ConsoleKey.Escape)
            {
                return Task.CompletedTask;
            }
            else
            {
                if (IsOrderOngoing)
                {
                    return Task.Run(() => Consume(context));
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
        }

        public Task Consume(ConsumeContext<IAkceptacjaZamówienia> context)
        {
            IsOrderOngoing = false;
            return Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Order no. {context.Message.CorrelationId} has been confirmed by the shop.");
        }

        public Task Consume(ConsumeContext<IOdrzucenieZamówienia> context)
        {
            IsOrderOngoing = false;
            return Console.Out.WriteLineAsync($"{Utils.printTimestamp()}[CANCELLED] Order no. {context.Message.CorrelationId} has been cancelled by the shop (reason: {context.Message.Reason}).");
        }
    }

    internal class Program
    {
        static void DisplayStatus()
        {
            Console.Clear();
            Console.WriteLine("Client A initialized. Press ESC to quit");
            Console.WriteLine("Press ENTER to order items");
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
            var task = bus.GetSendEndpoint(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/storequeue"));
            task.Wait();
            var sendEp = task.Result; 

            bus.Start();

            DisplayStatus();

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
                            Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Enter order quantity: ");
                            try
                            {
                                quantity = int.Parse(Console.ReadLine());
                                sendEp.Send(new StartZamówienia() { ClientID = "klient_A", Ilosc = quantity }, ctx =>
                                {
                                    ctx.ResponseAddress = new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd/recvqueueA");
                                });
                                instance.IsOrderOngoing = true;
                                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Input locked, awaiting order processing");
                                
                                while(instance.IsOrderOngoing)
                                {
                                    //busy waiting
                                    Thread.Sleep(100);
                                }

                                //clear input buffer
                                while (Console.KeyAvailable)
                                    Console.ReadKey(true);
                            }
                            catch (FormatException)
                            {
                                Console.Out.WriteLineAsync($"{Utils.printTimestamp()}Incorrect input. Order cancelled.");
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
