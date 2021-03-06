﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Bumblebee.Interfaces;

using OpenQA.Selenium;

namespace Bumblebee.Setup
{
	/// <summary>
	/// Creates thread-safe instances of sessions.
	/// </summary>
	/// <typeparam name="TSession"></typeparam>
	public class Threaded<TSession> where TSession : Session
	{
		public const string InvalidSessionTypeFormat = "The instance type specified ({0}) is not a valid session.  It must have a constructor that takes an IDriverEnvironment and/or ISettings.";
		private static readonly ThreadLocal<TSession> _session = new ThreadLocal<TSession>();

		private static TSession CurrentSession
		{
			get { return _session.Value; }
			set { _session.Value = value; }
		}

		/// <summary>
		/// Allows the creation of a Session-based type using a type derived from IDriverEnvironment that the system can initialize with a parameterless constructor.
		/// </summary>
		/// <typeparam name="TDriverEnvironment"></typeparam>
		/// <returns></returns>
		public static TSession With<TDriverEnvironment>() where TDriverEnvironment : IDriverEnvironment, new()
		{
			return With(new TDriverEnvironment());
		}

		public static TSession With<TDriverEnvironment>(ISettings settings) where TDriverEnvironment : IDriverEnvironment, new()
		{
			return With(new TDriverEnvironment(), settings);
		}

		/// <summary>
		/// Allows the creation of a Session-based type using an instance of a type of IDriverEnvironment.
		/// </summary>
		/// <param name="environment"></param>
		/// <returns></returns>
		public static TSession With(IDriverEnvironment environment)
		{
			if (CurrentSession != null)
			{
				CurrentSession.End();
				CurrentSession = null;
			}

			CurrentSession = GetInstanceOf<TSession>(environment);
			return CurrentSession;
		}

		public static TSession With(IDriverEnvironment environment, ISettings settings)
		{
			if (CurrentSession != null)
			{
				CurrentSession.End();
				CurrentSession = null;
			}

			CurrentSession = GetInstanceOf<TSession>(environment, settings);
			return CurrentSession;
		}

		private static T GetInstanceOf<T>(params object[] constructorArgs)
			where T:Session
		{
			var type = typeof(T);

			IList<Type> constructorSignature = constructorArgs.Select(arg => arg.GetType()).ToList();
			
			var constructor = type.GetConstructor(constructorSignature.ToArray());

			if (constructor != null) return constructor.Invoke(constructorArgs.ToArray()) as T;

			var message = String.Format(InvalidSessionTypeFormat, type);
			throw new ArgumentException(message);
		}

		public static TBlock CurrentBlock<TBlock>(IWebElement tag = null) where TBlock : IBlock
		{
			if (CurrentSession == null)
			{
				throw new NullReferenceException("You cannot access the CurrentBlock without first initializing the Session by calling With<TDriverEnvironment>().");
			}

			return CurrentSession.CurrentBlock<TBlock>();
		}

		public static void End()
		{
			if (CurrentSession != null)
			{
				CurrentSession.End();

				CurrentSession = null;
			}
		}
	}
}
