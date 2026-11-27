/* The catalog's name dictionary (/Names): the name trees the document writes from its own lists when it is saved. */

using System;
using System.Collections.Generic;
using Haru.Objects;

namespace Haru.Doc
{
    /// <summary>
    /// Writes the name trees of a document (ISO 32000-1, 7.7.4 and 7.9.6) into the catalog's name dictionary: the one
    /// the user set with <see cref="HpdfCatalog.SetNames"/>, else one of its own, created once. Entries of the user's
    /// dictionary that the document does not write are kept.
    /// </summary>
    internal sealed class HpdfNameDictionary
    {
        private readonly HpdfCatalog _catalog;
        private readonly HashSet<string> _writtenTrees = new HashSet<string>(StringComparer.Ordinal);
        private HpdfDict? _ownDictionary;

        internal HpdfNameDictionary(HpdfCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <summary>
        /// Writes the name tree <paramref name="key"/> (EmbeddedFiles, Dests, JavaScript); without entries, removes
        /// the tree an earlier save wrote, never one of the user.
        /// </summary>
        /// <param name="key">The key of the tree in the name dictionary.</param>
        /// <param name="entries">The names, unique, and their values.</param>
        internal void SetTree(string key, IReadOnlyCollection<(string Name, HpdfObject Value)> entries)
        {
            if (entries.Count == 0)
            {
                if (_writtenTrees.Remove(key))
                    _catalog.GetNames()?.Remove(key);
                return;
            }

            var names = _catalog.GetNames();
            if (names is null)
            {
                _ownDictionary ??= new HpdfDict();
                names = _ownDictionary;
                _catalog.SetNames(names);
            }

            names[key] = BuildTree(entries);
            _writtenTrees.Add(key);
        }

        /// <summary>
        /// Builds a name tree of one node: /Names [key1 value1 key2 value2 ...], the keys (text strings) sorted by
        /// their bytes, as a name tree requires.
        /// </summary>
        private static HpdfDict BuildTree(IReadOnlyCollection<(string Name, HpdfObject Value)> entries)
        {
            var sorted = new List<(byte[] Key, HpdfObject Value)>(entries.Count);
            foreach (var (name, value) in entries)
                sorted.Add((new HpdfString(name).Value, value));
            sorted.Sort((a, b) => a.Key.AsSpan().SequenceCompareTo(b.Key));

            var array = new HpdfArray();
            foreach (var (key, value) in sorted)
            {
                array.Add(new HpdfString(key));
                array.Add(value);
            }

            var tree = new HpdfDict();
            tree.Add("Names", array);
            return tree;
        }
    }
}
