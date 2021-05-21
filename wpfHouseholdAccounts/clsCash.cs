using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// clsCash の概要の説明です。
	/// </summary>
	public class Cash
	{
		private	DateTime	LastRegistDate	= new DateTime();
		private	long		BalanceAmount	= 0;		// 残高
        private long        BalanceAmountKabushiki = 0;   // 株式会社残高
		private long		BalanceAmountGoudou = 0;   // 合同会社残高

		private bool		RegistCheck		= false;
		private DateTime	RegistDate		= new DateTime();
		private long		RegistDebitAmount	= 0;
		private long		RegistCreditAmount	= 0;
		private long		RegistBalanceAmount = 0;
		private long		RegistDebitAmountKabushiki	= 0;
		private long		RegistCreditAmountKabushiki	= 0;
		private long		RegistBalanceAmountKabushiki = 0;
		private long		RegistDebitAmountGoudou = 0;
		private long		RegistCreditAmountGoudou = 0;
		private long		RegistBalanceAmountGoudou = 0;

		//////////////////
		/// 定数：定義  //
		//////////////////
		// データベース
		public const string DBTBL_CASH	= "現金";

		public Cash()
		{
			// 
			// TODO: コンストラクタ ロジックをここに追加してください。
			//

			// 現金の現在取得額、最新の日付を取得
			this.DatabaseSetProperty();
		}
		public void RegistDataCheck( DateTime myDate, long myDebitAmt, long myCreditAmt )
		{
			// 取得した日付、金額の妥当性のチェックを行う
			string ErrMessage = "";

			try
			{
				if ( myDate <= LastRegistDate )
				{
					ErrMessage = "指定された日付は既に入力済みです";
					throw new BussinessException( ErrMessage );
				}

				if ( myDebitAmt + BalanceAmount < myCreditAmt )
				{
					ErrMessage = "借方と貸方の金額では現金がマイナスになります。";
					throw new BussinessException( ErrMessage );
				}
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			RegistDate			= myDate;
			RegistDebitAmount	= myDebitAmt;
			RegistCreditAmount	= myCreditAmt;
			RegistBalanceAmount	= BalanceAmount + RegistDebitAmount - RegistCreditAmount;
			RegistCheck = true;

			return;
		}

        public void RegistCompanyDataCheckKabushiki(DateTime myDate, long myDebitAmt, long myCreditAmt)
        {
            // 取得した日付、金額の妥当性のチェックを行う
            string ErrMessage = "";

            try
            {
                if (myDate <= LastRegistDate)
                {
                    ErrMessage = "指定された日付は既に入力済みです";
                    throw new BussinessException(ErrMessage);
                }

                if (myDebitAmt + BalanceAmountKabushiki < myCreditAmt)
                {
                    ErrMessage = "借方と貸方の金額では会社現金がマイナスになります";
                    throw new BussinessException(ErrMessage);
                }

			}
			catch (BussinessException errbsn)
            {
                throw errbsn;
            }

            RegistDate = myDate;
            RegistDebitAmountKabushiki = myDebitAmt;
			RegistCreditAmountKabushiki = myCreditAmt;
			RegistBalanceAmountKabushiki = BalanceAmountKabushiki + RegistDebitAmountKabushiki - RegistCreditAmountKabushiki;
            RegistCheck = true;

            return;
        }

		public void RegistCompanyDataCheckGoudou(DateTime myDate, long myDebitAmt, long myCreditAmt)
		{
			// 取得した日付、金額の妥当性のチェックを行う
			string ErrMessage = "";

			try
			{
				if (myDate <= LastRegistDate)
				{
					ErrMessage = "指定された日付は既に入力済みです";
					throw new BussinessException(ErrMessage);
				}

				if (myDebitAmt + BalanceAmountGoudou < myCreditAmt)
				{
					ErrMessage = "借方と貸方の金額では会社現金がマイナスになります";
					throw new BussinessException(ErrMessage);
				}

			}
			catch (BussinessException errbsn)
			{
				throw errbsn;
			}

			RegistDate = myDate;
			RegistDebitAmountGoudou = myDebitAmt;
			RegistCreditAmountGoudou = myCreditAmt;
			RegistBalanceAmountGoudou = BalanceAmountGoudou + RegistDebitAmountGoudou - RegistCreditAmountGoudou;
			RegistCheck = true;

			return;
		}

		public MoneyNowData GetNowAssetDept()
		{
			// 
			MoneyNowData nowdata = new MoneyNowData();
			nowdata.Code		= Account.CODE_CASH;
			nowdata.Name		= "現金";
			nowdata.NowAmount	= BalanceAmount;

			return nowdata;
		}
        public MoneyNowData GetNowAssetDeptCompany()
        {
            // 
            MoneyNowData nowdata = new MoneyNowData();
            nowdata.Code = Account.CODE_CASHEXPENSE_KABUSHIKI;
            nowdata.Name = "株式会社現金";
            nowdata.NowAmount = BalanceAmountKabushiki;

            return nowdata;
        }

		public MoneyNowData GetNowAssetDeptCompanyGoudou()
		{
			// 
			MoneyNowData nowdata = new MoneyNowData();
			nowdata.Code = Account.CODE_CASHEXPENSE_GOUDOU;
			nowdata.Name = "合同会社現金";
			nowdata.NowAmount = BalanceAmountGoudou;

			return nowdata;
		}

		private void DatabaseSetProperty()
		{
			DbConnection myDbCon = new DbConnection();
			string mySqlCommand = "";

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

			try
			{
				myDbCon.openConnection();

				mySqlCommand = "SELECT COUNT(*) ";
				mySqlCommand = mySqlCommand + "    FROM " + Cash.DBTBL_CASH + " ";
				mySqlCommand = mySqlCommand + "    WHERE 年月日 = ( SELECT MAX(年月日) FROM " + Cash.DBTBL_CASH + " ) ";

				long myDbCount = myDbCon.getSqlCommandRow( mySqlCommand );

				if ( myDbCount <= 0 )
				{
					BalanceAmount = 0;

					return;
				}

				mySqlCommand = "SELECT 年月日, 残高, 会社残高, 合同残高 ";
				mySqlCommand = mySqlCommand + "    FROM " + Cash.DBTBL_CASH + " ";
				mySqlCommand = mySqlCommand + "    WHERE 年月日 = ( SELECT MAX(年月日) FROM " + Cash.DBTBL_CASH + " ) ";

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				myReader = myCommand.ExecuteReader();
				myReader.Read();

				// 取得した情報をプロパティへ設定
				LastRegistDate	= myReader.GetDateTime( 0 );
                BalanceAmount = DbExportCommon.GetDbMoney(myReader, 1);
                BalanceAmountKabushiki = DbExportCommon.GetDbMoney(myReader, 2);
				BalanceAmountGoudou = DbExportCommon.GetDbMoney(myReader, 3);

				myReader.Close();

				myDbCon.closeConnection();
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				myDbCon.closeConnection();
			}
		}
		public void DatabaseRefrect( DbConnection argDbCon )
		{
			DbConnection myDbCon;
			string mySqlCommand = "";
			string ErrMessage	= "";

			try
			{
				// 日付が初期値のままの場合はチェックされていない為にエラーにする
				if ( RegistCheck == false )
				{
					ErrMessage = "登録する現金情報の妥当性がチェックされていません。";
					throw new BussinessException( ErrMessage );
				}
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			if ( argDbCon == null )
				myDbCon = new DbConnection();
			else
				myDbCon = argDbCon;

            myDbCon.SetParameter(null);

            mySqlCommand = "INSERT INTO 現金 ";
            mySqlCommand = mySqlCommand + "( 年月日, 借方金額, 貸方金額, 残高, 会社借方金額, 会社貸方金額, 会社残高, 合同借方金額, 合同貸方金額, 合同残高 ) ";
            mySqlCommand = mySqlCommand + "VALUES( @年月日, @借方金額, @貸方金額, @残高, @会社借方金額, @会社貸方金額, @会社残高, @合同借方金額, @合同貸方金額, @合同残高 ) ";

            SqlParameter[] sqlparams = new SqlParameter[10];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = RegistDate.ToShortDateString();
            sqlparams[1] = new SqlParameter("@借方金額", SqlDbType.Int);
            sqlparams[1].Value = RegistDebitAmount;
            sqlparams[2] = new SqlParameter("@貸方金額", SqlDbType.Int);
            sqlparams[2].Value = RegistCreditAmount;
            sqlparams[3] = new SqlParameter("@残高", SqlDbType.Int);
            sqlparams[3].Value = RegistBalanceAmount;
            sqlparams[4] = new SqlParameter("@会社借方金額", SqlDbType.Int);
            sqlparams[4].Value = RegistDebitAmountKabushiki;
            sqlparams[5] = new SqlParameter("@会社貸方金額", SqlDbType.Int);
            sqlparams[5].Value = RegistCreditAmountKabushiki;
            sqlparams[6] = new SqlParameter("@会社残高", SqlDbType.Int);
            sqlparams[6].Value = RegistBalanceAmountKabushiki;
			sqlparams[7] = new SqlParameter("@合同借方金額", SqlDbType.Int);
			sqlparams[7].Value = RegistDebitAmountGoudou;
			sqlparams[8] = new SqlParameter("@合同貸方金額", SqlDbType.Int);
			sqlparams[8].Value = RegistCreditAmountGoudou;
			sqlparams[9] = new SqlParameter("@合同残高", SqlDbType.Int);
			sqlparams[9].Value = RegistBalanceAmountGoudou;

			myDbCon.SetParameter(sqlparams);

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}

			return;
		}

        public static void DbDeleteFromDate(DateTime myDate, DbConnection argDbCon)
        {
            DbConnection myDbCon;
            string mySqlCommand = "";

            if (argDbCon == null)
                myDbCon = new DbConnection();
            else
                myDbCon = argDbCon;

            myDbCon.SetParameter(null);

            mySqlCommand = "DELETE FROM 現金 ";
            mySqlCommand = mySqlCommand + "WHERE 年月日 = @年月日 ";

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@年月日", SqlDbType.DateTime);
            sqlparams[0].Value = myDate;

            myDbCon.SetParameter(sqlparams);

            try
            {
                myDbCon.execSqlCommand(mySqlCommand);
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }

            return;
        }

    }
}
