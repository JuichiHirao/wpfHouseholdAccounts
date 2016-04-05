using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;

namespace wpfHouseholdAccounts
{
    class Travel
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public List<TravelData> GetList(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT ID, CODE, NAME, DEPARTURE_DATE, ARRIVAL_DATE \n";
            SelectCommand = SelectCommand + "  DETAIL, REMARK, CREATE_DATE, UPDATE_DATE \n";
            SelectCommand = SelectCommand + "      FROM TRAVEL";

            myDbCon.openConnection();

            SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

            myDbCon.SetParameter(null);

            SqlDataReader reader = myDbCon.GetExecuteReader(SelectCommand);

            List<TravelData> listTravel = new List<TravelData>();

            if (reader.IsClosed)
            {
                _logger.Debug("reader.IsClosed");
                return null;
            }

            while (reader.Read())
            {
                TravelData data = new TravelData();

                data.Id = DbExportCommon.GetDbInt(reader, 0);
                data.Code = DbExportCommon.GetDbString(reader, 1);
                data.Name = DbExportCommon.GetDbString(reader, 2);
                data.DepartureDate = DbExportCommon.GetDbDateTime(reader, 3);
                data.ArrivalDate = DbExportCommon.GetDbDateTime(reader, 4);
                data.Detail = DbExportCommon.GetDbString(reader, 5);
                data.Remark = DbExportCommon.GetDbString(reader, 6);
                data.CreateDate = DbExportCommon.GetDbDateTime(reader, 7);
                data.UpdateDate = DbExportCommon.GetDbDateTime(reader, 8);

                listTravel.Add(data);
            }

            reader.Close();

            return listTravel;
        }

    }
}
