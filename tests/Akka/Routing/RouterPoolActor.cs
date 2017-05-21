﻿//-----------------------------------------------------------------------
// <copyright file="RouterPoolActor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
using Akka.Actor;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Routing
{
    /// <summary>
    /// INTERNAL API
    /// 
    /// Actor implementation for <see cref="Pool"/> routers.
    /// </summary>
    internal class RouterPoolActor : RouterActor
    {
        private readonly SupervisorStrategy _supervisorStrategy;

        /// <summary>
        /// TBD
        /// </summary>
        protected Pool Pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouterPoolActor"/> class.
        /// </summary>
        /// <param name="supervisorStrategy">The supervisor strategy.</param>
        /// <exception cref="ActorInitializationException">TBD</exception>
        public RouterPoolActor(SupervisorStrategy supervisorStrategy)
        {
            _supervisorStrategy = supervisorStrategy;

            var pool = Cell.RouterConfig as Pool;
            if (pool != null)
            {
                Pool = pool;
            }
            else
            {
                throw new ActorInitializationException($"RouterPoolActor can only be used with Pool, not {Cell.RouterConfig.GetType()}");
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        protected override SupervisorStrategy SupervisorStrategy()
        {
            return _supervisorStrategy;
        }

        /// <summary>
        /// Called when [receive].
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void OnReceive(object message)
        {
            var poolSize = message as AdjustPoolSize;
            if (poolSize != null)
            {
                if (poolSize.Change > 0)
                {
                    var newRoutees = Vector.Fill<Routee>(poolSize.Change)(() => Pool.NewRoutee(Cell.RouteeProps, Context));
                    Cell.AddRoutees(newRoutees);
                }
                else if (poolSize.Change < 0)
                {
                    var currentRoutees = Cell.Router.Routees.ToArray();

                    var abandon = currentRoutees
                        .Skip(currentRoutees.Length + poolSize.Change)
                        .ToList();

                    Cell.RemoveRoutees(abandon, true);
                }
            }
            else
            {
                base.OnReceive(message);
            }
        }


    }
}
