using ReactiveUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reactive.Concurrency;

namespace ACadSharp.Viewer.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels with ReactiveUI support
    /// </summary>
    public abstract class ViewModelBase : ReactiveObject
    {
        private bool _isBusy;
        private string _title = string.Empty;

        /// <summary>
        /// Indicates if the ViewModel is currently busy with an operation
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        /// <summary>
        /// Title of the ViewModel
        /// </summary>
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        /// <summary>
        /// Sets a property and raises the PropertyChanged event
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="field">Backing field</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>True if the value was changed</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }
    }
} 