using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArqanumCore.ViewModels.Account
{
    public class AccountViewModel : INotifyPropertyChanged
    {
        private string accountId = string.Empty;
        private string username = string.Empty;
        private string avatarUrl = string.Empty;
        private string? firstName;
        private string? lastName;
        private string? bio;

        public string AccountId
        {
            get => accountId;
            set => SetProperty(ref accountId, value);
        }

        public string Username
        {
            get => username;
            set => SetProperty(ref username, value);
        }

        public string AvatarUrl
        {
            get => avatarUrl;
            set => SetProperty(ref avatarUrl, value);
        }

        public string? FirstName
        {
            get => firstName;
            set => SetProperty(ref firstName, value);
        }

        public string? LastName
        {
            get => lastName;
            set => SetProperty(ref lastName, value);
        }

        public string? Bio
        {
            get => bio;
            set => SetProperty(ref bio, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public void UpdateFrom(InternalModels.Account account)
        {
            AccountId = account.AccountId;
            Username = account.Username;
            AvatarUrl = account.AvatarUrl ?? string.Empty;
            FirstName = account.FirstName;
            LastName = account.LastName;
            Bio = account.Bio;
        }
    }
}
