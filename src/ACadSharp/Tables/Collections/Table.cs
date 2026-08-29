using ACadSharp.Attributes;
using CSUtilities.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.Tables.Collections
{
	[DxfSubClass(DxfSubclassMarker.Table)]
	public abstract class Table<T> : CadObject, ITable, ICadCollection<T>, IObservableCadCollection<T>
		where T : TableEntry
	{
		public event EventHandler<CollectionChangedEventArgs> OnAdd;

		public event EventHandler<CollectionChangedEventArgs> OnRemove;

		/// <summary>
		/// Gets the number of entries in this table
		/// </summary>
		[DxfCodeValue(DxfReferenceType.Count, 70)]
		public int Count => this.entries.Count;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.TableEntry;

		/// <inheritdoc/>
		public override string SubclassMarker => DxfSubclassMarker.Table;

		protected abstract string[] defaultEntries { get; }

		protected readonly Dictionary<string, T> entries = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

		protected Table()
		{ }

		protected Table(CadDocument document)
		{
			this.Owner = document;
			document.RegisterCollection(this);
		}

		/// <summary>
		/// Add a <see cref="TableEntry"/> to the collection, this method triggers <see cref="OnAdd"/>
		/// </summary>
		/// <param name="item"></param>
		public virtual void Add(T item)
		{
			if (string.IsNullOrEmpty(item.Name))
			{
				item.Name = this.createName("unnamed");
			}

			this.add(item.Name, item);
		}

		/// <summary>
		/// Add multiple <see cref="TableEntry"/> to the collection, this method triggers <see cref="OnAdd"/>
		/// </summary>
		/// <param name="items"></param>
		public void AddRange(IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				this.Add(item);
			}
		}

		/// <summary>
		/// Tries to add the item to the collection, if an item with the same name already exists it returns the existing item.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public T TryAdd(T item)
		{
			if (this.TryGetValue(item.Name, out T existing))
			{
				return existing;
			}
			else
			{
				this.Add(item);
				return item;
			}
		}

		/// <summary>
		/// Determines whether the <see cref="Table{T}"/> contains the specified key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Contains(string key)
		{
			return this.entries.ContainsKey(key);
		}

		/// <summary>
		/// Create the default entries for the table if they don't exist
		/// </summary>
		public void CreateDefaultEntries()
		{
			foreach (string entry in this.defaultEntries)
			{
				if (this.Contains(entry))
					continue;

				this.Add((T)Activator.CreateInstance(typeof(T), new object[] { entry }));
			}
		}

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator()
		{
			return this.entries.Values.GetEnumerator();
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.entries.Values.GetEnumerator();
		}

		/// <summary>
		/// Removes a <see cref="TableEntry"/> from the collection, this method triggers <see cref="OnRemove"/>
		/// </summary>
		/// <param name="key">key in the table</param>
		/// <returns>The removed <see cref="TableEntry"/></returns>
		public T Remove(string key)
		{
			if (this.defaultEntries.Contains(key))
				return null;

			if (this.entries.Remove(key, out T item))
			{
				item.Owner = null;
				OnRemove?.Invoke(this, new CollectionChangedEventArgs(item));
				item.OnNameChanged -= this.onEntryNameChanged;
				return item;
			}

			return null;
		}

		/// <summary>
		/// Atomically validates and removes several non-default entries without
		/// raising the single-entry <see cref="OnRemove"/> event. Derived tables
		/// must publish one typed range notification after this method returns.
		/// </summary>
		/// <param name="keys">Distinct entry names to remove.</param>
		/// <returns>The detached entries in the requested order.</returns>
		protected T[] removeRange(IReadOnlyList<string> keys)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));
			if (keys.Count == 0)
				throw new ArgumentException("At least one entry name is required.", nameof(keys));

			var removed = new T[keys.Count];
			var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < keys.Count; i++)
			{
				string key = keys[i];
				if (string.IsNullOrWhiteSpace(key))
					throw new ArgumentException("Entry names cannot be empty.", nameof(keys));
				if (!names.Add(key))
					throw new ArgumentException($"Entry name '{key}' is duplicated.", nameof(keys));
				if (!this.entries.TryGetValue(key, out T item))
					throw new InvalidOperationException($"Entry '{key}' does not exist.");
				if (this.defaultEntries.Contains(item.Name, StringComparer.OrdinalIgnoreCase))
					throw new InvalidOperationException($"Default entry '{item.Name}' cannot be removed.");
				removed[i] = item;
			}

			foreach (T item in removed)
			{
				this.entries.Remove(item.Name);
				item.Owner = null;
				item.OnNameChanged -= this.onEntryNameChanged;
			}
			return removed;
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="item"></param>
		/// <returns>true if the <see cref="Table{T}"/> contains an element with the specified key; otherwise, false.</returns>
		public bool TryGetValue(string key, out T item)
		{
			return this.entries.TryGetValue(key, out item);
		}

		protected void add(string key, T item)
		{
			this.entries.Add(key, item);
			item.Owner = this;

			item.OnNameChanged += this.onEntryNameChanged;

			OnAdd?.Invoke(this, new CollectionChangedEventArgs(item));
		}

		protected void addHandlePrefix(T item)
		{
			item.Owner = this;
			item.OnNameChanged += this.onEntryNameChanged;

			OnAdd?.Invoke(this, new CollectionChangedEventArgs(item));

			string key = $"{item.Handle}:{item.Name}";

			this.entries.Add(key, item);
		}

		protected string createName(string prefix)
		{
			string name = prefix;
			int i = 0;
			while (this.entries.ContainsKey($"{prefix}{i}"))
			{
				i++;
			}

			return $"{prefix}{i}";
		}

		private void onEntryNameChanged(object sender, OnNameChangedArgs e)
		{
			if (this.defaultEntries.Contains(e.OldName, StringComparer.InvariantCultureIgnoreCase))
			{
				throw new ArgumentException($"The name {e.OldName} belongs to a default entry.");
			}

			var entry = this.entries[e.OldName];
			this.entries.Add(e.NewName, entry);
			this.entries.Remove(e.OldName);
		}

		public T this[string name]
		{
			get
			{
				return this.entries[name];
			}
		}
	}
}
