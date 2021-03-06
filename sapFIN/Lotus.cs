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
  public class Lotus
    {
      private Domino.NotesSessionClass session;
      private Domino.NotesDatabaseClass database;

      private const String DEFAULT_VIEW_NAME = "TURKEY SAP Changed Data";
      private const String DB_EQUIPMENT = @"FSS\Equipment.nsf";
      private const String DB_DISPATCH = @"FSS\Dispatch.nsf";
      private const String DB_MATERIAL = @"FSS\Material.nsf";
      private const String DB_CUSTOMER = @"FSS\Customer.nsf";
      private const String DB_USER = @"FSS\User.nsf";

      private const string DISPATCH_PARENT_FORM = "frm_ea";
      private const string DISPATCH_CHILD_FORM = "frm_gzea";

      private const string SAP_TERRITORY = "TR 001";
      private const string TEAM_READER = "FSS TR 001 MNG";
      private const string TEAM_READER_STAND_ALONE = "FSS TR 001";
      private const string READ_ALL = "[ReadAll]";

      private enum DISPATCH_TYPE : int { PARENT, CHILD };

      public Lotus()
      {

      }
      public void connect(String Password)
      {
          session = new Domino.NotesSessionClass();
          session.Initialize(Password);
      }
      public void disconnect()
      {
          session = null;
      }
      private void openDatabase(String FileName)
      {
          database = (Domino.NotesDatabaseClass)session.GetDatabase("",FileName,false);
      }
      public static String getItemValue(Domino.NotesDocumentClass Document, String Field)
      {
          /*if (Document == null) return "";          
          return (String)((Object[])Document.GetItemValue(Field))[0].ToString();*/

          return getItemValue(Document, Field, 0);
      }

      public static String getItemValue(Domino.NotesDocumentClass Document, String Field, int Maxlength)
      {
          if (Document == null) return "";
          String ret = (String)((Object[])Document.GetItemValue(Field))[0].ToString();

          return Maxlength == 0 || (Maxlength != 0 && ret.Length <= Maxlength) ? ret : ret.Substring(0, Maxlength);
      }

      public static decimal getItemValueAsDecimal(Domino.NotesDocumentClass Document, String Field)
      {
          if (Document == null) return 0;

          try
          {
              String r = (String)((Object[])Document.GetItemValue(Field))[0];
              if (r == "") return 0;
              return Decimal.Parse(r);
          }
          catch
          {
          }

          try
          {
              Double d = (Double)((Object[])Document.GetItemValue(Field))[0];
              return Decimal.Parse(d.ToString());
          }
          catch
          {
          }

          return (decimal)((Object[])Document.GetItemValue(Field))[0];
      }

      public static String[] getItemValues(Domino.NotesDocumentClass Document, String Field)
      {
          Object[] ar = ((Object[])Document.GetItemValue(Field));

          String[] ret = new String[ar.Length];
          for (int n = 0; n < ar.Length; n++) ret[n] = ar[n].ToString();

          return ret;
      }

      public static String parseSapDate(String SapDate)
      {
          // 2008-01-12 -> 12.01.2008
          return SapDate.Substring(8, 2) + '.' + SapDate.Substring(5, 2) + "." + SapDate.Substring(0, 4);
      }

      private void getLotusUser(String Pernr, out String Lname, out String Cname)
      {
          Lname = null;
          Cname = null;

          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_USER, false);
          Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("txttetechid = '" + Sap.shiftLeft(Pernr, "0") + "'", null, 99999);
          for (int n = 0; n < docs.Count; n++)
          {
              Domino.NotesDocumentClass doc = (Domino.NotesDocumentClass) docs.GetNthDocument(n);
              Lname = getItemValue(doc, "benutzer");
              Cname = getItemValue(doc, "benutzerInterface");
          }
      }

      public String getLotusPernr(String Lname)
      {
          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_USER, false);
          Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("benutzerInterface = '" + Lname + "'", null, 99999);
          for (int n = 1; n <= docs.Count; n++)
          {
              Domino.NotesDocumentClass doc = (Domino.NotesDocumentClass)docs.GetNthDocument(n);
              String ret = getItemValue(doc, "txttetechid");
              if (ret != null && ret.Trim().Length > 0) return ret;
          }
          
          return "";
      }

#region Ekipman Aktarimi
      public Domino.NotesViewClass getEquipments()
      {
         openDatabase(DB_EQUIPMENT);
         Domino.NotesViewClass view = (Domino.NotesViewClass)database.GetView(DEFAULT_VIEW_NAME);
         return view;
      }

      public void setEquipments(SAPWS.ZFSS_S_EQUIPMENT[] E)
      {
          Domino.NotesDocumentClass doc;
          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_EQUIPMENT,false);
          for (int n = 0; n < E.Length; n++)
          {
             //txtkdnr
              Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("numeqnr = '" + Sap.shiftLeft(E[n].EQUNR, "0") + "'", null, 99999);
             
              if (E[n].CHANGE_IND == "I" && (docs == null || docs.Count <= 0))
              {
                
                  doc = (Domino.NotesDocumentClass)database.CreateDocument();
                 
                  doc.AppendItemValue("FORM", "frm_eq");
                  //doc.AppendItemValue("KundenInfo", "KK KundenInfo");
                  doc.AppendItemValue("numeqicon", "78");
                  doc.AppendItemValue("numeqnr", Sap.shiftLeft(E[n].EQUNR, "0"));
                  doc.AppendItemValue("numsort", "2");
                  doc.AppendItemValue("txtartbenenn", E[n].EQKTX);
                  doc.AppendItemValue("txtkdnr", Sap.shiftLeft(E[n].PARNR, "0"));
                  doc.AppendItemValue("txtfabriknr", Sap.shiftLeft(E[n].SERNR, "0"));
                  doc.AppendItemValue("txtartsachnr", Sap.shiftLeft(E[n].MATNR, "0"));
                  doc.AppendItemValue("txtartpkid", getMaterialPmsId(Sap.shiftLeft(E[n].MATNR, "0")));
                  doc.AppendItemValue("numsvnr", Sap.shiftLeft(E[n].ZZVBELN, "0"));
                  doc.AppendItemValue("txtkdkrzbez", E[n].NAME1);
                  doc.AppendItemValue("dateqinbetriebnahme", E[n].DRTBAS);
                  doc.AppendItemValue("dateqgarantieende", E[n].DRTBTS);
                  doc.AppendItemValue("txtsvstatus", parseSapDate(E[n].ZZVNDAT));
                  doc.AppendItemValue("dateqletztewartung", parseSapDate(E[n].ZZLASTDATE));
                  doc.AppendItemValue("txtkdinventarnr", E[n].ZZEXTWG);
                  doc.AppendItemValue("txtBillingPartnerNo", E[n].Y2TEKNIK);

                  doc.AppendItemValue("ReadAll", READ_ALL);
                  doc.AppendItemValue("TeamReaderStandAlone", TEAM_READER_STAND_ALONE);
                  doc.AppendItemValue("SalesOfficeTemp", SAP_TERRITORY);
                  
                  doc.Save(false, false, false);
                  }
              else if ((E[n].CHANGE_IND == "I" && (docs != null && docs.Count > 0 )) || (E[n].CHANGE_IND == "U"))
              {
                  
                  for (int m = 0; m < docs.Count; m++)
                  {
                      doc = (Domino.NotesDocumentClass)docs.GetNthDocument(m);
                     
                      doc.ReplaceItemValue("FORM", "frm_eq");
                      doc.ReplaceItemValue("numeqicon", "78");
                      doc.ReplaceItemValue("numsort", "2");
                      doc.ReplaceItemValue("numeqnr", Sap.shiftLeft(E[n].EQUNR, "0"));
                      doc.ReplaceItemValue("txtartbenenn", E[n].EQKTX);
                      doc.ReplaceItemValue("txtkdnr", Sap.shiftLeft(E[n].PARNR, "0"));
                      doc.ReplaceItemValue("txtfabriknr", Sap.shiftLeft(E[n].SERNR, "0"));
                      doc.ReplaceItemValue("txtartsachnr", Sap.shiftLeft(E[n].MATNR, "0"));
                      doc.ReplaceItemValue("txtartpkid", getMaterialPmsId(Sap.shiftLeft(E[n].MATNR, "0")));
                      doc.ReplaceItemValue("numsvnr", Sap.shiftLeft(E[n].ZZVBELN, "0"));
                      doc.ReplaceItemValue("dateqletztewartung", E[n].ZZLASTDATE);
                      doc.ReplaceItemValue("dateqinbetriebnahme", E[n].DRTBAS);
                      doc.ReplaceItemValue("dateqgarantieende", E[n].DRTBTS);
                      doc.ReplaceItemValue("txtkdkrzbez", E[n].NAME1);
                      doc.ReplaceItemValue("txtsvstatus", E[n].ZZVNDAT);
                      doc.ReplaceItemValue("txtkdinventarnr", Sap.shiftLeft(E[n].ZZEXTWG, "0"));
                      doc.ReplaceItemValue("txtBillingPartnerNo", Sap.shiftLeft(E[n].Y2TEKNIK, "0"));

                      //doc.ReplaceItemValue("ReadAll", READ_ALL);
                      //doc.ReplaceItemValue("TeamReaderStandAlone", TEAM_READER_STAND_ALONE);
                      //doc.ReplaceItemValue("SalesOfficeTemp", SAP_TERRITORY);
                  
                      doc.Save(false, false, false);
                  }       
              }
          }
      }
#endregion
#region Dispatch Aktarimi
      public Domino.NotesViewClass getDispatchs()
      {
          openDatabase(DB_DISPATCH);
          Domino.NotesViewClass view1 = (Domino.NotesViewClass)database.GetView(DEFAULT_VIEW_NAME);
          return view1;
      }

      public void setDispatchs(SAPWS.ZFSS_S_DISPATCH[] E, SAPWS.ZFSS_S_DISPATCH_TEXT[] T, SAPWS.ZFSS_S_DISPATCH_Z7[] Z7, SAPWS.ZFSS_S_DISPATCH_EQUIPMENT[] DE)
      {
          String text;

          Domino.NotesDocumentClass doc;
          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_DISPATCH, false);
          for (int n = 0; n < E.Length; n++)
          {
              Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("numeanr = '" + Sap.shiftLeft(E[n].AUFNR, "0") + "'", null, 99999);

              if (E[n].CHANGE_IND == "I" && (docs == null || docs.Count <= 0))
              {
                  // Parent Dispatch
                  doc = (Domino.NotesDocumentClass)database.CreateDocument();
                  appendDispatchFields(ref doc, E[n], T, Z7, DE, DISPATCH_TYPE.PARENT, database);
                  doc.Save(true, true, true);

                  // Child Dispatch
                  doc = (Domino.NotesDocumentClass)database.CreateDocument();
                  appendDispatchFields(ref doc, E[n], T, Z7, DE, DISPATCH_TYPE.CHILD, database);
                  doc.Save(true, true, true);
              }
              else if ((E[n].CHANGE_IND == "I" && (docs != null && docs.Count > 0)) || (E[n].CHANGE_IND == "U"))
              {
                  for (int m = 0; m < docs.Count; m++)
                  {
                      doc = (Domino.NotesDocumentClass)docs.GetNthDocument(m);
                      switch (getItemValue(doc, "FORM"))
                      {
                          case DISPATCH_PARENT_FORM:
                              replaceDispatchFields(ref doc, E[n], T, Z7, DE, DISPATCH_TYPE.PARENT);
                              doc.Save(true, true, true);
                              break;
                          case DISPATCH_CHILD_FORM:
                              replaceDispatchFields(ref doc, E[n], T, Z7, DE, DISPATCH_TYPE.CHILD);
                              doc.Save(true, true, true);
                              break;
                      }
                  }
              }
          }
      }

      private void appendDispatchFields(ref Domino.NotesDocumentClass doc, SAPWS.ZFSS_S_DISPATCH E, SAPWS.ZFSS_S_DISPATCH_TEXT[] T, SAPWS.ZFSS_S_DISPATCH_Z7[] Z7, SAPWS.ZFSS_S_DISPATCH_EQUIPMENT[] DE, DISPATCH_TYPE DispatchType, Domino.NotesDatabaseClass Database)
      {
          String text;

          // Child?
          if (DispatchType == DISPATCH_TYPE.CHILD)
          {
              // Genel bilgiler
              doc.AppendItemValue("FORM", "frm_gzea");
              doc.AppendItemValue("SAPtxtNo", Sap.shiftLeft(buildChildAufnr(E.AUFNR), "0"));
              doc.AppendItemValue("numeqicon", 78);
              doc.AppendItemValue("numsort", 2);
              doc.AppendItemValue("txteqhistkennung", "X");
              doc.AppendItemValue("TXTEQSTATUS", "1");
              doc.AppendItemValue("_____txteqhistkennung", "X");

              // Ekipmana özel bilgiler
              for (int n = 0; n < DE.Length; n++)
              {
                  if (DE[n].AUFNR == E.AUFNR)
                  {
                      doc.AppendItemValue("txtartpkid", getMaterialPmsId(Sap.shiftLeft(DE[n].MATNR, "0")));
                      doc.AppendItemValue("numeqnr", Sap.shiftLeft(DE[n].EQUNR, "0"));
                      doc.AppendItemValue("txtfabriknr", Sap.shiftLeft(DE[n].SERNR, "0"));
                      doc.AppendItemValue("txteqsoftwareversion", DE[n].ZZSWVER);
                      doc.AppendItemValue("txteqstandort", DE[n].ZZLOCAT);
                      doc.AppendItemValue("dateqletztewartung", parseSapDate(DE[n].ZZLASTDATE));
                      doc.AppendItemValue("txtartsachnr", Sap.shiftLeft(DE[n].MATNR, "0"));
                  }
              }

              // Parent bağlantısı
              Domino.NotesDocumentClass parent = getParentDispatch(E.AUFNR, Database);
              if (parent != null)
              {
                  doc.MakeResponse(parent);
                  doc.AppendItemValue("$REF", parent.UniversalID);
              }
          }
          else
          {
              doc.AppendItemValue("FORM", "frm_ea");
              doc.AppendItemValue("numeaicon", 25);
          }

          doc.AppendItemValue("numeanr", Sap.shiftLeft(E.AUFNR, "0"));
          doc.AppendItemValue("anzeige1", E.NAME1);
          doc.AppendItemValue("txtartbenenn", E.NAME1);
          doc.AppendItemValue("txtTSR", E.ZFSE_VORNA + " " + E.ZFSE_NACHN);
          doc.AppendItemValue("SAPTSRID", Sap.shiftLeft(E.ZFSE, "0"));
          doc.AppendItemValue("txtpriority", E.ZJOBTYPE);
          doc.AppendItemValue("dateabestelltam", parseSapDate(E.AUFK_ERDAT));
          doc.AppendItemValue("dateeadue", parseSapDate(E.GSTRP));
          doc.AppendItemValue("txteabesteller", E.ZCONTACT_NAME1);
          doc.AppendItemValue("txtcontacttel", E.TEL_NUMBER);

          text = "";
          for (int x = 0; x < T.Length; x++) if (T[x].AUFNR == E.AUFNR && T[x].DTTYP == "R") text += (text.Length > 0 ? "\r\n" + T[x].TLINE : T[x].TLINE);
          doc.AppendItemValue("txteakrztxt", text);

          text = "";
          for (int x = 0; x < T.Length; x++) if (T[x].AUFNR == E.AUFNR && T[x].DTTYP == "I") text += (text.Length > 0 ? "\r\n" + T[x].TLINE : T[x].TLINE);
          doc.AppendItemValue("txtinfoIntern", text);

          text = "";
          for (int x = 0; x < T.Length; x++) if (T[x].AUFNR == E.AUFNR && T[x].DTTYP == "W") text += (text.Length > 0 ? "\r\n" + T[x].TLINE : T[x].TLINE);
          doc.AppendItemValue("WorkPerformed", text);

          doc.AppendItemValue("txteanrUS", Sap.shiftLeft(E.AUFNR, "0"));
          doc.AppendItemValue("CreatedBy", E.ERNAM_NAME_FIRST + " " + E.ERNAM_NAME_LAST);
          doc.AppendItemValue("txteajobtype", E.VAPLZ + " - " + E.VAPLZ_KTEXT);
          doc.AppendItemValue("txteastatus", "0");
          doc.AppendItemValue("Invoice", E.ZINVOICE == "X" ? "1" : "0");
          doc.AppendItemValue("numeaestvalue", Sap.shiftLeft(E.QMNUM, "0"));
          doc.AppendItemValue("txtkdkrzbez", E.NAME1);
          doc.AppendItemValue("txtkdnr", Sap.shiftLeft(E.KUNNR, "0"));
          doc.AppendItemValue("txtCustomerAddress", E.STRAS + " " + E.PSTLZ + " " + E.ORT01);

          doc.AppendItemValue("ReadAll", READ_ALL);
          doc.AppendItemValue("TeamReader", TEAM_READER);

          String l = "";
          String c = "";
          getLotusUser(E.ZFSE, out l, out c);
          doc.AppendItemValue("TeamReaderStandAlone", c);

          // Additional Readers
          for (int n = 0; n < Z7.Length; n++)
          {
              if (Z7[n].AUFNR == E.AUFNR)
              {
                  String lname = "";
                  String cname = "";

                  getLotusUser(Z7[n].PARNR, out lname, out cname);

                  if (lname != null)
                  {
                      doc.AppendItemValue("AdditionalReaders", lname);
                      doc.AppendItemValue("AdditionalReadersCN", cname);
                  }
              }
          }
      }

      private void replaceDispatchFields(ref Domino.NotesDocumentClass doc, SAPWS.ZFSS_S_DISPATCH E, SAPWS.ZFSS_S_DISPATCH_TEXT[] T, SAPWS.ZFSS_S_DISPATCH_Z7[] Z7, SAPWS.ZFSS_S_DISPATCH_EQUIPMENT[] DE, DISPATCH_TYPE DispatchType)
      {
          String text;

          // Child?
          if (DispatchType == DISPATCH_TYPE.CHILD)
          {
              // Genel bilgiler
              doc.ReplaceItemValue("FORM", "frm_gzea");
              doc.ReplaceItemValue("SAPtxtNo", Sap.shiftLeft(buildChildAufnr(E.AUFNR), "0"));

              // Ekipmana özel bilgiler
              for (int n = 0; n < DE.Length; n++)
              {
                  if (DE[n].AUFNR == E.AUFNR)
                  {
                      doc.ReplaceItemValue("txtartpkid", getMaterialPmsId(Sap.shiftLeft(DE[n].MATNR, "0")));
                      doc.ReplaceItemValue("numeqnr", Sap.shiftLeft(DE[n].EQUNR, "0"));
                      doc.ReplaceItemValue("txtfabriknr", Sap.shiftLeft(DE[n].SERNR, "0"));
                      doc.ReplaceItemValue("txteqsoftwareversion", DE[n].ZZSWVER);
                      doc.ReplaceItemValue("txteqstandort", DE[n].ZZLOCAT);
                      doc.ReplaceItemValue("dateqletztewartung", parseSapDate(DE[n].ZZLASTDATE));
                      doc.ReplaceItemValue("txtartsachnr", Sap.shiftLeft(DE[n].MATNR, "0"));
                  }
              }              
          }
          else
          {
              doc.ReplaceItemValue("FORM", "frm_ea");
              doc.ReplaceItemValue("numeaicon", 25);
          }

          doc.ReplaceItemValue("numeanr", Sap.shiftLeft(E.AUFNR, "0"));

          doc.ReplaceItemValue("anzeige1", E.NAME1);
          doc.ReplaceItemValue("txtartbenenn", E.NAME1);
          doc.ReplaceItemValue("txtTSR", E.ZFSE_VORNA + " " + E.ZFSE_NACHN);
          doc.ReplaceItemValue("SAPTSRID", Sap.shiftLeft(E.ZFSE, "0"));
          doc.ReplaceItemValue("txtpriority", E.ZJOBTYPE);
          doc.ReplaceItemValue("dateabestelltam", parseSapDate(E.AUFK_ERDAT));
          doc.ReplaceItemValue("dateeadue", parseSapDate(E.GSTRP));
          doc.ReplaceItemValue("txteabesteller", E.ZCONTACT_NAME1);
          doc.ReplaceItemValue("txtcontacttel", E.TEL_NUMBER);

          text = "";
          for (int x = 0; x < T.Length; x++) if (T[x].AUFNR == E.AUFNR && T[x].DTTYP == "R") text += (text.Length > 0 ? "\r\n" + T[x].TLINE : T[x].TLINE);
          doc.ReplaceItemValue("txteakrztxt", text);

          text = "";
          for (int x = 0; x < T.Length; x++) if (T[x].AUFNR == E.AUFNR && T[x].DTTYP == "I") text += (text.Length > 0 ? "\r\n" + T[x].TLINE : T[x].TLINE);
          doc.ReplaceItemValue("txtinfoIntern", text);

          text = "";
          for (int x = 0; x < T.Length; x++) if (T[x].AUFNR == E.AUFNR && T[x].DTTYP == "W") text += (text.Length > 0 ? "\r\n" + T[x].TLINE : T[x].TLINE);
          doc.ReplaceItemValue("WorkPerformed", text);

          doc.ReplaceItemValue("txteanrUS", Sap.shiftLeft(E.AUFNR, "0"));
          doc.ReplaceItemValue("CreatedBy", E.ERNAM_NAME_FIRST + " " + E.ERNAM_NAME_LAST);
          doc.ReplaceItemValue("txteajobtype", E.VAPLZ + " - " + E.VAPLZ_KTEXT);
          //doc.ReplaceItemValue("txteastatus", "0"); --> Bunu yapmıyoruz, trafik lambasıyla Update sırasında oynamasak daha iyi
          doc.ReplaceItemValue("Invoice", E.ZINVOICE == "X" ? "1" : "0");
          doc.ReplaceItemValue("numeaestvalue", Sap.shiftLeft(E.QMNUM, "0"));
          doc.ReplaceItemValue("txtkdkrzbez", E.NAME1);
          doc.ReplaceItemValue("txtkdnr", Sap.shiftLeft(E.KUNNR, "0"));
          doc.ReplaceItemValue("txtCustomerAddress", E.STRAS + " " + E.PSTLZ + " " + E.ORT01);

          //doc.ReplaceItemValue("ReadAll", READ_ALL);
          //doc.ReplaceItemValue("TeamReader", TEAM_READER);

          // Additional Readers
          doc.RemoveItem("AdditionalReaders");
          doc.RemoveItem("AdditionalReadersCN");

          for (int n = 0; n < Z7.Length; n++)
          {
              if (Z7[n].AUFNR == E.AUFNR)
              {
                  String lname = "";
                  String cname = "";

                  getLotusUser(Z7[n].PARNR, out lname, out cname);

                  if (lname != null)
                  {
                      doc.AppendItemValue("AdditionalReaders", lname);
                      doc.AppendItemValue("AdditionalReadersCN", cname);
                  }
              }
          }
      }

      private String buildChildAufnr(String Aufnr)
      {
          return Sap.shiftLeft(Aufnr, "0") + "01";
      }

      private Domino.NotesDocumentClass getParentDispatch(String Aufnr, Domino.NotesDatabaseClass Database)
      {
          String aufnr = Aufnr;
          Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)Database.Search("numeanr = '" + Sap.shiftLeft(aufnr, "0") + "'", null, 99999);
          for (int n = 0; n < docs.Count; n++)
          {
              Domino.NotesDocumentClass doc = (Domino.NotesDocumentClass)docs.GetNthDocument(n);
              if (getItemValue(doc, "FORM") == DISPATCH_PARENT_FORM) return doc;
          }

          return null;
      }


#endregion Dispatch
#region Malzeme Aktarimi
      public Domino.NotesViewClass getMaterials()
      {
          openDatabase(DB_MATERIAL);
          Domino.NotesViewClass view = (Domino.NotesViewClass)database.GetView(DEFAULT_VIEW_NAME);
          return view;
      }

      public Domino.NotesDocumentClass getMaterial(String Matnr)
      {
          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_MATERIAL, false);
          Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("txtartsachnr = '" + Sap.shiftLeft(Matnr, "0") + "'", null, 1);
          if (docs == null) return null;
          if (docs.Count <= 0) return null;

          return (Domino.NotesDocumentClass) docs.GetFirstDocument();
      }

      public String getMaterialPmsId(String Matnr)
      {
          Domino.NotesDocumentClass material = getMaterial(Matnr);
          return material != null ? Lotus.getItemValue(material, "txtartpkid") : "";
      }

      public void setMaterials(SAPWS.ZFSS_S_MALZEME[] E)
      {
          Domino.NotesDocumentClass doc;
          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_MATERIAL, false);
          for (int n = 0; n < E.Length; n++)
          {

              Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("txtartsachnr = '" + Sap.shiftLeft(E[n].MATNR, "0") + "'", null, 99999);

              if (E[n].CHANGE_IND == "I" && (docs == null || docs.Count <= 0))
              {
                  doc = (Domino.NotesDocumentClass)database.CreateDocument();
                  doc.AppendItemValue("FORM", "frm_art");
                  doc.AppendItemValue("txtartsachnr", Sap.shiftLeft(E[n].MATNR, "0"));
                  doc.AppendItemValue("numartme",E[n].MEINS);
                  doc.AppendItemValue("txtartbenenn", E[n].MAKTX);
                  doc.AppendItemValue("txtarterzeuggruppe", E[n].MATKL);
                  doc.AppendItemValue("Bcrit", E[n].XCHPF == "X" ? "Yes" : "No");
                  doc.AppendItemValue("BodySymbol", 0);
                  doc.AppendItemValue("SAPTerritory", SAP_TERRITORY);
                  doc.AppendItemValue("txtartleihgeraet", "0");
                  doc.Save(false, false, false);
              }
              else if ((E[n].CHANGE_IND == "I" && (docs != null && docs.Count > 0)) || (E[n].CHANGE_IND == "U"))
              {
                  for (int m = 0; m < docs.Count; m++)
                  {
                      doc = (Domino.NotesDocumentClass)docs.GetNthDocument(m);
                      doc.ReplaceItemValue("FORM", "frm_art");
                      doc.ReplaceItemValue("txtartsachnr", Sap.shiftLeft(E[n].MATNR, "0"));
                      doc.ReplaceItemValue("numartme", E[n].MEINS);
                      doc.ReplaceItemValue("txtartbenenn", E[n].MAKTX);
                      doc.ReplaceItemValue("txtarterzeuggruppe", E[n].MATKL);
                      doc.ReplaceItemValue("Bcrit", E[n].XCHPF == "X" ? "Yes" : "No");
                      doc.ReplaceItemValue("BodySymbol", 0);
                      doc.ReplaceItemValue("SAPTerritory", SAP_TERRITORY);
                      doc.ReplaceItemValue("txtartleihgeraet", "0");
                      doc.Save(false, false, false);
                  }
              }
          }
      }
#endregion Malzeme
#region Musteri
      public Domino.NotesViewClass getCustomers()
      {
          openDatabase(DB_CUSTOMER);
          Domino.NotesViewClass view = (Domino.NotesViewClass)database.GetView(DEFAULT_VIEW_NAME);
          return view;
      }
      public void setCustomers(SAPWS.ZFSS_S_MUSTERI[] E)
      {
          Domino.NotesDocumentClass doc;
          Domino.NotesDatabaseClass database = (Domino.NotesDatabaseClass)session.GetDatabase("", DB_CUSTOMER, false);
          for (int n = 0; n < E.Length; n++)
          {

              Domino.NotesDocumentCollectionClass docs = (Domino.NotesDocumentCollectionClass)database.Search("Kunden_Nummer = '" + Sap.shiftLeft(E[n].KUNNR, "0") + "'", null, 99999);

              if (E[n].CHANGE_IND == "I" && (docs == null || docs.Count <= 0))
              {
                  doc = (Domino.NotesDocumentClass)database.CreateDocument();
                  doc.AppendItemValue("FORM", "Kunde");
                  doc.AppendItemValue("Kunden_Nummer", Sap.shiftLeft(E[n].KUNNR, "0"));
                  doc.AppendItemValue("txtkdnr", Sap.shiftLeft(E[n].KUNNR, "0"));
                  doc.AppendItemValue("Suchname", E[n].NAME1);
                  doc.AppendItemValue("txtkdkrzbez", E[n].NAME1);
                  doc.AppendItemValue("CompanyCity", E[n].BEZEI);
                  doc.AppendItemValue("State", E[n].VKBEZ);
                  doc.AppendItemValue("CompanyMainZIP", E[n].BRTXT);
                  doc.AppendItemValue("CompanyAddress", E[n].STRAS + " " + E[n].PSTLZ + " " + E[n].ORT01);
                  doc.AppendItemValue("SalesGroup", Sap.shiftLeft(E[n].PERNR_Y2, "0"));
                  doc.AppendItemValue("SalesGroupTemp", Sap.shiftLeft(E[n].PERNR_Y2, "0"));
                  doc.AppendItemValue("TSRVan", E[n].VORNA_Y2 + " " + E[n].NACHN_Y2);
                  doc.AppendItemValue("TSRName", E[n].VORNA_Y2 + " " + E[n].NACHN_Y2);

                  doc.AppendItemValue("Region", SAP_TERRITORY);
                  doc.AppendItemValue("BodySymbol", 0);
                  doc.AppendItemValue("SalesOffice", SAP_TERRITORY);
                  doc.AppendItemValue("SalesOfficeTemp", SAP_TERRITORY);
                  doc.AppendItemValue("SAPStatus", "No");
                  doc.AppendItemValue("SAPTerritory", "NO");
                  doc.AppendItemValue("SAPTSRID", "0101");
                  doc.AppendItemValue("TeamReaderStandAlone", TEAM_READER_STAND_ALONE);
                  doc.AppendItemValue("ReadAll", READ_ALL);

                  doc.Save(false, false, false);
              }
              else if ((E[n].CHANGE_IND == "I" && (docs != null && docs.Count > 0)) || (E[n].CHANGE_IND == "U"))
              {
                  for (int m = 0; m < docs.Count; m++)
                  {
                      doc = (Domino.NotesDocumentClass)docs.GetNthDocument(m);
                      doc.ReplaceItemValue("Kunden_Nummer", Sap.shiftLeft(E[n].KUNNR, "0"));
                      doc.ReplaceItemValue("txtkdnr", Sap.shiftLeft(E[n].KUNNR, "0"));
                      doc.ReplaceItemValue("Suchname", E[n].NAME1);
                      doc.ReplaceItemValue("txtkdkrzbez", E[n].NAME1);
                      doc.ReplaceItemValue("CompanyCity", E[n].BEZEI);
                      doc.ReplaceItemValue("State", E[n].VKBEZ);
                      doc.ReplaceItemValue("CompanyMainZIP", E[n].BRTXT);
                      doc.ReplaceItemValue("CompanyAddress", E[n].STRAS + " " + E[n].PSTLZ + " " + E[n].ORT01);
                      doc.ReplaceItemValue("SalesGroup", Sap.shiftLeft(E[n].PERNR_Y2, "0"));
                      doc.ReplaceItemValue("SalesGroupTemp", Sap.shiftLeft(E[n].PERNR_Y2, "0"));
                      doc.ReplaceItemValue("TSRVan", E[n].VORNA_Y2 + " " + E[n].NACHN_Y2);
                      doc.ReplaceItemValue("TSRName", E[n].VORNA_Y2 + " " + E[n].NACHN_Y2);
                      doc.Save(false, false, false);
                  }
              }
          }
      }
      #endregion
  }
}
