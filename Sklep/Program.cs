using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiadomosci;
using MassTransit;
using MassTransit.Saga;
using System.Threading;
using MassTransit.Internals;
using System.Runtime.InteropServices.WindowsRuntime;
using MassTransit.SagaStateMachine;

namespace Sklep
{
    //Message types
    public class Timeout : ITimeout
    {
        public Guid CorrelationId { get; set; }
    }

    public class PytanieOPotwierdzenie : IPytanieOPotwierdzenie 
    {
        public int Ilosc { get; set; }
        public Guid CorrelationId { get; set; } 
    }

    public class PytanieOWolne : IPytanieoWolne
    {
        public int Ilosc { set; get; }
        public Guid CorrelationId { get; set; }
    }

    public class OdrzucenieZamówienia : IOdrzucenieZamówienia
    {
        public int Ilosc { get ; set ; }
        public Guid CorrelationId { get; set; }
    }

    public class AkceptacjaZamówienia : IAkceptacjaZamówienia
    {
        public int Ilosc { get; set; }
        public Guid CorrelationId { get; set; }
    }


    //Saga classes
    public class OrderProcessData : MassTransit.SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public string ClientID { get; set; }
        public int Quantity { get; set; }
        public Guid? TimeoutId { get; set; }
    }

    public class OrderProcessSaga : MassTransit.MassTransitStateMachine<OrderProcessData>
    {
        public State Unconfirmed { get; private set; }

        public State ConfirmedByClient { get; private set; }

        public State ConfirmedByStore { get; private set; }


        public Event<Wiadomosci.IStartZamówienia> StartEvent { get; private set; }
        public Event<Wiadomosci.IBrakPotwierdzenia> UnconfirmedEvent { get; private set; }
        public Event<Wiadomosci.IPotwierdzenie> ConfirmedEvent { get; private set; }
        public Event<Wiadomosci.IOdpowiedzWolneNegatywna> OutOfStockEvent { get; private set; }
        public Event<Wiadomosci.IOdpowiedzWolne> InStockEvent { get; private set; }
        public Event<Wiadomosci.ITimeout> TimeoutEvent { get; private set; }
        public Schedule<OrderProcessData, Wiadomosci.ITimeout> TO {  get; private set; }

        public OrderProcessSaga ()
        {
            InstanceState(x => x.CurrentState);
            Event(() => StartEvent, 
                x => x.CorrelateBy(
                    s => s.ClientID, 
                    ctx => ctx.Message.ClientID)
                .SelectId(ctx => Guid.NewGuid()));

            Schedule(() => TO,
                x => x.TimeoutId,
                x => { x.Delay = TimeSpan.FromSeconds(10); });

            Initially(
                When(StartEvent)
                    .Schedule(TO, ctx => new Timeout() { CorrelationId = ctx.Saga.CorrelationId })
                    .Then(ctx =>
                    {
                        ctx.Saga.ClientID = ctx.Message.ClientID;
                        ctx.Saga.Quantity = ctx.Message.Ilosc;
                    })
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"ID Klienta: {ctx.Message.ClientID}\tIlosc: {ctx.Saga.Quantity}\tID sagi: {ctx.Saga.CorrelationId}");
                    })
                    .Respond(ctx =>
                    {
                        return new PytanieOPotwierdzenie() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity};
                    })
                    .Respond(ctx =>
                    {
                        return new PytanieOWolne() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .TransitionTo(Unconfirmed)
                    );

            During(Unconfirmed,
                When(TimeoutEvent)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[Timeout] {ctx.Saga.CorrelationId} (klient {ctx.Saga.ClientID})");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize(),

                When(ConfirmedEvent)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"Potwierdzenie {ctx.Saga.CorrelationId} przez klienta {ctx.Saga.ClientID}");
                    })
                    .TransitionTo(ConfirmedByClient),

                When(UnconfirmedEvent)
                    .Unschedule(TO)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[ABORT]Anulowanie {ctx.Saga.CorrelationId} przez klienta {ctx.Saga.ClientID}");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize(),

                When(InStockEvent)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"Magazyn potwierdza {ctx.Saga.CorrelationId}");
                    })
                    .TransitionTo(ConfirmedByStore),

                When(OutOfStockEvent)
                    .Unschedule(TO)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[ABORT]Magazyn odrzuca {ctx.Saga.CorrelationId}");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize()
                    );

            During(ConfirmedByClient,
                When(TimeoutEvent)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[Timeout] {ctx.Saga.CorrelationId} (klient {ctx.Saga.ClientID})");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize(),

                When(InStockEvent)
                    .Unschedule(TO)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"Magazyn potwierdza {ctx.Saga.CorrelationId}");
                    })
                    .Respond(ctx => 
                    { 
                        return new AkceptacjaZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize(),

                When(OutOfStockEvent)
                    .Unschedule(TO)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[ABORT]Magazyn odrzuca {ctx.Saga.CorrelationId}");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize()
                );

            During(ConfirmedByStore,
                When(TimeoutEvent)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[Timeout] {ctx.Saga.CorrelationId} (klient {ctx.Saga.ClientID})");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize(),

                When(ConfirmedEvent)
                    .Unschedule(TO)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"Potwierdzenie {ctx.Saga.CorrelationId} przez klienta {ctx.Saga.ClientID}");
                    })
                    .Respond(ctx =>
                    {
                        return new AkceptacjaZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize(),

                When(UnconfirmedEvent)
                    .Unschedule(TO)
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync(Utils.printTimestamp() + $"[ABORT]Anulowanie {ctx.Saga.CorrelationId} przez klienta {ctx.Saga.ClientID}");
                    })
                    .Respond(ctx =>
                    {
                        return new OdrzucenieZamówienia() { CorrelationId = ctx.Saga.CorrelationId, Ilosc = ctx.Saga.Quantity };
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }
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

    //main app class
    internal class Program
    {
        static void DisplayStatus()
        {
            Console.Clear();
            Console.WriteLine("Shop process initialized. Press ESC to quit");
        }

        static void Main(string[] args)
        {
            bool exitFlag = false;
            int counter = 0;

            var repo = new InMemorySagaRepository<OrderProcessData>();
            var machine = new OrderProcessSaga();

            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                sbc.Host(new Uri("rabbitmq://cow.rmq2.cloudamqp.com/xgjwajpd"), h =>
                {
                    h.Username("xgjwajpd");
                    h.Password("gMGsMovgDYfZHxL1F7ca2sjkY_zhWKiN");
                });
                sbc.ReceiveEndpoint("storequeue", ep =>
                {
                    ep.StateMachineSaga(machine, repo);
                });
                sbc.UseInMemoryScheduler();
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
