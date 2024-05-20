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
        int ilosc { get; set; }
    }

    public interface IClientID
    {
        string clientID { get; set; }
    }

    public interface IStartZamówienia : IQuantity, IClientID, CorrelatedBy<Guid> { }
    public interface IPytanieOPotwierdzenie : IQuantity, CorrelatedBy<Guid> { }
    public interface IPotwierdzenie : CorrelatedBy<Guid> { }
    public interface IBrakPotwierdzenia : CorrelatedBy<Guid> { }
    public interface IPytanieoWolne : IQuantity { }
    public interface OdpowiedzWolne : CorrelatedBy<Guid> { }
    public interface OdpowiedzWolneNegatywna : CorrelatedBy<Guid> { }
    public interface AkceptacjaZamówienia : IQuantity { }
    public interface OdrzucenieZamówienia : IQuantity { }
}
