using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// clsBankAccount の概要の説明です。
	/// </summary>
	public class BankAccount
	{
		BankAccountData[] ArrBankData = new BankAccountData[20];
		public int	BankCount = 0;	// 配列に格納された件数
		private DateTime	RegistDate	= new DateTime();

		//////////////////
		/// 定数：定義  //
		//////////////////
		// データベース
		public const string DBTBL_BANKACCOUNT	= "預金";
		public const string DBTBL_DETAIL		= "預金明細";

		public BankAccount()
		{
			// 
			// TODO: コンストラクタ ロジックをここに追加してください。
			//
			// 登録日の設定（引数が未指定）
			RegistDate = System.DateTime.Now;

			for( int iArrayIndex = 0; iArrayIndex < ArrBankData.Length; iArrayIndex++ )
			{
				ArrBankData[iArrayIndex] = new BankAccountData();
			}
			
			this.DatabaseDepositSetArray();

		}
		public BankAccount( DateTime myDateTime )
		{
			// 
			// TODO: コンストラクタ ロジックをここに追加してください。
			//
			// 登録日の設定
			RegistDate = myDateTime;

			for( int iArrayIndex = 0; iArrayIndex < ArrBankData.Length; iArrayIndex++ )
			{
				ArrBankData[iArrayIndex] = new BankAccountData();
			}
			
			this.DatabaseDepositSetArray();

		}
		public MoneyNowData GetNowAssetDept()
		{
			MoneyNowData nowdata = new MoneyNowData();
			long	TotalAmount	= 0;

			for ( int iArrIndex = 0; iArrIndex < BankCount; iArrIndex++ )
			{
				TotalAmount	= TotalAmount + ArrBankData[iArrIndex].BalanceAmount;
			}
			nowdata.Code		= Account.CODE_BANK;
			nowdata.Name		= "預金";
			nowdata.NowAmount	= TotalAmount;

			return nowdata;
		}
        public List<MoneyNowData> GetNowAssetDeptDetail(Account myAccount)
		{
            List<MoneyNowData> moneyalldata = new List<MoneyNowData>();

			for ( int iArrIndex = 0; iArrIndex < BankCount; iArrIndex++ )
			{
                if (myAccount != null)
                {
                    if (myAccount.getDisableFlag(ArrBankData[iArrIndex].BankAccountCode))
                        continue;
                }
				MoneyNowData nowdata = new MoneyNowData();

				nowdata.Code		= ArrBankData[iArrIndex].BankAccountCode;
				nowdata.Name		= ArrBankData[iArrIndex].BankAccountName;
				nowdata.NowAmount	= ArrBankData[iArrIndex].BalanceAmount;
                nowdata.AccountKind = Account.KIND_ASSETS_DEPOSIT;

                moneyalldata.Add(nowdata);
			}

            return moneyalldata;
		}
		/// <summary>
		/// 預金データの配列を設定する
		/// </summary>
		private void DatabaseDepositSetArray()
		{
			DbConnection myDbCon = new DbConnection();

			string mySqlCommand = "";

			///////////////////////////////////////
			// MSTBL預金のデータを配列へ設定する //
			///////////////////////////////////////
			mySqlCommand = "SELECT 預金コード, 預金名, 預金種類 ";
			mySqlCommand = mySqlCommand + "FROM " + BankAccount.DBTBL_BANKACCOUNT + " ";
			mySqlCommand = mySqlCommand + "ORDER BY 預金コード ";

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

			try
			{
				myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				myReader = myCommand.ExecuteReader();

				int iArrayIndex;
				for( iArrayIndex = 0; iArrayIndex < ArrBankData.Length; iArrayIndex++ )
				{
					// 次のレコードがない場合はループを抜ける
					if (!myReader.Read())
						break;

					// データベースのレコードを配列へ設定
					ArrBankData[iArrayIndex].BankAccountCode = myReader.GetString( 0 );
					ArrBankData[iArrayIndex].BankAccountName = myReader.GetString( 1 );
					ArrBankData[iArrayIndex].BankAccountKind = myReader.GetString( 2 );
				}
				BankCount = iArrayIndex + 1;

				myReader.Close();

				///////////////////////////////////////////////
				// TBL預金明細の金額と登録日を配列へ設定する //
				///////////////////////////////////////////////
				for( iArrayIndex = 0; BankCount > iArrayIndex; iArrayIndex++ )
				{
					mySqlCommand = "SELECT 残高, 登録日 ";
					mySqlCommand = mySqlCommand + "FROM " + BankAccount.DBTBL_DETAIL + " ";
					mySqlCommand = mySqlCommand + "WHERE 登録日 = ( SELECT MAX(登録日) FROM " + BankAccount.DBTBL_DETAIL + " ";
					mySqlCommand = mySqlCommand + "                     WHERE 預金コード =  '" + ArrBankData[iArrayIndex].BankAccountCode + "') ";
					mySqlCommand = mySqlCommand + "    AND 預金コード =  '" + ArrBankData[iArrayIndex].BankAccountCode + "' ";

					myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

					myReader = myCommand.ExecuteReader();

					// レコードがない場合は次の配列を設定する
					if (!myReader.Read())
					{
						myReader.Close();
						continue;
					}

					// データベースのレコードを配列へ設定
					mySqlMoney = myReader.GetSqlMoney( 0 );
					ArrBankData[iArrayIndex].BalanceAmount = mySqlMoney.ToInt64();
					ArrBankData[iArrayIndex].RegistDate = myReader.GetDateTime( 1 );

					myReader.Close();
				}

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

			return;
		}
		public void DatabaseRefrect( DbConnection argDbCon )
		{
			DbConnection myDbCon;

			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

            myDbCon.SetParameter(null);

            // データベースに反映する
			int iArrayIndex;
			for( iArrayIndex = 0; BankCount > iArrayIndex; iArrayIndex++ )
			{
				if ( ArrBankData[iArrayIndex].DebitAmount > 0 || ArrBankData[iArrayIndex].CreditAmount > 0 )
				{
					long myBalanceAmount = ArrBankData[iArrayIndex].BalanceAmount + ArrBankData[iArrayIndex].DebitAmount - ArrBankData[iArrayIndex].CreditAmount;

					// 残高が０未満になる場合はエラーにする
					if ( myBalanceAmount < 0 )
					{
						string ErrMessage = "預金：" + ArrBankData[iArrayIndex].BankAccountName + "の金額が０未満になります";
						throw new BussinessException(ErrMessage);
					}

                    mySqlCommand = "INSERT INTO 預金明細 ";
                    mySqlCommand = mySqlCommand + "( 預金コード, 借方金額, 貸方金額, 残高, 登録日 ) ";
                    mySqlCommand = mySqlCommand + "VALUES( @預金コード, @借方金額, @貸方金額, @残高, @登録日 ) ";

                    SqlParameter[] sqlparams = new SqlParameter[5];

                    sqlparams[0] = new SqlParameter("@預金コード", SqlDbType.VarChar);
                    sqlparams[0].Value = ArrBankData[iArrayIndex].BankAccountCode;
                    sqlparams[1] = new SqlParameter("@借方金額", SqlDbType.Int);
                    sqlparams[1].Value = ArrBankData[iArrayIndex].DebitAmount;
                    sqlparams[2] = new SqlParameter("@貸方金額", SqlDbType.Int);
                    sqlparams[2].Value = ArrBankData[iArrayIndex].CreditAmount;
                    sqlparams[3] = new SqlParameter("@残高", SqlDbType.Int);
                    sqlparams[3].Value = myBalanceAmount;
                    sqlparams[4] = new SqlParameter("@登録日", SqlDbType.DateTime);
                    sqlparams[4].Value = RegistDate.ToShortDateString();
                    myDbCon.SetParameter(sqlparams);

					myDbCon.execSqlCommand( mySqlCommand );
				}

			}
		}

        public static void DbDeleteFromDate(DateTime myDate, DbConnection argDbCon)
        {
            DbConnection myDbCon;

            string mySqlCommand = "";

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            myDbCon.SetParameter(null);

            mySqlCommand = "DELETE FROM 預金明細 ";
            mySqlCommand = mySqlCommand + "WHERE 登録日 = @登録日 ";

            SqlParameter[] sqlparams = new SqlParameter[1];

            sqlparams[0] = new SqlParameter("@登録日", SqlDbType.DateTime);
            sqlparams[0].Value = myDate;
            myDbCon.SetParameter(sqlparams);

            myDbCon.execSqlCommand(mySqlCommand);
        }

		public void Deposit( string myCode, long DepoAmount )
		{
			// 預け入れる
			int iArrayIndex;
			for( iArrayIndex = 0; iArrayIndex < BankCount; iArrayIndex++ )
			{
				if ( ArrBankData[iArrayIndex].BankAccountCode == myCode )
					break;
			}
			// 一致する預金コードが存在しない場合はエラーにする
			if ( iArrayIndex == BankCount )
			{
				string ErrMessage = "預金コードとして設定された" + myCode + "は\n現在の預金コードには登録されていません";
				throw new BussinessException(ErrMessage);
			}
			// OBJ預金データの借方金額に足し込む
			ArrBankData[iArrayIndex].DebitAmount = ArrBankData[iArrayIndex].DebitAmount + DepoAmount;

			return;
		}
		public void Draw( string myCode, long DrawAmount )
		{
			// 引き出す
			int iArrayIndex;
			for( iArrayIndex = 0; iArrayIndex < BankCount; iArrayIndex++ )
			{
				if ( ArrBankData[iArrayIndex].BankAccountCode == myCode )
					break;
			}
			// 一致する預金コードが存在しない場合はエラーにする
			if ( iArrayIndex == BankCount )
			{
				string ErrMessage = "預金コードとして設定された" + myCode + "は\n現在の預金コードには登録されていません";
				throw new BussinessException(ErrMessage);
			}
			// 預金金額が０未満になる場合はエラーにする
			//   残高 ＝ 残高 ＋ 借方金額 － 貸方金額
		// 行の順番によっては一時的に０未満になる事もありえるのでコメントにする
			/*
			long myBalanceAmount = ArrBankData[iArrayIndex].BalanceAmount + ArrBankData[iArrayIndex].DebitAmount - ArrBankData[iArrayIndex].CreditAmount;
			if ( DrawAmount > myBalanceAmount )
			{
				string ErrMessage = "預金：" + ArrBankData[iArrayIndex].BankAccountName + "の金額が０未満になります";
				throw new BussinessException(ErrMessage);
			}
			 */
			// OBJ預金データの貸方金額に足し込む
			ArrBankData[iArrayIndex].CreditAmount = ArrBankData[iArrayIndex].CreditAmount + DrawAmount;

			return;
		}
	}
	public class BankAccountData
	{
		public BankAccountData()
		{
			BankAccountCode = "";
			BankAccountName = "";
		}
		public string	BankAccountCode;	// 預金コード
		public string	BankAccountName;	// 預金名
		public string	BankAccountKind;	// 預金種類
		public long		BalanceAmount;		// 残高
		public DateTime	RegistDate;			// 登録日
		public long		DebitAmount;		// 借方金額
		public long		CreditAmount;		// 貸方金額
	}
}