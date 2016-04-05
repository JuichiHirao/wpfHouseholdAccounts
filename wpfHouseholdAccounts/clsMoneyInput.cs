using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using System.Diagnostics;
using NLog;

namespace wpfHouseholdAccounts
{
    class MoneyInput
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

		public const int CACL_KUBUN_DEBIT	= 1;	// 借方
		public const int CACL_KUBUN_CREDIT	= 2;	// 貸方

        public static void CheckData(List<MoneyInputData> myList, Account myAccount)
        {
            foreach (MoneyInputData data in myList)
            {
                if (data.DebitCode == null || data.CreditCode == null)
                    throw new BussinessException("科目コードが未入力な行が存在します");

                if (data.DebitCode.Length <= 0 && data.CreditCode.Length <= 0)
                    throw new BussinessException("科目コードが未入力な行が存在します");

                string Name = myAccount.getName(data.DebitCode);

                if (Name.Length <= 0)
                    throw new BussinessException("無効な科目コード[" + data.DebitCode + "]が入力されています");

                Name = myAccount.getName(data.CreditCode);

                if (Name.Length <= 0)
                    throw new BussinessException("無効な科目コード[" + data.CreditCode + "]が入力されています");

                AccountData accountData = myAccount.GetItemFromCode(data.DebitCode);

                if (accountData != null && accountData.CrucialFlag)
                {
                    if (data.Remark.Trim().Length <= 0)
                        throw new BussinessException("摘要必須の科目コード[" + data.DebitCode + "]に摘要が入力されていません");
                }

                accountData = myAccount.GetItemFromCode(data.CreditCode);

                if (accountData != null && accountData.CrucialFlag)
                {
                    if (data.Remark.Trim().Length <= 0)
                        throw new BussinessException("摘要必須の科目コード[" + data.CreditCode + "]に摘要が入力されていません");
                }

            }
            return;
        }

        public static long CalcAccountTotal(List<MoneyInputData> myList, int myKubun, string myAccountCode)
        {
            long totalAmount = 0;
            long amount = 0;

            foreach(MoneyInputData data in myList)
            {
                // 引数：借貸方、科目コードが一致している場合のみ金額を合計
                if (myKubun == MoneyInput.CACL_KUBUN_DEBIT
                        && myAccountCode == data.DebitCode)
                    amount = data.Amount;
                else if (myKubun == MoneyInput.CACL_KUBUN_CREDIT
                        && myAccountCode == data.CreditCode)
                    amount = data.Amount;
                else
                    amount = 0;

                // 条件分岐の設定金額を合計へ足し込む
                totalAmount = totalAmount + amount;
            }

            return totalAmount;
        }

        public static bool IsImported(PaymentData myPaymentData, List<MoneyInputData> myList)
        {
            foreach(MoneyInputData data in myList)
            {
                if (myPaymentData.DebitCode.Equals(data.DebitCode)
                    && myPaymentData.CreditCode.Equals(data.CreditCode)
                    && (myPaymentData.Amount-100 <= data.Amount && myPaymentData.Amount+100 >= data.Amount))
                {
                    return true;
                }
            }
            return false;
        }
        public static void ExportXml(string myFilename, List<MoneyInputData> myData)
        {
            XElement root = new XElement("MoneyInputData");

            foreach (MoneyInputData data in myData)
            {
                if (data == null)
                    continue;

                if (data.Date == null
                    || data.DebitCode == null
                    || data.CreditCode == null)
                    continue;

                if (data.Date.Year <= 1900
                    && data.DebitCode.Length <= 0
                    && data.CreditCode.Length <= 0)
                    continue;

                root.Add(new XElement("MoneyInput"
                                    , new XElement("年月日", data.Date)
                                    , new XElement("借方コード", data.DebitCode)
                                    , new XElement("借方名", data.DebitName)
                                    , new XElement("貸方コード", data.CreditCode)
                                    , new XElement("貸方名", data.CreditName)
                                    , new XElement("金額", data.Amount)
                                    , new XElement("摘要", data.Remark)
                            ));
            }
            root.Save(myFilename);
        }
        public static List<MoneyInputData> ImportXml(string myFilename)
        {
            XElement root = XElement.Load(myFilename);

            var listAll = from element in root.Elements("MoneyInput")
                          select element;

            List<MoneyInputData> listInputData = new List<MoneyInputData>();

            foreach (XContainer xcon in listAll)
            {
                MoneyInputData inputdata = new MoneyInputData();

                try
                {
                    inputdata.Date = Convert.ToDateTime(xcon.Element("年月日").Value);
                    inputdata.DebitCode = xcon.Element("借方コード").Value;
                    inputdata.DebitName = xcon.Element("借方名").Value;
                    inputdata.CreditCode = xcon.Element("貸方コード").Value;
                    inputdata.CreditName = xcon.Element("貸方名").Value;
                    inputdata.Amount = Convert.ToInt64(xcon.Element("金額").Value);
                    inputdata.Remark = xcon.Element("摘要").Value;
                }
                catch (NullReferenceException nullex)
                {
                    Debug.Write(nullex);
                    // XML内にElementが存在しない場合に発生、無視する
                }

                listInputData.Add(inputdata);
            }

            return listInputData;
        }

        public static List<MoneySzeInputData> GetSzeSaveData()
        {
            if (!System.IO.File.Exists("SZEINPUT.xml"))
                return new List<MoneySzeInputData>();

            XElement root = XElement.Load("SZEINPUT.xml");

            var listAll = from element in root.Elements("MoneySzeInput")
                          select element;

            List<MoneySzeInputData> listSzeInputData = new List<MoneySzeInputData>();

            foreach (XContainer xcon in listAll)
            {
                MoneySzeInputData inputdata = new MoneySzeInputData();

                try
                {
                    inputdata.Date = Convert.ToDateTime(xcon.Element("年月日").Value);
                    inputdata.DebitCode = xcon.Element("借方コード").Value;
                    inputdata.DebitName = xcon.Element("借方名").Value;
                    inputdata.CreditCode = xcon.Element("貸方コード").Value;
                    inputdata.CreditName = xcon.Element("貸方名").Value;
                    inputdata.Amount = Convert.ToInt64(xcon.Element("金額").Value);
                    inputdata.Remark = xcon.Element("摘要").Value;
                }
                catch (NullReferenceException nullex)
                {
                    Debug.Write(nullex);
                    // XML内にElementが存在しない場合に発生、無視する
                }

                listSzeInputData.Add(inputdata);
            }

            return listSzeInputData;
        }

        public static void InsertDbMakeupDetailData(MakeupDetailData myDetailData, string myTableName, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "INSERT INTO " + myTableName;
            mySqlCommand = mySqlCommand + "( 年月日, 借方, 貸方, 金額, 摘要 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @年月日, @借方コード, @貸方コード, @金額, @摘要 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[5];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = myDetailData.Date;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value = myDetailData.DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = myDetailData.CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = myDetailData.Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = myDetailData.Remark;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public static void InsertDbData(MoneyInputData myData, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "INSERT INTO 金銭帳 ";
            mySqlCommand = mySqlCommand + "( 年月日, 借方, 貸方, 金額, 摘要 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @年月日, @借方コード, @貸方コード, @金額, @摘要 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[5];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = myData.Date;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value = myData.DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = myData.CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = myData.Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = myData.Remark;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public static void InsertDbSzeData(MoneySzeInputData mySzeData, DbConnection myDbCon)
        {
			DbConnection dbcon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
			else
                dbcon = new DbConnection();

			mySqlCommand = "INSERT INTO 金銭帳SZE ";
			mySqlCommand = mySqlCommand + "( 年月日, 借方, 貸方, 金額, 摘要 ) ";
			mySqlCommand = mySqlCommand + "VALUES( @年月日, @借方コード, @貸方コード, @金額, @摘要 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[5];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = mySzeData.Date;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value =  mySzeData.DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = mySzeData.CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value =  mySzeData.Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = mySzeData.Remark;
            dbcon.SetParameter(sqlparams);

			myDbCon.execSqlCommand(mySqlCommand);

			return;
        }
        public static void UpdateDb(MoneyInputData myInputData, string myTableName, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "UPDATE " + myTableName + " ";
            mySqlCommand = mySqlCommand + "  SET ";
            mySqlCommand = mySqlCommand + "      年月日 = @年月日 ";
            mySqlCommand = mySqlCommand + "      , 借方 = @借方コード ";
            mySqlCommand = mySqlCommand + "      , 貸方 = @貸方コード ";
            mySqlCommand = mySqlCommand + "      , 金額 = @金額 ";
            mySqlCommand = mySqlCommand + "      , 摘要 = @摘要 ";
            mySqlCommand = mySqlCommand + "  WHERE 金銭帳ＩＤ = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[6];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = myInputData.Date;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value = myInputData.DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = myInputData.CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = myInputData.Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = myInputData.Remark;
            sqlparams[5] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[5].Value = myInputData.id;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }
        public static void UpdateDbUsedCompanyArrear(int myId, int myFlag, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "UPDATE 金銭帳 ";
            mySqlCommand = mySqlCommand + "  SET ";
            mySqlCommand = mySqlCommand + "      USED_COMPANY_ARREAR = @USED_COMPANY_ARREAR ";
            mySqlCommand = mySqlCommand + "  WHERE 金銭帳ＩＤ = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[2];

            sqlparams[0] = new SqlParameter("@USED_COMPANY_ARREAR", SqlDbType.Int);
            sqlparams[0].Value = myFlag;
            sqlparams[1] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[1].Value = myId;

            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public static void UpdateDbMakeupDetailData(MakeupDetailData myDetailData, string myTableName, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "UPDATE " + myTableName + " ";
            mySqlCommand = mySqlCommand + "  SET ";
            mySqlCommand = mySqlCommand + "      年月日 = @年月日 ";
            mySqlCommand = mySqlCommand + "      , 借方 = @借方コード ";
            mySqlCommand = mySqlCommand + "      , 貸方 = @貸方コード ";
            mySqlCommand = mySqlCommand + "      , 金額 = @金額 ";
            mySqlCommand = mySqlCommand + "      , 摘要 = @摘要 ";
            mySqlCommand = mySqlCommand + "  WHERE 金銭帳ＩＤ = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[6];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = myDetailData.Date;
            sqlparams[1] = new SqlParameter("@借方コード", SqlDbType.VarChar);
            sqlparams[1].Value = myDetailData.DebitCode;
            sqlparams[2] = new SqlParameter("@貸方コード", SqlDbType.VarChar);
            sqlparams[2].Value = myDetailData.CreditCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = myDetailData.Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = myDetailData.Remark;
            sqlparams[5] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[5].Value = myDetailData.Id;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }
        public static void DeleteDbMakeupDetailData(MakeupDetailData myDetailData, string myTableName, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "DELETE FROM " + myTableName + " ";
            mySqlCommand = mySqlCommand + "  WHERE 金銭帳ＩＤ = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[0].Value = myDetailData.Id;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public static void DbDeleteFromDate(DateTime myDate, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "DELETE FROM 金銭帳 ";
            mySqlCommand = mySqlCommand + "  WHERE 年月日 = @年月日 ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = myDate;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public static List<MakeupDetailData> GetDataFromDate(DateTime myDate, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            int paramCnt = 0;
            mySqlCommand = "SELECT 金銭帳ＩＤ, 年月日, 借方, 貸方, 金額, 摘要, USED_COMPANY_ARREAR, CREATE_DATE FROM 金銭帳 ";
            mySqlCommand = mySqlCommand + "WHERE 年月日 = @年月日 ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[paramCnt] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[paramCnt].Value = myDate;
            dbcon.SetParameter(sqlparams);
            paramCnt++;

            myDbCon.openConnection();

            SqlCommand cmd = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlDataReader reader = myDbCon.GetExecuteReader(mySqlCommand);

            if (reader.IsClosed)
            {
                _logger.Debug("reader.IsClosed");
                throw new Exception("金銭帳の取得でreaderがクローズされています");
            }

            List<MakeupDetailData> listData = new List<MakeupDetailData>();
            if (reader.Read())
            {
                MakeupDetailData data = new MakeupDetailData();

                data.Id = DbExportCommon.GetDbInt(reader, 0);
                data.Date = DbExportCommon.GetDbDateTime(reader, 1);
                data.DebitCode = DbExportCommon.GetDbString(reader, 2);
                data.CreditCode = DbExportCommon.GetDbString(reader, 3);
                data.Amount = DbExportCommon.GetDbMoney(reader, 4);
                data.Remark = DbExportCommon.GetDbString(reader, 5);
                data.UsedCompanyArrear = DbExportCommon.GetDbInt(reader, 6);
                data.RegistDate = DbExportCommon.GetDbDateTime(reader, 7);

                listData.Add(data);
                //_logger.Debug("id [" + id + "]  残高 [" + balance + "]");
            }
            else
            {
                _logger.Debug("金銭帳に一致データ無し");
                return null;
            }

            reader.Close();

            return listData;
        }

        public static MakeupDetailData GetData(MakeupDetailData myDetailData, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            int paramCnt = 0;
            mySqlCommand = "SELECT 金銭帳ＩＤ, 年月日, 借方, 貸方, 金額, 摘要, USED_COMPANY_ARREAR, CREATE_DATE " 
                + "FROM 金銭帳 ";
            mySqlCommand = mySqlCommand + "WHERE ";

            if (myDetailData.Date.Year >= 2000)
            {
                if (paramCnt > 0)
                    mySqlCommand = mySqlCommand + " AND ";
                mySqlCommand = mySqlCommand + "  年月日 = @年月日 ";
                paramCnt++;
            }

            if (myDetailData.DebitCode.Length > 0)
            {
                if (paramCnt > 0)
                    mySqlCommand = mySqlCommand + " AND ";
                mySqlCommand = mySqlCommand + "  借方 = @借方 ";
                paramCnt++;
            }

            if (myDetailData.CreditCode.Length > 0)
            {
                if (paramCnt > 0)
                    mySqlCommand = mySqlCommand + " AND ";
                mySqlCommand = mySqlCommand + "  貸方 = @貸方 ";
                paramCnt++;
            }

            if (myDetailData.CreditCode.Length > 0)
            {
                if (paramCnt > 0)
                    mySqlCommand = mySqlCommand + " AND ";

                mySqlCommand = mySqlCommand + "  金額 = @金額 ";
                paramCnt++;
            }

            if (myDetailData.Remark.Length > 0)
            {
                if (paramCnt > 0)
                    mySqlCommand = mySqlCommand + " AND ";

                mySqlCommand = mySqlCommand + "  摘要 = @摘要 ";
                paramCnt++;
            }

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());
            DataTable dtSaraly = new DataTable();

            SqlParameter[] sqlparams = new SqlParameter[paramCnt];

            paramCnt = 0;
            if (mySqlCommand.IndexOf("@年月日") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@年月日", SqlDbType.DateTime);
                sqlparams[paramCnt].Value = myDetailData.Date;
                dbcon.SetParameter(sqlparams);
                paramCnt++;
            }

            if (mySqlCommand.IndexOf("@借方") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@借方", SqlDbType.VarChar);
                sqlparams[paramCnt].Value = myDetailData.DebitCode;
                dbcon.SetParameter(sqlparams);
                paramCnt++;
            }

            if (mySqlCommand.IndexOf("@貸方") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@貸方", SqlDbType.VarChar);
                sqlparams[paramCnt].Value = myDetailData.CreditCode;
                dbcon.SetParameter(sqlparams);
                paramCnt++;
            }

            if (mySqlCommand.IndexOf("@金額") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@金額", SqlDbType.Money);
                sqlparams[paramCnt].Value = myDetailData.Amount;
                dbcon.SetParameter(sqlparams);
                paramCnt++;
            }

            if (mySqlCommand.IndexOf("@摘要") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@摘要", SqlDbType.VarChar);
                sqlparams[paramCnt].Value = myDetailData.Remark;
                dbcon.SetParameter(sqlparams);
                paramCnt++;
            }

            myDbCon.openConnection();

            SqlCommand cmd = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlDataReader reader = myDbCon.GetExecuteReader(mySqlCommand);

            if (reader.IsClosed)
            {
                _logger.Debug("reader.IsClosed");
                throw new Exception("金銭帳の取得でreaderがクローズされています");
            }

            MakeupDetailData data = null;
            if (reader.Read())
            {
                data = new MakeupDetailData();

                data.Id = DbExportCommon.GetDbInt(reader, 0);
                data.Date = DbExportCommon.GetDbDateTime(reader, 1);
                data.DebitCode = DbExportCommon.GetDbString(reader, 2);
                data.CreditCode = DbExportCommon.GetDbString(reader, 3);
                data.Amount = DbExportCommon.GetDbMoney(reader, 4);
                data.Remark = DbExportCommon.GetDbString(reader, 5);
                data.UsedCompanyArrear = DbExportCommon.GetDbInt(reader, 6);
                data.RegistDate = DbExportCommon.GetDbDateTime(reader, 7);

                //_logger.Debug("id [" + id + "]  残高 [" + balance + "]");
            }
            else
            {
                _logger.Debug("金銭帳に一致データ無し");
                return null;
            }

            reader.Close();

            return data;
        }

        public static List<MakeupDetailData> GetInputDetail(string myTargetAccountCode, string myTargetUpperCode
                                                                , DateTime myFromDate, DateTime myToDate
                                                                , bool myFilterSze, bool myFilterApper, bool myFilterCard)
        {
            DbConnection dbcon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT 明細種別, 金銭帳ＩＤ AS ID, 年月日 \n";
            SelectCommand = SelectCommand + "        , 借方, 借方名 AS 借方科目名 \n";
            SelectCommand = SelectCommand + "        , 貸方, 貸方名 AS 貸方科目名 \n";
            SelectCommand = SelectCommand + "        , 金額, USED_COMPANY_ARREAR, 摘要 \n";
            SelectCommand = SelectCommand + "      FROM V_INPUTDETAIL \n";
            SelectCommand = SelectCommand + "      WHERE \n";
            SelectCommand = SelectCommand + "        ( ( ( 年月日 >= @from_date) AND (年月日 <= @to_date) AND 明細種別 in (1,3) ) \n";
            SelectCommand = SelectCommand + "          OR ( ( 登録日 >= @from_date) AND (登録日 <= @to_date) AND 明細種別 = 2) ) \n";

            if (!myTargetUpperCode.Equals("総合計")
                && !myTargetUpperCode.Equals("費用合計")
                && !myTargetUpperCode.Equals("収益合計"))
            {
                if (myTargetUpperCode.Length > 0)
                {
                    SelectCommand = SelectCommand + "              AND ( ( 借方上位 = @account_code OR 借方 = @account_code ) \n";
                    SelectCommand = SelectCommand + "                OR ( 貸方上位 = @account_code OR 貸方 = @account_code ) ) \n";
                }
                else
                {
                    SelectCommand = SelectCommand + "              AND ( 借方 = @account_code \n";
                    SelectCommand = SelectCommand + "                OR 貸方 = @account_code ) \n";
                }

                // カードと金銭帳で同じ行を表示しないための条件はビューへ追加したので不要
                //SelectCommand = SelectCommand + "              AND ( (借方種別 = '40' AND 貸方種別 not in ('21') AND 登録日 IS NULL ) OR 登録日 IS NOT NULL ) \n";

                // 未払いのみ表示の場合
                if (myFilterApper == true)
                    SelectCommand = SelectCommand + "              AND 貸方種別 = '22' \n";
            }
            else
            {
                if (myTargetUpperCode.Equals("費用合計"))
                    SelectCommand = SelectCommand + "              AND 借方種別 = '40' \n";
                else if (myTargetUpperCode.Equals("収益合計"))
                    SelectCommand = SelectCommand + "              AND 貸方種別 = '30' \n";
            }

            if (myFilterSze == true)
                SelectCommand = SelectCommand + "      AND 明細種別 = 3 \n"; // 金銭帳SZEのデータ
            else if (myFilterCard == true)
                SelectCommand = SelectCommand + "      AND 明細種別 = 2 \n"; // 借入明細履歴のデータ
            else if (myFilterApper == true)
                SelectCommand = SelectCommand + "      AND 明細種別 = 1 \n"; // 金銭帳のデータ
            else
                SelectCommand = SelectCommand + "      AND 明細種別 in (1, 2) \n";

            //Debug.Print(SelectCommand);
            dbcon.openConnection();

            SqlCommand cmd = new SqlCommand(SelectCommand, dbcon.getSqlConnection());

            //cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            cmd.Parameters["@from_date"].Value = myFromDate;

            cmd.Parameters.Add(new SqlParameter("@to_date", SqlDbType.DateTime));
            cmd.Parameters["@to_date"].Value = myToDate;

            if (myTargetUpperCode.Length > 0)
            {
                cmd.Parameters.Add(new SqlParameter("@account_code", SqlDbType.VarChar));
                cmd.Parameters["@account_code"].Value = myTargetUpperCode;
            }
            else
            {
                cmd.Parameters.Add(new SqlParameter("@account_code", SqlDbType.VarChar));
                cmd.Parameters["@account_code"].Value = myTargetAccountCode;
            }

            SqlDataReader reader = cmd.ExecuteReader();
            List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

            while (reader.Read())
            {
                MakeupDetailData data = new MakeupDetailData();

                data.Kind = DbExportCommon.GetDbInt(reader, 0);
                data.Id = DbExportCommon.GetDbInt(reader, 1);
                data.Date = DbExportCommon.GetDbDateTime(reader, 2);
                data.DebitCode = DbExportCommon.GetDbString(reader, 3);
                data.DebitName = DbExportCommon.GetDbString(reader, 4);
                data.CreditCode = DbExportCommon.GetDbString(reader, 5);
                data.CreditName = DbExportCommon.GetDbString(reader, 6);
                data.Amount = DbExportCommon.GetDbMoney(reader, 7);
                data.UsedCompanyArrear = DbExportCommon.GetDbInt(reader, 8);
                data.Remark = DbExportCommon.GetDbString(reader, 9);

                listDetail.Add(data);
            }

            reader.Close();

            return listDetail;
        }

        public static List<MakeupDetailData> GetInputDetailAll(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT 明細種別, 金銭帳ＩＤ AS ID, 年月日 \n";
            SelectCommand = SelectCommand + "        , 借方, 借方上位, 借方名 AS 借方科目名 \n";
            SelectCommand = SelectCommand + "        , 貸方, 貸方上位, 貸方名 AS 貸方科目名 \n";
            SelectCommand = SelectCommand + "        , 金額, USED_COMPANY_ARREAR, 摘要, 登録日, DATA_ORDER, BALANCE \n";
            SelectCommand = SelectCommand + "      FROM V_INPUTDETAIL \n";

            myDbCon.openConnection();

            SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

            myDbCon.SetParameter(null);

            SqlDataReader reader = myDbCon.GetExecuteReader(SelectCommand);

            List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

            if (reader.IsClosed)
            {
                _logger.Debug("reader.IsClosed");
                return null;
            }

            while (reader.Read())
            {
                MakeupDetailData data = new MakeupDetailData();

                data.Kind = DbExportCommon.GetDbInt(reader, 0);
                data.Id = DbExportCommon.GetDbInt(reader, 1);
                data.Date = DbExportCommon.GetDbDateTime(reader, 2);
                data.DebitCode = DbExportCommon.GetDbString(reader, 3);
                data.DebitUpperCode = DbExportCommon.GetDbString(reader, 4);
                data.DebitName = DbExportCommon.GetDbString(reader, 5);
                data.CreditCode = DbExportCommon.GetDbString(reader, 6);
                data.CreditUpperCode = DbExportCommon.GetDbString(reader, 7);
                data.CreditName = DbExportCommon.GetDbString(reader, 8);
                data.Amount = DbExportCommon.GetDbMoney(reader, 9);
                data.UsedCompanyArrear = DbExportCommon.GetDbInt(reader, 10);
                data.Remark = DbExportCommon.GetDbString(reader, 11);
                data.RegistDate = DbExportCommon.GetDbDateTime(reader, 12);
                data.DataOrder = DbExportCommon.GetDbInt(reader, 13);
                data.Balance = DbExportCommon.GetDbMoney(reader, 14);

                listDetail.Add(data);
            }

            reader.Close();

            return listDetail;
        }

        public static List<MakeupDetailData> GetInputNowCardDetail(DateTime myFromDate)
        {
            DbConnection dbcon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT 1 AS 明細種別, DETAIL.明細ＩＤ, 年月日 ";
            SelectCommand = SelectCommand + "        , 借方, MST_A.科目名 AS 借方科目名 ";
            SelectCommand = SelectCommand + "        , DETAIL.貸方, MST_B.科目名 AS 貸方科目名 ";
            SelectCommand = SelectCommand + "        , DETAIL.金額, DETAIL.摘要 ";
            SelectCommand = SelectCommand + "      FROM ";
            SelectCommand = SelectCommand + "        ( SELECT 借入明細.明細ＩＤ, 年月日 ";
            SelectCommand = SelectCommand + "              , 取引科目コード AS 借方, 借入取引コード AS 貸方 ";
            SelectCommand = SelectCommand + "              , 金額, 摘要 ";
            SelectCommand = SelectCommand + "            FROM 借入明細 ";
            SelectCommand = SelectCommand + "              RIGHT OUTER JOIN 支払予定 ";
            SelectCommand = SelectCommand + "                  ON 借入明細.明細ＩＤ =  支払予定.明細ＩＤ ";
            SelectCommand = SelectCommand + "            WHERE 支払予定.支払日 >= @from_date ";
            SelectCommand = SelectCommand + "        ) AS DETAIL ";
            SelectCommand = SelectCommand + "        LEFT OUTER JOIN ";
            SelectCommand = SelectCommand + "          科目 AS MST_A ON DETAIL.借方 = MST_A.科目コード ";
            SelectCommand = SelectCommand + "        LEFT OUTER JOIN ";
            SelectCommand = SelectCommand + "          科目 AS MST_B ON DETAIL.貸方 = MST_B.科目コード ";

            dbcon.openConnection();

            SqlCommand cmd = new SqlCommand(SelectCommand, dbcon.getSqlConnection());

            //cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@from_date", SqlDbType.DateTime));
            cmd.Parameters["@from_date"].Value = myFromDate;

            SqlDataReader reader = cmd.ExecuteReader();
            List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

            while (reader.Read())
            {
                MakeupDetailData data = new MakeupDetailData();

                data.Kind = DbExportCommon.GetDbInt(reader, 0);
                data.Id = DbExportCommon.GetDbInt(reader, 1);
                data.Date = DbExportCommon.GetDbDateTime(reader, 2);
                data.DebitCode = DbExportCommon.GetDbString(reader, 3);
                data.DebitName = DbExportCommon.GetDbString(reader, 4);
                data.CreditCode = DbExportCommon.GetDbString(reader, 5);
                data.CreditName = DbExportCommon.GetDbString(reader, 6);
                data.Amount = DbExportCommon.GetDbMoney(reader, 7);
                data.Remark = DbExportCommon.GetDbString(reader, 8);

                listDetail.Add(data);
            }

            reader.Close();

            return listDetail;
        }

    }
}
