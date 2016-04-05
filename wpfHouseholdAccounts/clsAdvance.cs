using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// 立替処理 の概要の説明です。
	/// </summary>
	public class Advance
	{
		AdvanceAccount[] ArrAdvanceAccount = new AdvanceAccount[40];
		public int	AdvanceCount	= 0;	// 配列に格納された件数
		public bool	boolCheckFlag	= false;	// DatabaseKeepAccurateCheckで使用
		DbConnection objDbCon = new DbConnection();

		public const string DBTBL_MASTER_NAME		= "立替";
		public const string DBTBL_DETAIL_NAME		= "立替明細";
		public const string DBTBL_HISTORY_NAME		= "立替明細履歴";
		public const string DBTBL_TEMPDETAIL_NAME	= "立替一時明細";
		public const string	ADVANCE_CODERANGE_FROM	= "1031";	// 立替コード有効範囲（FROM）
		public const string	ADVANCE_CODERANGE_TO	= "1049";	// 立替コード有効範囲（TO）


		public Advance()
		{
			// 
			// TODO: コンストラクタ ロジックをここに追加してください。
			//
			// 立替（マスタ）配列の初期化
			for( int iArrayIndex = 0; iArrayIndex < 40; iArrayIndex++ )
			{
				ArrAdvanceAccount[iArrayIndex] = new AdvanceAccount();
			}

			// データベースのTBL立替を配列へ格納
			DatabaseAdvanceSetArray();
		}
		public MoneyNowData GetNowAssetDept()
		{
			MoneyNowData nowdata = new MoneyNowData();

			string mySqlCommand = "";

			mySqlCommand = "SELECT SUM(金額) FROM " + Advance.DBTBL_DETAIL_NAME + " ";

			long TotalAmount = objDbCon.getAmountSql( mySqlCommand );

			nowdata.Code		= Account.CODE_ADVANCE;
			nowdata.NowAmount	= TotalAmount;

			return nowdata;

		}
		public string GetUsedAdvanceCode()
		{
			int CodeRangeFrom	= Convert.ToInt32(ADVANCE_CODERANGE_FROM);
			int CodeRangeTo		= Convert.ToInt32(ADVANCE_CODERANGE_TO);
			int	iAdvanceCode	= 0;

			string	WorkCode	= "";
			string	AdvanceCode	= "";

			// 立替が何もない場合はADVANCE_CODERANGE_FROMをリターンする
			if ( AdvanceCount == 0 )
				return ADVANCE_CODERANGE_FROM;

			// 範囲（TO）まで既に使用している場合は空きコードを取得する
			if (CodeRangeTo == Convert.ToInt32(ArrAdvanceAccount[AdvanceCount-1].AdvanceCode))
			{
				// 空きコードの取得
				for ( int iCode=CodeRangeFrom; iCode<=CodeRangeTo; iCode++ )
				{
					int iAdvCode, iIndex;
					for ( iAdvCode=CodeRangeFrom, iIndex=0; iAdvCode<=Convert.ToInt32(ADVANCE_CODERANGE_TO); iAdvCode++, iIndex++ )
					{
						WorkCode = Convert.ToString(iCode);
						if ( WorkCode == ArrAdvanceAccount[iIndex].AdvanceCode )
							break;
					}
					if ( WorkCode == ArrAdvanceAccount[iIndex].AdvanceCode )
					{
						// 直前のforループbreak時の判断と同一の場合は再度空きコード検索
						continue;
					}
					else
					{
						// 一致していない場合は空きコードと判断
						AdvanceCode = Convert.ToString(iCode);
						break;
					}
				}
				if ( AdvanceCode == "" )
					throw new BussinessException( "登録に有効な立替コードがありません" );
			}
			else
			{
				// 最後の立替コードにプラス１
				iAdvanceCode	= Convert.ToInt32(ArrAdvanceAccount[AdvanceCount-1].AdvanceCode);
				AdvanceCode		= Convert.ToString(++iAdvanceCode);
			}

			return AdvanceCode;

		}
		public void DatabaseAdvanceTempDetailSwitch( DateTime myReceiveDate, string myAdvanceCode, DbConnection argDbCon )
		{
			// 受取予定日と立替コードが一致するデータを一時明細から明細に移す
			DbConnection myDbCon;
			string	mySqlCommand	= "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO " + Advance.DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "( 年月日, 立替コード, 金額, 摘要 ) ";
			mySqlCommand = mySqlCommand + "SELECT 受取予定日, 立替コード, 金額, 摘要 ";
			mySqlCommand = mySqlCommand + "       '" + myReceiveDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "    FROM " + Advance.DBTBL_TEMPDETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "    WHERE 受取予定日 = '" + myReceiveDate.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "        AND 立替コード = '" + myAdvanceCode.Trim() + "' ";

			try
			{
				myDbCon.execSqlCommand( mySqlCommand );
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				objDbCon.closeConnection();
			}

			return;

		}
		public bool DatabaseKeepAccurateCheck( MoneyInputData myData, long myInputAmount, DbConnection argDbCon )
		{
			// 金銭帳で精算された金額と精算日の合計が一致する事の確認
			//		リターン [TRUE]	後処理を行う（履歴への移行、明細からの削除等）
			//               [FALSE]後処理は行わない

			DbConnection myDbCon;

			// 既にチェック済みの場合は処理は行わない
			if ( boolCheckFlag == true )
				return false;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string	mySqlCommand	= "";

			/////////////////////////////////////////////////
			// TBL立替明細の受取予定日の金額合計を算出する //
			/////////////////////////////////////////////////
			mySqlCommand = "SELECT SUM(金額) ";
			mySqlCommand = mySqlCommand + "FROM " + DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "WHERE 受取予定日 = '" + myData.Date + "' ";
			mySqlCommand = mySqlCommand + "    AND 立替コード = '" + myData.CreditCode + "' ";

			try
			{
				long mySummary = objDbCon.getAmountSql( mySqlCommand );

				// 受取予定日の合計が入力された立替金と違う場合
				if ( mySummary != myInputAmount )
				{
					// 違う場合は一時明細を金銭帳の金額合計に足しこむ（同受取予定日, 同立替コード）
					mySqlCommand = "SELECT SUM(金額) ";
					mySqlCommand = mySqlCommand + "FROM " + Advance.DBTBL_TEMPDETAIL_NAME + " ";
					mySqlCommand = mySqlCommand + "WHERE 受取予定日 = '" + myData.Date + "' ";
					mySqlCommand = mySqlCommand + "    AND 立替コード = '" + myData.CreditCode + "' ";

					myInputAmount += objDbCon.getAmountSql( mySqlCommand );

					// 足しこんでも違う場合はエラーにする
					if ( mySummary != myInputAmount )
					{
						string ErrMessage = "入力された立替金は" + String.Format("{0, 12:C} ", myInputAmount) + "です。\n"
							+ "立替として精算される" + String.Format("{0, 12:C} ", mySummary) + "の金額と不一致です\n";
						throw new BussinessException(ErrMessage);
					}
					// 一時明細を明細へ移す
					this.DatabaseAdvanceTempDetailSwitch( myData.Date, myData.CreditCode, myDbCon );
				}
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				objDbCon.closeConnection();
			}
			boolCheckFlag = true;

			return boolCheckFlag;
		}
        public string GetReceivableCode(string myAdvanceCode)
        {
            for (int IndexArr = 0; ArrAdvanceAccount[IndexArr].AdvanceCode.Length >= 0; IndexArr++)
            {
                if (ArrAdvanceAccount[IndexArr].AdvanceCode.Equals(myAdvanceCode))
                    return ArrAdvanceAccount[IndexArr].ReceivableCode;
            }

            return "";
        }
		/// <summary>
		/// 立替の配列を設定する
		/// </summary>
		private void DatabaseAdvanceSetArray()
		{
			DbConnection myDbCon = objDbCon;

			string	mySqlCommand	= "";
			int		iArrayIndex		= 0;

			///////////////////////////////////////
			// MSTBL預金のデータを配列へ設定する //
			///////////////////////////////////////
			mySqlCommand = "SELECT 立替コード, 立替先名, 受取コード ";
			mySqlCommand = mySqlCommand + "FROM " + DBTBL_MASTER_NAME + " ";
			mySqlCommand = mySqlCommand + "ORDER BY 立替コード";

			SqlCommand		myCommand;
			SqlDataReader	myReader;

			try
			{
				myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				myReader = myCommand.ExecuteReader();

				for( iArrayIndex = 0; iArrayIndex < 40; iArrayIndex++ )
				{
					// 次のレコードがない場合はループを抜ける
					if (!myReader.Read())
						break;

					// データベースのレコードを配列へ設定
					ArrAdvanceAccount[iArrayIndex].AdvanceCode	= myReader.GetString( 0 );
					ArrAdvanceAccount[iArrayIndex].AdvanceName	= myReader.GetString( 1 );
					ArrAdvanceAccount[iArrayIndex].ReceivableCode	= myReader.GetString( 2 );
				}
				AdvanceCount = iArrayIndex;

				myReader.Close();

/*
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
 */

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
		public long DatabaseAdvanceDetailCheck( string myAdvCode, DbConnection argDbCon )
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "SELECT COUNT(*) FROM " + Advance.DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "WHERE 立替コード = '" + myAdvCode + "' ";

			long myAdvCodeCount = myDbCon.getSqlCommandRow( mySqlCommand );

			return myAdvCodeCount;


		}
		public long DatabaseAdvanceDetailCheck( string myAdvCode )
		{
			DbConnection myDbCon = new DbConnection();
			string mySqlCommand = "";

			mySqlCommand = "SELECT COUNT(*) FROM " + Advance.DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "WHERE 立替コード = '" + myAdvCode + "' ";

			long myAdvCodeCount = myDbCon.getSqlCommandRow( mySqlCommand );

			return myAdvCodeCount;


		}
		public void DatabaseAdvanceInsert( AdvanceAccount myData, DbConnection argDbCon )
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO " + Advance.DBTBL_MASTER_NAME + " ";
			mySqlCommand = mySqlCommand + "( 立替コード, 立替名, 受取コード ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + "    ,'"	+ myData.AdvanceCode	+ "' ";			// 立替コード
			mySqlCommand = mySqlCommand + "    ,"	+ myData.AdvanceName	+ " ";			// 立替名
			mySqlCommand = mySqlCommand + "    ,'"	+ myData.ReceivableCode	+ "' ";			// 受取コード
			mySqlCommand = mySqlCommand + ") ";

			myDbCon.execSqlCommand( mySqlCommand );

			return;
		}
		public void DatabaseAdvanceDetailInsert( MoneyInputData myInData, DbConnection argDbCon )
		{
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO " + Advance.DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "( 年月日, 立替コード, 金額, 摘要 ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + "     '" + myInData.Date.ToShortDateString() + "' ";	// 年月日
			mySqlCommand = mySqlCommand + "    ,'" + myInData.DebitCode + "' ";					// 立替コード
			mySqlCommand = mySqlCommand + "    ," + myInData.Amount + " ";					// 金額
			mySqlCommand = mySqlCommand + "    ,'" + myInData.Remark + "' ";					// 摘要
			mySqlCommand = mySqlCommand + ") ";

			myDbCon.execSqlCommand( mySqlCommand );

			return;
		}
		public static void DatabaseAdvanceTempDetailInsert( MoneyInputData myInData, DbConnection argDbCon )
		{
			// 立替明細から受取予定日に一致するデータを一時明細へ挿入する
			//   金銭帳入力：貸方の場合
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO " + Advance.DBTBL_TEMPDETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "( 立替コード, 金額, 摘要, 受取予定日 ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + "     '" + myInData.DebitCode	+ "' ";		// 立替コード
			mySqlCommand = mySqlCommand + "    ,"  + myInData.Amount	+ " ";		// 金額
            mySqlCommand = mySqlCommand + "    ,'" + myInData.Remark + "' ";		// 摘要
			mySqlCommand = mySqlCommand + "    ,'" + myInData.Date		+ "' ";		// 受取予定日
			mySqlCommand = mySqlCommand + ") ";

			myDbCon.execSqlCommand( mySqlCommand );

			return;
		}
		public void DatabaseAdvanceHistoryInsert( DateTime myDateTime, DbConnection argDbCon )
		{
			// 立替明細から受取予定日に一致するデータを履歴へ挿入する
			//   金銭帳入力：貸方の場合
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "INSERT INTO " + Advance.DBTBL_HISTORY_NAME + " ";
			mySqlCommand = mySqlCommand + "( 年月日, 立替コード, 金額, 摘要, 受取日 ) ";
			mySqlCommand = mySqlCommand + "SELECT 年月日, 立替コード, 金額, 摘要, ";
			mySqlCommand = mySqlCommand + "       '" + myDateTime.ToShortDateString() + "' ";
			mySqlCommand = mySqlCommand + "    FROM " + Advance.DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "    WHERE 受取予定日 = '" + myDateTime.ToShortDateString() + "' ";

			myDbCon.execSqlCommand( mySqlCommand );

			return;
		}
		public void DatabaseAdvanceDetailDelete( DateTime myDateTime, DbConnection argDbCon )
		{
			// 立替明細から受取予定日に一致するデータを削除する
			//   金銭帳入力：貸方の場合
			DbConnection myDbCon;
			string mySqlCommand = "";

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			mySqlCommand = "DELETE FROM " + Advance.DBTBL_DETAIL_NAME + " ";
			mySqlCommand = mySqlCommand + "    WHERE 受取予定日 = '" + myDateTime.ToShortDateString() + "' ";

			myDbCon.execSqlCommand( mySqlCommand );

			return;
		}
	}
	public class AdvanceAccount
	{
		public AdvanceAccount()
		{
			AdvanceCode = "";
			AdvanceName = "";
			ReceivableCode	= "";
		}
		public string	AdvanceCode;			// 立替・貸付コード
		public string	AdvanceName;			// 立替・貸付名
		public string	ReceivableCode;			// 受取コード
	}
	public class AdvanceDetailData
	{
		public AdvanceDetailData()
		{
			AdvanceCode = "";
			AdvanceName = "";
		}
		public string	AdvanceCode;	// 立替コード
		public string	AdvanceName;	// 立替名
		public string	Amount;			// 金額
		//public string	
	}
}
