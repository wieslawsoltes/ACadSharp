using CSUtilities.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp;

/// <summary>
/// Collection formed by <see cref="CadObject"/> managed by an owner.
/// </summary>
/// <typeparam name="T"></typeparam>
public class CadObjectCollection<T> : IObservableCadCollection<T>
	where T : CadObject
{
	/// <inheritdoc/>
	public event EventHandler<CollectionChangedEventArgs> OnAdd;

	/// <summary>
	/// Occurs before an item is removed from the collection.
	/// </summary>
	/// <remarks>Subscribers can use this event to perform actions or validation before the removal operation
	/// completes. To cancel the removal, handle the event and set the appropriate property on the event arguments if
	/// supported.</remarks>
	public event EventHandler<CollectionChangedEventArgs> OnBeforeRemove;

	/// <inheritdoc/>
	public event EventHandler<CollectionChangedEventArgs> OnRemove;

	/// <summary>
	/// Gets the number of elements that are contained in the collection.
	/// </summary>
	public int Count { get { return this._orderedEntries.Count; } }

	/// <summary>
	/// Owner of the collection.
	/// </summary>
	public CadObject Owner { get; }

	protected readonly HashSet<T> _entries = new HashSet<T>();
	protected readonly List<T> _orderedEntries = new List<T>();

	/// <summary>
	/// Default constructor for a <see cref="CadObjectCollection{T}"/> with it's owner assigned.
	/// </summary>
	/// <param name="owner">Owner of the collection.</param>
	public CadObjectCollection(CadObject owner)
	{
		this.Owner = owner;
	}

	/// <summary>
	/// Add a <see cref="CadObject"/> to the collection, this method triggers <see cref="OnAdd"/>.
	/// </summary>
	/// <param name="item"></param>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	public virtual void Add(T item)
	{
		if (item is null) throw new ArgumentNullException(nameof(item));

		if (item.Owner != null)
			throw new ArgumentException($"Item {item.GetType().FullName} already has an owner", nameof(item));

		if (this._entries.Contains(item))
			throw new ArgumentException($"Item {item.GetType().FullName} is already in the collection", nameof(item));

		int previousCount = this.Count;
		this._entries.Add(item);
		this._orderedEntries.Add(item);
		item.Owner = this.Owner;

		OnAdd?.Invoke(this, new CollectionChangedEventArgs(item));
		this.onCountChanged(
			previousCount,
			CollectionIdentityMode.Normal,
			CollectionIdentityMode.Normal);
	}

	/// <summary>
	/// Adds a stable set of detached <see cref="CadObject"/> instances after
	/// validating the complete batch, then triggers <see cref="OnAdd"/>.
	/// </summary>
	/// <remarks>
	/// Invalid input leaves the collection unchanged. All structural additions
	/// and owner assignments are completed before notifications are published,
	/// so observers do not see a partially attached batch. Work and temporary
	/// storage are O(N) for N distinct items.
	/// </remarks>
	/// <param name="items">Distinct detached items to add.</param>
	public virtual void AddRange(IEnumerable<T> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		List<T> addition = new List<T>();
		HashSet<T> unique = new HashSet<T>();
		foreach (T item in items)
		{
			if (item is null)
				throw new ArgumentException("An addition batch cannot contain null items.", nameof(items));
			if (!unique.Add(item))
				throw new ArgumentException("An addition batch cannot contain duplicate items.", nameof(items));
			if (item.Owner != null || this._entries.Contains(item))
				throw new ArgumentException("Every addition-batch item must be detached from a collection.", nameof(items));

			addition.Add(item);
		}

		int previousCount = this.Count;
		foreach (T item in addition)
		{
			if (!this._entries.Add(item))
				throw new InvalidOperationException("A preflighted item could not be added to the collection.");
			this._orderedEntries.Add(item);
			item.Owner = this.Owner;
		}

		if (this.OnAdd != null)
		{
			foreach (T item in addition)
			{
				this.OnAdd.Invoke(this, new CollectionChangedEventArgs(item));
			}
		}

		this.onCountChanged(
			previousCount,
			CollectionIdentityMode.Normal,
			CollectionIdentityMode.Normal);
	}

	/// <summary>
	/// Attempts to remove a stable set of owned items after every removal has
	/// passed the collection's cancellable preflight event.
	/// </summary>
	/// <remarks>
	/// The collection is not mutated when any <see cref="OnBeforeRemove"/>
	/// subscriber cancels an item. After a successful preflight, all structural
	/// removals are completed before <see cref="OnRemove"/> notifications are
	/// published, so observers cannot see a partially removed batch. Work and
	/// temporary storage are O(N) for N distinct items.
	/// </remarks>
	/// <param name="items">Distinct items currently owned by this collection.</param>
	/// <returns>
	/// <see langword="true"/> when the complete batch was removed; otherwise,
	/// <see langword="false"/> when a preflight subscriber cancelled removal.
	/// </returns>
	public virtual bool TryRemoveRange(IEnumerable<T> items)
	{
		if (items is null)
			throw new ArgumentNullException(nameof(items));

		List<T> removal = new List<T>();
		HashSet<T> unique = new HashSet<T>();
		foreach (T item in items)
		{
			if (item is null)
				throw new ArgumentException("A removal batch cannot contain null items.", nameof(items));
			if (!unique.Add(item))
				throw new ArgumentException("A removal batch cannot contain duplicate items.", nameof(items));
			if (!ReferenceEquals(item.Owner, this.Owner) || !this._entries.Contains(item))
				throw new ArgumentException("Every removal-batch item must be owned by this collection.", nameof(items));

			removal.Add(item);
		}

		if (this.OnBeforeRemove != null)
		{
			foreach (T item in removal)
			{
				CollectionChangedEventArgs args = new CollectionChangedEventArgs(item);
				this.OnBeforeRemove.Invoke(this, args);
				if (args.Cancel)
					return false;
			}
		}

		// A preflight callback is allowed to inspect the collection, but it must not
		// mutate the proposed batch behind this transaction boundary.
		foreach (T item in removal)
		{
			if (!ReferenceEquals(item.Owner, this.Owner) || !this._entries.Contains(item))
				throw new InvalidOperationException("The collection changed during removal preflight.");
		}

		int previousCount = this.Count;
		foreach (T item in removal)
		{
			if (!this._entries.Remove(item))
				throw new InvalidOperationException("A preflighted item could not be removed from the collection.");
			item.Owner = null;
		}
		this._orderedEntries.RemoveAll(item => unique.Contains(item));

		if (this.OnRemove != null)
		{
			foreach (T item in removal)
			{
				this.OnRemove.Invoke(this, new CollectionChangedEventArgs(item));
			}
		}

		this.onCountChanged(
			previousCount,
			CollectionIdentityMode.Normal,
			CollectionIdentityMode.Normal);

		return true;
	}

	/// <summary>
	/// Removes all elements from the Collection.
	/// </summary>
	public void Clear()
	{
		Queue<T> q = new(this._orderedEntries);
		while (q.TryDequeue(out T entry))
		{
			this.Remove(entry);
		}
	}

	/// <inheritdoc/>
	public IEnumerator<T> GetEnumerator()
	{
		return this._orderedEntries.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this._orderedEntries.GetEnumerator();
	}

	/// <summary>
	/// Removes the specified item from the collection.
	/// </summary>
	/// <remarks>If an event handler for the removal is registered and cancels the operation, the item will not be
	/// removed. After successful removal, the item's owner is set to null and a removal event is raised.</remarks>
	/// <param name="item">The item to remove from the collection.</param>
	/// <returns>true if the item was successfully removed; otherwise, false.</returns>
	public virtual bool Remove(T item)
	{
		if (this.OnBeforeRemove != null)
		{
			CollectionChangedEventArgs args = new(item);
			this.OnBeforeRemove.Invoke(this, args);
			if (args.Cancel)
			{
				return false;
			}
		}

		int previousCount = this.Count;
		if (!this._entries.Remove(item))
		{
			return false;
		}

		this._orderedEntries.Remove(item);
		item.Owner = null;

		OnRemove?.Invoke(this, new CollectionChangedEventArgs(item));
		this.onCountChanged(
			previousCount,
			CollectionIdentityMode.Normal,
			CollectionIdentityMode.Normal);

		return true;
	}

	public T this[int index]
	{
		get
		{
			return this._orderedEntries.ElementAtOrDefault(index);
		}
	}

	/// <summary>
	/// Prepares one exact, reversible replacement of this collection's ordered
	/// contents.
	/// </summary>
	/// <remarks>
	/// The replacement may retain current items and add detached items. Removed
	/// document objects retain their handles in an inactive lease until the
	/// replacement is reverted or released. Plans may compose in strict history
	/// order; every transition rejects a collection that no longer exactly
	/// matches its expected contents. Creation and each transition use O(C + R)
	/// work and storage for C current and R replacement items.
	/// </remarks>
	public ReversibleReplacement CreateReversibleReplacement(
		IEnumerable<T> replacement)
	{
		return new ReversibleReplacement(this, replacement);
	}

	private bool transition(
		ReversibleReplacement plan,
		T[] expected,
		T[] target,
		T[] removal,
		T[] addition,
		ReplacementState targetState)
	{
		this.validateSequence(expected);
		this.validateTransitionItems(removal, addition);

		if (this.OnBeforeRemove != null)
		{
			foreach (T item in removal)
			{
				CollectionChangedEventArgs args = new CollectionChangedEventArgs(item);
				this.OnBeforeRemove.Invoke(this, args);
				if (args.Cancel)
					return false;
			}
		}

		this.validateSequence(expected);
		this.validateTransitionItems(removal, addition);
		int previousCount = this.Count;
		foreach (T item in removal)
		{
			if (!this._entries.Remove(item))
				throw new InvalidOperationException("A replacement removal could not be applied.");
			item.Owner = null;
		}
		foreach (T item in addition)
		{
			if (!this._entries.Add(item))
				throw new InvalidOperationException("A replacement addition could not be applied.");
			item.Owner = this.Owner;
		}
		this._orderedEntries.Clear();
		this._orderedEntries.AddRange(target);
		plan._state = targetState;

		if (this.OnRemove != null)
		{
			foreach (T item in removal)
			{
				this.OnRemove.Invoke(
					this,
					new CollectionChangedEventArgs(
						item,
						CollectionIdentityMode.Lease));
			}
		}
		if (this.OnAdd != null)
		{
			foreach (T item in addition)
			{
				CollectionIdentityMode mode = item.Document == null
					? CollectionIdentityMode.Normal
					: CollectionIdentityMode.Restore;
				this.OnAdd.Invoke(
					this,
					new CollectionChangedEventArgs(item, mode));
			}
		}
		this.onCountChanged(
			previousCount,
			CollectionIdentityMode.Lease,
			CollectionIdentityMode.Restore);
		return true;
	}

	private void validateSequence(T[] expected)
	{
		if (expected.Length != this._orderedEntries.Count)
			throw new InvalidOperationException("The collection count changed outside its replacement.");
		for (int i = 0; i < expected.Length; i++)
		{
			if (!ReferenceEquals(expected[i], this._orderedEntries[i]))
				throw new InvalidOperationException("The collection order changed outside its replacement.");
		}
	}

	private void validateTransitionItems(T[] removal, T[] addition)
	{
		foreach (T item in removal)
		{
			if (!ReferenceEquals(item.Owner, this.Owner) || !this._entries.Contains(item))
				throw new InvalidOperationException("A replacement removal no longer belongs to this collection.");
		}
		foreach (T item in addition)
		{
			if (item.Owner != null || this._entries.Contains(item))
				throw new InvalidOperationException("A replacement addition is no longer inactive.");
			if (item.Document != null && !item.Document.IsLeasedCadObject(item))
				throw new InvalidOperationException("A replacement addition has an invalid document lease.");
		}
	}

	private protected virtual void onCountChanged(
		int previousCount,
		CollectionIdentityMode removedMode,
		CollectionIdentityMode addedMode)
	{
	}

	private protected virtual void releaseInactiveSequenceObjects(
		int originalCount,
		int replacementCount)
	{
	}

	private protected virtual void validateInactiveSequenceObjects(
		int originalCount,
		int replacementCount)
	{
	}

	private protected virtual void retainSequenceReplacement(
		int originalCount,
		int replacementCount)
	{
	}

	internal enum ReplacementState : byte
	{
		New,
		Applied,
		Reverted,
		Released,
	}

	/// <summary>
	/// Owns one bounded reversible ordered-content replacement until
	/// <see cref="Release"/> permanently unregisters its inactive side.
	/// </summary>
	public sealed class ReversibleReplacement
	{
		private readonly CadObjectCollection<T> _collection;
		private readonly T[] _original;
		private readonly T[] _replacement;
		private readonly T[] _removedOriginal;
		private readonly T[] _addedReplacement;
		internal ReplacementState _state;

		/// <summary>Gets the number of items in the original collection.</summary>
		public int OriginalCount { get { return this._original.Length; } }

		/// <summary>Gets the number of items in the replacement collection.</summary>
		public int ReplacementCount { get { return this._replacement.Length; } }

		/// <summary>Gets whether the replacement contents are currently active.</summary>
		public bool IsApplied { get { return this._state == ReplacementState.Applied; } }

		internal ReversibleReplacement(
			CadObjectCollection<T> collection,
			IEnumerable<T> replacement)
		{
			if (replacement == null)
				throw new ArgumentNullException(nameof(replacement));

			this._collection = collection;
			this._original = collection._orderedEntries.ToArray();
			this._replacement = replacement.ToArray();
			HashSet<T> original = new HashSet<T>(this._original);
			HashSet<T> target = new HashSet<T>();
			foreach (T item in this._replacement)
			{
				if (item == null)
					throw new ArgumentException("A replacement cannot contain null items.", nameof(replacement));
				if (!target.Add(item))
					throw new ArgumentException("A replacement cannot contain duplicate items.", nameof(replacement));
				if (item is Objects.CadDictionary)
					throw new NotSupportedException("Reversible replacement does not support CAD dictionaries.");
				if (original.Contains(item))
				{
					if (!ReferenceEquals(item.Owner, collection.Owner))
						throw new ArgumentException("A retained replacement item has a different owner.", nameof(replacement));
				}
				else if (item.Owner != null || item.Document != null || item.Handle != 0)
				{
					throw new ArgumentException("Every new replacement item must be fully detached.", nameof(replacement));
				}
			}

			this._removedOriginal = this._original.Where(item => !target.Contains(item)).ToArray();
			this._addedReplacement = this._replacement.Where(item => !original.Contains(item)).ToArray();
			collection.retainSequenceReplacement(
				this._original.Length,
				this._replacement.Length);
		}

		/// <summary>Applies the replacement for the first time or after a revert.</summary>
		public bool TryApply()
		{
			if (this._state != ReplacementState.New && this._state != ReplacementState.Reverted)
				throw new InvalidOperationException("Only a new or reverted replacement can be applied.");
			return this._collection.transition(
				this,
				this._original,
				this._replacement,
				this._removedOriginal,
				this._addedReplacement,
				ReplacementState.Applied);
		}

		/// <summary>Restores the exact original ordered contents and handles.</summary>
		public bool TryRevert()
		{
			if (this._state != ReplacementState.Applied)
				throw new InvalidOperationException("Only an applied replacement can be reverted.");
			return this._collection.transition(
				this,
				this._replacement,
				this._original,
				this._addedReplacement,
				this._removedOriginal,
				ReplacementState.Reverted);
		}

		/// <summary>
		/// Releases the inactive side and unlocks the collection. The replacement
		/// cannot be used again.
		/// </summary>
		public void Release()
		{
			if (this._state == ReplacementState.Released)
				return;
			T[] inactive = this._state == ReplacementState.Applied
				? this._removedOriginal
				: this._state == ReplacementState.Reverted
					? this._addedReplacement
					: Array.Empty<T>();
			foreach (T item in inactive)
			{
				if (item.Document != null && !item.Document.IsLeasedCadObject(item))
					throw new InvalidOperationException("An inactive replacement item lost its lease.");
			}
			this._collection.validateInactiveSequenceObjects(
				this._original.Length,
				this._replacement.Length);
			foreach (T item in inactive)
			{
				if (item.Document != null)
				{
					item.Document.ReleaseLeasedCadObject(item);
				}
			}
			this._collection.releaseInactiveSequenceObjects(
				this._original.Length,
				this._replacement.Length);
			this._state = ReplacementState.Released;
		}
	}
}
