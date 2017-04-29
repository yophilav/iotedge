﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Agent.Core
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.Azure.Devices.Edge.Util;

    public class ModuleSet
    {
        public static ModuleSet Empty { get; } = new ModuleSet(ImmutableList<IModule>.Empty);

        readonly IImmutableDictionary<string, IModule> modules;

        public IImmutableList<IModule> Modules { get; }

        public ModuleSet(IList<IModule> modules)
        {
            this.modules = Preconditions.CheckNotNull(modules, nameof(modules))
                .ToImmutableDictionary(m => m.Name, m => m);

            this.Modules = this.modules.Values.ToImmutableList();
        }

        public static ModuleSet Create(params IModule[] modules) => new ModuleSet(modules.ToList());

        public bool TryGetModule(string key, out IModule module) => this.modules.TryGetValue(key, out module);

        public ModuleSet ApplyDiff(Diff diff)
        {
            IImmutableDictionary<string, IModule> updated = this.modules
                .SetItems(diff.Updated.Select(m => new KeyValuePair<string, IModule>(m.Name, m)))
                .RemoveRange(diff.Removed);
            return new ModuleSet(updated.Values.ToList());
        }

        // TODO use equality comparer instead of equals?
        public Diff Diff(ModuleSet other)
        {
            IEnumerable<IModule> created = this.modules.Keys
                .Except(other.modules.Keys)
                .Select(key => this.modules[key]);
            IEnumerable<string> removed = other.modules.Keys
                .Except(this.modules.Keys);
            IEnumerable<IModule> updated = this.modules.Keys
                .Intersect(other.modules.Keys)
                .Where(key => !this.modules[key].Equals(other.modules[key]))
                .Select(key => this.modules[key]);
            return new Diff(created.Concat(updated).ToList(), removed.ToList());
        }
    }
}