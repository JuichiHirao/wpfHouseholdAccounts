using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace wpfHouseholdAccounts
{
    public class PaymentData
    {
        public int Id { get; set; }
        public DateTime InputDate { get; set; }
        public string LoanCode { get; set; }       // 借入コード
        public string LoanName { get; set; }       // 借方名称
        public string DebitCode { get; set; }       // 借方（クレジットカード等の支払対象）
        public string DebitName { get; set; }       // 借方名称
        public string CreditCode { get; set; }      // 貸方（支払資産）
        public string CreditName { get; set; }      // 貸方名称
        public long Amount { get; set; }            // 金額
        public bool IsCapture { get; set; }             // MoneyInput画面で取込済？
        public string Remark { get; set; }
        public long PaymentAmount { get; set; }
        public string DisplayPaymentDate { get; set; }
        private DateTime _PaymentDate;
        public DateTime PaymentDate
        {
            get
            {
                return _PaymentDate;
            }
            set
            {
                _PaymentDate = value;

                if (_PaymentDate.Year == 1900)
                    DisplayPaymentDate = "";
                else
                    DisplayPaymentDate = _PaymentDate.ToString("yyyy/MM/dd");

            }
        }

        public bool DbExistCheck(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";
            SqlDataReader reader;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "SELECT 借方 FROM 金銭帳 ";
            mySqlCommand = mySqlCommand + "WHERE 年月日 = @年月日 ";
            mySqlCommand = mySqlCommand + "  AND 借方 = @借方コード AND 貸方 = @貸方コード AND 金額 = @金額 ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[4];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = PaymentDate;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value = DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = Amount;

            dbcon.SetParameter(sqlparams);

            reader = dbcon.GetExecuteReader(mySqlCommand);

            if (reader.Read())
            {
                reader.Close();
                return true;
            }
            reader.Close();

            return false;
        }

        public void DbDeletePaymentDecision(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "DELETE FROM 支払確定 ";
            mySqlCommand = mySqlCommand + "WHERE 確定ＩＤ = @確定ＩＤ ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@確定ＩＤ", SqlDbType.Int);
            sqlparams[0].Value = Id;

            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public void DbDeleteLoanDetail(DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "DELETE FROM 借入明細 ";
            mySqlCommand = mySqlCommand + "WHERE 明細ＩＤ = @明細ＩＤ ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@明細ＩＤ", SqlDbType.Int);
            sqlparams[0].Value = Id;

            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

    }
}
