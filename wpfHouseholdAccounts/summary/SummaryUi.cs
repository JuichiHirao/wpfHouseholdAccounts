using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace wpfHouseholdAccounts.summary
{
    class SummaryUi
    {
        public static int GetGridLength(int myKind)
        {
            if (myKind == 1)
                return 40;

            return 30;
        }
        public static TextBlock GetCaptionTextBlock(int myKind, string myText, int myRow)
        {
            TextBlock textblock = new TextBlock();

            textblock.Text = myText;

            if (myKind == 1)
            {
                textblock.SetValue(Grid.ColumnProperty, 0);
                textblock.Margin = new Thickness(3, 3, 3, 3);
                textblock.FontSize = 24;
            }
            else if (myKind == 2)
            {
                textblock.SetValue(Grid.ColumnProperty, 0);
                textblock.Margin = new Thickness(20, 3, 3, 3);
                textblock.FontSize = 18;
            }
            else if (myKind == 3)
            {
                textblock.SetValue(Grid.ColumnProperty, 0);
                textblock.Margin = new Thickness(50, 3, 3, 3);
                textblock.FontSize = 12;
            }
            else if (myKind == 4)
            {
                textblock.SetValue(Grid.ColumnProperty, 0);
                textblock.Margin = new Thickness(80, 3, 3, 3);
                textblock.FontSize = 12;
            }
            textblock.SetValue(Grid.RowProperty, myRow);

            return textblock;
        }
        public static TextBlock GetAmountTextBlock(int myKind, long myAmount, int myRow, bool myIsSub)
        {
            TextBlock textblock = new TextBlock();
            textblock.HorizontalAlignment = HorizontalAlignment.Right;

            if (myKind == 1)
            {
                textblock.Text = String.Format("{0:##,###,##0}", myAmount);
                textblock.SetValue(Grid.ColumnProperty, 1);
                textblock.Margin = new Thickness(3, 3, 3, 3);
                textblock.FontSize = 24;
            }
            else if (myKind == 2)
            {
                textblock.Text = String.Format("{0:##,###,##0}", myAmount);
                textblock.SetValue(Grid.ColumnProperty, 3);
                textblock.Margin = new Thickness(3, 3, 3, 3);
                textblock.FontSize = 18;
            }
            else if (myKind == 3)
            {
                if (myIsSub)
                {
                    textblock.Text = "(" + String.Format("{0:##,###,##0}", myAmount) + ")";
                    textblock.SetValue(Grid.ColumnProperty, 3);
                    textblock.Margin = new Thickness(3, 3, 3, 3);
                }
                else
                {
                    textblock.Text = String.Format("{0:##,###,##0}", myAmount);
                    textblock.SetValue(Grid.ColumnProperty, 2);
                    textblock.Margin = new Thickness(3, 3, 3, 3);
                    textblock.FontWeight = FontWeights.Bold;
                }

                textblock.FontSize = 14;
            }
            else if (myKind == 4)
            {
                if (myIsSub)
                {
                    textblock.Text = "(" + String.Format("{0:##,###,##0}", myAmount) + ")";
                    textblock.Margin = new Thickness(3, 3, 3, 3);
                    textblock.SetValue(Grid.ColumnProperty, 2);
                }
                else
                {
                    textblock.Text = String.Format("{0:##,###,##0}", myAmount);
                    textblock.Margin = new Thickness(3, 3, 3, 3);
                    textblock.SetValue(Grid.ColumnProperty, 1);
                }

                textblock.FontSize = 12;
            }
            textblock.SetValue(Grid.RowProperty, myRow);

            return textblock;
        }

    }

    class SummaryEverryAccountUi
    {
        public static TextBlock GetCaptionTextBlock(string myText, int myColumn, int myRow)
        {
            TextBlock textblock = new TextBlock();

            textblock.Text = myText;

            textblock.SetValue(Grid.ColumnProperty, myColumn);
            textblock.Margin = new Thickness(5, 5, 5, 5);
            textblock.FontSize = 14;

            textblock.SetValue(Grid.RowProperty, myRow);

            return textblock;
        }
        public static TextBlock GetAmountTextBlock(long myAmount, int myColumn, int myRow)
        {
            TextBlock textblock = new TextBlock();

            textblock.Text = String.Format("{0:##,###,##0}", myAmount);
            textblock.SetValue(Grid.ColumnProperty, myColumn);
            textblock.Margin = new Thickness(5, 5, 5, 5);
            textblock.FontSize = 18;
            textblock.HorizontalAlignment = HorizontalAlignment.Right;

            textblock.SetValue(Grid.RowProperty, myRow);

            return textblock;
        }

    }

}
