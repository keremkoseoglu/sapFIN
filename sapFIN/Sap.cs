using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
namespace sapFIN
{
   public  class Sap
    {
       private SAPWS.service sapService;
      
       private String reqid;
          
       public Sap()
       {
           //try
           //{
               sapService = new sapFIN.SAPWS.service();
               sapService.Url = sapService.Url.Replace("host-sap", "saptest.draeger.com");
               NetworkCredential nc = new NetworkCredential(Program.config.sapUsername, Program.config.sapPassword);
               sapService.UseDefaultCredentials = false;
               sapService.Credentials = nc;
           //}
           //catch(System.Net.Sockets.SocketException hata)
           //{
           //    Console.WriteLine("Hata oluştu...\n" + hata.Message);
           //}
              
       }
#region Ekipman Aktarimi       
       public SAPWS.ZFSS_S_EQUIPMENT[] getEquipments()
       {   
           SAPWS.ZFSS_GET_EQUIPMENT eq = new SAPWS.ZFSS_GET_EQUIPMENT();
           SAPWS.ZFSS_GET_EQUIPMENTResponse res = sapService.ZFSS_GET_EQUIPMENT(eq);
           reqid = res.E_REQID;
           return res.E_EQUIPMENT;
       }
       public void setEquipments(Domino.NotesViewClass view)
       {
           if (view == null) return;

           SAPWS.ZFSS_SET_EQUIPMENT eq = new sapFIN.SAPWS.ZFSS_SET_EQUIPMENT();
           eq.I_EQUIPMENT = new sapFIN.SAPWS.ZFSS_S_EQUIPMENT_IMPORT[view.AllEntries.Count];
           
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               // Ekipman bilgileri
               eq.I_EQUIPMENT[n - 1] = new sapFIN.SAPWS.ZFSS_S_EQUIPMENT_IMPORT();
               eq.I_EQUIPMENT[n - 1].EQUNR = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "numeqnr");
               eq.I_EQUIPMENT[n - 1].ZZSWVER = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "txteqsoftwareversion", 20);
               eq.I_EQUIPMENT[n - 1].ZZLOCAT = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "txteqstandort", 30);

               // Malzeme üzerinden gelecek bilgiler
               eq.I_EQUIPMENT[n - 1].ZZPMSID = Program.lotus.getMaterialPmsId(Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "txtartsachnr"));
           }
           SAPWS.ZFSS_SET_EQUIPMENTResponse res = sapService.ZFSS_SET_EQUIPMENT(eq);
           }
#endregion 
#region Dispatch Aktarimi
       public void getDispatchs(out SAPWS.ZFSS_S_DISPATCH[] D, out SAPWS.ZFSS_S_DISPATCH_TEXT[] T, out SAPWS.ZFSS_S_DISPATCH_Z7[] Z7, out SAPWS.ZFSS_S_DISPATCH_EQUIPMENT[] E)
       {
           SAPWS.ZFSS_GET_DISPATCH eq1 = new sapFIN.SAPWS.ZFSS_GET_DISPATCH();
           SAPWS.ZFSS_GET_DISPATCHResponse res1 = sapService.ZFSS_GET_DISPATCH(eq1);
           
           reqid = res1.E_REQID;

           D = res1.E_DISPATCH;
           T = res1.E_TEXT;
           Z7 = res1.E_Z7;
           E = res1.E_EQUIPMENT;
       }
       public void setDispatchs(Domino.NotesViewClass view)
       {
           ArrayList txtr, txti, txtw;
           int dispcount = 0;
           int disppos =  -1;

           if (view == null) return;

           // Verileri çek
           SAPWS.ZFSS_SET_DISPATCH eq = new sapFIN.SAPWS.ZFSS_SET_DISPATCH();

           // Toplamda kaç Dispatch ile karşı karşıyayız tespit et ve Dispatch değişkenlerini yarat
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               String form = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "FORM");
               if (form == "frm_ea")
               {
                   for (int m = 1; m <= view.GetNthDocument(n).Responses.Count; m++)
                   {
                       Domino.NotesDocumentClass sd = (Domino.NotesDocumentClass) view.GetNthDocument(n).Responses.GetNthDocument(m);
                       String subform = Lotus.getItemValue(sd, "FORM");
                       if (subform == "frm_gzea") dispcount++;
                   }
               }
           }
           if (dispcount <= 0) return;

           eq.I_DISPATCH = new sapFIN.SAPWS.ZFSS_S_DISPATCH_FLD[dispcount];
           eq.I_TEXT = new sapFIN.SAPWS.ZFSS_S_DISPATCH_TEXT[0];

           // Verileri aktar
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               Domino.NotesDocumentClass di = (Domino.NotesDocumentClass)view.GetNthDocument(n);

               String form = Lotus.getItemValue(di, "FORM");
               //if (form == "frm_ea" || form == "frm_gzea")
               if (form == "frm_ea")
               {

                   for (int m = 1; m <= view.GetNthDocument(n).Responses.Count; m++)
                   {
                       Domino.NotesDocumentClass sd = (Domino.NotesDocumentClass) view.GetNthDocument(n).Responses.GetNthDocument(m);
                       String subform = Lotus.getItemValue(sd, "FORM");
                       if (subform == "frm_gzea")
                       {
                           disppos++;

                           eq.I_DISPATCH[disppos] = new sapFIN.SAPWS.ZFSS_S_DISPATCH_FLD();
                           eq.I_DISPATCH[disppos].AUFNR = Lotus.getItemValue(sd, "SAPtxtNo").Replace("U", "");
                           eq.I_DISPATCH[disppos].NAME1 = Lotus.getItemValue(sd, "anzeige1", 30);
                           eq.I_DISPATCH[disppos].ZFSE = Lotus.getItemValue(sd, "SAPTSRID", 12);
                           eq.I_DISPATCH[disppos].ZJOBTYPE = Lotus.getItemValue(di, "txtpriority", 4);
                           eq.I_DISPATCH[disppos].AUFK_ERDAT = parseLotusDate(Lotus.getItemValue(di, "dateabestelltam"));
                           eq.I_DISPATCH[disppos].GSTRP = parseLotusDate(Lotus.getItemValue(di, "dateeadue"));
                           eq.I_DISPATCH[disppos].ZCONTACT_NAME1 = Lotus.getItemValue(di, "txteabesteller", 35);
                           eq.I_DISPATCH[disppos].TEL_NUMBER = Lotus.getItemValue(di, "txtcontacttel", 30);

                           txtr = splitText(Lotus.getItemValue(di, "txteakrztxt"), 132);
                           for (int t = 0; t < txtr.Count; t++)
                           {
                               eq.I_TEXT = expandDispatchTextTable(eq.I_TEXT);
                               eq.I_TEXT[eq.I_TEXT.Length - 1].AUFNR = eq.I_DISPATCH[disppos].AUFNR.Replace("U", "");
                               eq.I_TEXT[eq.I_TEXT.Length - 1].DTTYP = "R";
                               eq.I_TEXT[eq.I_TEXT.Length - 1].TLINE = (String)txtr[t];

                           }

                           txti = splitText(Lotus.getItemValue(di, "rtxtinfoIntern"), 132);
                           for (int t = 0; t < txti.Count; t++)
                           {
                               eq.I_TEXT = expandDispatchTextTable(eq.I_TEXT);
                               eq.I_TEXT[eq.I_TEXT.Length - 1].AUFNR = eq.I_DISPATCH[disppos].AUFNR.Replace("U", "");
                               eq.I_TEXT[eq.I_TEXT.Length - 1].DTTYP = "I";
                               eq.I_TEXT[eq.I_TEXT.Length - 1].TLINE = (String)txti[t];
                           }

                           txtw = splitText(Lotus.getItemValue(sd, "rtxtpruefbericht"), 132);
                           for (int t = 0; t < txtw.Count; t++)
                           {
                               eq.I_TEXT = expandDispatchTextTable(eq.I_TEXT);
                               eq.I_TEXT[eq.I_TEXT.Length - 1].AUFNR = eq.I_DISPATCH[disppos].AUFNR.Replace("U", "");
                               eq.I_TEXT[eq.I_TEXT.Length - 1].DTTYP = "W";
                               eq.I_TEXT[eq.I_TEXT.Length - 1].TLINE = (String)txtw[t];
                           }


                           //eq.I_DISPATCH[disppos].AUFNR = Lotus.getItemValue(sd, "txteanrUS"); --> GEREK YOK ZATEN ÜSTTE YAZDIK

                           String tmp = Lotus.getItemValue(di, "txteajobtype");
                           eq.I_DISPATCH[disppos].VAPLZ = tmp.Trim().Length <= 0 || tmp.IndexOf("-") <= 0 ? "" : tmp.Substring(0, tmp.IndexOf("-"));
                           if (eq.I_DISPATCH[disppos].VAPLZ.Length > 8) eq.I_DISPATCH[disppos].VAPLZ = eq.I_DISPATCH[disppos].VAPLZ.Substring(0, 8);

                           eq.I_DISPATCH[disppos].ZINVOICE = Lotus.getItemValue(di, "Invoice") == "1" ? "X" : "";
                           eq.I_DISPATCH[disppos].QMNUM = Lotus.getItemValue(sd, "numkdkostenstelle");
                           eq.I_DISPATCH[disppos].ZFSE = Lotus.getItemValue(di, "SAPTSRID");
                           eq.I_DISPATCH[disppos].KUNNR = Lotus.getItemValue(sd, "txtkdnr");
                           eq.I_DISPATCH[disppos].EQUNR = Lotus.getItemValue(sd, "numeqnr");
                           eq.I_DISPATCH[disppos].AUSVN = parseLotusDate(Lotus.getItemValue(di, "dateabestelltam"));

                           if (Lotus.getItemValue(sd, "TXTEQSTATUS") == "2") // yeşil ışık
                           {
                               eq.I_DISPATCH[disppos].AUSBS = parseToday();
                           }

                           eq.I_DISPATCH[disppos].ZFSSUST = Lotus.getItemValue(di, "numeanr");

                           // Additional readers
                           String[] ar = Lotus.getItemValues(sd, "AdditionalReaders");
                           eq.I_Z7 = new sapFIN.SAPWS.ZFSS_S_DISPATCH_Z7[ar.Length];
                           for (int a = 0; a < ar.Length; a++)
                           {
                               eq.I_Z7[a] = new sapFIN.SAPWS.ZFSS_S_DISPATCH_Z7();
                               eq.I_Z7[a].AUFNR = eq.I_DISPATCH[disppos].AUFNR.Replace("U", "");
                               eq.I_Z7[a].PARNR = Program.lotus.getLotusPernr(ar[a]);
                           }
                       } // frm_gzea

                   } // loop at subdocs
               } // IF FORM
           } // FOR
           SAPWS.ZFSS_SET_DISPATCHResponse res = sapService.ZFSS_SET_DISPATCH(eq);
       }

       public static SAPWS.ZFSS_S_DISPATCH_TEXT[] expandDispatchTextTable(SAPWS.ZFSS_S_DISPATCH_TEXT[] t)
       {
           SAPWS.ZFSS_S_DISPATCH_TEXT[] ret = new sapFIN.SAPWS.ZFSS_S_DISPATCH_TEXT[t.Length + 1];

           for (int n = 0; n < t.Length; n++) ret[n] = t[n];

           ret[t.Length] = new sapFIN.SAPWS.ZFSS_S_DISPATCH_TEXT();
           return ret;
       }

#endregion
#region Malzeme
       public SAPWS.ZFSS_S_MALZEME[] getMaterials()
       {
           SAPWS.ZFSS_GET_MALZEME eq = new sapFIN.SAPWS.ZFSS_GET_MALZEME();
           SAPWS.ZFSS_GET_MALZEMEResponse res = sapService.ZFSS_GET_MALZEME(eq);
           reqid = res.E_REQID;
           return res.E_MALZEME;
       }
       public void setMaterials(Domino.NotesViewClass view)
       {
           if (view == null) return;

           SAPWS.ZFSS_SET_MALZEME eq = new sapFIN.SAPWS.ZFSS_SET_MALZEME();
           eq.I_MALZEME = new sapFIN.SAPWS.ZFSS_S_MATNR_IMPORT[view.AllEntries.Count];

           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               eq.I_MALZEME[n - 1] = new sapFIN.SAPWS.ZFSS_S_MATNR_IMPORT();
               eq.I_MALZEME[n - 1].MATNR = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "txtartsachnr");
               eq.I_MALZEME[n - 1].ZZPMSID = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "txtartpkid", 20);            
           }
           SAPWS.ZFSS_SET_MALZEMEResponse res = sapService.ZFSS_SET_MALZEME(eq);
       }
#endregion
#region Musteri
       public SAPWS.ZFSS_S_MUSTERI[] getCustomers()
       {
           SAPWS.ZFSS_GET_MUSTERI eq = new sapFIN.SAPWS.ZFSS_GET_MUSTERI();
           SAPWS.ZFSS_GET_MUSTERIResponse res = sapService.ZFSS_GET_MUSTERI(eq);
           reqid = res.E_REQID;
           return res.E_MUSTERI;
       }
       public void setCustomers(Domino.NotesViewClass view)
       {
           if (view == null) return;

           SAPWS.ZFSS_SET_MUSTERI eq = new sapFIN.SAPWS.ZFSS_SET_MUSTERI();
           eq.I_MUSTERI = new sapFIN.SAPWS.ZFSS_S_MUSTERI[view.AllEntries.Count];
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               eq.I_MUSTERI[n - 1] = new sapFIN.SAPWS.ZFSS_S_MUSTERI();

               eq.I_MUSTERI[n - 1].KUNNR = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "Kunden_Nummer");
               //eq.I_MUSTERI[n-1].NAME1 = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n),"Suchname");
               //eq.I_MUSTERI[n - 1].NAME2 = Lotus.getItemValue((Domino.NotesDocumentClass)view.GetNthDocument(n), "CompanyAddress");
           }
           SAPWS.ZFSS_SET_MUSTERIResponse res = sapService.ZFSS_SET_MUSTERI(eq);
           
       }
#endregion
#region Teyit
       public void setConfirmations(Domino.NotesViewClass view)
       {
           int dispcount = 0;
           int disppos = -1;

           if (view == null) return;

           // Toplamda kaç Dispatch ile karşı karşıyayız tespit et ve Dispatch değişkenlerini yarat
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               Domino.NotesDocumentClass di = (Domino.NotesDocumentClass)view.GetNthDocument(n);
               String form = Lotus.getItemValue(di, "FORM");
               if (form == "frm_ea")
               {
                   for (int m = 1; m <= di.Responses.Count; m++)
                   {
                       Domino.NotesDocumentClass sd = (Domino.NotesDocumentClass) di.Responses.GetNthDocument(m);
                       String subform = Lotus.getItemValue(sd, "FORM");
                       if (subform == "frm_gzea")
                       {
                           for (int x = 1; x <= sd.Responses.Count; x++)
                           {
                               Domino.NotesDocumentClass co = (Domino.NotesDocumentClass)sd.Responses.GetNthDocument(x);
                               String subform2 = Lotus.getItemValue(co, "FORM");
                               if (subform2 == "frm_arbleist") dispcount++;
                           }
                       }
                   }
               }
           }
           if (dispcount <= 0) return;

           SAPWS.ZFSS_SET_CONFIRMATION eq = new sapFIN.SAPWS.ZFSS_SET_CONFIRMATION();
           eq.I_CONFIRMATION = new sapFIN.SAPWS.ZFSS_S_CONFIRMATION[dispcount];

           // Devam
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               Domino.NotesDocumentClass di = (Domino.NotesDocumentClass)view.GetNthDocument(n);
               String form = Lotus.getItemValue(di, "FORM");

               if (form == "frm_ea")
               {
                   for (int m = 1; m <= di.Responses.Count; m++)
                   {
                       Domino.NotesDocumentClass sd = (Domino.NotesDocumentClass) di.Responses.GetNthDocument(m);
                       String subform = Lotus.getItemValue(sd, "FORM");
                       if (subform == "frm_gzea")
                       {
                           for (int x = 1; x <= sd.Responses.Count; x++)
                           {
                               Domino.NotesDocumentClass co = (Domino.NotesDocumentClass) sd.Responses.GetNthDocument(x);
                               String subform2 = Lotus.getItemValue(co, "FORM");
                               if (subform2 == "frm_arbleist")
                               {
                                   disppos++;

                                   eq.I_CONFIRMATION[disppos] = new sapFIN.SAPWS.ZFSS_S_CONFIRMATION();
                                   eq.I_CONFIRMATION[disppos].ARBPL = Lotus.getItemValue(co, "numleiststdacttext", 6);
                                   eq.I_CONFIRMATION[disppos].BUDAT = parseLotusDate(Lotus.getItemValue(co, "dattatwork"));
                                   eq.I_CONFIRMATION[disppos].IEDZ_WAIT = parseLotusTime(Lotus.getItemValue(co, "numwaittimeende"));
                                   eq.I_CONFIRMATION[disppos].IEDZ_WORK = parseLotusTime(Lotus.getItemValue(co, "numleiststdende"));
                                   eq.I_CONFIRMATION[disppos].ISDZ_WAIT = parseLotusTime(Lotus.getItemValue(co, "numwaittimebeginn"));
                                   eq.I_CONFIRMATION[disppos].ISDZ_WORK = parseLotusTime(Lotus.getItemValue(co, "numleiststdbeginn"));
                                   eq.I_CONFIRMATION[disppos].ISMNE_TRAV = "H";
                                   eq.I_CONFIRMATION[disppos].ISMNE_WAIT = "H";
                                   eq.I_CONFIRMATION[disppos].ISMNE_WORK = "H";
                                   eq.I_CONFIRMATION[disppos].ISMNW_TRAV = Lotus.getItemValueAsDecimal(co, "numtraveltime");
                                   eq.I_CONFIRMATION[disppos].ISMNW_TRAV = Decimal.Parse(String.Format("{0:00.0}", eq.I_CONFIRMATION[disppos].ISMNW_TRAV));
                                   //eq.I_CONFIRMATION[disppos].ISMNW_WAIT = Lotus.getItemValueAsDecimal(co, "numwaittimeacttextprint");
                                   //eq.I_CONFIRMATION[disppos].ISMNW_WORK = Lotus.getItemValueAsDecimal(co, "numleiststd");
                                   eq.I_CONFIRMATION[disppos].LUNID = co.UniversalID;
                                   eq.I_CONFIRMATION[disppos].LTXA1 = Lotus.getItemValue(co, "txtbemerk", 40);
                                   eq.I_CONFIRMATION[disppos].AUFNR = Lotus.getItemValue(co, "SAPtxtNo", 40).Replace("U", "");
                               }
                           }
                       }
                   }
               }
           }
           SAPWS.ZFSS_SET_CONFIRMATIONResponse res = sapService.ZFSS_SET_CONFIRMATION(eq);
       }
       #endregion
#region Bileşen
       public void setComponents(Domino.NotesViewClass view)
       {
           int dispcount = 0;
           int disppos = -1;

           if (view == null) return;

           // Toplamda kaç Dispatch ile karşı karşıyayız tespit et ve Dispatch değişkenlerini yarat
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               Domino.NotesDocumentClass di = (Domino.NotesDocumentClass)view.GetNthDocument(n);
               String form = Lotus.getItemValue(di, "FORM");
               if (form == "frm_ea")
               {
                   for (int m = 1; m <= di.Responses.Count; m++)
                   {
                       Domino.NotesDocumentClass sd = (Domino.NotesDocumentClass)di.Responses.GetNthDocument(m);
                       String subform = Lotus.getItemValue(sd, "FORM");
                       if (subform == "frm_gzea")
                       {
                           for (int x = 1; x <= sd.Responses.Count; x++)
                           {
                               Domino.NotesDocumentClass co = (Domino.NotesDocumentClass)sd.Responses.GetNthDocument(x);
                               String subform2 = Lotus.getItemValue(co, "FORM");
                               if (subform2 == "frm_lum") dispcount++;
                           }
                       }
                   }
               }
           }
           if (dispcount <= 0) return;

           SAPWS.ZFSS_SET_COMPONENT eq = new sapFIN.SAPWS.ZFSS_SET_COMPONENT();
           eq.I_COMPONENT = new sapFIN.SAPWS.ZFSS_S_COMPONENT[dispcount];

           // Devam
           for (int n = 1; n <= view.AllEntries.Count; n++)
           {
               Domino.NotesDocumentClass di = (Domino.NotesDocumentClass)view.GetNthDocument(n);
               String form = Lotus.getItemValue(di, "FORM");

               if (form == "frm_ea")
               {
                   for (int m = 1; m <= di.Responses.Count; m++)
                   {
                       Domino.NotesDocumentClass sd = (Domino.NotesDocumentClass)di.Responses.GetNthDocument(m);
                       String subform = Lotus.getItemValue(sd, "FORM");
                       if (subform == "frm_gzea")
                       {
                           for (int x = 1; x <= sd.Responses.Count; x++)
                           {
                               Domino.NotesDocumentClass co = (Domino.NotesDocumentClass)sd.Responses.GetNthDocument(x);
                               String subform2 = Lotus.getItemValue(co, "FORM");
                               if (subform2 == "frm_lum")
                               {
                                   disppos++;

                                   eq.I_COMPONENT[disppos] = new sapFIN.SAPWS.ZFSS_S_COMPONENT();
                                   eq.I_COMPONENT[disppos].MATNR = Lotus.getItemValue(co, "txtartsachnr", 18);
                                   eq.I_COMPONENT[disppos].BDMNG = Lotus.getItemValueAsDecimal(co, "numlumverbrauch");
                                   eq.I_COMPONENT[disppos].MEINS = Lotus.getItemValue(co, "numartme");
                                   eq.I_COMPONENT[disppos].ZZACCNT = Lotus.getItemValue(co, "txtaccindcodeLUM");
                                   eq.I_COMPONENT[disppos].ZZSERNE = Lotus.getItemValue(co, "txtfabriknr");
                                   eq.I_COMPONENT[disppos].ZZSEROL = Lotus.getItemValue(co, "txtfabriknrout");
                                   eq.I_COMPONENT[disppos].POTX1 = Lotus.getItemValue(co, "txtbemerk");
                                   eq.I_COMPONENT[disppos].ZZVANLO = Lotus.getItemValue(co, "txtlumlagerort");
                                   eq.I_COMPONENT[disppos].LUNID = co.UniversalID;
                                   eq.I_COMPONENT[disppos].AUFNR = Lotus.getItemValue(co, "SAPtxtNo", 40).Replace("U", "");
                               }
                           }
                       }
                   }
               }
           }
           SAPWS.ZFSS_SET_COMPONENTResponse res = sapService.ZFSS_SET_COMPONENT(eq);
       }
       #endregion

       public void setStatu()
       {
           SAPWS.ZFSS_SET_STATU ss = new sapFIN.SAPWS.ZFSS_SET_STATU();
           ss.I_REQID = reqid;
           sapService.ZFSS_SET_STATU(ss);   
       }

       public static String shiftLeft(String S, String TrimChar)
       {
           if (S.Trim().Length <= 0) return "";

           String ret = S;
           while (ret.Substring(0, 1) == TrimChar)
           {
               ret = ret.Substring(1, ret.Length - 1);
               if (ret.Length <= 0) return ret;
           }
           return ret;  
       }

       public static ArrayList splitText(String Text, int Length)
       {
           ArrayList ret = new ArrayList();
           String text = Text;
           bool cont = true;

           while (cont)
           {
               ret.Add(text.Length < Length ? text : text.Substring(0, Length));

               if (text.Length <= Length)
               {
                   cont = false;
               }
               else
               {
                   text = text.Substring(Length, text.Length - Length);
               }
           }

           return ret;
       }

       public static String parseLotusDate(String LotusDate)
       {
           // 12.01.2008 -> 2008-01-12
           return LotusDate.Trim().Length <= 0 ? "" : LotusDate.Substring(6, 4) + '-' + LotusDate.Substring(3, 2) + "-" + LotusDate.Substring(0, 2);
       }

       public static String parseLotusTime(String LotusDate)
       {
           // 12.01.2008 15:16:17 -> 15:16:17
           return LotusDate.Trim().Length <= 0 ? "" : LotusDate.Substring(11, 8);

       }

       public static String parseToday()
       {
           // Today -> 2008-01-12
           String mon = System.DateTime.Now.Month.ToString();
           if (mon.Length == 1) mon = "0" + mon;

           String day = System.DateTime.Now.Day.ToString();
           if (day.Length == 1) day = "0" + day;

           return System.DateTime.Now.Year.ToString() + "-" + mon + "-" + day;
       }

   }
}
