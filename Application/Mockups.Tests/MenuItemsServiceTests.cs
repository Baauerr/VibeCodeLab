using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Mockups.Models.Cart;
using Mockups.Models.Menu;
using Mockups.Repositories.MenuItems;
using Mockups.Services.Carts;
using Mockups.Services.MenuItems;
using Mockups.Storage;
using Moq;
using Xunit;

namespace Mockups.Tests
{
    public class MenuItemsServiceTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetAllMenuItems_ReturnsViewModels()
        {
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            context.MenuItems.Add(
                new MenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "A",
                    Price = 10,
                    Description = "d",
                    Category = MenuItemCategory.Pizza,
                    IsVegan = false,
                    PhotoPath = string.Empty,
                }
            );
            context.MenuItems.Add(
                new MenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "B",
                    Price = 20,
                    Description = "d2",
                    Category = MenuItemCategory.Dessert,
                    IsVegan = true,
                    PhotoPath = string.Empty,
                }
            );
            await context.SaveChangesAsync();

            var repo = new MenuItemRepository(context);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartsMock = new Mock<ICartsService>();
            var svc = new MenuItemsService(envMock.Object, repo, cartsMock.Object);

            var result = await svc.GetAllMenuItems(null, Array.Empty<MenuItemCategory>());

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task CreateMenuItem_WhenNameExists_Throws()
        {
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var existing = new MenuItem
            {
                Id = Guid.NewGuid(),
                Name = "Same",
                Price = 5,
                Description = "x",
                Category = MenuItemCategory.Pizza,
                IsVegan = false,
                PhotoPath = string.Empty,
            };
            context.MenuItems.Add(existing);
            await context.SaveChangesAsync();

            var repo = new MenuItemRepository(context);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartsMock = new Mock<ICartsService>();
            var svc = new MenuItemsService(envMock.Object, repo, cartsMock.Object);

            var model = new Mockups.Models.Menu.CreateMenuItemViewModel
            {
                Name = "Same",
                Price = 1,
                Description = "d",
                Category = MenuItemCategory.Pizza,
                IsVegan = false,
            };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateMenuItem(model));
        }

        [Fact]
        public async Task DeleteMenuItem_ReturnsNull_WhenNotFound()
        {
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var repo = new MenuItemRepository(context);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartsMock = new Mock<ICartsService>();
            var svc = new MenuItemsService(envMock.Object, repo, cartsMock.Object);

            var res = await svc.DeleteMenuItem(Guid.NewGuid().ToString());
            Assert.Null(res);
        }

        [Fact]
        public async Task DeleteMenuItem_ReturnsTrue_WhenDeleted()
        {
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var item = new MenuItem
            {
                Id = Guid.NewGuid(),
                Name = "ToDelete",
                Price = 1,
                Description = "d",
                Category = MenuItemCategory.Pizza,
                IsVegan = false,
                PhotoPath = string.Empty,
            };
            context.MenuItems.Add(item);
            await context.SaveChangesAsync();

            var repo = new MenuItemRepository(context);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartsMock = new Mock<ICartsService>();
            var svc = new MenuItemsService(envMock.Object, repo, cartsMock.Object);

            var res = await svc.DeleteMenuItem(item.Id.ToString());
            Assert.True(res.Value);

            var fromDb = await repo.GetItemById(item.Id);
            Assert.Null(fromDb);
        }

        [Fact]
        public async Task GetAddToCartModel_Throws_WhenNotFound()
        {
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var repo = new MenuItemRepository(context);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartsMock = new Mock<ICartsService>();
            var svc = new MenuItemsService(envMock.Object, repo, cartsMock.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => svc.GetAddToCartModel(Guid.NewGuid().ToString())
            );
        }

        [Fact]
        public async Task AddItemToCart_CallsCartsService()
        {
            var context = CreateInMemoryContext(Guid.NewGuid().ToString());
            var repo = new MenuItemRepository(context);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartsMock = new Mock<ICartsService>();
            cartsMock
                .Setup(x => x.AddItemToCart(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var svc = new MenuItemsService(envMock.Object, repo, cartsMock.Object);

            await svc.AddItemToCart(Guid.NewGuid(), Guid.NewGuid().ToString(), 2);

            cartsMock.Verify(
                x => x.AddItemToCart(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Once
            );
        }
    }
}
