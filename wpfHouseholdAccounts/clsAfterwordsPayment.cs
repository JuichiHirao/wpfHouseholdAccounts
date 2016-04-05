using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace wpfHouseholdAccounts
{
    class AfterwordsPayment
    {
        public static List<AfterwordsPaymentData> GetData()
        {
            DbConnection dbcon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT 後日確認ＩＤ, 登録年月日, 借方, MST_A.科目名, 貸方, MST_B.科目名, 金額, 支払確定日, 摘要, 種別, 前回支払日, 順番, AREA, 確定日 ";
            SelectCommand = SelectCommand + "      FROM 後日確認払 ";
            SelectCommand = SelectCommand + "        LEFT OUTER JOIN ";
            SelectCommand = SelectCommand + "          科目 AS MST_A ON 借方 = MST_A.科目コード ";
            SelectCommand = SelectCommand + "        LEFT OUTER JOIN ";
            SelectCommand = SelectCommand + "          科目 AS MST_B ON 貸方 = MST_B.科目コード ";
            SelectCommand = SelectCommand + "  ORDER BY AREA, 前回支払日, 順番 ";

            dbcon.openConnection();

            SqlCommand cmd = new SqlCommand(SelectCommand, dbcon.getSqlConnection());

            //cmd.CommandType = CommandType.StoredProcedure;

            //cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            //cmd.Parameters["@from_date"].Value = myFromDate;

            SqlDataReader reader = cmd.ExecuteReader();
            List<AfterwordsPaymentData> listData = new List<AfterwordsPaymentData>();

            while (reader.Read())
            {
                AfterwordsPaymentData data = new AfterwordsPaymentData();

                data.Id = DbExportCommon.GetDbInt(reader, 0);
                data.RegistDate = DbExportCommon.GetDbDateTime(reader, 1);
                data.DebitCode = DbExportCommon.GetDbString(reader, 2);
                data.DebitName = DbExportCommon.GetDbString(reader, 3);
                data.CreditCode = DbExportCommon.GetDbString(reader, 4);
                data.CreditName = DbExportCommon.GetDbString(reader, 5);
                data.Amount = DbExportCommon.GetDbMoney(reader, 6);
                data.Remark = DbExportCommon.GetDbString(reader, 8);
                data.Kind = DbExportCommon.GetDbInt(reader, 9);
                data.LastTimePaymentDate = DbExportCommon.GetDbDateTime(reader, 10);
                data.OrderSameDate = DbExportCommon.GetDbInt(reader, 11);
                data.Area = DbExportCommon.GetDbInt(reader, 12);
                data.DecisionDate = DbExportCommon.GetDbDateTime(reader, 13);

                listData.Add(data);
            }
            reader.Close();

            return listData;
        }

    }
}
