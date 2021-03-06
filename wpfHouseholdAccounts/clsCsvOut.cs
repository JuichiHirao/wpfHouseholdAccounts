﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace wpfHouseholdAccounts
{
    class CsvOut
    {
        Account account;

        string Filename;

        public CsvOut(Account myAccount)
        {
            account = myAccount;
        }

        public void Execute()
        {
        }

        public string GetCsvData(MakeupDetailData myData)
        {
            MakeupDetailData dataCsv = new MakeupDetailData();

            Regex regex = new Regex("経費：(.*) (.*)");
            Match match = regex.Match(myData.CreditName);
            if (match.Success)
            {
                dataCsv.Date = myData.Date;
                dataCsv.DebitName = Convert.ToString(match.Groups[1]);
                dataCsv.CreditName = ConvertAssetName(Convert.ToString(match.Groups[2]));
                dataCsv.Amount = myData.Amount;
                dataCsv.Remark = myData.Remark;
            }
            else
            {
                dataCsv.Date = myData.Date;
                dataCsv.Amount = myData.Amount;

                if (myData.DebitCode == "10101")
                    dataCsv.DebitName = "現金";
                if (myData.CreditCode == "10101")
                    dataCsv.CreditName = "現金";

                if (myData.DebitCode == "10209")
                    dataCsv.DebitName = "普通預金";
                if (myData.CreditCode == "10209")
                    dataCsv.CreditName = "普通預金";

                if (myData.DebitCode.IndexOf("408") == 0)
                    dataCsv.DebitName = "旅費交通費";

                if (myData.DebitCode.IndexOf("520") == 0)
                    dataCsv.DebitName = "支払手数料";
                if (myData.DebitCode.IndexOf("502") == 0
                    || myData.DebitCode.IndexOf("503") == 0
                    || myData.DebitCode.IndexOf("506") == 0)
                    dataCsv.DebitName = "研究開発費";

                if (myData.DebitCode.IndexOf("4110") == 0) // 個人の税金
                    dataCsv.DebitName = "短期貸付金";

                if (myData.DebitCode.IndexOf("700") == 0)
                    dataCsv.DebitName = "福利厚生費";

                if (myData.DebitCode == "10101" && myData.CreditCode == "10209")
                    dataCsv.Remark = "小口現金";

                if (myData.CreditCode == "30201")
                    dataCsv.CreditName = "受取利息";

                if (myData.CreditCode == "30307")
                    dataCsv.CreditName = "雑収入";

                if (myData.CreditCode == "30401")
                    dataCsv.CreditName = "売掛金";
                if (myData.DebitCode == "13001")
                {
                    dataCsv.DebitName = "売掛金";
                    if (myData.CreditCode.IndexOf("320") == 0)
                    {
                        dataCsv.CreditName = "売上高";
                    }
                    if (myData.CreditCode.IndexOf("30402") == 0)
                    {
                        //dataCsv.CreditName = "仮受消費税";
                        dataCsv.CreditName = "仮受金";
                    }
                }

                if (myData.DebitCode == "30402")
                {
                    dataCsv.DebitName = "売掛金";
                    if (myData.CreditCode == "30300")
                    {
                        dataCsv.CreditName = "雑収入";
                    }
                }

                if (myData.DebitCode.IndexOf("431") == 0)
                    dataCsv.DebitName = "研究開発費";

                if (myData.DebitCode == "53001")
                {
                    dataCsv.DebitName = "役員報酬";
                    dataCsv.Remark = "平尾充一";

                    if (myData.CreditCode == "22011")
                    {
                        dataCsv.CreditName = "未払金";
                    }
                }
                if (myData.DebitCode == "53102")
                {
                    dataCsv.DebitName = "地代家賃";
                    dataCsv.Remark = "平尾充一";
                }
                if (myData.DebitCode == "53004")
                {
                    dataCsv.DebitName = "役員賞与";
                    dataCsv.Remark = "平尾充一";
                }
                if (myData.DebitCode == "53201")
                {
                    dataCsv.DebitName = "法定福利費";
                    dataCsv.Remark = "平尾充一";
                }
                if (myData.DebitCode == "53103")
                {
                    dataCsv.DebitName = "賃借料";
                    dataCsv.Remark = "MacbookPro";
                }

                if (myData.DebitCode.IndexOf("501") == 0)
                {
                    dataCsv.DebitName = "雑費";
                    dataCsv.Remark = myData.Remark;
                }

                if (myData.DebitCode == "53003")
                {
                    dataCsv.DebitName = "雑給";
                    dataCsv.Remark = "平尾静恵";
                }
                if (myData.DebitCode == "40901" || myData.DebitCode == "52001")
                    dataCsv.DebitName = "支払手数料";

                if (myData.DebitCode == "53002")
                {
                    dataCsv.DebitName = "租税公課";
                    dataCsv.Remark = "平尾充一";
                }
                if (myData.CreditCode == "53002")
                {
                    dataCsv.CreditName = "源泉税等預り金";
                    dataCsv.Remark = "平尾充一";
                }
                if (myData.DebitCode == "51300")
                    dataCsv.DebitName = "交際費";

                if (myData.DebitCode == "23201")
                {
                    dataCsv.DebitName = "福利厚生費";
                    dataCsv.Remark = "生協";
                }

                if (myData.DebitCode == "21002")
                {
                    dataCsv.CreditName = "短期貸付金";
                    dataCsv.DebitName = "現金";
                }
                if (myData.CreditCode == "21002")
                {
                    if (myData.DebitCode == "10101" || myData.DebitCode == "10209")
                        dataCsv.CreditName = "短期貸付金";

                    if (myData.DebitCode == "10101")
                        dataCsv.DebitName = "現金";
                    else if (myData.DebitCode == "10209")
                        dataCsv.DebitName = "普通預金";
                    else
                    {
                        dataCsv.DebitName = "短期貸付金";
                        dataCsv.CreditName = "現金";
                    }
                }
                if (myData.DebitCode.Equals("40911"))
                {
                    dataCsv.DebitName = "雑費";
                    dataCsv.Remark = myData.DebitName;
                }

                if (myData.DebitCode == "12011"
                    || myData.DebitCode == "20801")
                {
                    dataCsv.DebitName = "未払金";
                    if (myData.DebitCode == "12011")
                        dataCsv.Remark = "支払 平尾充一";
                    else if (myData.DebitCode == "20801")
                        dataCsv.Remark = "法人用JCBカード支払";
                    else
                        dataCsv.Remark = "不明";
                }

                // 仕訳の取消（金額の返却）
                if (myData.CreditCode.IndexOf("4") == 0)
                {
                    dataCsv.CreditName = "雑費";
                    dataCsv.Remark = "返却";
                }
                if (myData.CreditCode == "20801")
                {
                    dataCsv.CreditName = "未払金";
                    dataCsv.Remark = "法人用JCBカード支払：" + myData.Remark;

                    if (dataCsv.DebitName != null && dataCsv.DebitName.Length > 0)
                    {

                    }
                    else
                    {
                        if (myData.DebitCode.IndexOf("4010") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("401") == 0
                            || myData.DebitCode.IndexOf("513") == 0)
                        {
                            if (myData.Amount > 5000)
                                dataCsv.DebitName = "交際費";
                            else
                                dataCsv.DebitName = "雑費";
                        }
                        else if (myData.DebitCode.IndexOf("402") == 0 || myData.DebitCode.IndexOf("403") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.Equals("40911"))
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("404") == 0)
                            dataCsv.DebitName = "福利厚生費";
                        else if (myData.DebitCode.IndexOf("406") == 0 || myData.DebitCode.IndexOf("417") == 0)
                            dataCsv.DebitName = "通信費";
                        else if (myData.DebitCode.IndexOf("407") == 0)
                            dataCsv.DebitName = "交際費";
                        else if (myData.DebitCode.IndexOf("408") == 0)
                            dataCsv.DebitName = "旅費交通費";
                        else if (myData.DebitCode.IndexOf("409") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("42") == 0)
                            dataCsv.DebitName = "福利厚生費";
                        else if (myData.DebitCode.IndexOf("43") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("44") == 0)
                            dataCsv.DebitName = "福利厚生費";
                        else if (myData.DebitCode == "23201")
                        {
                            dataCsv.DebitName = "福利厚生費";
                            dataCsv.Remark = "生協";
                        }
                        else if (myData.DebitCode.IndexOf("50") == 0)
                            dataCsv.DebitName = "研究開発費";
                        else if (myData.DebitCode.IndexOf("60") == 0)
                            dataCsv.DebitName = "研究開発費";
                    }
                }

                if (myData.UsedCompanyArrear > 0)
                {
                    string kind = account.getAccountKind(myData.DebitCode);

                    if (kind == Account.KIND_ASSETS_COMPANY_ARREAR)
                        dataCsv.DebitName = "未払金";
                    else
                    {
                        string kindCredit = account.getAccountKind(myData.CreditCode);

                        if (myData.DebitCode.IndexOf("401") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("406") == 0)
                            dataCsv.DebitName = "通信費";
                        else if (myData.DebitCode.IndexOf("408") == 0)
                            dataCsv.DebitName = "旅費交通費";
                        else if (myData.DebitCode.IndexOf("415") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("419") == 0)
                            dataCsv.DebitName = "広告宣伝費";
                        else if (myData.DebitCode.IndexOf("42") == 0)
                            dataCsv.DebitName = "福利厚生費";
                        else if (myData.DebitCode.IndexOf("431") == 0)
                            dataCsv.DebitName = "雑費";
                        else if (myData.DebitCode.IndexOf("50") == 0)
                            dataCsv.DebitName = "研究開発費";
                        else if (myData.DebitCode.IndexOf("60") == 0)
                            dataCsv.DebitName = "研究開発費";
                        else if (myData.DebitCode == "23201")
                        {
                            dataCsv.DebitName = "福利厚生費";
                            myData.Remark = "生協";
                        }
                        else
                            dataCsv.DebitName = "雑費";

                        if (kindCredit == Account.KIND_DEPT_LOAN)
                        {
                            DateTime dt = Convert.ToDateTime(myData.RegistDate);
                            string cardName = "";
                            if (myData.CreditCode.Equals("20101"))
                                cardName = "JCB";
                            else if (myData.CreditCode.Equals("20401"))
                                cardName = "京王";
                            dataCsv.Remark = cardName + " " + dt.ToString("yyyy/MM/dd") + " " + myData.Remark;
                        }
                        else
                            dataCsv.Remark = myData.Remark;
                        dataCsv.CreditName = "未払金";
                    }
                }
                else
                {
                    if (dataCsv.CreditName == null || dataCsv.CreditName.Length <= 0)
                        dataCsv.CreditName = "未払金";
                }

                if (dataCsv.Remark == null || dataCsv.Remark.Length == 0)
                    dataCsv.Remark = myData.Remark;
            }
            string line = dataCsv.Date.ToString("yyyy/MM/dd") + "," + dataCsv.DebitName + ",\"" + dataCsv.Amount + "\",," + dataCsv.CreditName + ",\"" + dataCsv.Amount + "\",," + dataCsv.Remark;

            return line;
        }

        public string GetCsvDataArrear(Arrear myData)
        {
            
            MakeupDetailData dataCsv = new MakeupDetailData();

            dataCsv.Date = myData.JournalDate;
            dataCsv.Amount = myData.Amount;

            string DebitName = account.getName(myData.DebitCode);
            string CreditName = account.getName(myData.CreditCode);

            string kind = account.getAccountKind(myData.DebitCode);

            if (kind == Account.KIND_ASSETS_COMPANY_ARREAR)
            {
                return "";
                /*
                dataCsv.DebitName = "未払金";
                if (myData.CreditCode == "10101")
                    dataCsv.CreditName = "現金";
                if (myData.CreditCode == "10209")
                    dataCsv.CreditName = "普通預金";
                 */
            }
            else
            {
                string kindCredit = account.getAccountKind(myData.CreditCode);

                if (myData.DebitCode.IndexOf("401") == 0)
                    dataCsv.DebitName = "雑費";
                else if (myData.DebitCode.IndexOf("406") == 0)
                    dataCsv.DebitName = "通信費";
                else if (myData.DebitCode.IndexOf("408") == 0)
                    dataCsv.DebitName = "旅費交通費";
                else if (myData.DebitCode.IndexOf("415") == 0)
                    dataCsv.DebitName = "雑費";
                else if (myData.DebitCode.IndexOf("419") == 0)
                    dataCsv.DebitName = "広告宣伝費";
                else if (myData.DebitCode.IndexOf("431") == 0)
                    dataCsv.DebitName = "雑費";
                else if (myData.DebitCode.IndexOf("50") == 0)
                    dataCsv.DebitName = "研究開発費";
                else if (myData.DebitCode.IndexOf("60") == 0)
                    dataCsv.DebitName = "研究開発費";
                else if (myData.DebitCode == "53001")
                {
                    dataCsv.DebitName = "役員報酬";
                    myData.Remark = "平尾充一";

                    if (myData.CreditCode == "22011")
                    {
                        dataCsv.CreditName = "未払金";
                    }
                }
                else if (myData.DebitCode == "23201")
                {
                    dataCsv.DebitName = "福利厚生費";
                    myData.Remark = "生協";
                }
                else
                    dataCsv.DebitName = "研究開発費";

                if (kindCredit == Account.KIND_DEPT_LOAN)
                {
                    DateTime dt = Convert.ToDateTime(myData.CreateDate);
                    string cardName = "";
                    if (myData.CreditCode.Equals("20101"))
                        cardName = "JCB";
                    else if (myData.CreditCode.Equals("20401"))
                        cardName = "京王";
                    else if (myData.CreditCode.Equals("20701"))
                        cardName = "セゾン";
                    dataCsv.Remark = cardName + " " + dt.ToString("yyyy/MM/dd") + " " + myData.Remark;
                }
                else
                {
                    dataCsv.Remark = myData.Remark;
                }
                if (dataCsv.CreditName == null || dataCsv.CreditName.Length <= 0)
                    dataCsv.CreditName = "未払金";
            }

            string line = dataCsv.Date.ToString("yyyy/MM/dd") + "," + dataCsv.DebitName + ",\"" + dataCsv.Amount + "\",," + dataCsv.CreditName + ",\"" + dataCsv.Amount + "\",," + dataCsv.Remark;

            return line;
        }

        private string ConvertAssetName(string myName)
        {
            string name = myName;

            if (myName == "預金")
                name = "普通預金";

            return name;
        }

    }
}
