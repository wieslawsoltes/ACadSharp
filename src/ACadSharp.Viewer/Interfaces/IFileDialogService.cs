using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces
{
    /// <summary>
    /// Interface for file dialog operations
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Shows a file picker dialog for DWG files
        /// </summary>
        /// <returns>Selected file path or null if cancelled</returns>
        Task<string?> ShowDwgFilePickerAsync();

        /// <summary>
        /// Shows a file picker dialog for DXF files
        /// </summary>
        /// <returns>Selected file path or null if cancelled</returns>
        Task<string?> ShowDxfFilePickerAsync();
    }
} 