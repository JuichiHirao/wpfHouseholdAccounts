using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;

namespace wpfHouseholdAccounts
{
    class TravelData
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public string Detail { get; set; }
        public string Remark { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }

        public void SetNewCode(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT CODE FROM TRAVEL ORDER BY CODE DESC";

            myDbCon.openConnection();

            SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

            myDbCon.SetParameter(null);

            SqlDataReader reader = myDbCon.GetExecuteReader(SelectCommand);

            if (reader.IsClosed)
            {
                _logger.Debug("reader.IsClosed");
                return;
            }

            int code;
            Code = "70001";
            if (reader.Read())
            {
                code = Convert.ToInt32(DbExportCommon.GetDbString(reader, 0));
                code++;
                Code = Convert.ToString(code);
            }

            reader.Close();

            return;
        }

        public void DbExport(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string sqlcmd = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            sqlcmd = "INSERT INTO TRAVEL ( CODE, NAME, DEPARTURE_DATE, ARRIVAL_DATE, DETAIL, REMARK ) ";
            sqlcmd = sqlcmd + "VALUES( @CODE, @NAME, @DEPARTURE_DATE, @ARRIVAL_DATE, @DETAIL, @REMARK ) ";

            SqlCommand scmd = new SqlCommand(sqlcmd, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[6];

            sqlparams[0] = new SqlParameter("@CODE", SqlDbType.VarChar);
            sqlparams[0].Value = Code;
            sqlparams[1] = new SqlParameter("@NAME", SqlDbType.VarChar);
            sqlparams[1].Value = Name;
            sqlparams[2] = new SqlParameter("@DEPARTURE_DATE", SqlDbType.DateTime);
            sqlparams[2].Value = DepartureDate;
            sqlparams[3] = new SqlParameter("@ARRIVAL_DATE", SqlDbType.DateTime);
            sqlparams[3].Value = ArrivalDate;
            sqlparams[4] = new SqlParameter("@DETAIL", SqlDbType.VarChar);
            sqlparams[4].Value = Detail;
            sqlparams[5] = new SqlParameter("@REMARK", SqlDbType.VarChar);
            sqlparams[5].Value = Remark;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(sqlcmd);

            return;
        }

    }
}
