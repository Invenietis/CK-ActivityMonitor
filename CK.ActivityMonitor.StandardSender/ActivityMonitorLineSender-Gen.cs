// <auto-generated/>
#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.ActivityMonitor.StandardSender\ActivityMonitorSend-Gen.cs 
* (and CK.ActivityMonitor.StandardSender\ActivityMonitorSend-Gen.tt ) 
* is part of CK-Framework. 
*  
* CK-Framework is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CK-Framework is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CK-Framework.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright � 2007-2017, 
*     Invenietis <http://www.invenietis.com>,
*     In�Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion
//
// This file is generated by ActivityMonitorSend.Gen.tt
//
using System;
using System.Runtime.CompilerServices;
#nullable enable

namespace CK.Core
{
    /// <summary>
    /// Provides Send extension methods for <see cref="IActivityMonitorLineSender"/> and <see cref="ActivityMonitorGroupSender"/>.
    /// </summary>
    public static partial class ActivityMonitorSendExtension
    {
		#region Text  
		 
		/// <summary>
        /// Sends a text.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="text">The text of the log.</param>
        static public void Send( this IActivityMonitorLineSender @this, string? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, text );
        }
		 
		/// <summary>
        /// Sends a formatted text.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="format">The text format of the log with 1 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
        static public void Send( this IActivityMonitorLineSender @this, string? format, object? arg0 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			if( arg0 is Exception ) throw new ArgumentException( ActivityMonitorSenderExtension.PossibleWrongOverloadUseWithException, "arg0" );
			s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arg0 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="format">The text format of the log with 2 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
        static public void Send( this IActivityMonitorLineSender @this, string? format, object? arg0, object? arg1 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arg0, arg1 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="format">The text format of the log with 3 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
		/// <param name="arg2">Parameter to format (placeholder {2}).</param>
        static public void Send( this IActivityMonitorLineSender @this, string? format, object? arg0, object? arg1, object? arg2 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arg0, arg1, arg2 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="format">The text format of the log with 4 placeholders.</param>
		/// <param name="arguments">Multiple parameters to format.</param>
        static public void Send( this IActivityMonitorLineSender @this, string? format, params object?[] arguments )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arguments ) );
        }
		 
		/// <summary>
        /// Sends a text obtained through a delegate.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        public static void Send( this IActivityMonitorLineSender @this, Func<string?>? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
            s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, text == null ? null : text() );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T">Type of the parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param">Parameter of the <paramref name="text"/> delegate.</param>
        public static void Send<T>( this IActivityMonitorLineSender @this, Func<T,string?>? text, T param )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, text == null ? null : text(param) );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2>( this IActivityMonitorLineSender @this, Func<T1,T2,string?>? text, T1 param1, T2 param2 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend(  null, ActivityMonitor.Tags.Empty, text == null ? null : text(param1,param2) );
        }

		/// <summary>
        /// Sends a log with a text obtained through a parameterized delegate.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T3">Type of the third parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param3">Third parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2,T3>( this IActivityMonitorLineSender @this, Func<T1,T2,T3,string?>? text, T1 param1, T2 param2, T3 param3 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( null, ActivityMonitor.Tags.Empty, text == null ? null : text(param1,param2,param3) );
        }

		#endregion Text 

		#region TaggedText  
		 
		/// <summary>
        /// Sends a text with associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="text">The text of the log.</param>
        static public void Send( this IActivityMonitorLineSender @this, CKTrait? tags, string? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, tags, text );
        }
		 
		/// <summary>
        /// Sends a formatted text with associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 1 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
        static public void Send( this IActivityMonitorLineSender @this, CKTrait? tags, string? format, object? arg0 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			if( arg0 is Exception ) throw new ArgumentException( ActivityMonitorSenderExtension.PossibleWrongOverloadUseWithException, "arg0" );
			s.InitializeAndSend( null, tags, format == null ? null : String.Format( format, arg0 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 2 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
        static public void Send( this IActivityMonitorLineSender @this, CKTrait? tags, string? format, object? arg0, object? arg1 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, tags, format == null ? null : String.Format( format, arg0, arg1 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 3 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
		/// <param name="arg2">Parameter to format (placeholder {2}).</param>
        static public void Send( this IActivityMonitorLineSender @this, CKTrait? tags, string? format, object? arg0, object? arg1, object? arg2 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, tags, format == null ? null : String.Format( format, arg0, arg1, arg2 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 4 placeholders.</param>
		/// <param name="arguments">Multiple parameters to format.</param>
        static public void Send( this IActivityMonitorLineSender @this, CKTrait? tags, string? format, params object?[] arguments )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( null, tags, format == null ? null : String.Format( format, arguments ) );
        }
		 
		/// <summary>
        /// Sends a text obtained through a delegate with associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        public static void Send( this IActivityMonitorLineSender @this, CKTrait? tags, Func<string?>? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
            s.InitializeAndSend( null, tags, text == null ? null : text() );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate with associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T">Type of the parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param">Parameter of the <paramref name="text"/> delegate.</param>
        public static void Send<T>( this IActivityMonitorLineSender @this, CKTrait? tags, Func<T,string?>? text, T param )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( null, tags, text == null ? null : text(param) );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate with associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2>( this IActivityMonitorLineSender @this, CKTrait? tags, Func<T1,T2,string?>? text, T1 param1, T2 param2 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend(  null, tags, text == null ? null : text(param1,param2) );
        }

		/// <summary>
        /// Sends a log with a text obtained through a parameterized delegate with associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T3">Type of the third parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param3">Third parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2,T3>( this IActivityMonitorLineSender @this, CKTrait? tags, Func<T1,T2,T3,string?>? text, T1 param1, T2 param2, T3 param3 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( null, tags, text == null ? null : text(param1,param2,param3) );
        }

		#endregion TaggedText 

		#region ExceptionText  
		 
		/// <summary>
        /// Sends a log with an exception.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, null );
        }
		 
		/// <summary>
        /// Sends a text with an exception.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="text">The text of the log.</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, string? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, text );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="format">The text format of the log with 1 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, string? format, object? arg0 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arg0 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="format">The text format of the log with 2 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, string? format, object? arg0, object? arg1 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arg0, arg1 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="format">The text format of the log with 3 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
		/// <param name="arg2">Parameter to format (placeholder {2}).</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, string? format, object? arg0, object? arg1, object? arg2 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arg0, arg1, arg2 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="format">The text format of the log with 4 placeholders.</param>
		/// <param name="arguments">Multiple parameters to format.</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, string? format, params object?[] arguments )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, format == null ? null : String.Format( format, arguments ) );
        }
		 
		/// <summary>
        /// Sends a text obtained through a delegate with an exception.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        public static void Send( this IActivityMonitorLineSender @this, Exception? ex, Func<string?>? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
            s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, text == null ? null : text() );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate with an exception.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T">Type of the parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param">Parameter of the <paramref name="text"/> delegate.</param>
        public static void Send<T>( this IActivityMonitorLineSender @this, Exception? ex, Func<T,string?>? text, T param )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, text == null ? null : text(param) );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate with an exception.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2>( this IActivityMonitorLineSender @this, Exception? ex, Func<T1,T2,string?>? text, T1 param1, T2 param2 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend(  ex, ActivityMonitor.Tags.Empty, text == null ? null : text(param1,param2) );
        }

		/// <summary>
        /// Sends a log with a text obtained through a parameterized delegate with an exception.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T3">Type of the third parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param3">Third parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2,T3>( this IActivityMonitorLineSender @this, Exception? ex, Func<T1,T2,T3,string?>? text, T1 param1, T2 param2, T3 param3 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( ex, ActivityMonitor.Tags.Empty, text == null ? null : text(param1,param2,param3) );
        }

		#endregion ExceptionText 

		#region ExceptionTaggedText  
		 
		/// <summary>
        /// Sends a log with an exception and associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, tags, null );
        }
		 
		/// <summary>
        /// Sends a text with an exception and associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="text">The text of the log.</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, string? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, tags, text );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception and associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 1 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, string? format, object? arg0 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, tags, format == null ? null : String.Format( format, arg0 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception and associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 2 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, string? format, object? arg0, object? arg1 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, tags, format == null ? null : String.Format( format, arg0, arg1 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception and associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 3 placeholders.</param>
		/// <param name="arg0">Parameter to format (placeholder {0}).</param>
		/// <param name="arg1">Parameter to format (placeholder {1}).</param>
		/// <param name="arg2">Parameter to format (placeholder {2}).</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, string? format, object? arg0, object? arg1, object? arg2 )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, tags, format == null ? null : String.Format( format, arg0, arg1, arg2 ) );
        }
		 
		/// <summary>
        /// Sends a formatted text with an exception and associated tags.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param>
		/// <param name="format">The text format of the log with 4 placeholders.</param>
		/// <param name="arguments">Multiple parameters to format.</param>
        static public void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, string? format, params object?[] arguments )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
			s.InitializeAndSend( ex, tags, format == null ? null : String.Format( format, arguments ) );
        }
		 
		/// <summary>
        /// Sends a text obtained through a delegate with an exception and associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        public static void Send( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, Func<string?>? text )
        {
			ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
            if( s.IsRejected ) return;
            s.InitializeAndSend( ex, tags, text == null ? null : text() );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate with an exception and associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T">Type of the parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param">Parameter of the <paramref name="text"/> delegate.</param>
        public static void Send<T>( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, Func<T,string?>? text, T param )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( ex, tags, text == null ? null : text(param) );
        }

		/// <summary>
        /// Sends a text obtained through a parameterized delegate with an exception and associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2>( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, Func<T1,T2,string?>? text, T1 param1, T2 param2 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend(  ex, tags, text == null ? null : text(param1,param2) );
        }

		/// <summary>
        /// Sends a log with a text obtained through a parameterized delegate with an exception and associated tags.
		/// The delegate will be called only if the log is not filtered.
        /// </summary>
        /// <typeparam name="T1">Type of the first parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T2">Type of the second parameter that <paramref name="text"/> accepts.</typeparam>
        /// <typeparam name="T3">Type of the third parameter that <paramref name="text"/> accepts.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorLineSender"/> object.</param>
		/// <param name="ex">The exception. Must not be null.</param>
		/// <param name="tags">Tags for the log.</param> 
        /// <param name="text">Function that returns a string. Must not be null.</param>
        /// <param name="param1">First parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param2">Second parameter for the <paramref name="text"/> delegate.</param>
        /// <param name="param3">Third parameter for the <paramref name="text"/> delegate.</param>
        public static void Send<T1,T2,T3>( this IActivityMonitorLineSender @this, Exception? ex, CKTrait? tags, Func<T1,T2,T3,string?>? text, T1 param1, T2 param2, T3 param3 )
        {
            ActivityMonitorLineSender s = (ActivityMonitorLineSender)@this;
			if( s.IsRejected ) return;
            s.InitializeAndSend( ex, tags, text == null ? null : text(param1,param2,param3) );
        }

		#endregion ExceptionTaggedText 

		 
	}
}