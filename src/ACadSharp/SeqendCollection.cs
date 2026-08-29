using ACadSharp.Entities;
using System;
using System.Linq;

namespace ACadSharp;

/// <summary>
/// Represents a collection of <see cref="CadObject"/> ended by a <see cref="Entities.Seqend"/> entity.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SeqendCollection<T> : CadObjectCollection<T>, ISeqendCollection
	where T : CadObject
{
	public event EventHandler<CollectionChangedEventArgs> OnSeqendAdded;

	public event EventHandler<CollectionChangedEventArgs> OnSeqendRemoved;

	/// <summary>
	/// Sequence end entity for dxf.
	/// </summary>
	public Seqend Seqend
	{
		get
		{
			if (this.Count > 0)
				return this._seqend;
			else
				return null;
		}
		internal set
		{
			this._seqend = value;
			this._seqend.Owner = this.Owner;
		}
	}

	private Seqend _seqend;

	public SeqendCollection(CadObject owner) : base(owner)
	{
		this._seqend = new Seqend(owner);
	}

	private protected override void onCountChanged(
		int previousCount,
		CollectionIdentityMode removedMode,
		CollectionIdentityMode addedMode)
	{
		if (previousCount == 0 && this.Count > 0)
		{
			CollectionIdentityMode mode = this._seqend.Document == null
				? CollectionIdentityMode.Normal
				: addedMode;
			this.OnSeqendAdded?.Invoke(
				this,
				new CollectionChangedEventArgs(this._seqend, mode));
		}
		else if (previousCount > 0 && this.Count == 0)
		{
			this.OnSeqendRemoved?.Invoke(
				this,
				new CollectionChangedEventArgs(this._seqend, removedMode));
		}
	}

	private protected override void releaseInactiveSequenceObjects()
	{
		if (this._seqend.Document != null &&
			this._seqend.Document.IsLeasedCadObject(this._seqend))
		{
			this._seqend.Document.ReleaseLeasedCadObject(this._seqend);
		}
	}

	private protected override void validateInactiveSequenceObjects()
	{
		if (this.Count == 0 &&
			this._seqend.Document != null &&
			!this._seqend.Document.IsLeasedCadObject(this._seqend))
		{
			throw new InvalidOperationException("An inactive sequence end lost its lease.");
		}
	}
}
