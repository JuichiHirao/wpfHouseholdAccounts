using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// clsMethodPayment の概要の説明です。
	/// </summary>
	public class MethodPayment
	{
		//////////////////////
		/// 借入明細：定義  //
		//////////////////////
		protected DateTime Detail_Date;			// 年月日
		protected string	Detail_DealingCode;		// 借入取引コード
		protected string	Detail_AccountCode;		// 取引科目コード
		protected long	Detail_Amount;			// 金額
		protected string	Detail_Summary;			// 摘要
		protected long	Detail_PaymentAmount;	// 支払金額

		//////////////////
		/// 定数：定義  //
		//////////////////
		// データベース
		public const string DBTBL_DETAIL		= "借入明細";
		public const string DBTBL_DETAIL_HIST	= "借入明細履歴";
		public const string DBTBL_LOANTOTAL		= "借入集計";
		public const string DBTBL_PAYMENTDECISION	= "支払確定";
		public const string DBTBL_PAYMENTSCHEDULE	= "支払予定";

		public const string LOANTOTAL_REGKIND_LOAN	= "L";	// 借入
		public const string LOANTOTAL_REGKIND_PAY	= "P";	// 支払

		// メソッドDatabaseDetailGetPaymentで使用するモード
		public const int	DETAILGET_MODE_PAID		= 1;	// 支払済
		public const int	DETAILGET_MODE_UNPAID	= 2;	// 未支払

		public bool DatabaseDecisionCheck( DateTime myDateTime )
		{
			DbConnection dbCon = new DbConnection();

			long	myCount = 0;
			string	mySqlCommand = "";

			mySqlCommand = "SELECT COUNT(*) FROM 支払確定 ";
			mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + myDateTime.ToShortDateString() + "' ";
			myCount = dbCon.getSqlCommandRow( mySqlCommand );

			if ( myCount > 0 )
				return true;
			else
				return false;
		}
		public DateTime DatabaseTotalGetCalcBaseDate( string myLoanCode, DbConnection argDbCon )
		{
			// 支払となる対象の日付を取得する
			//   カードローン用
			DbConnection myDbCon;

			DateTime objDateTime = new DateTime();

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			return objDateTime;
		}
        public void DatabaseDetailInsert(DbConnection myDbCon)
        {
            if (myDbCon == null)
                myDbCon = new DbConnection();

            string mySqlCommand = "";

            mySqlCommand = "INSERT INTO 借入明細 ";
            mySqlCommand = mySqlCommand + "( 年月日, 借入取引コード, 取引科目コード, 金額, 摘要, 支払金額 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @年月日, @借入取引コード, @取引科目コード, @金額, @摘要, @支払金額 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[6];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = Detail_Date;
            sqlparams[1] = new SqlParameter("@借入取引コード", SqlDbType.VarChar);
            sqlparams[1].Value = Detail_DealingCode;
            sqlparams[2] = new SqlParameter("@取引科目コード", SqlDbType.VarChar);
            sqlparams[2].Value = Detail_AccountCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = Detail_Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = Detail_Summary;
            sqlparams[5] = new SqlParameter("@支払金額", SqlDbType.Int);
            sqlparams[5].Value = 0;

            myDbCon.SetParameter(sqlparams);

            try
            {
                myDbCon.execSqlCommand(mySqlCommand);
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }
        }
        public void DbDelete(DbConnection myDbCon)
		{/*
            if (myDbCon == null)
				myDbCon = new DbConnection();

			string mySqlCommand = "";

            mySqlCommand = "DELETE FROM 借入明細 ";
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

            SqlCommand scmd = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[6];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = Detail_Date;
            sqlparams[1] = new SqlParameter("@借入取引コード", SqlDbType.VarChar);
            sqlparams[1].Value = Detail_DealingCode;
            sqlparams[2] = new SqlParameter("@取引科目コード", SqlDbType.VarChar);
            sqlparams[2].Value = Detail_AccountCode;
            sqlparams[3] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[3].Value = Detail_Amount;
            sqlparams[4] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[4].Value = Detail_Summary;
            sqlparams[5] = new SqlParameter("@支払金額", SqlDbType.Int);
            sqlparams[5].Value = 0;

            myDbCon.SetParameter(sqlparams);

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			} */
		}
		/// <summary>
		/// TBL借入明細に分割払いで支払金額が０ではないデータがあるかのチェック
		/// 分割払いで１度以上支払われているデータ
		/// </summary>
		/// <param name="argDbCon"></param>
		public bool DatabaseDetailCheckPayment( string myLoanDealingCode, DbConnection argDbCon )
		{
			DbConnection	myDbCon;
			bool			boolTagetFlag = false;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "SELECT COUNT(*) FROM " + MethodPayment.DBTBL_DETAIL + " ";
			mySqlCommand = mySqlCommand + "WHERE ";
			mySqlCommand = mySqlCommand + "    借入取引コード = '" + myLoanDealingCode + "'";	// 借入取引コード
			mySqlCommand = mySqlCommand + "    AND 支払金額 > 0 ";									// 支払金額

			try
			{
				long lngTagetCount = myDbCon.getSqlCommandRow( mySqlCommand );


				if ( lngTagetCount > 0 )
					boolTagetFlag = true;
				else
					boolTagetFlag = false;
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}

			return boolTagetFlag;
		}
		/// <summary>
		/// TBL借入明細に分割払いで支払金額が０ではないデータをリターン
		/// 分割払いで１度以上支払われているデータ
		/// </summary>
		/// <param name="argDbCon"></param>
        public List<LoanDetailData> DatabaseDetailGetPayment(string myLoanDealingCode, int myGetMode, DbConnection argDbCon)
		{
			DbConnection	myDbCon;
			SqlCommand		myCommand;
			SqlDataReader	myReader;

            List<LoanDetailData> myListData = new List<LoanDetailData>();
			SqlMoney mySqlMoney;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "SELECT 明細ＩＤ, 年月日, 借入取引コード, 取引科目コード ";
			mySqlCommand = mySqlCommand + ", 金額, 摘要, 支払金額 ";
			mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_DETAIL + " ";
			mySqlCommand = mySqlCommand + "WHERE ";
			mySqlCommand = mySqlCommand + "    借入取引コード = '" + myLoanDealingCode + "'";	// 借入取引コード
			// mySqlCommand = mySqlCommand + " ";

			// モードにより付加するWHERE文を変える
				// １回以上支払われたデータを取得
			if ( myGetMode == DETAILGET_MODE_PAID )
				mySqlCommand = mySqlCommand + "    AND 支払金額 > 0 ";								// 支払金額
				// 新規に追加されたデータを取得
			else if ( myGetMode == DETAILGET_MODE_UNPAID )
				mySqlCommand = mySqlCommand + "    AND 支払金額 = 0 ";								// 支払金額

			try
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				while(myReader.Read())
				{
					LoanDetailData myData = new LoanDetailData();

					myData.Id   		= myReader.GetInt32(0);			// 明細ＩＤ
					myData.DealDate		= myReader.GetDateTime(1);		// 年月日
					myData.LoanDealingCode	= myReader.GetString(2);	// 借入取引コード
					myData.AccountCode	= myReader.GetString(3);		// 取引科目コード
					mySqlMoney			= myReader.GetSqlMoney(4);		// 金額
					myData.Amount		= mySqlMoney.ToInt64();
					myData.Summury		= myReader.GetString(5);		// 摘要
					mySqlMoney			= myReader.GetSqlMoney(6);		// 支払金額
					myData.PaymentAmount= mySqlMoney.ToInt64();

                    myListData.Add(myData);
				}

				myReader.Close();
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.closeConnection();
			}

            return myListData;
		}
		/// <summary>
		/// TBL借入明細に分割払いで支払金額が０ではない支払確定されたデータをリターン
		/// </summary>
		/// <param name="argDbCon"></param>
		public List<LoanDetailData> DatabaseDetailGetDecision( string myLoanDealingCode, DbConnection argDbCon )
		{
			DbConnection	myDbCon;
			LoanDetailDatas myDatas;
			SqlCommand		myCommand;
			SqlDataReader	myReader;

			SqlMoney mySqlMoney;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

            List<LoanDetailData> listData = new List<LoanDetailData>();

			string mySqlCommand = "";

			mySqlCommand = "SELECT 借入明細.明細ＩＤ, 年月日, 借入取引コード, 取引科目コード ";
			mySqlCommand = mySqlCommand + ", 金額, 摘要, 支払金額 ";
			mySqlCommand = mySqlCommand + "FROM " + DBTBL_DETAIL + " ";
			mySqlCommand = mySqlCommand + "  INNER JOIN " + DBTBL_PAYMENTSCHEDULE + " ";
			mySqlCommand = mySqlCommand + "  ON " + DBTBL_DETAIL + ".明細ＩＤ = " + DBTBL_PAYMENTSCHEDULE + ".明細ＩＤ ";
			mySqlCommand = mySqlCommand + "WHERE 借入取引コード = '" + Detail_DealingCode + "' ";

			// mySqlCommand = mySqlCommand + " ";

			try
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				while(myReader.Read())
				{
					LoanDetailData myData = new LoanDetailData();

					myData.Id		= myReader.GetInt32(0);			// 明細ＩＤ
					myData.DealDate		= myReader.GetDateTime(1);		// 年月日
					myData.LoanDealingCode	= myReader.GetString(2);	// 借入取引コード
					myData.AccountCode	= myReader.GetString(3);		// 取引科目コード
					mySqlMoney			= myReader.GetSqlMoney(4);		// 金額
					myData.Amount		= mySqlMoney.ToInt64();
					myData.Summury		= myReader.GetString(5);		// 摘要
					mySqlMoney			= myReader.GetSqlMoney(6);		// 支払金額
					myData.PaymentAmount= mySqlMoney.ToInt64();

                    listData.Add(myData);
				}

				myReader.Close();
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.closeConnection();
			}

            return listData;
		}
		public void DatabaseDecisionInsert( string myLoanCode, MoneyInputData myInData, DbConnection myDbCon )
		{
			DbConnection dbcon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (myDbCon != null)
                dbcon = myDbCon;
			else
                dbcon = new DbConnection();

			string mySqlCommand = "";

            mySqlCommand = "INSERT INTO 支払確定 ";
            mySqlCommand = mySqlCommand + "( 支払日, 借入コード, 借方, 貸方, 金額, 摘要 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @支払日, @借入コード, @借方, @貸方, @金額, @摘要 ) ";

            SqlCommand scmd = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

            SqlParameter[] sqlparams = new SqlParameter[6];

            sqlparams[0] = new SqlParameter("@支払日", SqlDbType.DateTime);
            sqlparams[0].Value = myInData.Date;
            sqlparams[1] = new SqlParameter("@借入コード", SqlDbType.VarChar);
            sqlparams[1].Value = myLoanCode;
            sqlparams[2] = new SqlParameter("@借方", SqlDbType.VarChar);
            sqlparams[2].Value = myInData.DebitCode;
            sqlparams[3] = new SqlParameter("@貸方", SqlDbType.VarChar);
            sqlparams[3].Value = myInData.CreditCode;
            sqlparams[4] = new SqlParameter("@金額", SqlDbType.Int);
            sqlparams[4].Value = myInData.Amount;
            sqlparams[5] = new SqlParameter("@摘要", SqlDbType.VarChar);
            sqlparams[5].Value = myInData.Remark;

            dbcon.SetParameter(sqlparams);

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public void DatabaseScheduleInsert( string myLoanCode, DateTime myPaymentDate, long myMeisaiId, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "INSERT INTO " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
			mySqlCommand = mySqlCommand + "( 支払日, 借入コード, 明細ＩＤ ) ";
            mySqlCommand = mySqlCommand + "  VALUES( @支払日, @借入コード, @明細ＩＤ )";

            SqlParameter[] sqlparams = new SqlParameter[3];

            sqlparams[0] = new SqlParameter("@支払日", SqlDbType.DateTime);
            sqlparams[0].Value = myPaymentDate;
            sqlparams[1] = new SqlParameter("@借入コード", SqlDbType.VarChar);
            sqlparams[1].Value = myLoanCode;
            sqlparams[2] = new SqlParameter("@明細ＩＤ", SqlDbType.Int);
            sqlparams[2].Value = myMeisaiId;

            myDbCon.SetParameter(sqlparams);

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public long DatabaseTotalGetAmount( string myLoanDealingCode, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			long	myAmount = 0;
			string	mySqlCommand = "";

			///////////////////////////////////////////////
			// TBL借入集計に対象データが存在する事の確認 //
			///////////////////////////////////////////////
			mySqlCommand = "SELECT COUNT(*) FROM 借入集計 ";
			mySqlCommand = mySqlCommand + "WHERE 登録日 = ";
			mySqlCommand = mySqlCommand + "      ( SELECT MAX(登録日) ";
			mySqlCommand = mySqlCommand + "            FROM 借入集計";
			mySqlCommand = mySqlCommand + "            WHERE 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "      ) ";
			mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + myLoanDealingCode + "' ";

			long myRowCount = myDbCon.getSqlCommandRow( mySqlCommand );

			/////////////////////////////////////
			// TBL借入集計から対象データを取得 //
			/////////////////////////////////////
			if ( myRowCount > 0 )
			{
				mySqlCommand = "SELECT 借入集計金額 FROM 借入集計 ";
				mySqlCommand = mySqlCommand + "WHERE 登録日 = ";
				mySqlCommand = mySqlCommand + "      ( SELECT MAX(登録日) ";
				mySqlCommand = mySqlCommand + "            FROM 借入集計";
				mySqlCommand = mySqlCommand + "            WHERE 借入取引コード = '" + myLoanDealingCode + "' ";
				mySqlCommand = mySqlCommand + "      ) ";
				mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + myLoanDealingCode + "' ";
/*
				myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				myReader = myCommand.ExecuteReader();

				myReader.Read();

				SqlMoney mySqlMoney = myReader.GetSqlMoney(0);
				myAmount = mySqlMoney.ToInt64();
 */
				myAmount = myDbCon.getAmountSql( mySqlCommand );

//				myReader.Close();

//				myDbCon.closeConnection();

				return myAmount;
			}

			return 0;
		}
		public void DatabaseTotalAdditional( string myLoanDealingCode, DateTime myTotalDate, long myAmount, DateTime myRegistDate, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			// 引数：登録日の年が1の場合は現在日付を取得、設定する
			//   ※ 年に1が設定されるのはインスタンスを作成しただけでデータ未設定の場合
			if ( myRegistDate.Year == 1 )
				myRegistDate = System.DateTime.Now;

			//////////////////////////////////////////////////
			// 借入取引コードの最新の借入集計金額を取得する //
			//////////////////////////////////////////////////
			long myTotalAmount = this.DatabaseTotalGetAmount( myLoanDealingCode, myDbCon );

			////////////////////////////////////////
			// 借入集計へ足し込んだ金額を登録する //
			////////////////////////////////////////
			mySqlCommand = "INSERT INTO 借入集計( 借入取引コード, 集計日, 借入集計金額, 登録種類, 登録日 )";
			mySqlCommand = mySqlCommand + "VALUES( '" + myLoanDealingCode + "'";
			mySqlCommand = mySqlCommand + ", '" + myTotalDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + ", " + System.Convert.ToString( myTotalAmount + myAmount ) + " ";
			mySqlCommand = mySqlCommand + ", '" + MethodPayment.LOANTOTAL_REGKIND_LOAN + "' ";
			mySqlCommand = mySqlCommand + ", '" + myRegistDate.ToShortDateString() + " " + myRegistDate.ToShortTimeString() + "') ";

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}						
		}
		public void DatabaseTotalSubtraction( string myLoanDealingCode, DateTime myTotalDate, long myAmount, DateTime myRegistDate, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			try
			{
				// 引数：登録日の年が1の場合は現在日付を取得、設定する
				//   ※ 年に1が設定されるのはインスタンスを作成しただけでデータ未設定の場合
				if ( myRegistDate.Year == 1 )
					myRegistDate = System.DateTime.Now;

				//////////////////////////////////////////////////
				// 借入取引コードの最新の借入集計金額を取得する //
				//////////////////////////////////////////////////
				long myTotalAmount = this.DatabaseTotalGetAmount( myLoanDealingCode, myDbCon );

				// 借入集計金額が返済後０未満になる場合
				if ( myTotalAmount < Detail_Amount )
				{
					string ErrMessage = "現在の残高は" + String.Format("{0, 12:C} ", myTotalAmount) + "です。\n"
						+ "    入力された金額" + String.Format("{0, 12:C} ", Detail_Amount) + "は残高を超えています\n";
					throw new BussinessException( ErrMessage );
				}

				////////////////////////////////////////
				// 借入集計へ足し込んだ金額を登録する //
				////////////////////////////////////////
				mySqlCommand = "INSERT INTO 借入集計( 借入取引コード, 集計日, 借入集計金額, 登録種類, 登録日 )";
				mySqlCommand = mySqlCommand + "VALUES( '" + myLoanDealingCode + "'";
				mySqlCommand = mySqlCommand + ", '" + myTotalDate.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + ", " + System.Convert.ToString( myTotalAmount - myAmount ) + " ";
				mySqlCommand = mySqlCommand + ", '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";
				mySqlCommand = mySqlCommand + ", '" + myRegistDate.ToShortDateString() + " " + myRegistDate.ToShortTimeString() + "') ";

				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}
		}
		/// <summary>
		/// 借入明細履歴へ登録する
		/// 　呼出元：PaymentNormal.Regist
		/// </summary>
		public void DatabaseHistoryInsert( long myDetailId, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO " + MethodPayment.DBTBL_DETAIL_HIST + " ";
			mySqlCommand = mySqlCommand + "( 年月日, 借入取引コード, 取引科目コード, 金額, 摘要, 登録日 ) ";
			mySqlCommand = mySqlCommand + "SELECT 年月日, 借入取引コード, 取引科目コード, 金額, 摘要, GETDATE() ";
			mySqlCommand = mySqlCommand + "    FROM 借入明細 ";
			mySqlCommand = mySqlCommand + "    WHERE 明細ＩＤ = " + myDetailId.ToString();

            myDbCon.SetParameter(null);

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		/// <summary>
		/// 借入明細履歴へ登録する
		/// 　呼出元：PaymentInstallmentPlan.Pay
		/// </summary>
		public void DatabaseHistoryInsert( LoanDetailData myData, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

            myDbCon.SetParameter(null);

			mySqlCommand = "INSERT INTO " + MethodPayment.DBTBL_DETAIL_HIST + " ";
			mySqlCommand = mySqlCommand + "( 年月日, 借入取引コード, 取引科目コード, 金額, 摘要, 登録日 ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + "'"	+ myData.DealDate.ToShortDateString() + "'";	// 年月日
			mySqlCommand = mySqlCommand + ", '"	+ myData.LoanDealingCode + "'";					// 借入取引コード
			mySqlCommand = mySqlCommand + ", '"	+ myData.AccountCode + "'";						// 取引科目コード
			mySqlCommand = mySqlCommand + ", "	+ myData.Amount;								// 金額
			mySqlCommand = mySqlCommand + ", '"	+ myData.Summury + "'";							// 摘要
			mySqlCommand = mySqlCommand + ", GETDATE()";										// 登録日
			mySqlCommand = mySqlCommand + ") ";

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public void DatabaseHistoryInsert( DbConnection argDbCon )
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "INSERT INTO " + MethodPayment.DBTBL_DETAIL_HIST + " ";
			mySqlCommand = mySqlCommand + "( 年月日, 借入取引コード, 取引科目コード, 金額, 摘要, 登録日 ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + "'" + Detail_Date.ToShortDateString() + "'";	// 日付
			mySqlCommand = mySqlCommand + ", '" + Detail_DealingCode + "'";				// 借入取引コード
			mySqlCommand = mySqlCommand + ", '" + Detail_AccountCode + "'";				// 取引科目コード
			mySqlCommand = mySqlCommand + ",  " + Detail_Amount + " ";					// 金額
			mySqlCommand = mySqlCommand + ", '" + Detail_Summary + "'";					// 摘要
			mySqlCommand = mySqlCommand + ", GETDATE() ";								// 登録日
			mySqlCommand = mySqlCommand + ") ";

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public void DatabaseDetailUpdatePaymentAmount( LoanDetailData myData, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "UPDATE " + MethodPayment.DBTBL_DETAIL + " ";
			mySqlCommand = mySqlCommand + "SET ";
			mySqlCommand = mySqlCommand + "   支払金額 = " + myData.PaymentAmount + " ";
			mySqlCommand = mySqlCommand + "WHERE 明細ＩＤ = " + myData.Id.ToString();

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public void DatabaseDetailDelete( long myDetailId, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "DELETE FROM " + MethodPayment.DBTBL_DETAIL + " ";
			mySqlCommand = mySqlCommand + "WHERE 明細ＩＤ = " + myDetailId.ToString();

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public void DatabaseDecisionDelete( long myDetailId, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "DELETE FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
			mySqlCommand = mySqlCommand + "WHERE 明細ＩＤ = " + myDetailId.ToString();

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public long CalcratePayInterest( string myLoanDealingCode, double myLoanInterestRate, DateTime myPaymentDate, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			///////////////////////////////////////////////////////////////
			// 支払のRowが存在しない場合は支払のＲｏｗを金額０で作成する //
			///////////////////////////////////////////////////////////////
			mySqlCommand = "SELECT COUNT(*) FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "WHERE 登録種類 = '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";

			long myRowCount = myDbCon.getSqlCommandRow( mySqlCommand );

			if ( myRowCount == 0 )
			{	
				DateTime myPastDate = myPaymentDate.AddYears( -1 );
				//myPastDate.AddYears( -1 );
				mySqlCommand = "INSERT INTO " + MethodPayment.DBTBL_LOANTOTAL + " ";
				mySqlCommand = mySqlCommand + "( 借入取引コード, 集計日, 借入集計金額, 登録種類, 登録日 ) ";
				mySqlCommand = mySqlCommand + "VALUES( '" + myLoanDealingCode + "' ";
				mySqlCommand = mySqlCommand + "    , '" + myPastDate.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "    , 0 ";
				mySqlCommand = mySqlCommand + "    , '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";
				mySqlCommand = mySqlCommand + "    , GETDATE() ";
				mySqlCommand = mySqlCommand + "    ) ";

				myDbCon.execSqlCommand( mySqlCommand );
			}

			//////////////////////////////////////////////
			// 借入集計から対象となるＲｏｗを取得する	//
			// 登録種類が支払時の最近の集計日を取得		//
			//////////////////////////////////////////////
			// Ｒｏｗが存在する事の確認
			mySqlCommand = "SELECT COUNT(*) FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "WHERE 集計日 >= ";
			mySqlCommand = mySqlCommand + "      ( ";
			mySqlCommand = mySqlCommand + "        SELECT MAX(集計日) FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "            WHERE 登録種類 = '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";
			mySqlCommand = mySqlCommand + "                  AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "      ) ";
			mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + myLoanDealingCode + "' ";

			myRowCount = myDbCon.getSqlCommandRow( mySqlCommand );

			if ( myRowCount <= 0 )
					return 0;

			//////////////////////////////////////////
			// 日数と利息検出対象の金額を配列へ設定 //
			//////////////////////////////////////////
			SqlDataAdapter	objDataAdapter	= new SqlDataAdapter();	// データアダプター
			DataSet			objDataSet		= new DataSet();		// データセット

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

			int[]	arrDateDiff	= new int[myRowCount];	// 差分の日数
			long[]	arrAmount	= new long[myRowCount];	// 借入金額

			mySqlCommand = "SELECT 集計日, 借入集計金額 FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "WHERE 集計日 >= ";
			mySqlCommand = mySqlCommand + "      ( ";
			mySqlCommand = mySqlCommand + "        SELECT MAX(集計日) FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "            WHERE 登録種類 = '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";
			mySqlCommand = mySqlCommand + "                  AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "      ) ";
			mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "ORDER BY 集計日";

			DateTime workDateTime = new DateTime();
			DateTime activeDateTime = new DateTime();

			try
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				long lIndex = 0;			// インデックス（全ての利息算出の状況を考慮してロング型）
				long myTargetAmount = 0;	// 利息対象金額
				long workTargetAmount = 0;	// 利息対象金額（前のＲｏｗ保管用）

				DbConnection myDbCon_DateDiff = new DbConnection();
				while( myReader.Read() )
				{
					activeDateTime		= myReader.GetDateTime( 0 );
					mySqlMoney			= myReader.GetSqlMoney( 1 );
					myTargetAmount		= mySqlMoney.ToInt64();
					// インスタンスを生成した直後は'1/1/1'に設定されるので
					// １より上の場合は200Xのデータとする（２件目以降）
					if ( workDateTime.Year > 1 )
					{
						// 前の日付との日数をＳＱＬで取得する（片端）
						mySqlCommand = "SELECT DATEDIFF( day, '" + workDateTime.ToShortDateString() + "', '" + activeDateTime.ToShortDateString() + "' )";

						arrDateDiff[lIndex] = System.Convert.ToInt16( myDbCon_DateDiff.getSqlCommandRow( mySqlCommand ) );

						// 金額の設定
						workDateTime		= activeDateTime;
						arrAmount[lIndex]	= workTargetAmount;

						lIndex++;
					}
					workTargetAmount = myTargetAmount;
					workDateTime	 = activeDateTime;
				}
				// 最後のＲｏｗと支払日の差分を設定する
				// 前の日付との日数をＳＱＬで取得する（片端）
				mySqlCommand = "SELECT DATEDIFF( day, '" + workDateTime.ToShortDateString() + "', '" + myPaymentDate.ToShortDateString() + "' )";
				arrDateDiff[lIndex] = System.Convert.ToInt16( myDbCon_DateDiff.getSqlCommandRow( mySqlCommand ) );

				// 金額の設定
				workDateTime		= activeDateTime;
				arrAmount[lIndex]	= workTargetAmount;

				myReader.Close();
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.closeConnection();
			}

			////////////////
			// 利息の計算 //
			////////////////
			double	myTotalInterest = 0;	// 利息合計
			double	myInterest = 0;
			double	myOneYearInterest = 0;
			double	myDayRate = 0;

			// 配列に格納されている情報から利息を計算して足し込む
			for ( long lIndexInterest = 0; lIndexInterest < myRowCount; lIndexInterest++ )
			{
				// 一年分の利息 ＝ 借入金額 × 利率
				myOneYearInterest = System.Convert.ToDouble(arrAmount[lIndexInterest]) * ( myLoanInterestRate / 100 );
				// 一年の利率対象日数の割合 ＝ 日数　／ ３６５
				myDayRate = System.Convert.ToDouble(arrDateDiff[lIndexInterest]) / 365;

				// 一年分の利息：小数点以下の切り捨て
				decimal myDecimal = Convert.ToDecimal(myOneYearInterest);
				decimal myDecimalSeisu = System.Decimal.Truncate(myDecimal);

				// 利息 ＝ 一年分の利息 × 一年の利率対象日数の割合
				myInterest = System.Decimal.ToDouble(myDecimalSeisu) * myDayRate;

				// 利息：小数点以下の切り捨て
				myDecimal	= Convert.ToDecimal(myInterest);
				myInterest	= Convert.ToDouble(Decimal.Truncate(myDecimal));

				// 利息合計に１インデックス分の利息を足し込む
				myTotalInterest = myTotalInterest + myInterest;
			}

			return System.Convert.ToInt64(myTotalInterest);
		/*
			/// 小数第2位を四捨五入します。
			public double Round(double arg1)
			{
				decimal dectmp = Convert.ToDecimal(arg1);
				dectmp *= 10;
				dectmp += 0.5m;
				dectmp = Decimal.Truncate(dectmp);
				dectmp /= 10;
				double dret = Convert.ToDouble(dectmp);
				return dret;
			}
		 */
		}
		public decimal CalcratePayInterest( string myLoanDealingCode, double myLoanInterestRate, DateTime myCalcBaseDate, DateTime myPaymentDate, DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			///////////////////////////////////////////////////////////////
			// 支払のRowが存在しない場合は支払のＲｏｗを金額０で作成する //
			///////////////////////////////////////////////////////////////
			mySqlCommand = "SELECT COUNT(*) FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "WHERE 登録種類 = '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";

			long myRowCount = myDbCon.getSqlCommandRow( mySqlCommand );

			if ( myRowCount == 0 )
			{	
				DateTime myPastDate = myPaymentDate.AddYears( -1 );
				//myPastDate.AddYears( -1 );
				mySqlCommand = "INSERT INTO " + MethodPayment.DBTBL_LOANTOTAL + " ";
				mySqlCommand = mySqlCommand + "( 借入取引コード, 集計日, 借入集計金額, 登録種類, 登録日 ) ";
				mySqlCommand = mySqlCommand + "VALUES( '" + myLoanDealingCode + "' ";
				mySqlCommand = mySqlCommand + "    , '" + myPastDate.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "    , 0 ";
				mySqlCommand = mySqlCommand + "    , '" + MethodPayment.LOANTOTAL_REGKIND_PAY + "' ";
				mySqlCommand = mySqlCommand + "    , GETDATE() ";
				mySqlCommand = mySqlCommand + "    ) ";

				myDbCon.execSqlCommand( mySqlCommand );
			}

			//////////////////////////////////////////////
			// 借入集計から対象となるＲｏｗを取得する	//
			// 登録種類が支払時の最近の集計日を取得		//
			//////////////////////////////////////////////
			// Ｒｏｗが存在する事の確認
			mySqlCommand = "SELECT COUNT(*) FROM ";
			mySqlCommand = mySqlCommand + "( ";
			mySqlCommand = mySqlCommand + "SELECT 集計日, 借入集計金額 FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "WHERE 集計日 >= '" + myCalcBaseDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "UNION ";
			mySqlCommand = mySqlCommand + "SELECT '" + myCalcBaseDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "     , 借入集計金額 ";
			mySqlCommand = mySqlCommand + "    FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "    WHERE 集計日 ";
			mySqlCommand = mySqlCommand + "              = ( SELECT MAX(集計日) FROM 借入集計 WHERE 集計日 < '" + myCalcBaseDate.ToShortDateString() + "' ) ";
			mySqlCommand = mySqlCommand + "        AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + ") AS 借入集計 ";

			myRowCount = myDbCon.getSqlCommandRow( mySqlCommand );

			if ( myRowCount <= 0 )
					return 0;

			//////////////////////////////////////////
			// 日数と利息検出対象の金額を配列へ設定 //
			//////////////////////////////////////////
			SqlDataAdapter	objDataAdapter	= new SqlDataAdapter();	// データアダプター
			DataSet			objDataSet		= new DataSet();		// データセット

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

			int[]	arrDateDiff	= new int[myRowCount];	// 差分の日数
			long[]	arrAmount	= new long[myRowCount];	// 借入金額

			mySqlCommand = "SELECT 集計日, 借入集計金額 FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "WHERE 集計日 >= '" + myCalcBaseDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "UNION ";
			mySqlCommand = mySqlCommand + "SELECT '" + myCalcBaseDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "     , 借入集計金額 ";
			mySqlCommand = mySqlCommand + "    FROM " + MethodPayment.DBTBL_LOANTOTAL + " ";
			mySqlCommand = mySqlCommand + "    WHERE 集計日 ";
			mySqlCommand = mySqlCommand + "              = ( SELECT MAX(集計日) FROM 借入集計 WHERE 集計日 < '" + myCalcBaseDate.ToShortDateString() + "' ) ";
			mySqlCommand = mySqlCommand + "        AND 借入取引コード = '" + myLoanDealingCode + "' ";
			mySqlCommand = mySqlCommand + "    ORDER BY 集計日 ";

			DateTime workDateTime = new DateTime();
			DateTime activeDateTime = new DateTime();

			try
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				long lIndex				= 0;	// インデックス（全ての利息算出の状況を考慮してロング型）
				long myTargetAmount		= 0;	// 利息対象金額
				long workTargetAmount	= 0;	// 利息対象金額（前のＲｏｗ保管用）

				DbConnection myDbCon_DateDiff = new DbConnection();
				while( myReader.Read() )
				{
					activeDateTime		= myReader.GetDateTime( 0 );
					mySqlMoney			= myReader.GetSqlMoney( 1 );
					myTargetAmount		= mySqlMoney.ToInt64();
					// インスタンスを生成した直後は'1/1/1'に設定されるので
					// １より上の場合は200Xのデータとする（２件目以降）
					if ( workDateTime.Year > 1 )
					{
						// 前の日付との日数をＳＱＬで取得する（片端）
						mySqlCommand = "SELECT DATEDIFF( day, '" + workDateTime.ToShortDateString() + "', '" + activeDateTime.ToShortDateString() + "' )";

						arrDateDiff[lIndex] = System.Convert.ToInt16( myDbCon_DateDiff.getSqlCommandRow( mySqlCommand ) );

						// 金額の設定
						workDateTime		= activeDateTime;
						arrAmount[lIndex]	= workTargetAmount;

						lIndex++;
					}
					workTargetAmount = myTargetAmount;
					workDateTime	 = activeDateTime;
				}
				// 最後のＲｏｗと支払日の差分を設定する
				// 前の日付との日数をＳＱＬで取得する（片端）
				mySqlCommand = "SELECT DATEDIFF( day, '" + workDateTime.ToShortDateString() + "', '" + myPaymentDate.ToShortDateString() + "' )";
				arrDateDiff[lIndex] = System.Convert.ToInt16( myDbCon_DateDiff.getSqlCommandRow( mySqlCommand ) );

				// 金額の設定
				workDateTime		= activeDateTime;
				arrAmount[lIndex]	= workTargetAmount;

				myReader.Close();
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				if ( myDbCon.isTransaction() == false )
					myDbCon.closeConnection();
			}

			////////////////
			// 利息の計算 //
			////////////////
			double	myTotalInterest = 0;	// 利息合計
			double	myInterest = 0;
			double	myOneYearInterest = 0;
			double	myDayRate = 0;

			// 配列に格納されている情報から利息を計算して足し込む
			for ( long lIndexInterest = 0; lIndexInterest < myRowCount; lIndexInterest++ )
			{
				// 一年分の利息 ＝ 借入金額 × 利率
				myOneYearInterest = System.Convert.ToDouble(arrAmount[lIndexInterest]) * ( myLoanInterestRate / 100 );
				// 一年の利率対象日数の割合 ＝ 日数　／ ３６５
				myDayRate = System.Convert.ToDouble(arrDateDiff[lIndexInterest]) / 365;

				// 一年分の利息：小数点以下の切り捨て
				decimal myDecimal = Convert.ToDecimal(myOneYearInterest);
				//decimal myDecimalSeisu = System.Decimal.Truncate(myDecimal);

				// 利息 ＝ 一年分の利息 × 一年の利率対象日数の割合
				myInterest = System.Decimal.ToDouble(myDecimal) * myDayRate;
				//myInterest = System.Decimal.ToDouble(myDecimalSeisu) * myDayRate;

				// 利息：小数点以下の切り捨て
				myDecimal	= Convert.ToDecimal(myInterest);
				myInterest	= Convert.ToDouble(myDecimal);
				//myInterest	= Convert.ToDouble(Decimal.Truncate(myDecimal));

				// 利息合計に１インデックス分の利息を足し込む
				myTotalInterest = myTotalInterest + myInterest;
			}
			decimal myDecimalInt = Convert.ToDecimal(myTotalInterest);
			myDecimalInt *= 100;
			myDecimalInt = Decimal.Truncate(myDecimalInt);
			myDecimalInt /= 100;
			//myTotalInterest = Convert.ToDouble(myDecimalInt);

			return myDecimalInt;
		/*
			/// 小数第2位を四捨五入します。
			public double Round(double arg1)
			{
				decimal dectmp = Convert.ToDecimal(arg1);
				dectmp *= 10;
				dectmp += 0.5m;
				dectmp = Decimal.Truncate(dectmp);
				dectmp /= 10;
				double dret = Convert.ToDouble(dectmp);
				return dret;
			}
		 */
		}
	/*
		public long CalcratePayInterest( long myLoanAmount, double myLoanInterestRate, DateTime myDealingDate, DateTime myPaymentDate )
		{
			DbConnection myDbCon = new DbConnection();

			string mySqlCommand = "";

			double	dblDateDiff		= 0.0;								// 取引日と支払日の日数
			double	dblLoanAmount	= Convert.ToDouble( myLoanAmount );	// 借入金額
			decimal	dcmInterest		= 0;								// 利息額

			// 取引日と支払日の差分を設定する
			// 前の日付との日数をＳＱＬで取得する（片端）
			mySqlCommand = "SELECT DATEDIFF( day, '" + myDealingDate.ToShortDateString() + "', '" + myPaymentDate.ToShortDateString() + "' )";

			try
			{
				dblDateDiff = System.Convert.ToDouble( myDbCon.getSqlCommandRow( mySqlCommand ) );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
					myDbCon.closeConnection();
			}

			// 利息率を％から按分する
			myLoanInterestRate = myLoanInterestRate / 100;

			// 利息額 ＝ 利息率 × 借入金額 × （日数 ÷ ３６５）
			dcmInterest = Convert.ToDecimal( myLoanInterestRate * dblLoanAmount * ( dblDateDiff / 365 ) );

			// 利息額から小数点以下を切り捨て
			dcmInterest = Decimal.Truncate( dcmInterest );

			// Long(Int64）に変換してリターン
			return ( Convert.ToInt64(dcmInterest) );

		}
	 */
		/// <summary>
		/// 借入明細の１件分の借入に対する利息の計算（小数点２桁以下を切り捨てる）
		/// </summary>
		/// <param name="myLoanAmount"></param>
		/// <param name="myLoanInterestRate"></param>
		/// <param name="myDealingDate"></param>
		/// <param name="myPaymentDate"></param>
		/// <returns></returns>
		public decimal CalcratePayInterest( long myLoanAmount, double myLoanInterestRate, DateTime myDealingDate, DateTime myPaymentDate )
		{
			DbConnection myDbCon = new DbConnection();

			string mySqlCommand = "";

			double	dblDateDiff		= 0.0;								// 取引日と支払日の日数
			double	dblLoanAmount	= Convert.ToDouble( myLoanAmount );	// 借入金額
			decimal	dcmInterest		= 0;								// 利息額

			// 取引日と支払日の差分を設定する
			// 前の日付との日数をＳＱＬで取得する（片端）
			mySqlCommand = "SELECT DATEDIFF( day, '" + myDealingDate.ToShortDateString() + "', '" + myPaymentDate.ToShortDateString() + "' )";

			try
			{
				dblDateDiff = Convert.ToDouble( myDbCon.getSqlCommandRow( mySqlCommand ) );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				myDbCon.closeConnection();
			}

			// 利息率を％から按分する
			myLoanInterestRate = myLoanInterestRate / 100;

			// 利息額 ＝ 利息率 × 借入金額 × （日数 ÷ ３６５）
			dcmInterest = Convert.ToDecimal( myLoanInterestRate * dblLoanAmount * ( dblDateDiff / 365 ) );

			// 利息額から小数点第３桁以下を切り捨て
			dcmInterest *= 100;
			dcmInterest = Decimal.Truncate( dcmInterest );
			dcmInterest /= 100;

			// Long(Int64）に変換してリターン
			return ( dcmInterest );

		}
		public void DetailSet_Date( DateTime myDate )
		{
			Detail_Date = myDate;
		}
		public void DetailSet_DealingCode( string myCode )
		{
			Detail_DealingCode = myCode;
		}
		public void DetailSet_AccountCode( string myCode )
		{
			Detail_AccountCode = myCode;
		}
		public void DetailSet_Amount( long myAmount )
		{
			Detail_Amount = myAmount;
		}
		public void DetailSet_Summary( string mySummary )
		{
			Detail_Summary = mySummary;
		}
		public void DetailSet_PaymentAmount( int myAmount )
		{
			Detail_PaymentAmount = myAmount;
		}
	}
	public class PaymentNormal : MethodPayment
	{
		public PaymentNormal()
		{
			// 特になし
		}
		public PaymentNormal( LoanDetailData myLoanDetailData )
		{
			// 引数のOBJ借入明細を各プロパティに設定する
			this.Detail_Date		= myLoanDetailData.DealDate;
			this.Detail_DealingCode	= myLoanDetailData.LoanDealingCode;
			this.Detail_AccountCode	= myLoanDetailData.AccountCode;
			this.Detail_Amount		= myLoanDetailData.Amount;
			this.Detail_Summary		= myLoanDetailData.Summury;
		}
		public string Regist( DbConnection myDbCon )
		{
			// 借入未集計明細へ登録する
			this.DatabaseDetailInsert( myDbCon );

			return "";
		}

        public string Delete(DbConnection myDbCon)
        {
            // 借入未集計明細へ登録する
            this.DbDelete(myDbCon);

            return "";
        }

        public void Decision(LoanData myLoanInfo, List<PaymentData> myListData, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			// 合計
            long total = 0;

            foreach (PaymentData data in myListData)
            {
                if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                    continue;

                total = total + data.Amount;
            }

			// OBJ金銭帳入力へ設定
			MoneyInputData indata = new MoneyInputData();

			indata.Date			= myLoanInfo.PaymentDate;
			indata.DebitCode	= myLoanInfo.DealingCode;			// 借方←借入取引コード
			indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
            indata.Amount = total;
            indata.Remark       = "";								// 摘要

			// 支払確定へ登録
            this.DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);

			// 支払予定へ登録
            foreach(PaymentData data in myListData)
            {
                if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                    continue;

                DatabaseScheduleInsert(myLoanInfo.LoanCode, myLoanInfo.PaymentDate, data.Id, myDbCon);
            }
		}
        public void Decision(LoanDetailDatas loandetails, DbConnection argDbCon)
        {
            DbConnection myDbCon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            // 合計
            long myTotal = loandetails.CalcTotal();

            // OBJ金銭帳入力へ設定
            MoneyInputData indata = new MoneyInputData();

            indata.Date = loandetails.PaymentDate;
            indata.DebitCode = loandetails.DealingCode;			// 借方←借入取引コード
            indata.CreditCode = loandetails.PaymentAccountCode;	// 貸方←支払先科目コード
            indata.Amount = myTotal;
            indata.Remark = "";								// 摘要

            // 支払確定へ登録
            this.DatabaseDecisionInsert(loandetails.LoanCode, indata, myDbCon);

            // 支払予定へ登録
            for (int iIndex = 0; loandetails.Count > iIndex; iIndex++)
            {
                LoanDetailData myData = (LoanDetailData)loandetails.GetInputData(iIndex);

                this.DatabaseScheduleInsert(loandetails.LoanCode, loandetails.PaymentDate, myData.Id, myDbCon);
            }
        }
        public string Pay(DbConnection argDbCon)
		{
			try
			{
				DbConnection myDbCon;

				// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
				if ( argDbCon != null )
					myDbCon = argDbCon;
				else
					myDbCon = new DbConnection();

				string	mySqlCommand = "";
				long	myTotalAmount = 0;		// 合計額

				//////////////////////////////////////
				// 借入明細の指定コード合計額を取得 //
				//////////////////////////////////////
				mySqlCommand = "SELECT SUM(金額) ";
				mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
				mySqlCommand = mySqlCommand + "  INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
				mySqlCommand = mySqlCommand + "  ON " + MethodPayment.DBTBL_PAYMENTSCHEDULE + ".明細ＩＤ ";
				mySqlCommand = mySqlCommand + "      = " + MethodPayment.DBTBL_DETAIL + ".明細ＩＤ ";
				mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + Detail_DealingCode + "' ";

				// ＳＱＬ文の実行
				//myTotalAmount = myDbCon.getSqlCommandRow( mySqlCommand );
				myTotalAmount = myDbCon.getAmountSql( mySqlCommand );

				// 金額が一致しない場合
				if ( myTotalAmount != Detail_Amount )
				{
					string ErrMessage = "支払予定時の確定合計額と金銭帳入力の金額が一致していません\n"
						+ "    確定時金額 = " + myTotalAmount.ToString() + "\n"
						+ "    金銭帳入力 = " + Detail_Amount.ToString() + "\n"
                        + "    対象コード = " + Detail_DealingCode + "\n";
                    throw new BussinessException( ErrMessage );
				}

				////////////////////////////////////////////////////////
				// 支払予定から一致するデータを検出（明細ＩＤの配列） //
				////////////////////////////////////////////////////////
				SqlCommand myCommand;
				SqlDataReader myReader;

				long[]	arrDetailId	= new long[50];	// 明細ＩＤ

				mySqlCommand = "SELECT 支払予定.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
				mySqlCommand = mySqlCommand + "  INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
				mySqlCommand = mySqlCommand + "  ON 支払予定.明細ＩＤ = 借入明細.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + Detail_DealingCode + "' ";

				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				int iIndex = 0;
				while( myReader.Read() )
				{
					arrDetailId[iIndex] = myReader.GetInt32( 0 );

					iIndex++;
				}
				int iRowCount = iIndex;
				
				myReader.Close();

				for( iIndex = 0; iRowCount > iIndex; iIndex++ )
				{
					////////////////////////
					// 借入明細履歴へ挿入 //
					////////////////////////
					this.DatabaseHistoryInsert( arrDetailId[iIndex], myDbCon );

					//////////////////////
					// 借入明細から削除 //
					//////////////////////
					this.DatabaseDetailDelete( arrDetailId[iIndex], myDbCon );

					//////////////////////
					// 支払予定から削除 //
					//////////////////////
					this.DatabaseDecisionDelete( arrDetailId[iIndex], myDbCon );
				}
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			return "";
		}
        public string PayCancel(DbConnection argDbCon)
        {
            try
            {
                DbConnection myDbCon;

                // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
                if (argDbCon != null)
                    myDbCon = argDbCon;
                else
                    myDbCon = new DbConnection();

                string mySqlCommand = "";
                long myTotalAmount = 0;		// 合計額

                // 借入明細履歴の指定コード合計額を取得
                mySqlCommand = "SELECT SUM(金額) ";
                mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
                mySqlCommand = mySqlCommand + "  INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
                mySqlCommand = mySqlCommand + "  ON " + MethodPayment.DBTBL_PAYMENTSCHEDULE + ".明細ＩＤ ";
                mySqlCommand = mySqlCommand + "      = " + MethodPayment.DBTBL_DETAIL + ".明細ＩＤ ";
                mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
                mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + Detail_DealingCode + "' ";

                // SQL文の実行
                //myTotalAmount = myDbCon.getSqlCommandRow( mySqlCommand );
                myTotalAmount = myDbCon.getAmountSql(mySqlCommand);

                // 金額が一致しない場合
                if (myTotalAmount != Detail_Amount)
                {
                    string ErrMessage = "支払予定時の確定合計額と金銭帳入力の金額が一致していません\n"
                        + "    確定時金額 = " + myTotalAmount.ToString() + "\n"
                        + "    金銭帳入力 = " + Detail_Amount.ToString() + "\n";
                    throw new BussinessException(ErrMessage);
                }

                // 明細履歴のデータを明細へ戻す


                ////////////////////////////////////////////////////////
                // 支払予定から一致するデータを検出（明細ＩＤの配列） //
                ////////////////////////////////////////////////////////
                SqlCommand myCommand;
                SqlDataReader myReader;

                long[] arrDetailId = new long[50];	// 明細ＩＤ

                mySqlCommand = "SELECT 支払予定.明細ＩＤ ";
                mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
                mySqlCommand = mySqlCommand + "  INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
                mySqlCommand = mySqlCommand + "  ON 支払予定.明細ＩＤ = 借入明細.明細ＩＤ ";
                mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
                mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + Detail_DealingCode + "' ";

                if (myDbCon.isTransaction() == false)
                    myDbCon.openConnection();

                myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

                if (myDbCon.isTransaction() == true)
                    myCommand.Transaction = myDbCon.GetTransaction();

                myReader = myCommand.ExecuteReader();

                int iIndex = 0;
                while (myReader.Read())
                {
                    arrDetailId[iIndex] = myReader.GetInt32(0);

                    iIndex++;
                }
                int iRowCount = iIndex;

                myReader.Close();

                for (iIndex = 0; iRowCount > iIndex; iIndex++)
                {
                    ////////////////////////
                    // 借入明細履歴へ挿入 //
                    ////////////////////////
                    this.DatabaseHistoryInsert(arrDetailId[iIndex], myDbCon);

                    //////////////////////
                    // 借入明細から削除 //
                    //////////////////////
                    this.DatabaseDetailDelete(arrDetailId[iIndex], myDbCon);

                    //////////////////////
                    // 支払予定から削除 //
                    //////////////////////
                    this.DatabaseDecisionDelete(arrDetailId[iIndex], myDbCon);
                }
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }
            catch (BussinessException errbsn)
            {
                throw errbsn;
            }

            return "";

/* Decision
            DbConnection myDbCon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            // 合計
            long myTotal = loandetails.CalcTotal();

            // OBJ金銭帳入力へ設定
            MoneyInputData indata = new MoneyInputData();

            indata.Date = loandetails.PaymentDate;
            indata.DebitCode = loandetails.DealingCode;			// 借方←借入取引コード
            indata.CreditCode = loandetails.PaymentAccountCode;	// 貸方←支払先科目コード
            indata.Amount = myTotal;
            indata.Remark = "";								// 摘要

            // 支払確定へ登録
            this.DatabaseDecisionInsert(loandetails.LoanCode, indata, myDbCon);

            // 支払予定へ登録
            for (int iIndex = 0; loandetails.Count > iIndex; iIndex++)
            {
                LoanDetailData myData = (LoanDetailData)loandetails.GetInputData(iIndex);

                this.DatabaseScheduleInsert(loandetails.LoanCode, loandetails.PaymentDate, myData.Id, myDbCon);
            }
 */
        }
    }
	public class PaymentRivolving : MethodPayment
	{
		public PaymentRivolving()
		{
			// 特になし
		}
		public PaymentRivolving( LoanDetailData myLoanDetailData )
		{
			this.Detail_Date		= myLoanDetailData.DealDate;
			this.Detail_DealingCode	= myLoanDetailData.LoanDealingCode;
			this.Detail_AccountCode	= myLoanDetailData.AccountCode;
			this.Detail_Amount		= myLoanDetailData.Amount;
			this.Detail_Summary		= myLoanDetailData.Summury;
		}
		public string Regist( DbConnection myDbCon )
		{
			// 借入明細へ登録する
			this.DatabaseDetailInsert( myDbCon );

			return "";
		}
        public string Delete(DbConnection myDbCon)
        {
            // 借入未集計明細へ登録する
            this.DbDelete(myDbCon);

            return "";
        }
        public void Decision(LoanData myLoanInfo, List<PaymentData> myListData, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			// 合計の算出
            long total = 0;

            foreach (PaymentData data in myListData)
            {
                if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                    continue;

                total = total + data.Amount;
            }

			try
			{
				long paymentAmount = 0;	// 支払額（支払確定へ登録する金額）

				// 借入集計の借入取引内容に対する最新の金額データを取得
				//     存在しない場合は０を設定
                long balanceAmount = this.DatabaseTotalGetAmount(myLoanInfo.DealingCode, myDbCon);
				// 最新の金額データを合計額を足し込む
                balanceAmount = balanceAmount + total;

				//////////////////////////////////
				// 支払確定へ登録する金額を算出 //
				//////////////////////////////////
				// 支払予定額を算出
				long loanPaymentAmount = 0;
				// 支払確定日がボーナス月である場合は借入支払額に夏冬それぞれのボーナス支払額を加算する
                if (myLoanInfo.PaymentDate.Month == myLoanInfo.BonusSummerMonth)
					// 借入支払額 ＝ ボーナス夏支払額 ＋ 通常月支払額
                    loanPaymentAmount = myLoanInfo.BonusSPaymentAmount + myLoanInfo.NormalPaymentAmount;
                else if (myLoanInfo.PaymentDate.Month == myLoanInfo.BonusWinterMonth)
					// 借入支払額 ＝ ボーナス冬支払額 ＋ 通常月支払額
                    loanPaymentAmount = myLoanInfo.BonusSPaymentAmount + myLoanInfo.NormalPaymentAmount;
				else
					// 借入支払額 ＝ 通常月支払額
                    loanPaymentAmount = myLoanInfo.NormalPaymentAmount;

				// 借入残高（借入集計金額）が支払予定額未満の場合
                if (balanceAmount < loanPaymentAmount)
                    paymentAmount = balanceAmount;
				else
                    paymentAmount = loanPaymentAmount;

				////////////////////
				// 借入集計へ登録 //
				////////////////////
                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    DatabaseTotalAdditional(myLoanInfo.DealingCode,
                                                    data.InputDate,
                                                    data.Amount,
                                                    DateTime.Now,
                                                    myDbCon);
                }

				//////////////////////////////////////////////
				// 支払確定へ登録する利息コード、金額を算出 //
				//////////////////////////////////////////////
				// 利息計上の日は借入先で指定された日を設定する
				//   例）毎月１０日の場合は支払日が月曜の１１日
				//       だが利息計算対象の日は１０を設定
				DateTime myPaymentDate;
				long myInterest = 0;
                if (myLoanInfo.PaymentDay > 0 && myLoanInfo.PaymentDay < 32)
                    myPaymentDate = new DateTime(myLoanInfo.PaymentDate.Year
                                                    , myLoanInfo.PaymentDate.Month
                                                    , myLoanInfo.PaymentDay
												);
				else
                    myPaymentDate = myLoanInfo.PaymentDate;

				// ※ ＤＢへは検索だが、利息金額算出と別にコネクションを作成して検索すると
				//    トランザクションの確定待ちになってしまう（デッドロック）
				//    その為、検索だがトランザクション内で行なう
                myInterest = this.CalcratePayInterest(myLoanInfo.DealingCode
                                                        , myLoanInfo.PaymentRate
														, myPaymentDate
														, myDbCon
													);

				//////////////////////////////////
				// 支払確定へ登録：支払予定金額 //
				//////////////////////////////////
				// 支払確定に登録用のＩ／Ｆ：OBJ金銭帳入力へ設定
				MoneyInputData indata = new MoneyInputData();

				indata.Date			= myLoanInfo.PaymentDate;
				indata.DebitCode	= myLoanInfo.DealingCode;			// 借方←借入取引コード
				indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
                indata.Amount = paymentAmount;
                indata.Remark       = "";								// 摘要

                if (paymentAmount > 0)
				{
					// 支払確定へ登録
                    DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);
				}

				//////////////////////////////////////
				// 支払確定へ登録：支払予定利息金額 //
				//////////////////////////////////////
				if ( myInterest > 0 )
				{
					// 支払確定に登録用のＩ／Ｆ：OBJ金銭帳入力へ設定
					indata = new MoneyInputData();

					indata.Date			= myLoanInfo.PaymentDate;
					indata.DebitCode	= myLoanInfo.RateAccountCode;		// 借方←利率科目コード
					indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
					indata.Amount		= myInterest;
                    indata.Remark       = "";								// 摘要

					// 支払確定へ登録
                    this.DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);
				}

				// 支払予定へ登録
                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    DatabaseScheduleInsert(myLoanInfo.LoanCode, myPaymentDate, data.Id, myDbCon);
                }
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}

		public string Pay( DbConnection argDbCon )
		{
			try
			{
				DbConnection myDbCon;

				// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
				if ( argDbCon != null )
					myDbCon = argDbCon;
				else
					myDbCon = new DbConnection();

				string	mySqlCommand = "";

				////////////////////////////////////
				// 借入集計から入力金額を減額する //
				////////////////////////////////////
				DateTime myDateTime = new DateTime();
				this.DatabaseTotalSubtraction( Detail_DealingCode, Detail_Date, Detail_Amount, myDateTime, myDbCon );

				////////////////////////////////////////////////////////
				// 支払予定から一致するデータを検出（明細ＩＤの配列） //
				////////////////////////////////////////////////////////
				SqlCommand myCommand;
				SqlDataReader myReader;

				long[]	arrDetailId	= new long[50];	// 明細ＩＤ

				mySqlCommand = "SELECT 支払予定.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
				mySqlCommand = mySqlCommand + "  INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
				mySqlCommand = mySqlCommand + "  ON 支払予定.明細ＩＤ = 借入明細.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + Detail_DealingCode + "' ";

				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				int iIndex = 0;
				while( myReader.Read() )
				{
					arrDetailId[iIndex] = myReader.GetInt32( 0 );

					iIndex++;
				}
				int iRowCount = iIndex;
				
				myReader.Close();

				for( iIndex = 0; iRowCount > iIndex; iIndex++ )
				{
					////////////////////////
					// 借入明細履歴へ挿入 //
					////////////////////////
					this.DatabaseHistoryInsert( arrDetailId[iIndex], myDbCon );

					//////////////////////
					// 借入明細から削除 //
					//////////////////////
					this.DatabaseDetailDelete( arrDetailId[iIndex], myDbCon );

					//////////////////////
					// 支払予定から削除 //
					//////////////////////
					this.DatabaseDecisionDelete( arrDetailId[iIndex], myDbCon );
				}
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			return "";

		}
	}

	public class PaymentCashing : MethodPayment
	{
		public PaymentCashing()
		{
			// 特になし
		}
		public PaymentCashing( LoanDetailData myLoanDetailData )
		{
			// 引数のOBJ借入明細を各プロパティに設定する
			this.Detail_Date		= myLoanDetailData.DealDate;
			this.Detail_DealingCode	= myLoanDetailData.LoanDealingCode;
			this.Detail_AccountCode	= myLoanDetailData.AccountCode;
			this.Detail_Amount		= myLoanDetailData.Amount;
			this.Detail_Summary		= myLoanDetailData.Summury;
		}
		public string Regist( DbConnection myDbCon )
		{
			// 借入未集計明細へ登録する
			this.DatabaseDetailInsert( myDbCon );

			return "";
		}
        public void Decision(LoanData myLoanInfo, List<PaymentData> myListData, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			try
			{
				long totalInterest = 0;	// 利息合計額

				//////////////////////////////////
				// 支払確定へ登録する金額を算出 //
				//////////////////////////////////
                // 合計
                long total = 0;

                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    total = total + data.Amount;
                }

				//////////////////////////////////////////////////////
				// 支払確定へ登録する利息コード、利息合計金額を算出 //
				//////////////////////////////////////////////////////
                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    // 利息計上の日は借入先で指定された日を設定する
                    //   例）毎月１０日の場合は支払日が月曜の１１日
                    //       だが利息計算対象の日は１０を設定
                    DateTime myPaymentDate;
                    if (myLoanInfo.PaymentDay > 0 && myLoanInfo.PaymentDay < 32)
                        myPaymentDate = new DateTime(myLoanInfo.PaymentDate.Year
                                                        , myLoanInfo.PaymentDate.Month
                                                        , myLoanInfo.PaymentDay
                                                        );
                    else
                        myPaymentDate = myLoanInfo.PaymentDate;

                    decimal interest = this.CalcratePayInterest(data.Amount,
                        myLoanInfo.PaymentRate,
                        data.InputDate,
                        myPaymentDate
                        );

                    // 利息額を足し込む
                    totalInterest = Convert.ToInt64(totalInterest + interest);
                }
				
				//////////////////////////////////
				// 支払確定へ登録：支払予定金額 //
				//////////////////////////////////
				// 支払確定に登録用のＩ／Ｆ：OBJ金銭帳入力へ設定
				MoneyInputData indata = new MoneyInputData();

				indata.Date			= myLoanInfo.PaymentDate;
				indata.DebitCode	= myLoanInfo.DealingCode;			// 借方←借入取引コード
				indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
				indata.Amount		= total;
                indata.Remark       = "";								// 摘要

				// 支払確定へ登録
                this.DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);

				//////////////////////////////////////
				// 支払確定へ登録：支払予定利息金額 //
				//////////////////////////////////////
                if (totalInterest > 0)
				{
					// 支払確定に登録用のＩ／Ｆ：OBJ金銭帳入力へ設定
					indata = new MoneyInputData();

					indata.Date			= myLoanInfo.PaymentDate;
					indata.DebitCode	= myLoanInfo.RateAccountCode;		// 借方←利率科目コード
					indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
					indata.Amount		= totalInterest;
                    indata.Remark       = "";								// 摘要

					// 支払確定へ登録
                    this.DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);
				}

				////////////////////
				// 支払予定へ登録 //
				////////////////////
                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    DatabaseScheduleInsert(myLoanInfo.LoanCode, myLoanInfo.PaymentDate, data.Id, myDbCon);
                }

			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
		}
		public string Pay( DbConnection argDbCon )
		{
			try
			{
				DbConnection myDbCon;

				// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
				if ( argDbCon != null )
					myDbCon = argDbCon;
				else
					myDbCon = new DbConnection();

				string	mySqlCommand = "";

				////////////////////////////////////////////////////////
				// 支払予定から一致するデータを検出（明細ＩＤの配列） //
				////////////////////////////////////////////////////////
				SqlCommand myCommand;
				SqlDataReader myReader;

				long[]	arrDetailId	= new long[50];	// 明細ＩＤ

				mySqlCommand = "SELECT 支払予定.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
				mySqlCommand = mySqlCommand + "  INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
				mySqlCommand = mySqlCommand + "  ON 支払予定.明細ＩＤ = 借入明細.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "    AND 借入取引コード = '" + Detail_DealingCode + "' ";

				if ( myDbCon.isTransaction() == false )
					myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				if ( myDbCon.isTransaction() == true )
					myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				int iIndex = 0;
				while( myReader.Read() )
				{
					arrDetailId[iIndex] = myReader.GetInt32( 0 );

					iIndex++;
				}
				int iRowCount = iIndex;
				
				myReader.Close();

				for( iIndex = 0; iRowCount > iIndex; iIndex++ )
				{
					////////////////////////
					// 借入明細履歴へ挿入 //
					////////////////////////
					this.DatabaseHistoryInsert( arrDetailId[iIndex], myDbCon );

					//////////////////////
					// 借入明細から削除 //
					//////////////////////
					this.DatabaseDetailDelete( arrDetailId[iIndex], myDbCon );

					//////////////////////
					// 支払予定から削除 //
					//////////////////////
					this.DatabaseDecisionDelete( arrDetailId[iIndex], myDbCon );
				}
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			return "";
		}
	}
	public class PaymentSlide : MethodPayment
	{
		public PaymentSlide()
		{
			// 特になし
		}
		public PaymentSlide( LoanDetailData myLoanDetailData )
		{
			// 引数のOBJ借入明細を各プロパティに設定する
			this.Detail_Date		= myLoanDetailData.DealDate;
			this.Detail_DealingCode	= myLoanDetailData.LoanDealingCode;
			this.Detail_AccountCode	= myLoanDetailData.AccountCode;
			this.Detail_Amount		= myLoanDetailData.Amount;
			this.Detail_Summary		= myLoanDetailData.Summury;
		}
		public string Regist( DbConnection argDbCon )
		{
			DbConnection myDbCon;

			try
			{
				// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
				if ( argDbCon != null )
					myDbCon = argDbCon;
				else
					myDbCon = new DbConnection();

				// 借入計明細へ登録する
				this.DatabaseHistoryInsert( myDbCon );

				// 借入集計へ加算登録する
				DateTime myDateTime = new DateTime();
				this.DatabaseTotalAdditional( Detail_DealingCode, Detail_Date, Detail_Amount, myDateTime, myDbCon );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			return "";
		}
		public string Pay( DbConnection argDbCon )
		{
			DbConnection myDbCon;

			try
			{
				// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
				if ( argDbCon != null )
					myDbCon = argDbCon;
				else
					myDbCon = new DbConnection();

				// 借入計明細へ登録する
				this.DatabaseHistoryInsert( myDbCon );

				// 借入集計へ減算登録する
				DateTime myDateTime = new DateTime();
				this.DatabaseTotalSubtraction( Detail_DealingCode, Detail_Date, Detail_Amount, myDateTime, myDbCon );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			return "";
		}

	}


}
