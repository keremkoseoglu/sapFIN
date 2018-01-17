using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
namespace sapFIN
{
   public class Config
    {
       private DataTable dt;
       public Config()
       {
           DataSet ds = new DataSet();
           ds.ReadXml("config.xml");
           dt = ds.Tables[0];
       }
       public String lotusPassword
       {
           get
           {
               return (String) dt.Rows[0]["lotusPassword"] ;
           }
       }
       public String sapUsername
       {
           get
           {
               return (String) dt.Rows[0]["sapUsername"];
           }
       }
       public String sapPassword
       {
           get
           {
               return (String)dt.Rows[0]["sapPassword"];
           }
       }
       
    }
}
