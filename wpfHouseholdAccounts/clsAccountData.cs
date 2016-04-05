using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;

namespace wpfHouseholdAccounts
{
    public class AccountData
    {
        public string Code { get; set; }        // 科目コード
        public string Name { get; set; }        // 科目名
        public string Kana { get; set; }        // 科目名
        public string Kind { get; set; }        // 科目種別
        public string UpperCode { get; set; }   // 上位科目コード
        public bool DisableFlag { get; set; }   // 無効フラグ
        public bool CrucialFlag { get; set; }   // 摘要必須

        public AccountData()
        {
            Code = "";
            Name = "";
            Kana = "";
            Kind = "";
            UpperCode = "";
            DisableFlag = false;
            CrucialFlag = false;
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

            sqlcmd = "INSERT INTO 科目 ( 科目コード, 科目名, 科目種別, 上位科目コード, 無効フラグ, 摘要必須, ふりがな ) ";
            sqlcmd = sqlcmd + "VALUES( @科目コード, @科目名, @科目種別, @上位科目コード, @無効フラグ, @摘要必須, @ふりがな ) ";

            SqlCommand scmd = new SqlCommand(sqlcmd, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[7];

            sqlparams[0] = new SqlParameter("@科目コード", SqlDbType.VarChar);
            sqlparams[0].Value = Code;
            sqlparams[1] = new SqlParameter("@科目名", SqlDbType.VarChar);
            sqlparams[1].Value = Name;
            sqlparams[2] = new SqlParameter("@科目種別", SqlDbType.VarChar);
            sqlparams[2].Value = Kind;
            sqlparams[3] = new SqlParameter("@上位科目コード", SqlDbType.VarChar);
            sqlparams[3].Value = UpperCode;
            sqlparams[4] = new SqlParameter("@無効フラグ", SqlDbType.Bit);
            sqlparams[4].Value = DisableFlag;
            sqlparams[5] = new SqlParameter("@摘要必須", SqlDbType.Bit);
            sqlparams[5].Value = CrucialFlag;
            sqlparams[6] = new SqlParameter("@ふりがな", SqlDbType.VarChar);
            sqlparams[6].Value = Kana;

            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(sqlcmd);

            return;
        }
    }
}
