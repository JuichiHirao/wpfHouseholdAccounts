using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace wpfHouseholdAccounts
{
	class Budget
	{
        BudgetData[] ArrBudgetData = null;
        public int BudgetCount = 0;	// 配列に格納された件数
        private DateTime RegistDate;

        public Budget(DateTime myRegistDate)
        {
            RegistDate = myRegistDate;

            // 配列のインスタンスを設定
            BudgetCount = DatabaseGetBudgetMax();

            if ( BudgetCount <= 0 )
					throw new BussinessException("予算として登録されたコードが存在しません");

            ArrBudgetData = new BudgetData[BudgetCount];

            for (int iArrayIndex = 0; iArrayIndex < BudgetCount; iArrayIndex++)
                ArrBudgetData[iArrayIndex] = new BudgetData();

            this.DatabaseSetArray();
        }

        public void Compilation(string myBudgetCode, string myAssetCode, long DepoAmount)
        {
            // 予算に対応する資産コードが違う場合はエラーとする
            string ErrMessage = "";
            if (!CheckAssetCode(myBudgetCode, myAssetCode))
            {
                ErrMessage = "予算[" + myBudgetCode + "]を保管している資産[" + myAssetCode + "]が違います";
                throw new BussinessException(ErrMessage);
            }
            // 予算編成する（金額をプラスする）
            for (int IdxArr = 0; IdxArr < BudgetCount; IdxArr++)
            {
                if (ArrBudgetData[IdxArr].BudgetCode.Equals(myBudgetCode))
                {
                    ArrBudgetData[IdxArr].Calculation(DepoAmount);
                    return;
                }
            }

            // 一致する予算コードが存在しない場合はエラーにする
            ErrMessage = "予算コードとして設定された" + myBudgetCode + "は\n現在の予算コードには登録されていません";
            throw new BussinessException(ErrMessage);
        }
        public void Appropriation(string myBudgetCode, string myAssetCode, long DrawAmount)
        {
            // 予算支出する（金額をマイナスする）
            for (int IdxArr = 0; IdxArr < BudgetCount; IdxArr++)
            {
                if (ArrBudgetData[IdxArr].BudgetCode.Equals(myBudgetCode))
                {
                    ArrBudgetData[IdxArr].Calculation(DrawAmount*-1L);
                    return;
                }
            }
            // 一致する予算コードが存在しない場合はエラーにする
            string ErrMessage = "予算コードとして設定された" + myBudgetCode + "は\n現在の予算コードには登録されていません";
            throw new BussinessException(ErrMessage);
        }
        /// <summary>
        /// 指定された予算コードが資産コードと一致しているかチェック
        /// ※ パラメータの資産コードが予算コードの場合はさらに資産コードを取得してチェックする
        /// </summary>
        /// <param name="myBudgetCode"></param>
        /// <param name="myAssetCode"></param>
        /// <returns>一致している場合はtrue、不一致の場合はfalse</returns>
        public bool CheckAssetCode(string myBudgetCode, string myAssetCode)
        {
            for (int IdxArr = 0; IdxArr < BudgetCount; IdxArr++)
            {
                if (ArrBudgetData[IdxArr].BudgetCode.Equals(myBudgetCode))
                {
                    // 資産コードが無しの場合は資産コードを配列に格納してfalseでリターン
                    // ※ 予算集計に行が未存在
                    if (ArrBudgetData[IdxArr].AssetCode.Length <= 0)
                    {
                        ArrBudgetData[IdxArr].AssetCode = myAssetCode;
                        return true;
                    }

                    // 資産コードが違う場合はエラー
                    if (ArrBudgetData[IdxArr].AssetCode.Equals(myAssetCode))
                        return true;

                    try
                    {
                        BudgetData data = this.GetArrayBudgetData(myAssetCode);

                        if (ArrBudgetData[IdxArr].Equals(data.AssetCode))
                            return true;
                        else
                            return false;
                    }
                    catch (BussinessException)
                    {
                        // GetArrayBudgetDataで一致するデータが存在しない場合に
                        // 本エラーとなる
                        // 存在しないということは予算以外の科目種別の場合
                        return false;
                    }
                }
            }
            // 一致する予算コードが存在しない場合はエラーにする
            string ErrMessage = "予算コードとして設定された" + myBudgetCode + "は\n現在の予算コードには登録されていません";
            throw new BussinessException(ErrMessage);
        }
        /// <summary>
        /// 予算データの配列を設定する
        /// </summary>
        private void DatabaseSetArray()
        {
            DbConnection myDbCon = new DbConnection();

            string mySqlCommand = "";

            //////////////////////////////////
            // 予算のデータを配列へ設定する //
            //////////////////////////////////
            mySqlCommand = "SELECT 予算.予算コード, 予算.名前, ISNULL(予算集計.資産コード, '') ";
            mySqlCommand = mySqlCommand + ", ISNULL(予算集計.残高,0) AS 残高 ";
            mySqlCommand = mySqlCommand + "FROM 予算 LEFT JOIN 予算集計 ";
            mySqlCommand = mySqlCommand + "  ON 予算.予算コード = 予算集計.予算コード ";
            mySqlCommand = mySqlCommand + "ORDER BY 予算.予算コード ";

            SqlCommand myCommand;
            SqlDataReader myReader;
            SqlMoney mySqlMoney;

            try
            {
                myDbCon.openConnection();

                myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

                myReader = myCommand.ExecuteReader();

                for (int IndexArr = 0; IndexArr < 10; IndexArr++)
                {
                    // 次のレコードがない場合はループを抜ける
                    if (!myReader.Read())
                        break;

                    // データベースのレコードを配列へ設定
                    ArrBudgetData[IndexArr].BudgetCode = myReader.GetString(0);
                    ArrBudgetData[IndexArr].Name = myReader.GetString(1);
                    ArrBudgetData[IndexArr].AssetCode = myReader.GetString(2);
                    mySqlMoney = myReader.GetSqlMoney(3);
                    ArrBudgetData[IndexArr].Balance = mySqlMoney.ToInt64();

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
        /// 予算データの配列の要素数をデータベース「予算テーブル」から取得する
        /// </summary>
        private int DatabaseGetBudgetMax()
        {
            DbConnection myDbCon = new DbConnection();

            string mySqlCommand = "";

            ///////////////////////////////////////
            // MSTBL預金のデータを配列へ設定する //
            ///////////////////////////////////////
            mySqlCommand = "SELECT COUNT(*) ";
            mySqlCommand = mySqlCommand + "FROM 予算 ";

            SqlCommand myCommand;
            SqlDataReader myReader;

            try
            {
                myDbCon.openConnection();

                myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

                myReader = myCommand.ExecuteReader();

                // 次のレコードがない場合はループを抜ける
                if (myReader.Read())
                {
                    int Count = myReader.GetInt32(0);

                    return Count;
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

            return 0;
        }
        public void DatabaseRefrect(DbConnection argDbCon)
        {
            DbConnection myDbCon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            // データベースに反映する
            for (int IdxArr = 0; BudgetCount > IdxArr; IdxArr++)
            {
                if (ArrBudgetData[IdxArr].IsUpdate())
                {
                    // 残高が０未満になる場合はエラーにする
                    if (ArrBudgetData[IdxArr].Balance < 0)
                    {
                        string ErrMessage = "予算：" + ArrBudgetData[IdxArr].Name + "の金額が０未満になります";
                        throw new BussinessException(ErrMessage);
                    }
                    
                    //   予算テーブルに存在する場合
                    if (ExistBudgetMakeup(ArrBudgetData[IdxArr].BudgetCode, myDbCon))
                    {
                        // 集計を更新
                        DatabaseMakeupUpdate(ArrBudgetData[IdxArr].BudgetCode, ArrBudgetData[IdxArr], myDbCon);
                    }
                    else
                    {
                        // 集計に挿入
                        DatabaseMakeupInsert(ArrBudgetData[IdxArr].BudgetCode, ArrBudgetData[IdxArr].AssetCode, ArrBudgetData[IdxArr].Balance, myDbCon);
                    }
                    // 履歴へ挿入
                    DatabaseHistoryInsert(RegistDate, ArrBudgetData[IdxArr], myDbCon);
                }

            }
        }

        // 予算集計：行の存在チェック
        public bool ExistBudgetMakeup(string myBudgetCode, DbConnection argDbCon)
        {
            DbConnection myDbCon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            int ExistCount = myDbCon.getIntSql("SELECT COUNT(*) FROM 予算集計 WHERE 予算コード = '" + myBudgetCode + "'");

            if (ExistCount > 0)
                return true;
            else
                return false;
        }

        private BudgetData GetArrayBudgetData(string myBudgetCode)
        {
            // 予算支出する（金額をマイナスする）
            for (int IdxArr = 0; IdxArr < BudgetCount; IdxArr++)
            {
                if (ArrBudgetData[IdxArr].BudgetCode == myBudgetCode)
                {
                    return ArrBudgetData[IdxArr];
                }
            }

            // 一致する予算コードが存在しない場合はエラーにする
            string ErrMessage = "予算コードとして設定された" + myBudgetCode + "は\n現在の予算コードには登録されていません";
            throw new BussinessException(ErrMessage);

        }
        // 履歴：挿入
        public void DatabaseHistoryInsert(DateTime myDate, BudgetData myBudgetData, DbConnection argDbCon)
        {
            DbConnection myDbCon;

            // 引数にコネクションが指定されていた場合は指定されたコネクションを使用
            if (argDbCon != null)
                myDbCon = argDbCon;
            else
                myDbCon = new DbConnection();

            string mySqlCommand = "";

            mySqlCommand = "INSERT INTO 予算履歴 ";
            mySqlCommand = mySqlCommand + "( 年月日, 予算コード, 資産コード, 算入, 支出, 残高 ) ";
            mySqlCommand = mySqlCommand + "VALUES( ";
            mySqlCommand = mySqlCommand + "'" + myDate.ToShortDateString() + "'";	// 日付
            mySqlCommand = mySqlCommand + ", '" + myBudgetData.BudgetCode + "'";			// 予算コード
            mySqlCommand = mySqlCommand + ", '" + myBudgetData.AssetCode + "'";				// 資産コード
            mySqlCommand = mySqlCommand + ", " + myBudgetData.CompilationAmount + " ";		// 算入
            mySqlCommand = mySqlCommand + ", " + myBudgetData.AppropriationAmount + " ";	// 支出
            mySqlCommand = mySqlCommand + ",  " + myBudgetData.Balance + " ";				// 残高
            mySqlCommand = mySqlCommand + ") ";

            try
            {
                myDbCon.execSqlCommand(mySqlCommand);
            }
            catch (SqlException errsql)
            {
                throw errsql;
            }
        }

		// 集計：挿入
		public void DatabaseMakeupInsert(string myBudgetCode, string myAssetCode, long myBalance, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "INSERT INTO 予算集計 ";
			mySqlCommand = mySqlCommand + "( 予算コード, 資産コード, 残高 ) ";
			mySqlCommand = mySqlCommand + "VALUES( ";
			mySqlCommand = mySqlCommand + " '" + myBudgetCode + "'";						// 予算コード
            mySqlCommand = mySqlCommand + ", '" + myAssetCode + "'";				// 資産コード
			mySqlCommand = mySqlCommand + ",  " + myBalance + " ";					// 金額
			mySqlCommand = mySqlCommand + ") ";

			try
			{
				myDbCon.execSqlCommand(mySqlCommand);
			}
			catch (SqlException errsql)
			{
				throw errsql;
			}
		}
		// 集計：更新
        public void DatabaseMakeupUpdate(string myBudgetCode, BudgetData myInData, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "UPDATE 予算集計 ";
			mySqlCommand = mySqlCommand + "SET 残高 = "+ myInData.Balance + " ";
			mySqlCommand = mySqlCommand + "WHERE 予算コード = '" + myBudgetCode + "' ";
			//mySqlCommand = mySqlCommand + "WHERE 予算コード = '" + myBudgetCode + "' AND 資産コード = '" + myAssetCode + "' ";

			try
			{
				myDbCon.execSqlCommand(mySqlCommand);
			}
			catch (SqlException errsql)
			{
				throw errsql;
			}
		}
		// 集計：削除
		public void DatabaseMakeupDelete(string myBudgetCode, string myAssetCode, DbConnection argDbCon)
		{
			DbConnection myDbCon;

			// 引数にコネクションが指定されていた場合は指定されたコネクションを使用
			if (argDbCon != null)
				myDbCon = argDbCon;
			else
				myDbCon = new DbConnection();

			string mySqlCommand = "";

			mySqlCommand = "DELETE FROM 予算集計 ";
			mySqlCommand = mySqlCommand + "WHERE 予算コード = '" + myBudgetCode + "' AND 資産コード = '" + myAssetCode + "' ";

			try
			{
				myDbCon.execSqlCommand(mySqlCommand);
			}
			catch (SqlException errsql)
			{
				throw errsql;
			}
		}
	}
}
