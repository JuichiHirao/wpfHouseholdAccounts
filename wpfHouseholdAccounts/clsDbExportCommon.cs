using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace wpfHouseholdAccounts
{
	class DbExportCommon
	{
		public static string GetDbString(SqlDataReader myReader, int myColumnNo)
		{
			string myData = "";
			try
			{
                if (myReader.IsDBNull(myColumnNo))
                    myData = "";
                else
				    myData = myReader.GetString(myColumnNo);
			}
			catch (Exception)
			{
				myData = "";
			}
			return myData;
		}
        public static bool GetDbBool(SqlDataReader myReader, int myColumnNo)
        {
            bool myData = false;
            try
            {
                myData = myReader.GetBoolean(myColumnNo);
            }
            catch (Exception)
            {
                myData = false;
            }
            return myData;
        }
        public static int GetDbInt(SqlDataReader myReader, int myColumnNo)
		{
			int myData = 0;
			try
			{
                if (myReader.IsDBNull(myColumnNo))
                    myData = 0;
                else
				    myData = myReader.GetInt32(myColumnNo);
			}
			catch (Exception)
			{
				myData = 0;
			}
			return myData;
		}
		public static long GetDbMoney(SqlDataReader myReader, int myColumnNo)
		{
			long myData = 0;
			try
			{
				Decimal myDecimalData = myReader.GetSqlMoney(myColumnNo).ToDecimal();
				string myStrData = Decimal.Round(myDecimalData,0).ToString();
				myData = long.Parse(myStrData);
			}
			catch (Exception)
			{
				myData = 0;
			}
			return myData;
		}
		public static DateTime GetDbDateTime(SqlDataReader myReader, int myColumnNo)
		{
			DateTime myData = new DateTime(1900, 1, 1);
			try
			{
                if (myReader.IsDBNull(myColumnNo))
                    return myData;

                myData = myReader.GetDateTime(myColumnNo);
            }
			catch (Exception)
			{
				myData = new DateTime(1900, 1, 1);
			}
			return myData;
		}
    }
}
