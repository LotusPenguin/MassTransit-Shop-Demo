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
    public class OrderProcessData : MassTransit.SagaStateMachineInstance
    {
        public Guid CorrelationId { get ; set; }
        public string CurrentState { get; set; }
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
            Event(() => StartEvent, x => x.CorrelateBy())
        }

    }

    internal class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
