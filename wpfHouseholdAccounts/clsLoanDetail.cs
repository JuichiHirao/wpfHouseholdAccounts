using System;
using System.Collections;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// clsLoanDetail の概要の説明です。
	/// コレクション：借入明細：確定画面とのＩ／Ｆ
	/// </summary>
	public class LoanDetailDatas : CollectionBase
	{
		public DateTime	PaymentDate			= new DateTime();
		public string	LoanCode			= "";	// 借入コード
		public string	DealingCode			= "";	// 借入取引コード
		public double	PaymentRate			= 0.0;	// 支払利率
		public string	PaymentAccountCode	= "";	// 支払先科目コード
		public string	RateAccountCode		= "";	// 利率科目コード
		public long		NormalPaymentAmount	= 0;	// 通常月支払額
		public long		BonusSPaymentAmount	= 0;	// ボーナス月支払額（Summer）
		public long		BonusWPaymentAmount	= 0;	// ボーナス月支払額（Winter）
		public int		BonusSummerMonth	= 0;	// ボーナス加算月（Summer）
		public int		BonusWinterMonth	= 0;	// ボーナス加算月（Winter）
		public int		PaymentDay			= 0;	// 支払日
		public int		PlanTimes			= 0;	// 分割回数

		public int AddInputData( LoanDetailData myInputData )
		{
			int ResultAdd = 0;

			if ( myInputData.LoanDealingCode.Length > 0 )
				ResultAdd = InnerList.Add( myInputData );

			return ResultAdd;
		}
		public LoanDetailData GetInputData( int myIndex )
		{
			return (LoanDetailData)InnerList[myIndex];
		}
		public long CalcTotal()
		{
			long myTotalAmount = 0;

			for( int iIndex = 0; iIndex < InnerList.Count; iIndex++ )
			{
				LoanDetailData myInData = (LoanDetailData)InnerList[iIndex];

				myTotalAmount = myTotalAmount + myInData.Amount;
			}

			return myTotalAmount;
		}

	}
	public class LoanDetailData
	{
		public long		Id { get; set; }	            // 明細ＩＤ
		public DateTime DealDate { get; set; }	        // 年月日
        public string LoanDealingCode { get; set; }	// 借入取引コード
        public string LoanDealingName { get; set; }	// 借入取引コード
        public string AccountCode { get; set; }		// 取引科目コード
        public string AccountName { get; set; }		// 取引科目コード
		public long		Amount { get; set; }			// 金額
		public string	Summury { get; set; }			// 摘要
		public long		PaymentAmount { get; set; }		// 支払金額
	}
}
