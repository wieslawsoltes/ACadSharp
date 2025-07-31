using ACadSharp.Viewer.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ACadSharp.Viewer.Services;

/// <summary>
/// JSON-based settings service for persistent storage
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public SettingsService()
    {
        // Store settings in user's AppData/Local directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsDirectory = Path.Combine(appDataPath, "ACadSharp.Viewer");
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(_settingsDirectory);
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
    
    /// <summary>
    /// Loads settings from JSON file
    /// </summary>
    /// <typeparam name="T">Type of settings to load</typeparam>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <param name="defaultSettings">Default settings to return if none found</param>
    /// <returns>Loaded settings or default if not found</returns>
    public async Task<T> LoadSettingsAsync<T>(string settingsKey, T defaultSettings) where T : class
    {
        try
        {
            var filePath = GetSettingsFilePath(settingsKey);
            
            if (!File.Exists(filePath))
            {
                return defaultSettings;
            }
            
            var json = await File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                return defaultSettings;
            }
            
            var settings = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return settings ?? defaultSettings;
        }
        catch (Exception)
        {
            // If any error occurs, return default settings
            return defaultSettings;
        }
    }
    
    /// <summary>
    /// Saves settings to JSON file
    /// </summary>
    /// <typeparam name="T">Type of settings to save</typeparam>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <param name="settings">Settings to save</param>
    /// <returns>Task representing the save operation</returns>
    public async Task SaveSettingsAsync<T>(string settingsKey, T settings) where T : class
    {
        try
        {
            var filePath = GetSettingsFilePath(settingsKey);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - settings save failure shouldn't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to save settings '{settingsKey}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Deletes settings file
    /// </summary>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <returns>Task representing the delete operation</returns>
    public async Task DeleteSettingsAsync(string settingsKey)
    {
        try
        {
            var filePath = GetSettingsFilePath(settingsKey);
            
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw
            System.Diagnostics.Debug.WriteLine($"Failed to delete settings '{settingsKey}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks if settings file exists
    /// </summary>
    /// <param name="settingsKey">Unique key for the settings</param>
    /// <returns>True if settings exist, false otherwise</returns>
    public async Task<bool> SettingsExistAsync(string settingsKey)
    {
        try
        {
            var filePath = GetSettingsFilePath(settingsKey);
            return await Task.FromResult(File.Exists(filePath));
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets the file path for a settings key
    /// </summary>
    /// <param name="settingsKey">Settings key</param>
    /// <returns>Full file path</returns>
    private string GetSettingsFilePath(string settingsKey)
    {
        // Sanitize the key to make it a valid filename
        var sanitizedKey = string.Join("_", settingsKey.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_settingsDirectory, $"{sanitizedKey}.json");
    }
}