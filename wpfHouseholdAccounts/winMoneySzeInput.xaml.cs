using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Data.SqlClient;
using NLog;
using System.ComponentModel;

namespace wpfHouseholdAccounts
{
    /// <summary>
    /// winMoneySzeInput.xaml の相互作用ロジック
    /// </summary>
    public partial class winMoneySzeInput : Window
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        DbConnection dbcon;
        Account account;
        List<AccountData> listAccount;
        List<AccountData> listAccountDetail;
        private bool _isTabPressed;

        private int dispinfoBeforeGridRowId = 0;

        public winMoneySzeInput()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // XMLから保存データを取得（存在する場合のみ）
            dgriMoneySzeInput.ItemsSource = MoneyInput.GetSzeSaveData();

            dbcon = new DbConnection();
            account = new Account();

            // リストボックスへ費用項目の表示
            Binding items = new Binding();

            items.Source = account.GetTwoDigitItems();
            items.Path = new PropertyPath(".");

            lstAccountExpense.SetBinding(ItemsControl.ItemsSourceProperty, items);

            listAccount = account.GetItems();
            lstAccountDetail.ItemsSource = listAccount;

            listAccountDetail = account.GetItems();
            lstAccountDetail.ItemsSource = listAccountDetail;


            dgriMoneySzeInput.Width = lgridMainSze.ActualWidth - 10;

            dgriMoneySzeInput.Columns[7].Width = new DataGridLength(CalcurateColumnWidth(dgriMoneySzeInput));
        }

        private double CalcurateColumnWidth(DataGrid datagrid)
        {
            double winX = lgridMainSze.ActualWidth - 10;
            double colTotal = 0;
            foreach (DataGridColumn col in datagrid.Columns)
            {
                if (col.Header.Equals("摘要"))
                    continue;

                DataGridLength colw = col.Width;
                double w = colw.DesiredValue;
                colTotal += w;
            }

            return winX - colTotal - 8; // ScrollBarが表示されない場合は8
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                XElement root = new XElement("SzeData");

                List<MoneyInputData> inputdata = (List<MoneyInputData>)dgriMoneySzeInput.ItemsSource;

                foreach (MoneyInputData data in inputdata)
                {
                    if (data == null)
                        continue;

                    if (data.Date == null
                        || data.DebitCode == null
                        || data.CreditCode == null)
                        continue;

                    if (data.Date.Year <= 1900
                        && data.DebitCode.Length <= 0
                        && data.CreditCode.Length <= 0)
                        continue;

                    root.Add(new XElement("MoneySzeInput"
                                        , new XElement("年月日", data.Date)
                                        , new XElement("借方コード", data.DebitCode)
                                        , new XElement("借方名", data.DebitName)
                                        , new XElement("貸方コード", data.CreditCode)
                                        , new XElement("貸方名", data.CreditName)
                                        , new XElement("金額", data.Amount)
                                        , new XElement("摘要", data.Remark)
                                ));
                }
                root.Save("SZEINPUT.xml");
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Window_Closing ", ex);
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void btnRegist_Click(object sender, RoutedEventArgs e)
        {
            var InputData = dgriMoneySzeInput.ItemsSource;

            // データベース：トランザクションを開始
            dbcon.BeginTransaction("MONEYINPUTBEGIN");

            List<MoneyInputData> regenddata = new List<MoneyInputData>();
            List<MoneyInputData> inputdata = (List<MoneyInputData>)dgriMoneySzeInput.ItemsSource;
            try
            {
                foreach (MoneyInputData data in inputdata)
                {
                    Debug.Print("id [" + data.id + "]");

                    if (data == null)
                        continue;

                    if (data.Date == null
                        || data.DebitCode == null
                        || data.CreditCode == null)
                        continue;

                    if (data.Date.Year <= 1900
                        && data.DebitCode.Length <= 0
                        && data.CreditCode.Length <= 0)
                        continue;

                    MoneyInput.InsertDbData(data, dbcon);
                    regenddata.Add(data);
                }
            }
            catch (SqlException sqlex)
            {
                Debug.Write(sqlex);
                MessageBox.Show("SqlException発生 " + sqlex.Message);
                dbcon.RollbackTransaction();
                return;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("btnRegist_Click ", ex);
                Debug.Write(ex);
                MessageBox.Show("Exception発生 " + ex.Message);
                dbcon.RollbackTransaction();
                return;
            }
            dbcon.CommitTransaction();
            foreach (MoneyInputData data in regenddata)
            {
                inputdata.Remove(data);
            }
            dgriMoneySzeInput.Items.Refresh();

        }

        private void dgriMoneySzeInput_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            try
            {
                // 新規追加行の場合は何もしない
                if (dgriMoneySzeInput.CurrentCell.Item == CollectionView.NewItemPlaceholder)
                    return;

                DataGridCell cell = e.OldFocus as DataGridCell;
                MoneyInputData data = dgriMoneySzeInput.CurrentCell.Item as MoneyInputData;
                object obj = dgriMoneySzeInput.CurrentItem;

                if (cell == null)
                {
                    if (e.OldFocus is TextBox)
                    {
                        var txtbox = e.OldFocus as TextBox;
                        cell = txtbox.Parent as DataGridCell;

                        if (cell.Column.Header.Equals("借CD")
                                || cell.Column.Header.Equals("貸CD"))
                        {
                            if (cell.Column.Header.Equals("借CD"))
                                data.DebitCode = txtbox.Text;
                            else if (cell.Column.Header.Equals("貸CD"))
                                data.CreditCode = txtbox.Text;

                            lstAccountExpense.Visibility = System.Windows.Visibility.Collapsed;
                            lstAccountDetail.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        else if (cell.Column.Header.Equals("日のみ"))
                        {
                            if (txtbox.Text.Length > 0)
                            {
                                int nowday = DateTime.Now.Day;
                                int inputday = Convert.ToInt32(txtbox.Text);

                                try
                                {
                                    if (inputday > nowday)
                                        data.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, inputday);
                                    else
                                        data.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, inputday);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    txtbox.Text = "";
                                }
                            }
                            return;
                        }
                        else if (cell.Column.Header.Equals("年月日"))
                        {
                            // 直接年月日に入力した場合は表示用から格納年月日へ変換する
                            if (txtbox.Text.Length > 0)
                            {
                                DateTime dt;
                                try
                                {
                                    dt = Convert.ToDateTime(txtbox.Text);
                                }
                                catch (FormatException)
                                {
                                    return;
                                }

                                data.Date = dt;
                            }
                            return;
                        }
                    }
                }

                if (cell == null)
                {
                    // 何もしない、後続のelse ifを実行させない
                    Debug.Print("行の新規作成");
                }
                else if (data.id == dispinfoBeforeGridRowId)
                {
                    if (cell.Column.Header.Equals("借CD"))
                    {
                        if (data.DebitCode == null || data.DebitCode.Length <= 0)
                        {
                            data.DebitCode = Account.CODE_CASH;
                            data.DebitName = account.getName(Account.CODE_CASH);
                        }
                        else
                            data.DebitName = account.getName(data.DebitCode);
                    }
                    else if (cell.Column.Header.Equals("貸CD"))
                    {
                        if (data.CreditCode == null || data.CreditCode.Length <= 0)
                        {
                            data.CreditCode = Account.CODE_CASH;
                            data.CreditName = account.getName(Account.CODE_CASH);
                        }
                        else
                            data.CreditName = account.getName(data.CreditCode);
                    }
                }

                dispinfoBeforeGridRowId = data.id;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("dgriMoneySzeInput_LostKeyboardFocus ", ex);
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        private void dgriMoneySzeInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.Print("PreviewKeyDown " + e.Key);
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                _isTabPressed = true;
                Debug.Print("PreviewKeyDown TAB or Enter");
            }
        }

        private void dgriMoneySzeInput_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            try
            {
                Debug.Print("RowEditEnding");

                // http://blogs.msdn.com/b/vinsibal/archive/2009/04/14/5-more-random-gotchas-with-the-wpf-datagrid.aspx
                // リターンキーを押下した場合は、FocusとSelectCellsが違って、入力しにくくなるので
                // リターンキーでもフォーカス、選択セルが移動するように対応
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    // タブ、エンターキーによる、かつ同イベントの移動元が前の行である場合
                    if (_isTabPressed &&
                        e.Row.Item == dgriMoneySzeInput.Items[dgriMoneySzeInput.Items.Count - 2])
                    {
                        int colIndex = 1;
                        var rowToSelect = dgriMoneySzeInput.Items[dgriMoneySzeInput.Items.Count - 1];
                        var colToSelect = dgriMoneySzeInput.Columns[colIndex];
                        int rowIndex = dgriMoneySzeInput.Items.IndexOf(rowToSelect);

                        // リターンキーからの同イベントの場合は、別スレッドでFocusを行うので
                        // FocusとSelectCellsが違ってしまう
                        // そのため、スレッド内で同処理を行うのでコメント化（URLのオリジナルとの違い）
                        //dgriMoneySzeInput.SelectedCells.Clear();
                        //dgriMoneySzeInput.SelectedCells.Add(
                        //        new DataGridCellInfo(rowToSelect, colToSelect));

                        this.Dispatcher.BeginInvoke(new DispatcherOperationCallback((param) =>
                        {
//                            System.Threading.Thread.Sleep(500);
                            var cell = DataGridHelper.GetCell(dgriMoneySzeInput, rowIndex, colIndex);
                            // クリアしないと２つのセルが選択された状態になる
                            dgriMoneySzeInput.SelectedCells.Clear();
                            cell.Focus();
                            cell.IsSelected = true;

                            //dgriMoneySzeInput.BeginEdit();
                            return null;
                        }), DispatcherPriority.Background, new object[] { null });
                    }
                    _isTabPressed = false;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("dgriMoneySzeInput_RowEditEnding ", ex);
                Debug.Print(ex.Message);
                Debug.Write(ex);
            }

        }

        private void dgriMoneySzeInput_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Debug.Print("GotKeyboardFocus [" + e.NewFocus.GetType() + "]");
            if (e.NewFocus is TextBox)
            {
                var txtbox = e.NewFocus as TextBox;

                DataGridCell cell = txtbox.Parent as DataGridCell;

                if (cell == null)
                    return;

                if (cell.Column.Header.Equals("借CD") || cell.Column.Header.Equals("貸CD"))
                {
                    lstAccountExpense.Visibility = System.Windows.Visibility.Visible;
                    lstAccountDetail.Visibility = System.Windows.Visibility.Visible;

                    ICollectionView view = CollectionViewSource.GetDefaultView(listAccountDetail);

                    new TextSearchFilterForGrid(view, txtbox);
                }
            }
        }
    }
}
