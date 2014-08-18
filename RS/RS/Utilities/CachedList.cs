using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Utilities
{
    public class CachedList<T> : IEnumerable<T>
    {
        //Last refreah time for the list
        private DateTime? LastRefreshed = null;

        //The time interval to auto-refresh the list's data is requested from this object again
        //If this doesn't have a value, the cached list is never refreshed (aside from the initial population, or manual re-population)
        private TimeSpan? AutoRefreshInterval = null;

        public CachedList(Func<List<T>> RefreshFunction, TimeSpan? AutoRefreshInterval = null)
        {
            this.RefreshFunction = RefreshFunction;
            this.AutoRefreshInterval = AutoRefreshInterval;

            _CachedList = new List<T>();
        }

        public Func<List<T>> RefreshFunction { get; set; }

        public void Refresh()
        {
            //Update the time once prior to calling the refresh function to prevent recursive refreshing on linked object
            LastRefreshed = DateTime.Now;

            //Update the cached list by calling the refresh function for this list
            _CachedList = RefreshFunction();

            //Update the last refreshed time again now that the refresh is actually completed
            LastRefreshed = DateTime.Now;
        }

        private List<T> _CachedList;

        public List<T> List
        {
            //Before returing the list, see if an auto-refresh is required based on properties of this object
            get
            {
                //Default to needing to be refreshed
                bool RefreshRequired = true;

                //If any refresh has happened, check to see if another refresh is needed
                if (LastRefreshed.HasValue)
                {
                    //If the auto refresh interval is set to anything, check if it's currently time to do another refresh now
                    if (AutoRefreshInterval.HasValue)
                    {
                        //Determine the amount of time passed since the last refresh
                        TimeSpan timeSinceLastRefresh = DateTime.Now - LastRefreshed.Value;

                        //If it was already refreshed in less time than the interval, then we don't need to refresh now...
                        if (timeSinceLastRefresh <= AutoRefreshInterval)
                        {
                            RefreshRequired = false;
                        }
                    }
                    else
                    {
                        //If the auto refresh interval doesn't have a value, then we never auto-refresh
                        RefreshRequired = false;
                    }
                }

                if (RefreshRequired)
                {
                    this.Refresh();
                }

                return _CachedList;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return List.GetEnumerator();
        }

    }
}
