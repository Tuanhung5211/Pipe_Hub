using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.UI.Models;

/// <summary>
/// Binding model 2 chiều cho ProfileForm (INotifyPropertyChanged).
/// </summary>
public class ProfileBindingModel : INotifyPropertyChanged
{
    private string _email = string.Empty;
    private string _fullName = string.Empty;
    private string _country = string.Empty;

    public string Email
    {
        get => _email;
        set { if (_email != value) { _email = value; OnPropertyChanged(); } }
    }

    public string FullName
    {
        get => _fullName;
        set { if (_fullName != value) { _fullName = value; OnPropertyChanged(); } }
    }

    public string Country
    {
        get => _country;
        set { if (_country != value) { _country = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Load từ UserDto vào binding model.</summary>
    public void LoadFrom(UserDto dto)
    {
        Email = dto.Email;
        FullName = dto.FullName;
        Country = dto.Country ?? string.Empty;
    }
}
