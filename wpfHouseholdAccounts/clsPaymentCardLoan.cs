using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// PaymentCardLoan の概要の説明です。
	/// </summary>
	public class PaymentCardLoan : MethodPayment
	{
		public PaymentCardLoan()
		{
			// 特になし
		}
		public PaymentCardLoan( LoanDetailData myLoanDetailData )
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
                long myBalanceAmount = this.DatabaseTotalGetAmount(myLoanInfo.DealingCode, myDbCon);
				// 最新の金額データを合計額を足し込む
                myBalanceAmount = myBalanceAmount + total;

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
                if (myBalanceAmount < loanPaymentAmount)
                    paymentAmount = myBalanceAmount;
				else
                    paymentAmount = loanPaymentAmount;

				// 借入集計に登録する登録日を現在日付から取得
				DateTime		myDateTimeRegist = DateTime.Now;
				//LoanDetailData	myData;

				//////////////////////////////////////////////
				// 支払確定へ登録する利息コード、金額を算出 //
				//////////////////////////////////////////////
				// １．利息計上の日は借入先で指定された日を設定する
				//   　例）毎月１０日の場合は支払日が月曜の１１日
				//     　  だが利息計算の対象となるのは１０を設定
				// ２．算出する日付を最後の支払日ではなく前回のカードローン支払日にする

				DateTime myPaymentDate;						// 支払日
				DateTime myCalcBaseDate	= new DateTime();	// 利息計算対象開始日

				decimal	myInterest				= 0;	// 借入集計の利息
				decimal	myInterestDetail		= 0;	// 借入明細の利息
				decimal	myInterestTotalDetail	= 0;	// 借入明細の合計利息
				// 借入集計の利息を計算
                if (myLoanInfo.PaymentDay > 0 && myLoanInfo.PaymentDay < 32)
				{
					// 支払日の算出
                    myPaymentDate = new DateTime(myLoanInfo.PaymentDate.Year
                        , myLoanInfo.PaymentDate.Month
                        , myLoanInfo.PaymentDay
						);
					// 利息計算対象開始日の算出（支払日の前月＋１日）
					myCalcBaseDate	= myPaymentDate.AddMonths(-1);
					//myCalcBaseDate	= myCalcBaseDate.AddDays(1);
				}
				else
				{
					// 支払日の算出
                    myPaymentDate = myLoanInfo.PaymentDate;
					// 利率対象開始日の算出（支払日の前月＋１日）
					myCalcBaseDate.AddMonths(-1);
					//myCalcBaseDate	= myCalcBaseDate.AddDays(1);
				}

				// OBJ借入明細の利息を計算
                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    myInterestDetail = this.CalcratePayInterest(data.Amount
                        , myLoanInfo.PaymentRate
                        , data.InputDate
						, myPaymentDate );
					myInterestTotalDetail += myInterestDetail;

                }

				// 計算対象の日付を取得
				// 　前回の支払いがない場合
				// ※ ＤＢへは検索だが、利息金額算出と別にコネクションを作成して検索すると
				//    トランザクションの確定待ちになってしまう（デッドロック）
				//    その為、検索だがトランザクション内で行なう
                myInterest = this.CalcratePayInterest(myLoanInfo.DealingCode
                    , myLoanInfo.PaymentRate	// 支払利息
					, myCalcBaseDate			// 計算開始基準日
					, myPaymentDate				// 支払日
					, myDbCon );				// OBJデータベース

				// 利息合計額 ＝ 利息合計額 ＋ 借入明細利息
				myInterest += myInterestTotalDetail;
				//myInterest += myInterestDetail;

				//////////////////////////////////
				// 支払確定へ登録：支払予定金額 //
				//////////////////////////////////
				// 支払確定に登録用のＩ／Ｆ：OBJ金銭帳入力へ設定
				MoneyInputData indata = new MoneyInputData();

				indata.Date			= myLoanInfo.PaymentDate;
				indata.DebitCode	= myLoanInfo.DealingCode;			// 借方←借入取引コード
				indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
				indata.Amount		= paymentAmount;
                indata.Remark       = "";								// 摘要

				// 支払確定へ登録
                if (paymentAmount > 0)
                    this.DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);

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
					indata.Amount		= Convert.ToInt64(myInterest);
                    indata.Remark       = "";								// 摘要

					// 支払確定へ登録
                    DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);
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

				////////////////////////////////////////////////
				// 借入集計から入力金額を加算、または減算する //
				////////////////////////////////////////////////
				// 加算の金額を算出
				mySqlCommand = "SELECT SUM( 明細.金額 ) AS 合計額 ";
				mySqlCommand = mySqlCommand + "FROM ";
				mySqlCommand = mySqlCommand + "( ";
				mySqlCommand = mySqlCommand + " SELECT 金額 ";
				mySqlCommand = mySqlCommand + "    FROM " + MethodPayment.DBTBL_PAYMENTSCHEDULE + " ";
				mySqlCommand = mySqlCommand + "      INNER JOIN " + MethodPayment.DBTBL_DETAIL + " ";
				mySqlCommand = mySqlCommand + "      ON 支払予定.明細ＩＤ = 借入明細.明細ＩＤ ";
				mySqlCommand = mySqlCommand + "    WHERE 支払日 = '" + Detail_Date.ToShortDateString() + "' ";
				mySqlCommand = mySqlCommand + "        AND 借入取引コード = '" + Detail_DealingCode + "' ";
				mySqlCommand = mySqlCommand + ") AS 明細 ";

				long myTotalAmount = myDbCon.getAmountSql( mySqlCommand );

				// 支払金額と明細からの合計金額の大きい方で１件だけ集計にＲｏｗ作成する
				DateTime myDateTime = new DateTime();
				if ( Detail_Amount < myTotalAmount )
				{
					myTotalAmount -= Detail_Amount;
					// 合計額を借入集計に加算
					this.DatabaseTotalAdditional( Detail_DealingCode, Detail_Date, myTotalAmount, myDateTime, myDbCon );
				}
				else
				{
					Detail_Amount -= myTotalAmount;
					// 合計額を借入集計に減算
					this.DatabaseTotalSubtraction( Detail_DealingCode, Detail_Date, Detail_Amount, myDateTime, myDbCon );
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


				//////////////////////////////////////////
				// 借入明細関連のテーブルのメンテナンス	//
				//////////////////////////////////////////
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
}
