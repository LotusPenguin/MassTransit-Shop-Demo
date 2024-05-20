using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiadomosci
{
    public interface IQuantity
    {
        int ilosc { get; set; }
    }

    public interface IStartZamówienia : IQuantity { }
    public interface IPytanieOPotwierdzenie : IQuantity { }
    public interface IPotwierdzenie { }
    public interface IBrakPotwierdzenia { }
    public interface IPytanieoWolne : IQuantity { }
    public interface OdpowiedzWolneNegatywna { }
    public interface AkceptacjaZamówienia : IQuantity { }
    public interface OdrzucenieZamówienia : IQuantity { }
}
