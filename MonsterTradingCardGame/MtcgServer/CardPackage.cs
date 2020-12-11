using System;
using System.Collections.Generic;

namespace MtcgServer
{
    internal record CardPackage (Guid Id, int Price, IList<ICard> Cards);
}
