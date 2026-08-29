using System;

namespace ACadSharp;

internal enum CollectionIdentityMode : byte
{
	Normal,
	Lease,
	Restore,
}

public class CollectionChangedEventArgs : EventArgs
{
	internal CollectionIdentityMode IdentityMode { get; }

	/// <summary>
	/// Gets or sets a value indicating whether the current operation should be canceled.
	/// </summary>
	/// <remarks>
	/// This property is used by the OnBeforeAdd and OnBeforeRemove events to cancel the add or remove operation.
	/// </remarks>
	public bool Cancel { get; set; } = false;

	/// <summary>
	/// Item that is beeing added or removed from the collection
	/// </summary>
	public CadObject Item { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="CollectionChangedEventArgs"/> class with the specified item that is being added or removed from the collection.
	/// </summary>
	/// <param name="item">The item that is being added or removed from the collection.</param>
	public CollectionChangedEventArgs(CadObject item)
		: this(item, CollectionIdentityMode.Normal)
	{
	}

	internal CollectionChangedEventArgs(
		CadObject item,
		CollectionIdentityMode identityMode)
	{
		this.Item = item;
		this.IdentityMode = identityMode;
	}
}
