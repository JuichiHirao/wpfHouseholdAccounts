using System;
using System.Collections.Generic;
using System.Text;

namespace wpfHouseholdAccounts
{
    class BudgetData
	{
		public string	BudgetCode	= "";
		public string	AssetCode	= "";
		public string	Name		= "";
		public long		Amount		= 0;
		public long		Balance		= 0;			// 残高
		// メソッドcalculationを使用した後に格納
		public long		CompilationAmount	= 0;	// 編成金額
		public long		AppropriationAmount	= 0;	// 支出金額

        public bool UpdateFlag = false;

		public void Calculation(long myAmount)
		{
            Balance = Balance + myAmount;

			if ( myAmount > 0 )
                CompilationAmount = CompilationAmount + myAmount;
			else
				AppropriationAmount =AppropriationAmount + myAmount * -1L;

            UpdateFlag = true;
		}

        /// <summary>
        /// 同オブジェクト内の金額が「Calculation」により更新された場合はtrueにされる
        /// </summary>
        /// <returns></returns>
        public bool IsUpdate()
        {
            return UpdateFlag;
        }
	}
}
