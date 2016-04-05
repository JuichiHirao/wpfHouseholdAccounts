using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace wpfHouseholdAccounts
{
	class BudgetAccount
	{
		BudgetAccountData[] ArrBudgetData = new BudgetAccountData[10];
		public int Count = 0;

		public BudgetAccount()
		{
			DbConnection dbcon = new DbConnection();

			Count = dbcon.getIntSql("SELECT COUNT(*) FROM 予算");

			ArrBudgetData = new BudgetAccountData[Count];

			for (int ArrIndex = 0; ArrIndex < Count; ArrIndex++)
			{
				ArrBudgetData[ArrIndex] = new BudgetAccountData();
			}
			
			this.DatabaseSetArray();

		}
		private void DatabaseSetArray()
		{
			DbConnection myDbCon = new DbConnection();

			string mySqlCommand = "";

			///////////////////////////////////////
			// MSTBL予算のデータを配列へ設定する //
			///////////////////////////////////////
			mySqlCommand = "SELECT 予算集計.予算コード, 資産コード, 予算.名前, 残高 ";
			mySqlCommand = mySqlCommand + "FROM 予算集計 ";
			mySqlCommand = mySqlCommand + "  INNER JOIN 予算 ";
            mySqlCommand = mySqlCommand + "    ON 予算集計.予算コード = 予算.予算コード";
            mySqlCommand = mySqlCommand + "  INNER JOIN 科目";
            mySqlCommand = mySqlCommand + "    ON 予算集計.予算コード = 科目.科目コード";
            mySqlCommand = mySqlCommand + "    WHERE ( 科目.無効フラグ = 'FALSE' OR 科目.無効フラグ IS NULL )";

			SqlCommand		myCommand;
			SqlDataReader	myReader;
			SqlMoney		mySqlMoney;

			try
			{
				myDbCon.openConnection();

				myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

				myReader = myCommand.ExecuteReader();

				for (int ArrIndex = 0; ArrIndex < 10; ArrIndex++)
				{
					// 次のレコードがない場合はループを抜ける
					if (!myReader.Read())
						break;

					// データベースのレコードを配列へ設定
					ArrBudgetData[ArrIndex].Code = myReader.GetString(0);
					ArrBudgetData[ArrIndex].AssetCode = myReader.GetString(1);
					ArrBudgetData[ArrIndex].Name = myReader.GetString(2);
					mySqlMoney	= myReader.GetSqlMoney(3);
					ArrBudgetData[ArrIndex].BalanceAmount = mySqlMoney.ToInt64();
				}

				myReader.Close();

				myDbCon.closeConnection();
			}
			catch (SqlException errsql)
			{
				throw errsql;
			}
			finally
			{
				myDbCon.closeConnection();
			}

			return;
		}
		/// <summary>
		/// 金銭帳入力の現在の情報に表示する為のMoneyNowDatasを生成
		/// </summary>
		/// <returns></returns>
        public List<MoneyNowData> GetNowInfo()
		{
            List<MoneyNowData> moneyalldata = new List<MoneyNowData>();

			for (int ArrIndex = 0; ArrIndex < Count; ArrIndex++)
			{
                if (ArrBudgetData[ArrIndex].Code.Length <= 0)
                    continue;

				MoneyNowData nowdata = new MoneyNowData();

				nowdata.Code = ArrBudgetData[ArrIndex].Code;
				nowdata.Name = ArrBudgetData[ArrIndex].Name;
                nowdata.AccountKind = Account.KIND_ASSETS_BUDGET;
				nowdata.NowAmount = ArrBudgetData[ArrIndex].BalanceAmount;

                moneyalldata.Add(nowdata);
			}

            return moneyalldata;
		}
		/// <summary>
		/// 指定された予算コードに一致する資産コードを取得する
		/// </summary>
		/// <param name="myAssetCode">資産コード</param>
		/// <returns></returns>
		public string GetAssetCode(string myBudgetCode)
		{
			for (int IndexArr = 0; IndexArr < Count; IndexArr++)
			{
				if (ArrBudgetData[IndexArr].Code.Equals(myBudgetCode))
					return ArrBudgetData[IndexArr].AssetCode;
			}

			return "";
		}
		/// <summary>
		/// 指定された資産コードに一致する予算の合計額を取得する
		/// </summary>
		/// <param name="myAssetCode">資産コード</param>
		/// <returns></returns>
		public long GetTotalAmount(string myAssetCode)
		{
			long Amount = 0;

			for (int ArrIndex = 0; ArrIndex < Count; ArrIndex++)
			{
				if ( ArrBudgetData[ArrIndex].AssetCode.Equals(myAssetCode) )
					Amount += ArrBudgetData[ArrIndex].BalanceAmount;
			}

			return Amount;
		}
	}
	public class BudgetAccountData
	{
		public BudgetAccountData()
		{
			Code = "";
			AssetCode = "";
		}
		public string Code;			// 預金コード
		public string AssetCode;	// 預金コード
		public string Name;			// 預金名
		public string Kind;			// 預金種類
		public long BalanceAmount;	// 残高
	}

}
