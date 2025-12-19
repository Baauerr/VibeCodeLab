using Mockups.Models.Cart;
using Mockups.Models.Menu;
using Mockups.Repositories.MenuItems;
using Mockups.Services.Carts;
using Mockups.Storage;

namespace Mockups.Services.MenuItems
{
    public class MenuItemsService : IMenuItemsService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly MenuItemRepository _menuItemRepository;
        private readonly ICartsService _cartsService;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "jpg",
            "jpeg",
            "png",
        };

        public MenuItemsService(
            IWebHostEnvironment environment,
            MenuItemRepository menuItemRepository,
            ICartsService cartsService
        )
        {
            _environment = environment;
            _menuItemRepository = menuItemRepository;
            _cartsService = cartsService;
        }

        public async Task CreateMenuItem(CreateMenuItemViewModel model)
        {
            var sameMenuItem = await _menuItemRepository.GetItemByName(model.Name);
            if (sameMenuItem != null)
            {
                throw new ArgumentException(
                    $"Menu item with same name ({model.Name}) already exists"
                );
            }

            var fileNameWithPath = string.Empty;
            if (model.File != null)
            {
                var rawFileName = Path.GetFileName(model.File.FileName ?? string.Empty);
                var extension = Path.GetExtension(rawFileName).TrimStart('.');
                if (!AllowedExtensions.Contains(extension))
                {
                    throw new ArgumentException("Attached file's extension is not supported");
                }

                fileNameWithPath = $"files/{Guid.NewGuid()}-{rawFileName}";

                var webRoot = _environment?.WebRootPath;
                if (string.IsNullOrEmpty(webRoot))
                {
                    webRoot = Directory.GetCurrentDirectory();
                }

                var destPath = Path.Combine(webRoot, fileNameWithPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? webRoot);

                using var fs = new FileStream(destPath, FileMode.Create);
                await model.File.CopyToAsync(fs);
            }

            var newMenuItem = new MenuItem
            {
                Name = model.Name,
                Price = model.Price,
                Description = model.Description,
                Category = model.Category,
                IsVegan = model.IsVegan,
                PhotoPath = fileNameWithPath,
            };

            await _menuItemRepository.AddItem(newMenuItem);
        }

        public async Task<List<MenuItemViewModel>> GetAllMenuItems(
            bool? isVegan,
            MenuItemCategory[]? category
        )
        {
            var itemVMs = new List<MenuItemViewModel>();

            var items = new List<MenuItem>();

            var hasCategory = category != null && category.Any();
            if (isVegan != null && hasCategory)
            {
                items = await _menuItemRepository.GetAllMenuItems((bool)isVegan, category);
            }
            else if (hasCategory)
            {
                items = await _menuItemRepository.GetAllMenuItems(category!);
            }
            else if (isVegan != null)
            {
                items = await _menuItemRepository.GetAllMenuItems((bool)isVegan);
            }
            else
            {
                items = await _menuItemRepository.GetAllMenuItems();
            }
            foreach (var item in items)
            {
                itemVMs.Add(
                    new MenuItemViewModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Description = item.Description,
                        Price = item.Price,
                        Category = item.Category,
                        IsVegan = item.IsVegan,
                        PhotoPath = item.PhotoPath,
                    }
                );
            }

            return itemVMs;
        }

        public async Task<bool?> DeleteMenuItem(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var item = await _menuItemRepository.GetItemById(guid);

            if (item == null)
            {
                return null;
            }

            await _menuItemRepository.DeleteItem(item);

            return true;
        }

        public async Task<MenuItemViewModel?> GetItemModelById(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var item = await _menuItemRepository.GetItemById(guid);

            if (item == null)
            {
                return null;
            }

            return new MenuItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                Category = item.Category,
                IsVegan = item.IsVegan,
                PhotoPath = item.PhotoPath,
            };
        }

        public async Task AddItemToCart(Guid userID, string itemId, int amount)
        {
            await _cartsService.AddItemToCart(userID, itemId, amount);
        }

        public async Task<AddToCartViewModel> GetAddToCartModel(string itemId)
        {
            if (!Guid.TryParse(itemId, out var itemGuid))
                throw new ArgumentException("Invalid item id format", nameof(itemId));

            var item = await _menuItemRepository.GetItemById(itemGuid);

            if (item == null)
            {
                throw new KeyNotFoundException();
            }

            return new AddToCartViewModel
            {
                Item = new MenuItemViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    Category = item.Category,
                    IsVegan = item.IsVegan,
                    PhotoPath = item.PhotoPath,
                },
            };
        }

        public async Task<string?> GetItemNameById(Guid itemId)
        {
            return (await _menuItemRepository.GetItemById(itemId))?.Name;
        }
    }
}
