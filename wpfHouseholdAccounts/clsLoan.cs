using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Data;
using System.Collections;

namespace wpfHouseholdAccounts
{
	/// <summary>
	/// clsLoan の概要の説明です。
	/// </summary>
	public class Loan
	{
		// 属性の設定
		MoneyInputData InputData;
		DbConnection dbCon;

        LoanTotalData[] arrLoanTotal = new LoanTotalData[20];

		// 借入ＩＤ
		private string	LoanCode		= "";	// 借入コード
		private string	DealingId		= "";	// 借入取引コード
		private string	DealingName		= "";	// 取引名
		private double	PaymentRate		= 0.0;	// 支払利率
		private string	PaymentKind		= "";	// 支払種類
		private string	PaymentAccountCode	= "";	// 支払先科目コード
		private string	RateAccountCode		= "";	// 利率科目コード
		private long	NormalPaymentAmount	= 0;	// 通常月支払額
		private long	BonusSPaymentAmount	= 0;	// ボーナス月支払額（Summer）
		private long	BonusWPaymentAmount	= 0;	// ボーナス月支払額（Winter）
		private int		BonusSummerMonth	= 0;	// ボーナス加算月（Summer）
		private int		BonusWinterMonth	= 0;	// ボーナス加算月（Winter）
		private int		PaymentDay			= 0;	// 支払日
		private int		PlanTimes			= 0;	// 分割回数

		// 締切日
		// 支払日
		// 備考

		//////////////////
		/// 定数：定義  //
		//////////////////
		// 科目マスタ：借入取引内容 Dealings
		public const string	DEALINGS_NORMAL		= "1";	// 通常払い 
		public const string	DEALINGS_RIVOLVING	= "2";	// リボルビング払い Revolving
		public const string	DEALINGS_CASHING	= "3";	// キャッシング
		public const string	DEALINGS_SLIDE		= "4";	// 残高スライド払い
		public const string	DEALINGS_CARDLOAN	= "5";	// カードローン
		public const string	DEALINGS_NO_RATEINR	= "6";	// 個人用利率なし Rate Interest
		public const string	DEALINGS_INSTPLAN	= "7";	// 分割払い

		// データベース
		public const string MSTTBL_DEALINGS	= "借入取引";
		public const string MSTTBL_CONTENT	= "借入取引内容";

		public Loan()
		{
			// 
			// TODO: コンストラクタ ロジックをここに追加してください。
			//
			dbCon = new DbConnection();

			for( int iArrayIndex = 0; iArrayIndex < 20; iArrayIndex++ )
			{
                arrLoanTotal[iArrayIndex] = new LoanTotalData();
			}

		}

        /// <summary>
        /// 金銭帳入力時の生成
        /// 　トランザクション不要のときはＤＢコネクションにnullを設定
        /// </summary>
        /// <param name="myInData"></param>
        public void Cancel(MakeupDetailData myData, Account myAccount, DbConnection myDbCon)
        {
            int paramCnt = 0;

            try
            {
                if (myDbCon == null)
                    myDbCon = new DbConnection();

                // OBJ支払方法へのインターフェースOBJ借入明細を設定
                LoanDetailData loandetail = new LoanDetailData();

                loandetail.DealDate = myData.Date;
                loandetail.Amount = myData.Amount;    // 金額
                loandetail.Summury = myData.Remark;	// 摘要
                //loandetail.PaymentAmount

                string creditcodekind = myAccount.getAccountKind(myData.CreditCode);

                // 貸方の場合
                if (creditcodekind.Equals(Account.KIND_DEPT_LOAN))
                {
                    LoanData loanInfo = this.GetDbLoanData(myData.CreditCode, myData.CreditCode);

                    loandetail.LoanDealingCode = myData.CreditCode;	// 借入取引コード
                    loandetail.AccountCode = myData.DebitCode;	// 取引科目コード

                    // 支払種類により生成するオブジェクト、使用するメソッドを設定
                    switch (loanInfo.PaymentKind)
                    {
                        // 通常払い
                        case Loan.DEALINGS_NORMAL:
                            PaymentNormal myPayNormal = new PaymentNormal(loandetail);
                            myPayNormal.Delete(myDbCon);
                            break;
                        // リボルビング払い
                        case Loan.DEALINGS_RIVOLVING:
                            PaymentRivolving myPayRivolving = new PaymentRivolving(loandetail);
                            myPayRivolving.Delete(myDbCon);
                            break;
                        // キャッシング
                        case Loan.DEALINGS_CASHING:
                            PaymentCashing myPayCashing = new PaymentCashing(loandetail);
                            myPayCashing.Regist(myDbCon);
                            break;
                        // カードローン
                        case Loan.DEALINGS_CARDLOAN:
                            PaymentCardLoan myPayCardLoan = new PaymentCardLoan(loandetail);
                            myPayCardLoan.Regist(myDbCon);
                            break;
                        // 残高スライド
                        case Loan.DEALINGS_SLIDE:
                            PaymentSlide myPaySlide = new PaymentSlide(loandetail);
                            myPaySlide.Regist(myDbCon);
                            break;
                        // 個人用利率無し
                        case Loan.DEALINGS_NO_RATEINR:
                            PaymentSlide myPayNoRateInr = new PaymentSlide(loandetail);
                            myPayNoRateInr.Regist(myDbCon);
                            break;
                        // 分割払い
                        case Loan.DEALINGS_INSTPLAN:	// 登録時に分割回数は不要
                            PaymentInstallmentPlan myPayInstallmentPlan = new PaymentInstallmentPlan(loandetail);
                            myPayInstallmentPlan.Regist(myDbCon);
                            break;
                        default:
                            break;
                    }
                }
                // 借方の場合
                else
                {
                    LoanData loanInfo = this.GetDbLoanData(myData.DebitCode, myData.DebitCode);

                    loandetail.LoanDealingCode = myData.DebitCode;	// 借入取引コード
                    loandetail.AccountCode = myData.CreditCode;	// 取引科目コード
                    // 支払種類により生成するオブジェクト、使用するメソッドを設定
                    switch (loanInfo.PaymentKind)
                    {
                        // 通常払い
                        case Loan.DEALINGS_NORMAL:
                            PaymentNormal myPayNormal = new PaymentNormal(loandetail);
                            myPayNormal.PayCancel(myDbCon);
                            break;
                        // リボルビング払い
                        case Loan.DEALINGS_RIVOLVING:
                            PaymentRivolving myPayRivolving = new PaymentRivolving(loandetail);
                            myPayRivolving.Pay(myDbCon);
                            break;
                        // キャッシング
                        case Loan.DEALINGS_CASHING:
                            PaymentCashing myPayCashing = new PaymentCashing(loandetail);
                            myPayCashing.Pay(myDbCon);
                            break;
                        // カードローン
                        case Loan.DEALINGS_CARDLOAN:
                            PaymentCardLoan myPayCardLoan = new PaymentCardLoan(loandetail);
                            myPayCardLoan.Pay(myDbCon);
                            break;
                        // 残高スライド
                        case Loan.DEALINGS_SLIDE:
                            PaymentSlide myPaySlide = new PaymentSlide(loandetail);
                            myPaySlide.Pay(myDbCon);
                            break;
                        // 個人用利率無し
                        case Loan.DEALINGS_NO_RATEINR:
                            PaymentSlide myPayNoRateInr = new PaymentSlide(loandetail);
                            myPayNoRateInr.Pay(myDbCon);
                            break;
                        // 分割払い
                        case Loan.DEALINGS_INSTPLAN:
                            PaymentInstallmentPlan myPayInstallmentPlan = new PaymentInstallmentPlan(loandetail, PlanTimes);
                            myPayInstallmentPlan.Pay(myDbCon);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (BussinessException errbsn)
            {
                throw errbsn;
            }
        }

        /// <summary>
		/// 金銭帳入力時の生成
		/// 　トランザクション不要のときはＤＢコネクションにnullを設定
		/// </summary>
		/// <param name="myInData"></param>
		public Loan(MoneyInputData myInData, Account myAccount, DbConnection argDbCon )
		{
			try
			{
				if ( argDbCon != null )
					dbCon = argDbCon;
				else
					dbCon = new DbConnection();

				// OBJ支払方法へのインターフェースOBJ借入明細を設定
				LoanDetailData loandetail = new LoanDetailData();
						
				loandetail.DealDate			= myInData.Date;
				loandetail.Amount			= myInData.Amount;		// 金額
				loandetail.Summury			= myInData.Remark;	// 摘要
				//loandetail.PaymentAmount

                string creditcodekind = myAccount.getAccountKind(myInData.CreditCode);

				// 貸方の場合
                if (creditcodekind.Equals(Account.KIND_DEPT_LOAN))
				{
                    LoanData loanInfo = this.GetDbLoanData(myInData.CreditCode, myInData.CreditCode);

                    loandetail.LoanDealingCode  = myInData.CreditCode;	// 借入取引コード
					loandetail.AccountCode		= myInData.DebitCode;	// 取引科目コード

					// 支払種類により生成するオブジェクト、使用するメソッドを設定
                    switch (loanInfo.PaymentKind)
					{
							// 通常払い
						case Loan.DEALINGS_NORMAL :
							PaymentNormal myPayNormal = new PaymentNormal(loandetail);
							myPayNormal.Regist( dbCon );
							break;
							// リボルビング払い
						case Loan.DEALINGS_RIVOLVING :
							PaymentRivolving myPayRivolving = new PaymentRivolving(loandetail);
							myPayRivolving.Regist( dbCon );
							break;
							// キャッシング
						case Loan.DEALINGS_CASHING :
							PaymentCashing myPayCashing = new PaymentCashing(loandetail);
							myPayCashing.Regist( dbCon );
							break;
							// カードローン
						case Loan.DEALINGS_CARDLOAN :
							PaymentCardLoan myPayCardLoan = new PaymentCardLoan(loandetail);
							myPayCardLoan.Regist( dbCon );
							break;
							// 残高スライド
						case Loan.DEALINGS_SLIDE :
							PaymentSlide myPaySlide = new PaymentSlide(loandetail);
							myPaySlide.Regist( dbCon );
							break;
						// 個人用利率無し
						case Loan.DEALINGS_NO_RATEINR :
							PaymentSlide myPayNoRateInr = new PaymentSlide(loandetail);
							myPayNoRateInr.Regist( dbCon );
							break;
						// 分割払い
						case Loan.DEALINGS_INSTPLAN :	// 登録時に分割回数は不要
							PaymentInstallmentPlan myPayInstallmentPlan = new PaymentInstallmentPlan(loandetail);
							myPayInstallmentPlan.Regist( dbCon );
							break;
						default:
							break;
					}
				}
				// 借方の場合
				else
				{
                    LoanData loanInfo = this.GetDbLoanData(myInData.DebitCode, myInData.DebitCode);

                    loandetail.LoanDealingCode  = myInData.DebitCode;	// 借入取引コード
					loandetail.AccountCode		= myInData.CreditCode;	// 取引科目コード
					// 支払種類により生成するオブジェクト、使用するメソッドを設定
                    switch (loanInfo.PaymentKind)
					{
						// 通常払い
						case Loan.DEALINGS_NORMAL :
							PaymentNormal myPayNormal = new PaymentNormal(loandetail);
							myPayNormal.Pay( dbCon );
							break;
						// リボルビング払い
						case Loan.DEALINGS_RIVOLVING :
							PaymentRivolving myPayRivolving = new PaymentRivolving(loandetail);
							myPayRivolving.Pay( dbCon );
							break;
						// キャッシング
						case Loan.DEALINGS_CASHING :
							PaymentCashing myPayCashing = new PaymentCashing(loandetail);
							myPayCashing.Pay( dbCon );
							break;
						// カードローン
						case Loan.DEALINGS_CARDLOAN :
							PaymentCardLoan myPayCardLoan = new PaymentCardLoan(loandetail);
							myPayCardLoan.Pay( dbCon );
							break;
						// 残高スライド
						case Loan.DEALINGS_SLIDE :
							PaymentSlide myPaySlide = new PaymentSlide(loandetail);
							myPaySlide.Pay( dbCon );
							break;
						// 個人用利率無し
						case Loan.DEALINGS_NO_RATEINR :
							PaymentSlide myPayNoRateInr = new PaymentSlide(loandetail);
							myPayNoRateInr.Pay( dbCon );
							break;
						// 分割払い
						case Loan.DEALINGS_INSTPLAN :
							PaymentInstallmentPlan myPayInstallmentPlan = new PaymentInstallmentPlan(loandetail, PlanTimes );
							myPayInstallmentPlan.Pay( dbCon );
							break;
						default:
							break;
					}
				}
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}
		}
        /// <summary>
        /// 借入確定時の生成
        /// </summary>
        /// <param name="myInData"></param>
        public Loan(string myLoanCode, DateTime myPaymentDate, List<PaymentData> myListData, DbConnection argDbCon)
        {
            DbConnection myDbCon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            try
            {
                PaymentData data = myListData[0];

                // 借入取引コードから取引内容の情報をデータベースから取得
                LoanData loanInfo = this.GetDbLoanData(data.CreditCode, data.CreditCode);
                loanInfo.PaymentDate = myPaymentDate;

                // 支払種類により生成するオブジェクト、使用するメソッドを設定
                switch (loanInfo.PaymentKind)
                {
                    // 通常払い
                    case Loan.DEALINGS_NORMAL:
                        PaymentNormal myPayNormal = new PaymentNormal();
                        myPayNormal.Decision(loanInfo, myListData, myDbCon);
                        break;
                    // リボルビング払い
                    case Loan.DEALINGS_RIVOLVING:
                        PaymentRivolving myPayRivolving = new PaymentRivolving();
                        myPayRivolving.Decision(loanInfo, myListData, myDbCon);
                        break;
                    case Loan.DEALINGS_CASHING:
                        PaymentCashing myPayCashing = new PaymentCashing();
                        myPayCashing.Decision(loanInfo, myListData, myDbCon);
                        break;
                    case Loan.DEALINGS_CARDLOAN:
                        PaymentCardLoan myPayCardLoan = new PaymentCardLoan();
                        myPayCardLoan.Decision(loanInfo, myListData, myDbCon);
                        break;
                    case Loan.DEALINGS_SLIDE:
                        // 支払確定では残高スライドを確定する事はない
                        break;
                    case Loan.DEALINGS_NO_RATEINR:
                        break;
                    // 分割払い
                    case Loan.DEALINGS_INSTPLAN:
                        PaymentInstallmentPlan myPayInstallmentPlan = new PaymentInstallmentPlan(PlanTimes);
                        myPayInstallmentPlan.Decision(loanInfo, myListData, myDbCon);
                        break;
                    default:
                        break;
                }
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }

        }
        
        /// <summary>
		/// 借入確定時の生成（借入集計にのみ存在するデータの確定）
        ///   新対応で引数が被ったので、仮にmyFlagを追加したがメソッド内では未使用
		/// </summary>
		/// <param name="myInData"></param>
        public Loan(string myLoanDealingCode, DateTime myPaymentDate, List<PaymentData> myListData, int myFlag, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			try
			{
				LoanDetailDatas myDatas = new LoanDetailDatas();

				// 支払日を設定：カレンダーで指定された日付
				myDatas.PaymentDate = myPaymentDate;

				// 借入取引コードから取引内容の情報をデータベースから取得
                LoanData loanInfo = GetDbLoanData(myLoanDealingCode, myLoanDealingCode);

				// 支払種類により生成するオブジェクト、使用するメソッドを設定
                switch (loanInfo.PaymentKind)
				{
						// リボルビング払い
					case Loan.DEALINGS_RIVOLVING :
						PaymentRivolving myPayRivolving = new PaymentRivolving();
                        myPayRivolving.Decision(loanInfo, myListData, myDbCon);
						break;
						// カードローン
					case Loan.DEALINGS_CARDLOAN :
						PaymentCardLoan myPayCardLoan = new PaymentCardLoan();
                        myPayCardLoan.Decision(loanInfo, myListData, myDbCon);
						break;
						// 分割払い（TBL借入集計にはないが、同メソッドからも呼び出される）
					case Loan.DEALINGS_INSTPLAN :
						PaymentInstallmentPlan myPayInstallmentPlan = new PaymentInstallmentPlan( PlanTimes );
                        myPayInstallmentPlan.Decision(loanInfo, myListData, myDbCon);
						break;
					default:
						break;
				}
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}

		}
		public LoanData GetDbLoanData( string myTargetCode, string myTargetNextCode )
		{
			DbConnection myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "SELECT 借入取引コード, 借入コード, 借入取引名, ISNULL(支払利率, 0), rtrim(支払種類), ";
			mySqlCommand = mySqlCommand +     "支払先科目コード, ISNULL(利率科目コード, ''), ";
			mySqlCommand = mySqlCommand +     "通常月支払額, ボーナス夏加算額, ボーナス冬加算額, ";
			mySqlCommand = mySqlCommand +     "ボーナス夏加算月, ボーナス冬加算月, ";
			mySqlCommand = mySqlCommand +     "支払日, 分割回数 ";
			mySqlCommand = mySqlCommand + "FROM " + Loan.MSTTBL_CONTENT + " ";
			mySqlCommand = mySqlCommand + "WHERE 借入取引コード = '" + myTargetCode + "' ";
			mySqlCommand = mySqlCommand + "    OR 借入取引コード = '" + myTargetNextCode + "' ";

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

            LoanData data = null;
			try
			{
				myDbCon.openConnection();

				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );

				myReader = myCommand.ExecuteReader();

				myReader.Read();

                data = new LoanData();

                data.DealingCode        = myReader.GetString(0);
                data.LoanCode           = myReader.GetString(1);
                data.LoanName            = myReader.GetString(2);
                data.PaymentRate        = myReader.GetDouble(3);
                data.PaymentKind        = myReader.GetString(4);
				data.PaymentAccountCode	= myReader.GetString( 5 );
				data.RateAccountCode		= myReader.GetString( 6 );
				mySqlMoney			        = myReader.GetSqlMoney( 7 );
				data.NormalPaymentAmount    = mySqlMoney.ToInt64();
				mySqlMoney			        = myReader.GetSqlMoney( 8 );
				data.BonusSPaymentAmount	= mySqlMoney.ToInt64();
				mySqlMoney			        = myReader.GetSqlMoney( 9 );
				data.BonusWPaymentAmount	= mySqlMoney.ToInt64();
				data.BonusSummerMonth	    = myReader.GetInt32( 10 );
				data.BonusWinterMonth	    = myReader.GetInt32( 11 );
				data.PaymentDay			    = myReader.GetInt32( 12 );
				data.PlanTimes			    = myReader.GetInt32( 13 );

				myReader.Close();

				myDbCon.closeConnection();
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			finally
			{
				//if ( myReader != null )
				//myReader.Close();

				myDbCon.closeConnection();
			}

            return data;
		}
		public void DatabaseDetailUpdate( LoanDetailData myDetailData, DbConnection argDbCon )
		{
			DbConnection	myDbCon;
			MoneyInputData	myInData = new MoneyInputData();

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if ( argDbCon != null )
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			// 借入明細から明細ＩＤに一致するデータを取得
			mySqlCommand = "SELECT 年月日, 取引科目コード AS 借方, 借入取引コード AS 貸方, 金額, 摘要, 支払金額 ";
			mySqlCommand = mySqlCommand + "FROM " + MethodPayment.DBTBL_DETAIL + " ";
			mySqlCommand = mySqlCommand + "WHERE 明細ＩＤ = " + myDetailData.Id + " ";

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

			try
			{
				myCommand = new SqlCommand( mySqlCommand, myDbCon.getSqlConnection() );
				myCommand.Transaction = myDbCon.GetTransaction();

				myReader = myCommand.ExecuteReader();

				myReader.Read();

				myInData.Date		= myReader.GetDateTime( 0 );	// 年月日
				myInData.DebitCode	= myReader.GetString( 1 );		// 借方（借入取引コード）
				myInData.CreditCode	= myReader.GetString( 2 );		// 貸方（取引科目コード）
				mySqlMoney			= myReader.GetSqlMoney( 3 );	// 金額
				myInData.Amount		= mySqlMoney.ToInt64();
                myInData.Remark     = myReader.GetString(4);		// 摘要

				myReader.Close();

				// 借入明細を更新
				mySqlCommand = "UPDATE " +  MethodPayment.DBTBL_DETAIL + " ";
				mySqlCommand = mySqlCommand + "SET ";
				mySqlCommand = mySqlCommand + "    借入取引コード = '" + myDetailData.LoanDealingCode + "' ";
				mySqlCommand = mySqlCommand + "    , 取引科目コード = '" + myDetailData.AccountCode + "' ";
				mySqlCommand = mySqlCommand + "    , 金額 = " + myDetailData.Amount + " ";
				mySqlCommand = mySqlCommand + "    , 摘要 = '" + myDetailData.Summury + "' ";
				//mySqlCommand = mySqlCommand + "支払金額 = " + " ";
				mySqlCommand = mySqlCommand + "WHERE 明細ＩＤ = " + myDetailData.Id + " ";

				myDbCon.execSqlCommand( mySqlCommand );

				// 金銭帳の同一データを更新
				MoneyInput myMonIn = new MoneyInput();

                MoneyInput.UpdateDb(myInData, "金銭帳", dbCon);
			}
			catch( SqlException errsql )
			{
				throw errsql;
			}
			catch( BussinessException errbsn )
			{
				throw errbsn;
			}

			return;
		}

        public static List<string> GetListLoanDealingCode(string myLoanCode)
        {
            DbConnection myDbCon = new DbConnection();
            SqlDataReader reader;

            string SelectCommand = "";

            List<string> listLoanDealingCode = new List<string>();

            SelectCommand = "SELECT 借入取引コード ";
            SelectCommand = SelectCommand + "FROM 借入取引内容 ";
            SelectCommand = SelectCommand + "WHERE 借入コード = @借入コード ";
            SelectCommand = SelectCommand + "ORDER BY 借入取引コード ";

            try
            {
                myDbCon.openConnection();

                SqlCommand command = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                SqlParameter param = new SqlParameter("@借入コード", SqlDbType.VarChar);
                param.Value = myLoanCode;
                command.Parameters.Add(param);

                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string code = DbExportCommon.GetDbString(reader, 0);

                    listLoanDealingCode.Add(code);
                }

                reader.Close();

                myDbCon.closeConnection();

            }
            catch (SqlException errsql)
            {
                String strMessage = "";

                strMessage = errsql.Message;
                strMessage = "データベースでエラーが発生しました\n" + strMessage;

                throw errsql;
            }

            return listLoanDealingCode;
        }

        public static List<PaymentData> GetListDetail()
        {
            DbConnection myDbCon = new DbConnection();
            SqlCommand myCommand;
            SqlDataReader reader;

            string SelectCommand = "";

            List<PaymentData> listPaymentData = new List<PaymentData>();

            SelectCommand = "SELECT 借入明細.明細ＩＤ, 年月日, 取引科目コード, A.科目名, 借入取引コード, B.科目名, 金額, 摘要, 支払金額, 支払予定.支払日, 支払予定.借入コード ";
            SelectCommand = SelectCommand + "    FROM 借入明細 ";
            SelectCommand = SelectCommand + "      LEFT OUTER JOIN 科目 AS A ";
            SelectCommand = SelectCommand + "        ON 借入明細.取引科目コード = A.科目コード ";
            SelectCommand = SelectCommand + "      LEFT OUTER JOIN 科目 AS B ";
            SelectCommand = SelectCommand + "        ON 借入明細.借入取引コード = B.科目コード ";
            SelectCommand = SelectCommand + "      LEFT OUTER JOIN 支払予定 ";
            SelectCommand = SelectCommand + "        ON 借入明細.明細ＩＤ = 支払予定.明細ＩＤ ";

            try
            {
                myDbCon.openConnection();

                myCommand = new SqlCommand(SelectCommand, myDbCon.getSqlConnection());

                reader = myCommand.ExecuteReader();

                while (reader.Read())
                {
                    PaymentData data = new PaymentData();

                    data.Id = DbExportCommon.GetDbInt(reader, 0);
                    data.InputDate = DbExportCommon.GetDbDateTime(reader, 1);
                    data.DebitCode = DbExportCommon.GetDbString(reader, 2);
                    data.DebitName = DbExportCommon.GetDbString(reader, 3);
                    data.CreditCode = DbExportCommon.GetDbString(reader, 4);
                    data.CreditName = DbExportCommon.GetDbString(reader, 5);
                    data.Amount = DbExportCommon.GetDbMoney(reader, 6);
                    data.Remark = DbExportCommon.GetDbString(reader, 7);
                    data.PaymentAmount = DbExportCommon.GetDbMoney(reader, 8);
                    data.PaymentDate = DbExportCommon.GetDbDateTime(reader, 9);

                    listPaymentData.Add(data);
                }

                reader.Close();

                myDbCon.closeConnection();

            }
            catch (SqlException errsql)
            {
                String strMessage = "";

                strMessage = errsql.Message;
                strMessage = "データベースでエラーが発生しました\n" + strMessage;

                throw errsql;
            }

            return listPaymentData;
        }
	}
    public class LoanData
    {
        public DateTime PaymentDate { get; set; }           // 支払日
		public string	LoanCode { get; set; }              // 借入コード
        public string   LoanName { get; set; }              // 借入名
		public string	DealingCode { get; set; }	        // 借入取引コード
		public double	PaymentRate { get; set; }	        // 支払利率
        public string   PaymentKind { get; set; }           // 支払種類
		public string	PaymentAccountCode { get; set; }	// 支払先科目コード
		public string	RateAccountCode { get; set; }	    // 利率科目コード
		public long		NormalPaymentAmount { get; set; }	// 通常月支払額
		public long		BonusSPaymentAmount { get; set; }	// ボーナス月支払額（Summer）
		public long		BonusWPaymentAmount { get; set; }	// ボーナス月支払額（Winter）
		public int		BonusSummerMonth { get; set; }	    // ボーナス加算月（Summer）
		public int		BonusWinterMonth { get; set; }	    // ボーナス加算月（Winter）
		public int		PaymentDay { get; set; }	        // 支払日
		public int		PlanTimes { get; set; }	            // 分割回数

        public LoanData()
        {
            PaymentDate = new DateTime();
            LoanCode = "";
            DealingCode = "";
            PaymentRate = 0.0;
            PaymentAccountCode = "";
            RateAccountCode = "";
            NormalPaymentAmount = 0;
            BonusSPaymentAmount = 0;
            BonusWPaymentAmount = 0;
            BonusSummerMonth = 0;
            BonusWinterMonth = 0;
            PaymentDay = 0;
            PlanTimes = 0;
        }
    }
	public class LoanTotalData
	{
        public LoanTotalData()
		{
			LoanDealingCode = "";
			TotalAmount = 0;
		}
		public string	LoanDealingCode;	// 借入取引コード
		public long		TotalAmount;		// 合計金額（借入明細＋借入集計）
	}
}
