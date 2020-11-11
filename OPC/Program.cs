using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vlix;

namespace VlixOPC
{
    class Program
    {
        static void Main(string[] args)
        {
            string PipeName = "VlixOPC"; string ProgramDataDir = "Vlix\\VlixOPC";
            if (args == null || args.Length == 1) PipeName = args[0];
            //if (args == null || args.Length == 2) ProgramDataDir = args[1];
            Global.Product = new ProductDetails("VlixOPC", typeof(OPCBackEndConfig), ProgramDataDir);            
            bool SendResult = (new EmbeddedResourceFileSender()).SendFiles("VlixOPC", "SendToProgramDataDirectory", Global.Product.ProgramDataDirectory, "*", false, Assembly.GetExecutingAssembly());
            if (!SendResult) throw new CustomException("FATAL ERROR: Unable to access Directory '" + Global.Product.ProgramDataDirectory);
            
            
            OPCBackEnd oPCBackEnd = new OPCBackEnd(PipeName);
            Console.ReadKey();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
