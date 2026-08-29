using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ACadSharp.Tables.Collections;

/// <summary>Provides one immutable set of layers changed by a table operation.</summary>
public sealed class LayerCollectionChangedEventArgs : EventArgs
{
	private readonly Layer[] _layers;
	private readonly HashSet<Layer> _layerSet;

	/// <summary>Layers in deterministic operation order.</summary>
	public ReadOnlyMemory<Layer> Layers => this._layers;

	internal LayerCollectionChangedEventArgs(Layer[] layers)
	{
		this._layers = layers ?? throw new ArgumentNullException(nameof(layers));
		this._layerSet = new HashSet<Layer>(layers, LayerReferenceComparer.Instance);
	}

	/// <summary>Returns whether the range contains the exact layer instance.</summary>
	public bool Contains(Layer layer)
	{
		return layer != null && this._layerSet.Contains(layer);
	}

	private sealed class LayerReferenceComparer : IEqualityComparer<Layer>
	{
		public static LayerReferenceComparer Instance { get; } = new LayerReferenceComparer();

		public bool Equals(Layer x, Layer y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(Layer obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
