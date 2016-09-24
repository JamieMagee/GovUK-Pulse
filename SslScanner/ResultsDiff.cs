using System;
using System.Collections.Generic;
using SslScanner.Objects;

namespace SslScanner
{
    public class ResultsDiff
    {
        private readonly List<GovDomain> _previous;
        private readonly List<GovDomain> _current;
        private ChangeSet changeSet = new ChangeSet();

        public ResultsDiff(List<GovDomain> previous, List<GovDomain> current)
        {
            _previous = previous;
            _current = current;
        }

        public ChangeSet Run()
        {
            foreach (var previousDomain in _previous)
            {
                var currentDomain = _current.Find(x => x.domain.Equals(previousDomain.domain));
                if (currentDomain!= null && !currentDomain.Equals(previousDomain))
                {
                    changeSet.diffList.Add(new ChangeSet.Diff(previousDomain, currentDomain));
                }
            }

            return changeSet;
        }
    }
}