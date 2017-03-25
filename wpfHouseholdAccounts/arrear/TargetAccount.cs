using NLog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpfHouseholdAccounts.arrear
{
    class TargetAccountData
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public long InputAmount { get; set; }

        public long AdjustAmount { get; set; }
    }

    class TargetAccount
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        DbConnection dbcon = new DbConnection();

        public TargetAccount(DbConnection myDbCon)
        {
            dbcon = myDbCon;
        }

        public List<TargetAccountData> GetList()
        {
            dbcon.openConnection();

            string sql = "SELECT 未払.未払コード, 未払名, SUM(DETAIL1.金額) AS INPUT_AMOUNT, SUM(DETAIL2.金額) AS ADJUST_AMOUNT "
                            + "FROM 未払 "
                            + "    LEFT JOIN (SELECT * FROM 未払明細 WHERE 支払予定日 IS NULL) AS DETAIL1 "
                            + "      ON 未払.未払コード = DETAIL1.未払コード "
                            + "    LEFT JOIN (SELECT * FROM 未払明細 WHERE 支払予定日 IS NOT NULL) AS DETAIL2 "
                            + "      ON 未払.未払コード = DETAIL2.未払コード "
                            + "  GROUP BY 未払.未払コード, 未払.未払名 ";

            SqlDataReader reader = dbcon.GetExecuteReader(sql);

            List<TargetAccountData> listData = new List<TargetAccountData>();

            if (reader.IsClosed)
            {
                throw new Exception("arrear.TargetAccountDataの取得でreaderがクローズされています");
            }

            while (reader.Read())
            {
                TargetAccountData data = new TargetAccountData();

                data.Code = DbExportCommon.GetDbString(reader, 0);
                data.Name = DbExportCommon.GetDbString(reader, 1);
                data.InputAmount = DbExportCommon.GetDbMoney(reader, 2);
                data.AdjustAmount = DbExportCommon.GetDbMoney(reader, 3);

                listData.Add(data);

                _logger.Debug("id [" + data.Code + "]  入力 [" + data.InputAmount + "]");
            }

            reader.Close();

            return listData;
        }
    }
}
