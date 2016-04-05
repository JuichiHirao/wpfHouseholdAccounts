using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;

namespace wpfHouseholdAccounts
{
    public class InputTextKana
    {
         //"a", "i", "u", "e", "o" "あ", "い", "う", "え", "お"
        public string[] Alpha = {
                                    "sha", "shi","shu", "she", "sho"
                                    , "sya", "syi","syu", "sye", "syo"
                                    , "gya", "gyi","gyu", "gye", "gyo"
                                    , "zya", "zyi","zyu", "zye", "zyo"
                                    , "ja", "ji","ju", "je", "jo"
                                    , "ga", "gi", "gu", "ge", "go"
                                    , "za", "zi", "zu", "ze", "zo"
                                    , "da", "di", "du", "de", "do"
                                    , "ba", "bi", "bu", "be", "bo"
                                    , "ka", "ki", "ku", "ke", "ko"
                                    , "sa", "si", "su", "se", "so"
                                    , "ta", "ti", "tu", "te", "to"
                                    , "na", "ni", "nu", "ne", "no"
                                    , "ha", "hi", "hu", "he", "ho"
                                    , "ma", "mi", "mu", "me", "mo"
                                    , "ya", "yu", "yo", "wa", "wo"
                                    , "ra", "ri", "ru", "re", "ro"
                                    , "a", "i", "u", "e", "o", "n"
                                };
        public string[] Kana =  {
                                    "しゃ", "し", "しゅ", "しぇ", "しょ"
                                   , "しゃ", "し", "しゅ", "しぇ", "しょ"
                                   , "ぎゃ", "ぎぃ", "ぎゅ", "ぎぇ", "ぎょ"
                                   , "じゃ", "じぃ", "じゅ", "じぇ", "じょ"
                                   , "じゃ", "じ", "じゅ", "じぇ", "じょ"
                                   , "が", "ぎ", "ぐ", "げ", "ご"
                                   , "ざ", "じ", "ず", "ぜ", "ぞ"
                                   , "だ", "ぢ", "づ", "で", "ど"
                                   , "ば", "び", "ぶ", "べ", "ぼ"
                                   , "か", "き", "く", "け", "こ"
                                   , "さ", "し", "す", "せ", "そ"
                                   , "た", "ち", "つ", "て", "と"
                                   , "な", "に", "ぬ", "ね", "の"
                                   , "は", "ひ", "ふ", "へ", "ほ"
                                   , "ま", "み", "む", "め", "も"
                                   , "や", "ゆ", "よ"
                                   , "ら", "り", "る", "れ", "ろ"
                                   , "わ", "を"
                                   , "あ", "い", "う", "え", "お", "ん"
                               };
        public int[] AlphaLen = {
                                    3, 3, 3, 3, 3
                                    , 3, 3, 3, 3, 3
                                    , 3, 3, 3, 3, 3
                                    , 3, 3, 3, 3, 3
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2, 2
                                    , 2, 2, 2, 2, 2
                                    , 2, 2
                                    , 1, 1, 1, 1, 1, 1
                                };
    }
    /// <summary>
    /// DataGridのコード入力時に使用、数字のみ入力で３バイトを入力した後に結果を表示する
    ///
    /// </summary>
    public class TextSearchFilterForGrid
    {
        public TextSearchFilterForGrid(
            ICollectionView filteredView,
            TextBox textBox)
        {
            InputTextKana text = new InputTextKana();

            string filterText = "";
            string inputFilterText = "";
            string recognitionHirakana = ""; // 認識されたひらがな
            Regex regNum = new Regex("\\d+");

            filteredView.Filter = delegate(object obj)
            {
                if (String.IsNullOrEmpty(filterText))
                    return false;

                // 数字3バイトを入力しないと表示しない
                if (filterText.Length < 2)
                    return false;

                AccountData data = obj as AccountData;

                int index = 0;
                if (regNum.IsMatch(filterText))
                {
                    index = data.Code.IndexOf(
                        filterText,
                        0,
                        StringComparison.InvariantCultureIgnoreCase);

                    // 2文字の場合は上位科目のみを表示（最後の桁が0のデータ）
                    if (data.Code.Length >= 5)
                    {
                        string d = data.Code.Substring(4, 1);

                        if (filterText.Length == 2 && !d.Equals("0"))
                            return false;
                    }
                }

                // 先頭からの一致の場合のみ表示対象とする
                return index == 0;
            };

            textBox.TextChanged += delegate
            {
                inputFilterText = textBox.Text;
                //Debug.Print(inputFilterText);
                regNum = new Regex("\\d+");
                if (regNum.IsMatch(inputFilterText))
                {
                    // 数字の場合は何もしない
                    filterText = inputFilterText;
                }
                else
                {
                    string[] result = new string[inputFilterText.Length];

                    string workText = inputFilterText;
                    string work2Text = workText;
                    for (int idx = 0; idx < text.Alpha.Length; idx++)
                    {
                        int pos = workText.IndexOf(text.Alpha[idx]);

                        if (pos >= 0)
                            result[pos] = text.Kana[idx];
                        else
                            continue;

                        if (text.AlphaLen[idx] == 3)
                            work2Text = workText.Replace(text.Alpha[idx], "   ");
                        else if (text.AlphaLen[idx] == 2)
                            work2Text = workText.Replace(text.Alpha[idx], "  ");
                        else if (text.AlphaLen[idx] == 1)
                            work2Text = workText.Replace(text.Alpha[idx], " ");

                        workText = work2Text;
                    }
                    //Debug.Print("work" + workText);

                    string SearchKana = "";

                    foreach (string charKana in result)
                    {
                        if (charKana != null && charKana != "")
                            SearchKana += charKana;
                    }
                    recognitionHirakana = SearchKana;

                    //Debug.Print("Search [" + recognitionHirakana + "]");
                    filterText = recognitionHirakana;
                }

                filteredView.Refresh();
            };
        }
    }
    public class KindSearchFilter
    {
        public KindSearchFilter(
            ICollectionView filteredView,
            int kind)
        {
            InputTextKana text = new InputTextKana();

            string filterText = "";
            string inputFilterText = "";
            string recognitionHirakana = ""; // 認識されたひらがな
            Regex regNum = new Regex("\\d+");

            filteredView.Filter = delegate(object obj)
            {
                if (String.IsNullOrEmpty(filterText))
                    return true;

                AfterwordsPaymentData data = obj as AfterwordsPaymentData;

                if (kind > 0)
                {
                    if (data.Kind == kind)
                        return true;
                    else
                        return false;

                }

                return true;
            };
        }
    }
}
