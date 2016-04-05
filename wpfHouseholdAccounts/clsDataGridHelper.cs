using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Threading.Tasks;

namespace wpfHouseholdAccounts
{
    public static class DataGridHelper
    {
        /// <summary>
        /// Gets the visual child of an element
        /// http://code.google.com/p/artur02/source/browse/trunk/DataGridExtensions/DataGridHelper.cs
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="parent">The parent of the expected element</param>
        /// <returns>A visual child</returns>
        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        public static DependencyObject GetParent(this DependencyObject depObject, string name, bool recursive)
        {
            var parent = VisualTreeHelper.GetParent(depObject) as FrameworkElement;
            if (parent != null)
            {
                if (parent.Name == name)
                    return parent;

                if (recursive)
                    return parent.GetParent(name, true);
            }

            return null;
        }

        public static DependencyObject GetParent(this DependencyObject depObject, Type t, bool recursive)
        {
            var parent = VisualTreeHelper.GetParent(depObject) as FrameworkElement;
            if (parent != null)
            {
                Type pt = parent.GetType();
                if (pt == t)
                    return parent;

                if (recursive)
                    return parent.GetParent(t, true);
            }

            return null;
        }

        public static DependencyObject GetParentByBaseType(this DependencyObject depObject, Type t, bool recursive)
        {
            var parent = VisualTreeHelper.GetParent(depObject) as FrameworkElement;
            if (parent != null)
            {
                Type pt = parent.GetType();
                if (pt.BaseType == t)
                    return parent;

                if (recursive)
                    return parent.GetParentByBaseType(t, true);
            }

            return null;
        }


        /// <summary>
        /// Gets the specified cell of the DataGrid
        /// </summary>
        /// <param name="grid">The DataGrid instance</param>
        /// <param name="row">The row of the cell</param>
        /// <param name="column">The column index of the cell</param>
        /// <returns>A cell of the DataGrid</returns>
        public static DataGridCell GetCell(this DataGrid grid, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

                if (presenter == null)
                {
                    grid.ScrollIntoView(row, grid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }

                if (presenter == null)
                    return null;

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);

                return cell;
            }
            return null;
        }

        /// <summary>
        /// Gets the specified cell of the DataGrid
        /// </summary>
        /// <param name="grid">The DataGrid instance</param>
        /// <param name="row">The row index of the cell</param>
        /// <param name="column">The column index of the cell</param>
        /// <returns>A cell of the DataGrid</returns>
        public static DataGridCell GetCell(this DataGrid grid, int row, int column)
        {
            DataGridRow rowContainer = grid.GetRow(row);
            return grid.GetCell(rowContainer, column);
        }

        /// <summary>
        /// Gets the specified row of the DataGrid
        /// </summary>
        /// <param name="grid">The DataGrid instance</param>
        /// <param name="index">The index of the row</param>
        /// <returns>A row of the DataGrid</returns>
        public static DataGridRow GetRow(this DataGrid grid, int index)
        {
            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // May be virtualized, bring into view and try again.
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.Items[index]);
                row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        /// <summary>
        /// Gets the selected row of the DataGrid
        /// </summary>
        /// <param name="grid">The DataGrid instance</param>
        /// <returns></returns>
        public static DataGridRow GetSelectedRow(this DataGrid grid)
        {
            return (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem);
        }

        // ここから追加
        static public int GetRowIndex(DataGrid dg, DataGridCellInfo dgci)
        {
            DataGridRow dgrow = (DataGridRow)dg.ItemContainerGenerator.ContainerFromItem(dgci.Item);
            return dgrow.GetIndex();
        }

        static public int GetColIndex(DataGrid dg, DataGridCellInfo dgci)
        {
            return dgci.Column.DisplayIndex;
        }

    }
}
