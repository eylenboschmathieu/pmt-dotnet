using Microsoft.EntityFrameworkCore;

using PMT.Data;
using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Tests.Repository;

[TestClass]
public class UserTests {
    private UserRepository _repo = null!;

    [TestInitialize]
    public async Task Init() {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InMemoryDatabase{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        Role[] roles = [
            new Role { Id = 1, Name = "Admin", DelegationDepth = int.MaxValue },
            new Role { Id = 2, Name = "Manager", ParentId = 1, DelegationDepth = 1 },
            new Role { Id = 3, Name = "Paramedic", ParentId = 2, DelegationDepth = 0 },
            new Role { Id = 4, Name = "Doctor", ParentId = 2, DelegationDepth = 1 },
            new Role { Id = 5, Name = "Nurse", ParentId = 4, DelegationDepth = 0 }
        ];

        User[] users = [
            new User {
                Id = 1,
                Name = "John",
                Email = "john@gmail.com",
                GoogleId = "1234",
                Roles = [
                    roles[0]
                ]
            },
            new User {
                Id = 2,
                Name = "Jane",
                Email = "jane@gmail.com",
                GoogleId = "5678",
                Roles = [
                    roles[1], roles[2]
                ]
            },
            new User {
                Id = 3,
                Name = "Foo",
                Email = "foo@gmail.com",
                GoogleId = "ABCD",
                Roles = [
                    roles[0], roles[4]
                ]
            },
            new User {
                Id = 4,
                Email = "bar@gmail.com",
                GoogleId = "EFGH",
                Roles = [
                    roles[3], roles[4]
                ]
            }
        ];

        await context.Roles.AddRangeAsync(roles);
        await context.Users.AddRangeAsync(users);

        await context.SaveChangesAsync();
        _repo = new UserRepository(context);
    }

    [TestMethod]
    public async Task CreateUser() {
        User newUser = new() {
            Name = "Foo Bar",
            Email = "foobar@gmail.com"
        };
        User? u = await _repo.CreateAsync(newUser);
        Assert.AreEqual("Foo Bar", u.Name);
    }

    [TestMethod]
    public async Task UpdateUser() {
        User? u = await _repo.GetAsync(1);
        Assert.IsNotNull(u);
        u.Name = "Foo Bar";
        Assert.IsTrue(await _repo.UpdateAsync(u));
    }

    [TestMethod]
    public async Task DeleteUser() {
        Assert.IsTrue(await _repo.DeleteAsync(2));
        Assert.IsNull(await _repo.GetAsync(2));
    }

    [TestMethod]
    public async Task FindAllUsers() {
        Assert.HasCount(4, await _repo.GetAllAsync());
    }

    [TestMethod]
    public async Task FindUserById() {
        User? u = await _repo.GetAsync(2);
        Assert.IsNotNull(u);
        Assert.AreEqual(2, u.Id);
    }

    [TestMethod]
    public async Task FindUserByGoogleId() {
        User? u = await _repo.FindByGoogleId("ABCD");
        Assert.IsNotNull(u);
        Assert.AreEqual(3, u.Id);
    }

    [TestMethod]
    public async Task FindUserByEmail() {
        User? u = await _repo.FindByEmail("foo@gmail.com");
        Assert.IsNotNull(u);
        Assert.AreEqual(3, u.Id);
    }

    [TestMethod]
    public async Task SetUserActiveOrInactive() {
        bool b1 = await _repo.SetActive(1, true);
        Assert.IsTrue(b1);
        Assert.IsTrue((await _repo.GetAsync(1))!.Active);

        bool b2 = await _repo.SetActive(1, false);
        Assert.IsTrue(b2);
        Assert.IsFalse((await _repo.GetAsync(1))!.Active);
    }

    [TestMethod]
    public async Task GetListOfActiveUsersWhoseNameIsNotNull() {
        IEnumerable<User> users = await _repo.FindSelect();
        Assert.HasCount(0, users);
        
        User? u = await _repo.GetAsync(2);
        u!.Active = true;
        await _repo.UpdateAsync(u);
        
        u = await _repo.GetAsync(3);
        u!.Active = true;
        await _repo.UpdateAsync(u);
        
        u = await _repo.GetAsync(4);
        u!.Active = true;
        await _repo.UpdateAsync(u);

        users = await _repo.FindSelect();
        Assert.HasCount(2, users);

        u = await _repo.GetAsync(2);
        u!.Active = false;
        await _repo.UpdateAsync(u);
        users = await _repo.FindSelect();
        Assert.HasCount(1, users);
    }

    [TestMethod]
    public async Task FindUserByIdIncludingRoles() {
        User? u = await _repo.FindByIdWithRoles(4);

        Assert.IsNotNull(u);
        Assert.HasCount(2, u.Roles);
    }

    [TestMethod]
    public async Task FindUserData() {
        User? u = await _repo.FindUserData(2);
        Assert.IsNotNull(u);
    }
}
