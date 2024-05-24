using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;

namespace Wiadomosci
{
    public interface IQuantity
    {
        int Ilosc { get; set; }
    }

    public interface IClientID
    {
        string ClientID { get; set; }
    }

    public interface IStartZamówienia : IQuantity, IClientID, CorrelatedBy<Guid> { }
    public interface IPytanieOPotwierdzenie : IQuantity, CorrelatedBy<Guid> { }
    public interface IPotwierdzenie : CorrelatedBy<Guid> { }
    public interface IBrakPotwierdzenia : CorrelatedBy<Guid> { }
    public interface IPytanieoWolne : IQuantity, CorrelatedBy<Guid> { }
    public interface IOdpowiedzWolne : CorrelatedBy<Guid> { }
    public interface IOdpowiedzWolneNegatywna : CorrelatedBy<Guid> { }
    public interface IAkceptacjaZamówienia : IQuantity, CorrelatedBy<Guid> { }
    public interface IOdrzucenieZamówienia : IQuantity, CorrelatedBy<Guid> 
    {
        string Reason { get; set; }
    }
    public interface ITimeout : CorrelatedBy<Guid> 
    {
        Uri ResponseAddress { get; set; }
    }
}
