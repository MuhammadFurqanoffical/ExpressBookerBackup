using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExpressBookerProject.Utilities
{
    public class Singleton
    {
        private static Singleton _instance;
        private static readonly object _lockObject = new object();

        // Private constructor to prevent instantiation from outside
        private Singleton() { }

        // Public static method to get the instance of the Singleton class
        public static Singleton GetInstance()
        {
            // Double-check locking for thread safety
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new Singleton();
                    }
                }
            }
            return _instance;
        }
    }
}

  