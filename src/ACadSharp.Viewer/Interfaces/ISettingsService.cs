using System.Threading.Tasks;

namespace ACadSharp.Viewer.Interfaces;

/// <summary>
/// Interface for application settings management
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from storage
    /// </summary>
    /// <typeparam name="T">Type of settings to load</typeparam>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <param name="defaultSettings">Default settings to return if none found</param>
    /// <returns>Loaded settings or default if not found</returns>
    Task<T> LoadSettingsAsync<T>(string settingsKey, T defaultSettings) where T : class;
    
    /// <summary>
    /// Saves settings to storage
    /// </summary>
    /// <typeparam name="T">Type of settings to save</typeparam>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <param name="settings">Settings to save</param>
    /// <returns>Task representing the save operation</returns>
    Task SaveSettingsAsync<T>(string settingsKey, T settings) where T : class;
    
    /// <summary>
    /// Deletes settings from storage
    /// </summary>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <returns>Task representing the delete operation</returns>
    Task DeleteSettingsAsync(string settingsKey);
    
    /// <summary>
    /// Checks if settings exist in storage
    /// </summary>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <returns>True if settings exist, false otherwise</returns>
    Task<bool> SettingsExistAsync(string settingsKey);
}