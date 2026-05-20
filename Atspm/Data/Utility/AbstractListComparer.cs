#region license
// Copyright 2026 Utah Department of Transportation
// for Data - Utah.Udot.Atspm.Data.Utility/AbstractListComparer.cs
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections;

namespace Utah.Udot.Atspm.Data.Utility
{
    /// <summary>
    /// <see cref="ValueComparer"/> used to compare an <see cref="IEnumerable"/> of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class AbstractListComparer<T> : ValueComparer<IEnumerable<T>>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public AbstractListComparer() : base(
            (c1, c2) => SequenceEqualSafe(c1, c2),
            c => GetHashCodeSafe(c),
            c => SnapshotSafe(c))
        { }

        private static bool SequenceEqualSafe(IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            if (first is null || second is null)
            {
                return false;
            }

            return first.SequenceEqual(second);
        }

        private static int GetHashCodeSafe(IEnumerable<T>? source)
        {
            if (source is null)
            {
                return 0;
            }

            var hash = new HashCode();
            foreach (var item in source)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }

        private static List<T> SnapshotSafe(IEnumerable<T>? source)
        {
            return source?.ToList() ?? new List<T>();
        }
    }
}
