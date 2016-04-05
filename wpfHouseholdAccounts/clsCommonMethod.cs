using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace wpfHouseholdAccounts
{
    class CommonMethod
    {
        public static long GetLong(string myStrLong)
        {
            long data = 0;
            data = Convert.ToInt64(myStrLong);

            return data;
        }
        public static DateTime GetDateTime(string myStrLDateTime)
        {
            if (myStrLDateTime.Length <= 0)
                return new DateTime(1900,1,1);

            DateTime dt;
            dt = Convert.ToDateTime(myStrLDateTime);

            return dt;
        }
        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }
        public static List<T> FindVisualChild<T>(UIElement myPanel, string myType) where T : UIElement
        {
            UIElement parent = myPanel;
            UIElement child;
            T childT = null;
            int idx = 0;
            int max = VisualTreeHelper.GetChildrenCount(parent);

            List<T> list = new List<T>();
            while (idx < max)
            {
                child = VisualTreeHelper.GetChild(parent, idx) as UIElement;

                string type = child.GetType().ToString();

                if (type.IndexOf(myType) >= 0)
                {
                    childT = child as T;
                    list.Add(childT);
                }
                //Debug.Print(type);
                idx++;
            }
            return list;
        }
    }
}
