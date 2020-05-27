
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

// http://archive.visualstudiomagazine.com/2005_02/magazine/columns/net2themax/Listing2.aspx

namespace Util
{
    public class UniversalComparer<T> : IComparer, IComparer<T>
    {
        private SortKey[] sortKeys;
        private bool m_errMsgShowed = false;
        private readonly string m_sortOrder = "";

        public UniversalComparer(string sort)
        {
            if (string.IsNullOrEmpty(sort)) sort = "";
            m_sortOrder = sort;

            Type type = typeof(T);
            // Split the list of properties.
            string[] props = sort.Split(',');
            // Prepare the array that holds information on sort criteria.
            sortKeys = new SortKey[props.Length - 1 + 1];

            // Parse the sort string.
            for (int i = 0; i <= props.Length - 1; i++)
            {
                // Get the N-th member name.
                string memberName = props[i].Trim();
                if (memberName.ToLower().EndsWith(" desc"))
                {
                    // Discard the DESC qualifier.
                    sortKeys[i].Descending = true;
                    memberName = memberName.Remove(memberName.Length - 5).TrimEnd();
                }
                // Search for a field or a property with this name.
                sortKeys[i].FieldInfo = type.GetField(memberName);
                sortKeys[i].sMemberName = memberName;
                if (sortKeys[i].FieldInfo == null)
                    sortKeys[i].PropertyInfo = type.GetProperty(memberName);
            }
        }

        public int Compare(object x, object y)
        {
            // Implementation of IComparer.Compare
            return Compare((T)x, (T)y);
        }

        public int Compare(T x, T y)
        {

            // Implementation of IComparer(Of T).Compare

            // Deal with simplest cases first.
            if (x == null) 
            {
                if (y == null) return 0;// Two null objects are equal.
                return -1;// A null object is less than any non-null object.
            }
            else if (y == null) return 1; // Any non-null object is greater than a null object.

            // Iterate over all the sort keys.
            for (int i = 0; i <= sortKeys.Length - 1; i++)
            {
                object value1;
                object value2;
                SortKey sortKey = sortKeys[i];
                // Read either the field or the property.
                if (sortKey.FieldInfo != null)
                {
                    value1 = sortKey.FieldInfo.GetValue(x);
                    value2 = sortKey.FieldInfo.GetValue(y);
                }
                else
                {
                    if (sortKey.PropertyInfo == null)
                    {
                        if (!m_errMsgShowed)
                        {
                            string errMsg = 
                                "UniversalComparer:Compare: A comparison key was not found:" + 
                                Environment.NewLine +
                                " the indicated field does not exist" + Environment.NewLine +
                                " or is not public!" + Environment.NewLine +
                                typeof(T).ToString() + ": " + sortKeys[i].sMemberName + ": " + 
                                m_sortOrder;
                            Debug.WriteLine(errMsg);
                            //MessageBox.Show(errMsg, "App title", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            m_errMsgShowed = true;
                        }
                        return 0;
                    }
                    value1 = sortKey.PropertyInfo.GetValue(x, null);
                    value2 = sortKey.PropertyInfo.GetValue(y, null);
                }

                int res;
                if (value1 == null && value2 == null)
                    // Two null objects are equal.
                    res = 0;
                else if (value1 == null)
                    // A null object is always less than a non-null object.
                    res = -1;
                else if (value2 == null)
                    // Any object is greater than a null object.
                    res = 1;
                else
                    // Compare the two values, assuming that they support IComparable.
                    res = ((IComparable)value1).CompareTo(value2);

                // If values are different, return this value to caller.
                if (res != 0)
                {
                    // Negate it if sort direction is descending.
                    if (sortKey.Descending)
                        res = -res;
                    return res;
                }
            }

            // If we get here the two objects are equal.
            return 0;
        }

        private struct SortKey // Nested type to store detail on sort keys
        {
            public FieldInfo FieldInfo;
            public PropertyInfo PropertyInfo;
            // True if sort is descending.
            public bool Descending;
            public string sMemberName;
        }
    }
}