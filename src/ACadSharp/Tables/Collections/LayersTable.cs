using System;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.Tables.Collections
{
	public class LayersTable : Table<Layer>
	{
		/// <summary>
		/// Raised once after an atomic <see cref="RemoveRange"/> operation. The
		/// existing <see cref="Table{T}.OnRemove"/> event remains single-entry only.
		/// </summary>
		public event EventHandler<LayerCollectionChangedEventArgs> OnRemoveRange;

		/// <inheritdoc/>
		public override ObjectType ObjectType => ObjectType.LAYER_CONTROL_OBJ;

		/// <inheritdoc/>
		public override string ObjectName => DxfFileToken.TableLayer;

		protected override string[] defaultEntries { get { return new string[] { Layer.DefaultName }; } }

		internal LayersTable() { }

		internal LayersTable(CadDocument document) : base(document) { }

		/// <summary>
		/// Atomically validates and removes several non-default layers, then raises
		/// one <see cref="OnRemoveRange"/> notification containing the removed layers.
		/// </summary>
		/// <param name="keys">Distinct layer names to remove.</param>
		/// <returns>The detached layers in the requested order.</returns>
		public Layer[] RemoveRange(IEnumerable<string> keys)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));

			Layer[] removed = this.removeRange(keys.ToArray());
			this.OnRemoveRange?.Invoke(this, new LayerCollectionChangedEventArgs(removed));
			return removed;
		}
	}
}
