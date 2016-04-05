using System;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// PaymentInstallmentPlan（分割払い） の概要の説明です。
	/// </summary>
	public class PaymentInstallmentPlan : MethodPayment
	{
		private int  PaymentTimes			= 0;	// 支払回数

		public PaymentInstallmentPlan()
		{
			// 特になし
		}
		public PaymentInstallmentPlan( int myPlanTimes )
		{
			PaymentTimes = myPlanTimes;
		}
		public PaymentInstallmentPlan( LoanDetailData myLoanDetailData, int myPlanTimes )
		{
			this.Detail_Date		= myLoanDetailData.DealDate;
			this.Detail_DealingCode	= myLoanDetailData.LoanDealingCode;
			this.Detail_AccountCode	= myLoanDetailData.AccountCode;
			this.Detail_Amount		= myLoanDetailData.Amount;
			this.Detail_Summary		= myLoanDetailData.Summury;

			PaymentTimes = myPlanTimes;
		}
		public PaymentInstallmentPlan( LoanDetailData myLoanDetailData )
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

			try
			{
				long myTotalPaymentAmount	= 0;	// 支払合計額（支払確定へ登録する金額）

				//////////////////////////////////////////
				// 既に支払開始済の分割払いの金額を算出 //
				//////////////////////////////////////////
				//LoanDetailDatas myDatas = new LoanDetailDatas();
				//LoanDetailData	myData;
                List<LoanDetailData> listData = this.DatabaseDetailGetPayment(myLoanInfo.DealingCode, DETAILGET_MODE_PAID, myDbCon);

				// OBJ借入明細の支払を計算
                foreach (LoanDetailData data in listData)
                {
                    long myPaymentAmount = data.Amount / PaymentTimes;

                    myTotalPaymentAmount += myPaymentAmount;
                }

				//////////////////////////////
				// 新規分割払いの金額を算出 //
				//////////////////////////////
				// OBJ借入明細の利息を計算
                foreach (PaymentData data in myListData)
                {
                    if (!data.CreditCode.Equals(myLoanInfo.DealingCode))
                        continue;

                    if (data.PaymentAmount == 0)
                    {
                        // 明細の１件分の支払金額を算出
                        //   ※ 分割払いで出た端数を付加
                        long myPaymentAmount = data.Amount / PaymentTimes;
                        // 端数を算出
                        long myFractionAmount = data.Amount - (myPaymentAmount * PaymentTimes);
                        myPaymentAmount += myFractionAmount;

                        myTotalPaymentAmount += myPaymentAmount;
                    }
                }

				//////////////////////////////////
				// 支払確定へ登録：支払予定金額 //
				//////////////////////////////////
				// 支払確定に登録用のＩ／Ｆ：OBJ金銭帳入力へ設定
				MoneyInputData indata = new MoneyInputData();

				indata.Date			= myLoanInfo.PaymentDate;
				indata.DebitCode	= myLoanInfo.DealingCode;			// 借方←借入取引コード
				indata.CreditCode	= myLoanInfo.PaymentAccountCode;	// 貸方←支払先科目コード
				indata.Amount		= myTotalPaymentAmount;
				indata.Remark		= "";								// 摘要


				// 支払確定へ登録
				if ( myTotalPaymentAmount > 0 )
                    this.DatabaseDecisionInsert(myLoanInfo.LoanCode, indata, myDbCon);

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
			DbConnection myDbCon;

			long myTotalPaymentAmount	= 0;	// 支払合計額（支払確定へ登録する金額）

			try
			{
				// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
				if ( argDbCon != null )
					myDbCon = argDbCon;
				else
					myDbCon = new DbConnection();

				//////////////////////////
				// 分割払いの金額を算出 //
				//////////////////////////
				//LoanDetailDatas myDatas = new LoanDetailDatas();
				//LoanDetailData	myData;
                List<LoanDetailData> listData = new List<LoanDetailData>();
                List<LoanDetailData>  listPayment = this.DatabaseDetailGetPayment(Detail_DealingCode, DETAILGET_MODE_PAID, myDbCon);
				List<LoanDetailData> listDecision = this.DatabaseDetailGetDecision( Detail_DealingCode, myDbCon );

                foreach (LoanDetailData data in listPayment)
                    listData.Add(data);
                foreach (LoanDetailData data in listDecision)
                    listData.Add(data);


				// OBJ借入明細の支払を計算
				long myPaymentAmount = 0;
				long myFractionAmount = 0;
				long[]	arrOneTotalAmount	= new long[50];	// １件の支払合計金額
				long[]	arrOneAmount		= new long[50];	// １回分の支払金額
                int idx = 0;
                foreach(LoanDetailData data in listData)
                {
                    // 既に支払開始済の分割払いの金額を算出
                    if (data.PaymentAmount > 0)
                    {
                        // 明細の１件分の支払金額を算出
                        //   ※ 分割払いで出た端数は新規追加時に付加する為、ここでは
                        //      切り捨てた金額をインクリメントする
                        myPaymentAmount = data.Amount / PaymentTimes;

                        myTotalPaymentAmount += myPaymentAmount;
                    }
                    // 確定された分割払いの金額を算出
                    else
                    {
                        // 明細の１件分の支払金額を算出
                        myPaymentAmount = data.Amount / PaymentTimes;

                        // 端数を算出
                        myFractionAmount = data.Amount - (myPaymentAmount * PaymentTimes);
                        myPaymentAmount += myFractionAmount;
                    }
                    arrOneTotalAmount[idx] = data.PaymentAmount + myPaymentAmount;
                    arrOneAmount[idx] = myPaymentAmount;
                    myTotalPaymentAmount += myPaymentAmount;
                    idx++;
                }

                //////////////////////////////////////////
				// 借入明細関連のテーブルのメンテナンス	//
				//////////////////////////////////////////
				// データベースへの反映
                idx = 0;
                foreach(LoanDetailData data in listData)
                {
                    data.PaymentAmount = arrOneTotalAmount[idx];

                    // 支払金額によりTBL借入明細へ更新、削除
                    if (data.Amount == data.PaymentAmount)
                        // 借入明細から削除
                        this.DatabaseDetailDelete(data.Id, myDbCon);
                    else
                        // 借入明細を更新
                        this.DatabaseDetailUpdatePaymentAmount(data, myDbCon);

                    // 借入明細履歴へ挿入
                    data.Amount = arrOneAmount[idx];
                    this.DatabaseHistoryInsert(data, myDbCon);

                    // 支払予定から削除
                    this.DatabaseDecisionDelete(data.Id, myDbCon);

                    idx++;
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
