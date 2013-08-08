// ****************************************************************
// Copyright 2008, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using System;

namespace PclUnit.Style.Nunit
{
    /// <summary>
    /// Used on a method, marks the test with a timeout value in milliseconds. 
    /// The test will be run in a separate thread and is cancelled if the timeout 
    /// is exceeded. Used on a method or assembly, sets the default timeout 
    /// for all contained test methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class TimeoutAttribute : Attribute
    {
        public int Timeout { get; set; }

        /// <summary>
        /// Construct a TimeoutAttribute given a time in milliseconds
        /// </summary>
        /// <param name="timeout">The timeout value in milliseconds</param>
        public TimeoutAttribute(int timeout)
        {
            Timeout = timeout;
        }
    }
}
