using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace wpfHouseholdAccounts
{
    public class Account
    {
        List<AccountData> listAccount;

        // 科目マスタ：科目種別
		public const string	KIND_ASSETS			= "10";		// 資産
		public const string	KIND_ASSETS_CASH	= "11";		// 資産：現金
		public const string	KIND_ASSETS_DEPOSIT	= "12";		// 資産：預金
		public const string	KIND_ASSETS_ADVANCE	= "13";		// 資産：立替・貸付金
		public const string	KIND_ASSETS_SAVINGS	= "14";		// 資産：各種積立預金
		public const string	KIND_ASSETS_BUDGET	= "15";		// 資産：予算
        public const string	KIND_ASSETS_COMPANY_ARREAR	= "1201";	// 資産：会社未払
		public const string	KIND_DEPT			= "20";		// 負債
		public const string	KIND_DEPT_LOAN		= "21";		// 負債：借入
		public const string	KIND_DEPT_APPEAR	= "22";		// 負債：未払
        public const string KIND_PAYMENT_ARREAR = "2210";   // 負債：支払対象の未払
        public const string KIND_NO_PAYMENT_ARRER = "2211"; // 負債：支払対象外の未払
		public const string	KIND_PROFIT			= "30";		// 収益
        public const string KIND_COMPANY_CASH = "1010"; // 会社現金
        public const string KIND_COMPANY_EXPENSE        = "31";		// 会社経費：現金
        public const string KIND_COMPANY_EXPENSE_BANK   = "32";		// 会社経費：預金
        public const string KIND_EXPENSE_GOUDOU        = "34";		// 合同会社経費：現金
        public const string KIND_EXPENSE_BANK_GOUDOU   = "35";		// 合同会社経費：預金
        public const string KIND_EXPENSE = "40";		// 費用Expense
		public const string	KIND_EXPENSE_FLOATING	= "40";		// 費用：生活流動
		public const string	KIND_EXPENSE_FIXED  	= "41";		// 費用：生活固定
		public const string	KIND_EXPENSE_CHILD  	= "42";		// 費用：子供
		public const string	KIND_EXPENSE_LARGE  	= "43";		// 費用：大きい出費
        public const string KIND_EXPENSE_CULTURE    = "44";		// 費用：教養・旅行
        public const string KIND_EXPENSE_BUSINESS   = "50";		// 費用：仕事・IT関係
        public const string KIND_EXPENSE_INTERESTED = "60";		// 費用：趣味
        public const string KIND_TRAVEL             = "70";		// 旅行（まとめ用の特別項目）

        		// 科目マスタ：各コード
		public const string	CODE_CASH			= "10100";	// 現金
        public const string CODE_CASHEXPENSE_KABUSHIKI = "10101";   // 会社経費現金（会社経費支払分）
        public const string CODE_CASHEXPENSE_GOUDOU = "10102";      // 会社経費現金（会社経費支払分）
        public const string CODE_BANK           = "10200";	// 預金
		public const string CODE_THETAINC_BANK  = "10209";  // シータ（株）の預金口座
        public const string CODE_THETALCC_BANK  = "10214";	// シータ合同会社の預金口座
        public const string	CODE_ADVANCE		= "10400";	// 立替金（支払確定）
		public const string	CODE_AFTERWORDS		= "10200";	// 後日支払：銀行口座（支払確定）
		public const string	CODE_LOAN			= "20000";	// 借入金
		public const string	CODE_LOAN_THETAINC	= "21002";	// シータ株式会社 借入金
        public const string	CODE_BADDEPTS		= "49902";	// 貸倒時の科目コード
		public const string CODE_SALARY			= "30103";	// 給料

        public Account()
        {
            Debug.Print("Account 生成");
            DbConnection dbcon = new DbConnection();
            listAccount = new List<AccountData>();

            SqlDataAdapter sqlada = new SqlDataAdapter();

            string SelectCommand = "";

            SelectCommand = "SELECT 科目コード, 科目名, ふりがな, 科目種別, 上位科目コード, ISNULL(無効フラグ,0) AS 無効フラグ, ISNULL(摘要必須,0) AS 摘要必須 ";
            SelectCommand = SelectCommand + "  FROM 科目 ";
            SelectCommand = SelectCommand + "  ORDER BY 科目コード";

            try
            {
                dbcon.openConnection();

                SqlCommand cmd = new SqlCommand(SelectCommand, dbcon.getSqlConnection());

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    AccountData data = new AccountData();

                    data.Code = DbExportCommon.GetDbString(reader, 0);
                    data.Name = DbExportCommon.GetDbString(reader, 1);
                    data.Kana = DbExportCommon.GetDbString(reader, 2);
                    data.Kind = DbExportCommon.GetDbString(reader, 3);
                    data.UpperCode = DbExportCommon.GetDbString(reader, 4);
                    data.DisableFlag = DbExportCommon.GetDbBool(reader, 5);
                    data.CrucialFlag = DbExportCommon.GetDbBool(reader, 6);

                    listAccount.Add(data);
                }

                reader.Close();

                dbcon.closeConnection();

            }
            catch (SqlException errsql)
            {
                Debug.Write(errsql);
            }
            catch (Exception err)
            {
                Debug.Write(err);
            }

            return;

        }

        public AccountData GetItemFromCode(string myCode)
        {
            foreach(AccountData data in listAccount)
            {
                if (data.Code.Equals(myCode))
                    return data;
            }
            return null;
        }

        public List<AccountData> GetItems()
        {
            AccountData[] data = null;

            data = new AccountData[listAccount.Count()];
            listAccount.CopyTo(data);

            return data.ToList<AccountData>();
        }
        public AccountData[] GetUpperData()
        {
            //AccountData[] data = null;

            List<AccountData> listAccount = new List<AccountData>();

            AccountData data = new AccountData();
            data.Code = "";

            return null;
        }
        public AccountData[] GetTwoDigitItems()
        {
            AccountData[] data = null;
            IEnumerable<AccountData> finddata = from account in listAccount
                                                where account.UpperCode == "" && account.Kind == "0"
                                                select account;

            data = new AccountData[finddata.Count()];
            finddata.ToList<AccountData>().CopyTo(data);
            return data;
        }
        public AccountData[] GetItemsUpperExpenseOnly()
        {
            AccountData[] data = null;
            IEnumerable<AccountData> finddata = from account in listAccount
                                                where account.UpperCode == "" && account.Kind == "40"
                                                select account;

            data = new AccountData[finddata.Count()];
            finddata.ToList<AccountData>().CopyTo(data);
            return data;
        }
        public string getAccountKind(string myCode)
        {
            IEnumerable<AccountData> finddata = from account in listAccount
                                   where account.Code == myCode
                                   select account;

            foreach (AccountData data in finddata)
                return data.Kind;

            return "";
        }

        public string getName(string myCode)
        {
            IEnumerable<AccountData> finddata = from account in listAccount
                                                where account.Code == myCode
                                                select account;

            foreach (AccountData data in finddata)
                return data.Name;

            return "";
        }

        public bool getDisableFlag(string myCode)
        {
            IEnumerable<AccountData> finddata = from account in listAccount
                                                where account.Code == myCode
                                                select account;

            foreach (AccountData data in finddata)
                return data.DisableFlag;

            return false;
        }

        public bool IsKindAssets(string myKind)
        {
            int kind = Convert.ToInt32(myKind);
            if (kind >= 10 && kind <= 19)
                return true;

            return false;
        }
        public bool IsKindProfit(string myKind)
        {
            int kind = Convert.ToInt32(myKind);
            if (kind >= 30 && kind <= 39)
                return true;

            return false;
        }
    }
}
