using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;

namespace wpfHouseholdAccounts
{
    class Arrear
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public int Id { get; set; }
        public int DataOrder { get; set; }
        public int JournalId { get; set; }
        public DateTime JournalDate { get; set; }
        public string DebitCode { get; set; }
        public string CreditCode { get; set; }
        public long Amount { get; set; }
        public string Remark { get; set; }
        public long Balance { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }

        /// <summary>
        /// 互換のためにメソッド名で作成のみ
        /// </summary>
        /// <param name="myInData"></param>
        /// <param name="argDbCon"></param>
        public void Adjustment(MoneyInputData myInData, DbConnection argDbCon)
        {
            // 未払明細から未払明細履歴へデータを移す
            DatabaseSwitchDetail2History(myInData.DebitCode, myInData.CreditCode, myInData.Date, argDbCon);

            // 未払明細から削除
            DatabaseDetailDelete(myInData.DebitCode, myInData.Date, argDbCon);
        }

        public void DatabaseSwitchDetail2History(string myArrearCode, string myPaymentCode, DateTime myAdjustmentDate, DbConnection argDbCon)
        {
            DbConnection myDbCon;
            string SqlExecCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            SqlExecCommand = "INSERT INTO 未払明細履歴 ";
            SqlExecCommand = SqlExecCommand + "     ( 年月日, 未払コード, 借方コード, 支払コード, 金額, 摘要, 支払日 ) ";
            SqlExecCommand = SqlExecCommand + "    SELECT 年月日,未払コード,借方コード,'" + myPaymentCode + "', 金額,摘要,支払予定日 ";
            SqlExecCommand = SqlExecCommand + "        FROM 未払明細 ";
            SqlExecCommand = SqlExecCommand + "    WHERE 未払コード = '" + myArrearCode + "' AND 支払予定日 = '" + myAdjustmentDate.ToShortDateString() + "' ";

            myDbCon.execSqlCommand(SqlExecCommand);
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

            mySqlCommand = "UPDATE 未払明細履歴 ";
            mySqlCommand = mySqlCommand + "  SET ";
            mySqlCommand = mySqlCommand + "      USED_COMPANY_ARREAR = @USED_COMPANY_ARREAR ";
            mySqlCommand = mySqlCommand + "  WHERE 明細履歴ＩＤ = @ID ";

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
            mySqlCommand = "SELECT 明細履歴ＩＤ, 年月日, 借方コード, 未払コード, 金額, 摘要, USED_COMPANY_ARREAR, CREATE_DATE "
                + "FROM 未払明細履歴 ";
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
                mySqlCommand = mySqlCommand + "  借方コード = @借方コード ";
                paramCnt++;
            }

            if (myDetailData.CreditCode.Length > 0)
            {
                if (paramCnt > 0)
                    mySqlCommand = mySqlCommand + " AND ";
                mySqlCommand = mySqlCommand + "  未払コード = @未払コード ";
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

            if (mySqlCommand.IndexOf("@借方コード") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@借方コード", SqlDbType.VarChar);
                sqlparams[paramCnt].Value = myDetailData.DebitCode;
                dbcon.SetParameter(sqlparams);
                paramCnt++;
            }

            if (mySqlCommand.IndexOf("@未払コード") >= 0)
            {
                sqlparams[paramCnt] = new SqlParameter("@未払コード", SqlDbType.VarChar);
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
                throw new Exception("未払明細履歴の取得でreaderがクローズされています");
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
                _logger.Debug("未払明細履歴に一致データ無し");
                return null;
            }

            reader.Close();

            return data;
        }

        public void DatabaseDetailDelete(string myArrearCode, DateTime myPaymentScheduleDate, DbConnection argDbCon)
        {
            DbConnection myDbCon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            mySqlCommand = "DELETE FROM 未払明細 ";
            mySqlCommand = mySqlCommand + "WHERE 未払コード = '" + myArrearCode + "' AND 支払予定日 = '" + myPaymentScheduleDate.ToShortDateString() + "' ";

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public int GetMaxDataOrder(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            myDbCon.openConnection();

            int maxDataOrder = myDbCon.getCountSql("SELECT MAX(DATA_ORDER) FROM COMPANY_ARREARS_DETAIL");

            return maxDataOrder;
        }

        public long GetBalance(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT ID, BALANCE \n";
            SelectCommand = SelectCommand + "      FROM COMPANY_ARREARS_DETAIL \n";
            SelectCommand = SelectCommand + "      ORDER BY DATA_ORDER DESC ";

            myDbCon.openConnection();

            SqlDataReader reader = null;

            long balance = -1;
            try
            {
                SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                myDbCon.SetParameter(null);

                reader = myDbCon.GetExecuteReader(SelectCommand);

                List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

                if (reader.IsClosed)
                {
                    _logger.Debug("reader.IsClosed");
                    throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                }

                int id = 0;
                if (reader.Read())
                {
                    id = DbExportCommon.GetDbInt(reader, 0);
                    balance = DbExportCommon.GetDbMoney(reader, 1);

                    _logger.Debug("id [" + id + "]  残高 [" + balance + "]");
                }
                else
                {
                    _logger.Debug("COMPANY_ARREARS_DETAIL データ無し");
                    return 0;
                }
            }
            finally
            {
                reader.Close();
            }

            if (balance < 0)
                throw new Exception("COMPANY_ARREARS_DETAILの残高が不正です");

            return balance;
        }

        public static Arrear GetDataByJournalId(int myJournalId, DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string SelectCommand = "";

            SelectCommand = "    SELECT ID, DATA_ORDER, JOURNAL_ID, JOURNAL_DATE, DEBIT_CODE, CREDIT_CODE, AMOUNT, REMARK, BALANCE, CREATE_DATE, UPDATE_DATE ";
            SelectCommand = SelectCommand + "FROM COMPANY_ARREARS_DETAIL ";
            SelectCommand = SelectCommand + "WHERE JOURNAL_ID = @JOURNAL_ID AND AMOUNT > 0 ";
            SelectCommand = SelectCommand + "ORDER BY DATA_ORDER DESC ";

            myDbCon.openConnection();

            SqlDataReader reader = null;

            Arrear arrear = null;
            try
            {
                SqlCommand cmd = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@JOURNAL_ID", SqlDbType.Int);
                sqlparams[0].Value = myJournalId;

                myDbCon.SetParameter(sqlparams);

                reader = myDbCon.GetExecuteReader(SelectCommand);

                List<MakeupDetailData> listDetail = new List<MakeupDetailData>();

                if (reader.IsClosed)
                {
                    _logger.Debug("reader.IsClosed");
                    throw new Exception("COMPANY_ARREARS_DETAILの残高の取得でreaderがクローズされています");
                }

                if (reader.Read())
                {
                    arrear = new Arrear();

                    arrear.Id = DbExportCommon.GetDbInt(reader, 0);
                    arrear.DataOrder = DbExportCommon.GetDbInt(reader, 1);
                    arrear.JournalId = DbExportCommon.GetDbInt(reader, 2);
                    arrear.JournalDate = DbExportCommon.GetDbDateTime(reader, 3);
                    arrear.DebitCode = DbExportCommon.GetDbString(reader, 4);
                    arrear.CreditCode = DbExportCommon.GetDbString(reader, 5);
                    arrear.Amount = DbExportCommon.GetDbMoney(reader, 6);
                    arrear.Remark = DbExportCommon.GetDbString(reader, 7);
                    arrear.Balance = DbExportCommon.GetDbMoney(reader, 8);
                    arrear.CreateDate = DbExportCommon.GetDbDateTime(reader, 9);
                    arrear.UpdateDate = DbExportCommon.GetDbDateTime(reader, 10);

                    _logger.Debug("id [" + arrear.Id + "]  残高 [" + arrear.Balance + "]");
                }
                else
                {
                    _logger.Debug("COMPANY_ARREARS_DETAIL データ無し");
                    return arrear;
                }
            }
            finally
            {
                reader.Close();
            }

            return arrear;
        }

        public void Regist(MoneyInputData myData, int myDataOrder, DbConnection myDbCon)
        {
            DbConnection dbcon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            MakeupDetailData detailData = new MakeupDetailData();

            detailData.Date = myData.Date;
            detailData.DebitCode = myData.DebitCode;
            detailData.CreditCode = myData.CreditCode;
            detailData.Amount = myData.Amount;
            detailData.Remark = myData.Remark;

            Regist(detailData, myDataOrder, myDbCon);

            return;
        }

        public void Regist(MakeupDetailData myData, int myDataOrder, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            // 残高は計算して算出する
            long balance = GetBalance(dbcon);
            long balanceOrg = balance;

            if (balance < 0)
                throw new Exception("GetBalanceの戻り値 COMPANY_ARREARS_DETAILの残高が不正です");

            Account account = new Account();

            string kind = account.getAccountKind(myData.DebitCode);
            if (kind.Equals(Account.KIND_ASSETS_COMPANY_ARREAR))
                balance = balance - myData.Amount;
            else
                balance = balance + myData.Amount;

            if (balance < 0)
                throw new BussinessException("会社未払金の金額がマイナスになります\n  残高 " + balanceOrg + "  金額 " + myData.Amount);

            mySqlCommand = "INSERT INTO COMPANY_ARREARS_DETAIL ";
            mySqlCommand = mySqlCommand + "( DATA_ORDER, JOURNAL_ID, JOURNAL_DATE, DEBIT_CODE, CREDIT_CODE, AMOUNT, REMARK, BALANCE ) ";
            mySqlCommand = mySqlCommand + "VALUES( @DATA_ORDER, @JOURNAL_ID, @JOURNAL_DATE, @DEBIT_CODE, @CREDIT_CODE, @AMOUNT, @REMARK, @BALANCE ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[8];

            sqlparams[0] = new SqlParameter("@DATA_ORDER", SqlDbType.Int);
            sqlparams[0].Value = myDataOrder;
            sqlparams[1] = new SqlParameter("@JOURNAL_ID", SqlDbType.Int);
            sqlparams[1].Value = myData.Id;
            sqlparams[2] = new SqlParameter("@JOURNAL_DATE", SqlDbType.DateTime);
            sqlparams[2].Value = myData.Date;
            sqlparams[3] = new SqlParameter("@DEBIT_CODE", SqlDbType.VarChar);
            sqlparams[3].Value = myData.DebitCode;
            sqlparams[4] = new SqlParameter("@CREDIT_CODE", SqlDbType.VarChar);
            sqlparams[4].Value = myData.CreditCode;
            sqlparams[5] = new SqlParameter("@AMOUNT", SqlDbType.Int);
            sqlparams[5].Value = myData.Amount;
            sqlparams[6] = new SqlParameter("@REMARK", SqlDbType.VarChar);
            sqlparams[6].Value = myData.Remark;
            sqlparams[7] = new SqlParameter("@BALANCE", SqlDbType.Money);
            sqlparams[7].Value = balance;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        /// <summary>
        /// 計算済みのCOMPANY_ARREARS_DETAILを更新する
        /// </summary>
        /// <param name="myData"></param>
        /// <param name="myDataOrder"></param>
        /// <param name="myDbCon"></param>
        public static void Update(MakeupDetailData myData, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            if (myData.Balance < 0)
                throw new Exception("GetBalanceの戻り値 COMPANY_ARREARS_DETAILの残高が不正です");

            mySqlCommand = "UPDATE COMPANY_ARREARS_DETAIL ";
            mySqlCommand += "  SET ";
            mySqlCommand += "    DATA_ORDER = @DataOrder ";
            mySqlCommand += "    , JOURNAL_ID = @JournalId ";
            mySqlCommand += "    , JOURNAL_DATE = @JournalDate ";
            mySqlCommand += "    , DEBIT_CODE = @DebitCode ";
            mySqlCommand += "    , CREDIT_CODE = @CreditCode ";
            mySqlCommand += "    , AMOUNT = @Amount ";
            mySqlCommand += "    , REMARK = @Remark ";
            mySqlCommand += "    , BALANCE = @Balance ";
            mySqlCommand += "  WHERE ID = @CompanyArrearsDetailId ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[9];

            sqlparams[0] = new SqlParameter("@DataOrder", SqlDbType.Int);
            sqlparams[0].Value = myData.DataOrder;
            sqlparams[1] = new SqlParameter("@JournalId", SqlDbType.Int);
            sqlparams[1].Value = myData.Id;
            sqlparams[2] = new SqlParameter("@JournalDate", SqlDbType.DateTime);
            sqlparams[2].Value = myData.Date;
            sqlparams[3] = new SqlParameter("@DebitCode", SqlDbType.VarChar);
            sqlparams[3].Value = myData.DebitCode;
            sqlparams[4] = new SqlParameter("@CreditCode", SqlDbType.VarChar);
            sqlparams[4].Value = myData.CreditCode;
            sqlparams[5] = new SqlParameter("@Amount", SqlDbType.Int);
            sqlparams[5].Value = myData.Amount;
            sqlparams[6] = new SqlParameter("@Remark", SqlDbType.VarChar);
            sqlparams[6].Value = myData.Remark;
            sqlparams[7] = new SqlParameter("@Balance", SqlDbType.Money);
            sqlparams[7].Value = myData.Balance;
            sqlparams[8] = new SqlParameter("@CompanyArrearsDetailId", SqlDbType.Int);
            sqlparams[8].Value = myData.CompanyArrearsDetailId;
            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

        public void Remove(int myId, DbConnection myDbCon)
        {
            DbConnection dbcon;
            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
            else
                dbcon = new DbConnection();

            mySqlCommand = "DELETE FROM COMPANY_ARREARS_DETAIL ";
            mySqlCommand = mySqlCommand + "WHERE ID = @ID ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, dbcon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@ID", SqlDbType.Int);
            sqlparams[0].Value = myId;

            dbcon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);

            return;
        }

    }
	class ArrearOld
	{
		// 未払リスト用の配列
		DbConnection dbcon = new DbConnection();

		public string[]	Code;			// 未払コード
		public string[] Name;			// 未払名
		public long[]	InputAmount;	// 入力合計
		public long[]	AdjustAmount;	// 精算済み金額
		public int		Count;			// 未払件数

		public ArrearOld(DbConnection myDbCon)
		{
			dbcon = myDbCon;

			Load();
		}
        public ArrearOld()
		{
		}
		private void Load()
		{
			dbcon = new DbConnection();

            dbcon.openConnection();

			Count = dbcon.getIntSql("SELECT COUNT(*) FROM 未払");

			Code			= new string[Count];
			Name			= new string[Count];
			InputAmount		= new long[Count];
			AdjustAmount	= new long[Count];

            SqlCommand command = new SqlCommand("SELECT 未払コード, 未払名 FROM 未払", dbcon.getSqlConnection());

            SqlDataReader reader = command.ExecuteReader();

            Count = 0;
			int Index = 0;

            do
            {
                while (reader.Read())
                {
					Code[Index] = DbExportCommon.GetDbString(reader, 0);
					Name[Index] = DbExportCommon.GetDbString(reader, 1);

					Index++;
				}
            } while (reader.NextResult());
            reader.Close();

			Count = Index;

			if (Count == 0)
            {
                dbcon.closeConnection();
                return;
            }
			string SelectCommand = "";

			for( int ArrIndex=0; ArrIndex<Count; ArrIndex++ )
			{
				SelectCommand = "SELECT SUM(金額) AS 入力合計 ";
				SelectCommand = SelectCommand + "FROM 未払明細 ";
				SelectCommand = SelectCommand + "WHERE 未払コード = '" + Code[ArrIndex] + "' ";

				InputAmount[ArrIndex] = dbcon.getAmountSql(SelectCommand);

				SelectCommand = "SELECT SUM(金額) AS 精算済み金額 ";
				SelectCommand = SelectCommand + "FROM 未払明細 ";
				SelectCommand = SelectCommand + "WHERE 未払コード = '" + Code[ArrIndex] + "' ";
				SelectCommand = SelectCommand + "    AND 支払予定日 IS NOT NULL ";

				AdjustAmount[ArrIndex] = dbcon.getAmountSql(SelectCommand);
			}

            return;
		}
		public long GetTotalAmount(string myArrearCode, DbConnection argDbCon)
		{
			DbConnection myDbCon;
			string SelectCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			SelectCommand = "SELECT SUM(金額) AS 精算済み金額 ";
			SelectCommand = SelectCommand + "FROM 未払明細 ";
			SelectCommand = SelectCommand + "WHERE 未払コード = '" + myArrearCode + "' ";
			SelectCommand = SelectCommand + "    AND 支払予定日 IS NOT NULL ";

			long TotalAmount = myDbCon.getAmountSql(SelectCommand);

			return TotalAmount;
		}
		public void Adjustment(MoneyInputData myInData, DbConnection argDbCon)
		{
			// 未払明細から未払明細履歴へデータを移す
			DatabaseSwitchDetail2History(myInData.DebitCode, myInData.CreditCode, myInData.Date, argDbCon);

			// 未払明細から削除
			DatabaseDetailDelete(myInData.DebitCode,  myInData.Date, argDbCon);
		}
		public void DatabaseSwitchDetail2History(string myArrearCode, string myPaymentCode, DateTime myAdjustmentDate, DbConnection argDbCon)
		{
			DbConnection myDbCon;
			string SqlExecCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			SqlExecCommand = "INSERT INTO 未払明細履歴 ";
			SqlExecCommand = SqlExecCommand + "     ( 年月日, 未払コード, 借方コード, 支払コード, 金額, 摘要, 支払日 ) ";
			SqlExecCommand = SqlExecCommand + "    SELECT 年月日,未払コード,借方コード,'" + myPaymentCode + "', 金額,摘要,支払予定日 ";
			SqlExecCommand = SqlExecCommand + "        FROM 未払明細 ";
			SqlExecCommand = SqlExecCommand + "    WHERE 未払コード = '" + myArrearCode + "' AND 支払予定日 = '" + myAdjustmentDate.ToShortDateString() + "' ";

			myDbCon.execSqlCommand(SqlExecCommand);
		}
		public void DatabaseDetailInsert(ArrearInputData myInData, DbConnection argDbCon)
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO 未払明細 ";
			mySqlCommand = mySqlCommand + "( 年月日, 未払コード, 借方コード, 金額, 摘要 ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + "     '" + myInData.Date.ToShortDateString() + "' ";	// 年月日
			mySqlCommand = mySqlCommand + "    ,'" + myInData.ArrearCode + "' ";					// 未払コード
			mySqlCommand = mySqlCommand + "    ,'" + myInData.DebitCode + "' ";					// 借方コード
			mySqlCommand = mySqlCommand + "    ," + myInData.Amount + " ";					// 金額
			mySqlCommand = mySqlCommand + "    ,'" + myInData.Summary + "' ";					// 摘要
			mySqlCommand = mySqlCommand + ") ";

			myDbCon.execSqlCommand(mySqlCommand);

			return;
		}
		public void DatabaseDetailUpdate(int myDetailId, DateTime myPaymentScheduleDate, DbConnection argDbCon)
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			// 精算キャンセルでNULL更新を行う為、引数チェックしてSQLを操作
			string SqlPaymentSchedule = "";

			if ( myPaymentScheduleDate == null )
				SqlPaymentSchedule = "支払予定日 = null ";
			else
				SqlPaymentSchedule = "支払予定日 = '" + myPaymentScheduleDate.ToShortDateString() + "' ";

			mySqlCommand = "UPDATE 未払明細 ";
			mySqlCommand = mySqlCommand + "SET ";
			mySqlCommand = mySqlCommand + "    支払予定日 = '" + myPaymentScheduleDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "WHERE 明細ID = " + myDetailId + " ";

			myDbCon.execSqlCommand(mySqlCommand);

			return;
		}
		public void DatabaseDetailUpdateCancel(string myArrearCode, DbConnection argDbCon)
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "UPDATE 未払明細 ";
			mySqlCommand = mySqlCommand + "SET ";
			mySqlCommand = mySqlCommand + "    支払予定日 = null ";
			mySqlCommand = mySqlCommand + "WHERE 未払コード = '" + myArrearCode + "' ";

			myDbCon.execSqlCommand(mySqlCommand);

			return;
		}
		public void DatabaseDetailDelete(string myArrearCode, DateTime myPaymentScheduleDate, DbConnection argDbCon)
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "DELETE FROM 未払明細 ";
			mySqlCommand = mySqlCommand + "WHERE 未払コード = '" + myArrearCode + "' AND 支払予定日 = '" + myPaymentScheduleDate.ToShortDateString() + "' ";

			myDbCon.execSqlCommand(mySqlCommand);

			return;
		}

	}
}
