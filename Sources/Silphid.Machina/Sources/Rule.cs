﻿using System;
using Silphid.Requests;

namespace Silphid.Machina
{
    public class Rule : IRule, IRequestHandler
    {
        private readonly Predicate<object> _predicate;
        private Func<IRequest, IRequest> _handler;

        public Rule(Predicate<object> predicate)
        {
            _predicate = predicate;
        }

        public bool MatchesState(object state) => _predicate(state);
        public IRequest Handle(IRequest request) => _handler?.Invoke(request) ?? request;

        /// <summary>
        /// Adds an handler for given request type TRequest, which should return null (if request was fully handled)
        /// or the same request (if it still needs to be handled) or another request (if another request should
        /// be handled instead).
        /// </summary>
        public void Handle<TRequest>(Func<TRequest, IRequest> handler)
        {
            if (_handler != null)
                throw new InvalidOperationException("Can only set request handler once per state rule.");
                
            _handler = x => handler((TRequest) x);
        }

        /// <summary>
        /// Adds an handler for given request type TRequest, which is assumed to always handle the request fully.
        /// </summary>
        public void Handle<TRequest>(Action<TRequest> handler)
        {
            if (_handler != null)
                throw new InvalidOperationException("Can only set request handler once per state rule.");
                
            _handler = x =>
            {
                handler((TRequest) x);
                return null;
            };
        }

        /// <summary>
        /// Adds an handler for given request type TRequest, which is assumed to never handle the request fully.
        /// </summary>
        public void HandlePartially<TRequest>(Action<TRequest> handler)
        {
            if (_handler != null)
                throw new InvalidOperationException("Can only set request handler once per state rule.");
                
            _handler = x =>
            {
                handler((TRequest) x);
                return x;
            };
        }
    }
}