using System;using System.IO;using System.Linq;using HarborCity;
// Compiled ONLY with frozen30ede55 reader, never a production legacy adapter.
static class CityOrderFixtureExport {
 static void Main(string[] args){var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));if(!CityOrderWire.Envelope(s,false).SequenceEqual(CityCoreSave.Write(s)))throw new Exception("Witness lost a baseline fact");File.WriteAllBytes(args[1],CityOrderWire.Envelope(s,true));Console.WriteLine("Exported same-input test fixture with observed baseline member order; no lost historical order inferred");}
}
