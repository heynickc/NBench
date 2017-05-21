﻿//-----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Routing;
using Akka.Util;

namespace Akka.Actor
{
    /// <summary>
    ///     Settings are the overall ActorSystem Settings which also provides a convenient access to the Config object.
    ///     For more detailed information about the different possible configuration options, look in the Akka .NET
    ///     Documentation under "Configuration"
    /// </summary>
    public class Settings
    {
        private readonly Config _userConfig;
        private Config _fallbackConfig;

        /// <summary>
        /// Combines the user config and the fallback chain of configs
        /// </summary>
        private void RebuildConfig()
        {
            Config = _userConfig.SafeWithFallback(_fallbackConfig);

            //if we get a new config definition loaded after all ActorRefProviders have been started, such as Akka.Persistence...
            if(System != null && System.Dispatchers != null)
                System.Dispatchers.ReloadPrerequisites(new DefaultDispatcherPrerequisites(System.EventStream, System.Scheduler, this, System.Mailboxes));
        }

        /// <summary>
        /// Injects a system config at the top of the fallback chain
        /// </summary>
        /// <param name="config">TBD</param>
        public void InjectTopLevelFallback(Config config)
        {
            _fallbackConfig = config.SafeWithFallback(_fallbackConfig);
            RebuildConfig();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Settings" /> class.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <param name="config">The configuration.</param>
        /// <exception cref="ConfigurationException">
        /// This exception is thrown if the 'akka.actor.provider' configuration item is not a valid type name or a valid actor ref provider.
        /// </exception>
        public Settings(ActorSystem system, Config config)
        {
            _userConfig = config;
            _fallbackConfig = ConfigurationFactory.Default();
            RebuildConfig();

            System = system;
            
            ConfigVersion = Config.GetString("akka.version");
            ProviderClass = GetProviderClass(Config.GetString("akka.actor.provider"));
            var providerType = Type.GetType(ProviderClass);
            if (providerType == null)
                throw new ConfigurationException($"'akka.actor.provider' is not a valid type name : '{ProviderClass}'");
            if (!typeof(IActorRefProvider).IsAssignableFrom(providerType))
                throw new ConfigurationException($"'akka.actor.provider' is not a valid actor ref provider: '{ProviderClass}'");
            
            SupervisorStrategyClass = Config.GetString("akka.actor.guardian-supervisor-strategy");

            AskTimeout = Config.GetTimeSpan("akka.actor.ask-timeout", allowInfinite: true);
            CreationTimeout = Config.GetTimeSpan("akka.actor.creation-timeout");
            UnstartedPushTimeout = Config.GetTimeSpan("akka.actor.unstarted-push-timeout");

            SerializeAllMessages = Config.GetBoolean("akka.actor.serialize-messages");
            SerializeAllCreators = Config.GetBoolean("akka.actor.serialize-creators");

            LogLevel = Config.GetString("akka.loglevel");
            StdoutLogLevel = Config.GetString("akka.stdout-loglevel");
            Loggers = Config.GetStringList("akka.loggers");
            LoggersDispatcher = Config.GetString("akka.loggers-dispatcher");
            LoggerStartTimeout = Config.GetTimeSpan("akka.logger-startup-timeout");

            //handled
            LogConfigOnStart = Config.GetBoolean("akka.log-config-on-start");
            LogDeadLetters = 0;
            switch (Config.GetString("akka.log-dead-letters"))
            {
                case "on":
                case "true":
                    LogDeadLetters = int.MaxValue;
                    break;
                case "off":
                case "false":
                    LogDeadLetters = 0;
                    break;
                default:
                    LogDeadLetters = Config.GetInt("akka.log-dead-letters");
                    break;
            }
            LogDeadLettersDuringShutdown = Config.GetBoolean("akka.log-dead-letters-during-shutdown");
            AddLoggingReceive = Config.GetBoolean("akka.actor.debug.receive");
            DebugAutoReceive = Config.GetBoolean("akka.actor.debug.autoreceive");
            DebugLifecycle = Config.GetBoolean("akka.actor.debug.lifecycle");
            FsmDebugEvent = Config.GetBoolean("akka.actor.debug.fsm");
            DebugEventStream = Config.GetBoolean("akka.actor.debug.event-stream");
            DebugUnhandledMessage = Config.GetBoolean("akka.actor.debug.unhandled");
            DebugRouterMisconfiguration = Config.GetBoolean("akka.actor.debug.router-misconfiguration");
            Home = Config.GetString("akka.home") ?? "";
            DefaultVirtualNodesFactor = Config.GetInt("akka.actor.deployment.default.virtual-nodes-factor");

            SchedulerClass = Config.GetString("akka.scheduler.implementation");
            SchedulerShutdownTimeout = Config.GetTimeSpan("akka.scheduler.shutdown-timeout");
            //TODO: dunno.. we don't have FiniteStateMachines, don't know what the rest is
            /*              
                final val SchedulerClass: String = getString("akka.scheduler.implementation")
                final val Daemonicity: Boolean = getBoolean("akka.daemonic")                
                final val DefaultVirtualNodesFactor: Int = getInt("akka.actor.deployment.default.virtual-nodes-factor")
             */
        }

        private static string GetProviderClass(string provider)
        {
            switch (provider)
            {
                case "local": return typeof(LocalActorRefProvider).FullName;
                case "remote": return "Akka.Remote.RemoteActorRefProvider, Akka.Remote";
                case "cluster": return "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster";
                default: return provider;
            }
        }

        /// <summary>
        ///     Gets the system.
        /// </summary>
        /// <value>The system.</value>
        public ActorSystem System { get; private set; }

        /// <summary>
        ///     Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public Config Config { get; private set; }

        /// <summary>
        ///     Gets the configuration version.
        /// </summary>
        /// <value>The configuration version.</value>
        public string ConfigVersion { get; private set; }

        /// <summary>
        ///     Gets the provider class.
        /// </summary>
        /// <value>The provider class.</value>
        public string ProviderClass { get; private set; }

        /// <summary>
        ///     Gets the supervisor strategy class.
        /// </summary>
        /// <value>The supervisor strategy class.</value>
        public string SupervisorStrategyClass { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [serialize all messages].
        /// </summary>
        /// <value><c>true</c> if [serialize all messages]; otherwise, <c>false</c>.</value>
        public bool SerializeAllMessages { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [serialize all creators].
        /// </summary>
        /// <value><c>true</c> if [serialize all creators]; otherwise, <c>false</c>.</value>
        public bool SerializeAllCreators { get; private set; }

        /// <summary>
        ///     Gets the default timeout for <see cref="Futures.Ask" /> calls.
        /// </summary>
        /// <value>The ask timeout.</value>
        public TimeSpan AskTimeout { get; private set; }

        /// <summary>
        ///     Gets the creation timeout.
        /// </summary>
        /// <value>The creation timeout.</value>
        public TimeSpan CreationTimeout { get; private set; }

        /// <summary>
        ///     Gets the unstarted push timeout.
        /// </summary>
        /// <value>The unstarted push timeout.</value>
        public TimeSpan UnstartedPushTimeout { get; private set; }

        /// <summary>
        ///     Gets the log level.
        /// </summary>
        /// <value>The log level.</value>
        public string LogLevel { get; private set; }

        /// <summary>
        ///     Gets the stdout log level.
        /// </summary>
        /// <value>The stdout log level.</value>
        public string StdoutLogLevel { get; private set; }

        /// <summary>
        ///     Gets the loggers.
        /// </summary>
        /// <value>The loggers.</value>
        public IList<string> Loggers { get; private set; }

        /// <summary>
        ///     Gets the default loggers dispatcher.
        /// </summary>
        /// <value>The loggers dispatcher.</value>
        public string LoggersDispatcher { get; private set; }

        /// <summary>
        ///     Gets the logger start timeout.
        /// </summary>
        /// <value>The logger start timeout.</value>
        public TimeSpan LoggerStartTimeout { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [log configuration on start].
        /// </summary>
        /// <value><c>true</c> if [log configuration on start]; otherwise, <c>false</c>.</value>
        public bool LogConfigOnStart { get; private set; }

        /// <summary>
        ///     Gets the log dead letters.
        /// </summary>
        /// <value>The log dead letters.</value>
        public int LogDeadLetters { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [log dead letters during shutdown].
        /// </summary>
        /// <value><c>true</c> if [log dead letters during shutdown]; otherwise, <c>false</c>.</value>
        public bool LogDeadLettersDuringShutdown { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [add logging receive].
        /// </summary>
        /// <value><c>true</c> if [add logging receive]; otherwise, <c>false</c>.</value>
        public bool AddLoggingReceive { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [debug automatic receive].
        /// </summary>
        /// <value><c>true</c> if [debug automatic receive]; otherwise, <c>false</c>.</value>
        public bool DebugAutoReceive { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [debug event stream].
        /// </summary>
        /// <value><c>true</c> if [debug event stream]; otherwise, <c>false</c>.</value>
        public bool DebugEventStream { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [debug unhandled message].
        /// </summary>
        /// <value><c>true</c> if [debug unhandled message]; otherwise, <c>false</c>.</value>
        public bool DebugUnhandledMessage { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [debug router misconfiguration].
        /// </summary>
        /// <value><c>true</c> if [debug router misconfiguration]; otherwise, <c>false</c>.</value>
        public bool DebugRouterMisconfiguration { get; private set; }

        /// <summary>
        ///     Gets the home.
        /// </summary>
        /// <value>The home.</value>
        public string Home { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [debug lifecycle].
        /// </summary>
        /// <value><c>true</c> if [debug lifecycle]; otherwise, <c>false</c>.</value>
        public bool DebugLifecycle { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        public bool FsmDebugEvent { get; private set; }

        /// <summary>
        /// The number of default virtual nodes to use with <see cref="ConsistentHashingRoutingLogic"/>.
        /// </summary>
        public int DefaultVirtualNodesFactor { get; private set; }

        /// <summary>
        /// Gets the scheduler implementation used by this system.
        /// </summary>
        public string SchedulerClass { get; private set; }

        /// <summary>
        /// TBD
        /// </summary>
        public TimeSpan SchedulerShutdownTimeout { get; private set; }

        /// <summary>
        ///     Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Config.ToString();
        }
    }
}
