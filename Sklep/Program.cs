using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiadomosci;
using MassTransit;
using MassTransit.Saga;

namespace Sklep
{
    //Message types
    class PytanieOPotwierdzenie : IPytanieOPotwierdzenie 
    {
        public int ilosc { get ; set ; } 
        public Guid CorrelationId {  get; set; } 
    }


    public class OrderProcessData : MassTransit.SagaStateMachineInstance
    {
        public Guid CorrelationId { get ; set; }
        public string CurrentState { get; set; }
        public string ClientID { get; set; }
    }

    public class OrderProcessSaga : MassTransit.MassTransitStateMachine<OrderProcessData>
    {
        public State Unconfirmed { get; private set; }

        public State Confirmed { get; private set; }

        public State Cancelled { get; private set; }


        public Event<Wiadomosci.IStartZamówienia> StartEvent { get; private set; }
        public Event<Wiadomosci.IBrakPotwierdzenia> UnconfirmedEvent { get; private set; }
        public Event<Wiadomosci.IPotwierdzenie> ConfirmedEvent { get; private set; }
        public Event<Wiadomosci.OdpowiedzWolneNegatywna> OutOfStockEvent { get; private set; }
        public Event<Wiadomosci.OdpowiedzWolne> InStockEvent { get; private set; }

        public OrderProcessSaga ()
        {
            InstanceState(x => x.CurrentState);
            Event(() => StartEvent, x => x.CorrelateBy(
                s => s.ClientID, ctx => ctx.Message.clientID).SelectId(context => Guid.NewGuid()));

            Initially(
                When(StartEvent)
                    .Then(context =>
                    { })
                    .ThenAsync(ctx =>
                    {
                        return Console.Out.WriteLineAsync($"ID Klienta: {ctx.Message.clientID}\tID sagi: {ctx.Saga.CorrelationId}");
                    })
                    .Respond(ctx =>
                    {
                        return new PytanieOPotwierdzenie() { CorrelationId = ctx.Saga.CorrelationId };
                    })
                    .TransitionTo(Unconfirmed)
                    );
            During(Unconfirmed,
                When(ConfirmedEvent)
                    .Then(ctx => { Console.WriteLine("koniec"); })
                    .Finalize());
            SetCompletedWhenFinalized();
        }

    }

    internal class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
