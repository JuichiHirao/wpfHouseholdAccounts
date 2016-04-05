using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace wpfHouseholdAccounts
{
	class Environment
	{
		public string[] Name	= null;
		public string[] Value	= null;

		int Count = 0;

		public Environment()
		{
			DbConnection myDbCon = new DbConnection();

			Count = myDbCon.getIntSql("SELECT COUNT(*) FROM 環境設定");

			Name	= new string[Count];
			Value	= new string[Count];

			string mySqlCommand;
			mySqlCommand = "SELECT 名前, 値 ";
			mySqlCommand = mySqlCommand + "FROM 環境設定 ";

			SqlCommand		myCommand;
			SqlDataReader	myReader;

			try
			{
				myDbCon.openConnection();

				myCommand = new SqlCommand(mySqlCommand, myDbCon.getSqlConnection());

				myReader = myCommand.ExecuteReader();

				for (int ArrIndex = 0; ArrIndex < Count; ArrIndex++)
				{
					// 次のレコードがない場合はループを抜ける
					if (!myReader.Read())
						break;

					// データベースのレコードを配列へ設定
					Name[ArrIndex]	= DbExportCommon.GetDbString(myReader, 0);
					Value[ArrIndex]	= DbExportCommon.GetDbString(myReader, 1);
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
		public string GetValue(string myName)
		{
			for( int IndexArr=0; IndexArr<Count; IndexArr++ )
			{
                if (Name[IndexArr] == null)
                    continue;

				if ( Name[IndexArr].Equals(myName) )
					return Value[IndexArr];
			}
			return "";
		}
		public void SetData(string myName, string myValue)
		{
			DbConnection myDbCon = new DbConnection();
			string mySqlCommand;

			try
			{
				myDbCon.openConnection();

				int Count = myDbCon.getIntSql("SELECT COUNT(*) FROM 環境設定 WHERE 名前 = '" + myName + "'");

				if ( Count > 0 )
				{
					mySqlCommand = "UPDATE 環境設定 ";
					mySqlCommand = mySqlCommand + "SET 値 = '" + myValue + "' ";
					mySqlCommand = mySqlCommand + "WHERE 名前 = '" + myName + "' ";
				}
				else
				{
					mySqlCommand = "INSERT INTO 環境設定(名前, 値) ";
					mySqlCommand = mySqlCommand + "VALUES('" + myName + "', '" + myValue + "')";
				}

				myDbCon.execSqlCommand(mySqlCommand);
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
        /// 主に後日確認画面から実行される
        /// CARDのToggleボタンの状態から、設定された支払情報、日付を取得する
        ///   データベースにはCARD確定情報に「2014/2/16,85000」のように格納
        /// </summary>
        /// <param name="myCreditCode"></param>
        /// <returns></returns>
        public object[] GetCardDecision(string myCreditCode)
        {
            object[] objData = new object[2];
            objData[1] = 0;

            // カード確定情報
            string cardInfo = GetValue("CARD確定情報" + myCreditCode);
            string[] split = { "," };
            if (cardInfo != null && cardInfo.Length > 0)
            {
                string[] arrCardInfo = cardInfo.Split(split, StringSplitOptions.None);

                if (arrCardInfo[0].Length > 0)
                {
                    objData[0] = CommonMethod.GetDateTime(arrCardInfo[0]);
                    //dtpickerDecisionSchedule.SelectedDate = dt;
                }
                objData[1] = CommonMethod.GetLong(arrCardInfo[1]);
                //dispinfoData.CardDecisionAmount = CommonMethod.GetLong(arrCardInfo[1]);
            }

            return objData;
        }

        /// <summary>
        /// 主に後日確認画面から実行される
        /// CARDのToggleボタンの状態で入力した支払情報、日付をデータベースに格納する
        ///   データベースにはCARD確定情報に「2014/2/16,85000」のように格納
        /// </summary>
        /// <param name="myCreditCode"></param>
        /// <param name="myDate"></param>
        /// <param name="myAmount"></param>
        public void SetCardDecision(string myCreditCode, DateTime? myDate, long myAmount)
        {
            long amount = 0;

            if (myAmount > 0)
                amount = myAmount;

            string dtStr = "";

            if (myDate != null && myDate.HasValue)
            {
                DateTime dt = (DateTime)myDate;
                dtStr = dt.ToString("yyyy/MM/dd");
            }

            // カード確定情報
            SetData("CARD確定情報" + myCreditCode, dtStr + "," + amount);
        }

	}
}
