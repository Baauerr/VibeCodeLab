using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Mockups.Models.Account;
using Mockups.Repositories.Addresses;
using Mockups.Services.Addresses;
using Mockups.Storage;

namespace Mockups.Services.Users
{
    public class UsersService : IUsersService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IAddressesService _addressesService;

        public UsersService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IAddressesService addressesService
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _addressesService = addressesService;
        }

        private async Task<User> GetUserOrThrow(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new UnauthorizedAccessException();
            return user;
        }

        public async Task Login(LoginViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with email = {model.Email} does not exist.");
            }

            var isValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isValidPassword)
            {
                throw new InvalidDataException("Incorrect password.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            if (user.Roles?.Any() == true)
            {
                var roles = user.Roles.Select(x => x.Role).ToList();
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Name)));
            }

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(2),
                IsPersistent = true,
            };

            await _signInManager.SignInWithClaimsAsync(user, authProperties, claims);
        }

        public async Task Register(RegisterViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            var user = new User
            {
                Email = model.Email,
                UserName = model.Email,
                Name = model.Name,
                Phone = model.Phone,
                BirthDate = model.BirthDate,
            };

            var userCreationResult = await _userManager.CreateAsync(user, model.Password);

            if (userCreationResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, ApplicationRoleNames.User);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return;
            }
            var errors = string.Join(", ", userCreationResult.Errors.Select(x => x.Description));
            throw new ArgumentException(errors);
        }

        public async Task Logout()
        {
            // Выход из системы == удаление куки
            await _signInManager.SignOutAsync();
        }

        public async Task<EditUserDataViewModel> GetEditUserDataViewModel(Guid userId)
        {
            var user = await GetUserOrThrow(userId);

            return new EditUserDataViewModel
            {
                Name = user.Name,
                Phone = user.Phone,
                BirthDate = user.BirthDate,
            };
        }

        public async Task EditUserData(EditUserDataViewModel model, Guid userId)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var user = await GetUserOrThrow(userId);

            user.Name = model.Name;
            user.Phone = model.Phone;
            user.BirthDate = model.BirthDate;

            await _userManager.UpdateAsync(user);
        }

        public async Task<IndexViewModel> GetUserInfo(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }

            var userAddresses = _addressesService.GetAddressesByUserId(userId);
            var userAddressModels = userAddresses
                .Select(address => new AddressShortViewModel
                {
                    AddressString = address.GetAddressString(),
                    Note = address.Note,
                    Name = address.Name,
                    Id = address.Id,
                })
                .ToList();

            return new IndexViewModel
            {
                Phone = user.Phone,
                Name = user.Name,
                BirthDate = user.BirthDate,
                Addresses = userAddressModels,
            };
        }
    }
}
