//********************************************************************************
//* Issue Number:
//* Description
//* Company       : Çözümevi Consulting
//* Created on    : 27-10-2008
//* Created by    : XAYILDIZ
//* Last Modified by & on :  a.y. / 10-11-2008
//********************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

namespace sapFIN
{
    class Program
    {
        public static Config config;
        public static Lotus lotus;
        public static Sap sap;

        private const String version = "1.1.0";

        static void Main(string[] args)
        {
            try
            {
                // Ön hazırlıklar
                appendLog("SAP FSS Integration v" + version);
                appendLog("Cozumevi Consulting");
                appendLog("www.cozumevi.com");

                config = new Config();

                lotus = new Lotus();
                lotus.connect(config.lotusPassword);

                sap = new Sap();

                // Müşteriler
                //if (paramExists(args, "-mus"))
                {
                    appendLog("Müşteriler SAP -> FSS yönünde aktariliyor");
                    lotus.setCustomers(sap.getCustomers());
                    sap.setStatu();

                    appendLog("Müşteriler FSS -> SAP yönünde aktariliyor");
                    sap.setCustomers(lotus.getCustomers());
                }

                // Malzemeler
                if (paramExists(args, "-mal"))
                {
                    appendLog("Malzemeler SAP -> FSS yönünde aktariliyor");
                    lotus.setMaterials(sap.getMaterials());
                    sap.setStatu();

                    appendLog("Malzemeler FSS -> SAP yönünde aktariliyor");
                    sap.setMaterials(lotus.getMaterials());
                }

                // Ekipmanlar
                if (paramExists(args, "-eki"))
                {
                    appendLog("Ekipmanlar SAP -> FSS yönünde aktariliyor");
                    lotus.setEquipments(sap.getEquipments());
                    sap.setStatu();

                    appendLog("Ekipmanlar FSS -> SAP yönünde aktariliyor");
                    sap.setEquipments(lotus.getEquipments());
                }

                // Siparişler (Dispatch)
                /// Burada özellikle önce FSS -> SAP yaptık
                /// Zira Dispatch'lerin alanlarının neredeyse tamamı iki tarafta da değişebilir
                /// Ve biz FSS'te yapılan değişiklikleri daha doğru diye varsayıyoruz
                if (paramExists(args, "-sip"))
                {
                    appendLog("Siparişler FSS -> SAP yönünde aktariliyor");
                    sap.setDispatchs(lotus.getDispatchs());
                }

                /*appendLog("Siparişler SAP -> FSS yönünde aktariliyor");
                SAPWS.ZFSS_S_DISPATCH[] sapDis;
                SAPWS.ZFSS_S_DISPATCH_TEXT[] sapDist;
                SAPWS.ZFSS_S_DISPATCH_Z7[] sapZ7;
                SAPWS.ZFSS_S_DISPATCH_EQUIPMENT[] sapDispEqui;
                sap.getDispatchs(out sapDis, out sapDist, out sapZ7, out sapDispEqui);
                lotus.setDispatchs(sapDis, sapDist, sapZ7, sapDispEqui);
                sap.setStatu();*/

                // Teyitler
                if (paramExists(args, "-tey"))
                {
                    appendLog("Teyitler FSS -> SAP yönünde aktariliyor");
                    sap.setConfirmations(lotus.getDispatchs());
                }

                // Bileşenler
                if (paramExists(args, "-bil"))
                {
                    appendLog("Bileşenler FSS -> SAP yönünde aktariliyor");
                    sap.setComponents(lotus.getDispatchs());
                }

                // Final
                lotus.disconnect();
                appendLog("Program bitti");
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
                Console.ReadKey();
            }
        }

        private static bool paramExists(string[] args, string param)
        {
            for (int n = 0; n < args.Length; n++) if (args[n] == param) return true;
            return false;
        }

        public static void appendLog(String Text)
        {
           
                Console.WriteLine(
                     "[" +
                     DateTime.Now.Year.ToString() +

                     (DateTime.Now.Month.ToString().Length == 1 ? "0" : "") +
                     DateTime.Now.Month.ToString() +

                     (DateTime.Now.Day.ToString().Length == 1 ? "0" : "") +
                     DateTime.Now.Day.ToString() +

                     (DateTime.Now.Hour.ToString().Length == 1 ? "0" : "") +
                     DateTime.Now.Hour.ToString() +

                     (DateTime.Now.Minute.ToString().Length == 1 ? "0" : "") +
                     DateTime.Now.Minute.ToString() +

                     (DateTime.Now.Second.ToString().Length == 1 ? "0" : "") +
                     DateTime.Now.Second.ToString() +
                     "] " +
                     Text);
           
        }
    }
}
